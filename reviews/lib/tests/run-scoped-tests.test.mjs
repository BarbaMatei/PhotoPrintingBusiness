// Tests for run-scoped-tests.mjs: command construction, output parsing (dotnet and vitest),
// worklog stamping, and the machine-global lock. Every run uses --cmd with a `node -e`
// stand-in — never dotnet or npm.
//
// Usage: node reviews/lib/tests/run-tests.mjs --only run-scoped
import { check, run } from './lib.mjs'
import { mkdtempSync, writeFileSync, readFileSync, existsSync, unlinkSync, rmSync } from 'node:fs'
import { join } from 'node:path'
import { tmpdir } from 'node:os'

const LOCK_PATH = join(tmpdir(), 'photoprint-test.lock')
const backupLock = () => { try { return readFileSync(LOCK_PATH, 'utf8') } catch { return null } }
const restoreLock = before => {
  try { unlinkSync(LOCK_PATH) } catch { /* nothing to remove */ }
  if (before !== null) writeFileSync(LOCK_PATH, before)
}

function lastEvent(T, target) {
  const wlPath = join(T, 'reviews', target, 'worklog.jsonl')
  if (!existsSync(wlPath)) return null
  const lines = readFileSync(wlPath, 'utf8').split(/\r?\n/).filter(l => l.trim())
  return lines.length ? JSON.parse(lines[lines.length - 1]) : null
}

const T = mkdtempSync(join(tmpdir(), 'run-scoped-'))
const target = '960-run-scoped-target'

{
  const passDotnet = "node -e \"console.log('Passed! - Failed: 0, Passed: 5, Skipped: 1, Total: 6')\""
  const r = run('run-scoped-tests.mjs',
    ['--root', T, target, '--kind', 'green', '--filter', 'Foo.Bar', '--cmd', passDotnet])
  check('a passing dotnet-style command exits 0', r.code === 0, `exit ${r.code}: ${r.out.trim()}`)
  const ev = lastEvent(T, target)
  check('the stamped event records the parsed dotnet counts',
    !!ev && ev.ev === 'test-run' && ev.kind === 'green' && ev.filter === 'Foo.Bar' &&
    ev.passed === 5 && ev.failed === 0 && ev.skipped === 1, JSON.stringify(ev))
  check('duration_s is a measured non-negative number', !!ev && typeof ev.duration_s === 'number' && ev.duration_s >= 0, JSON.stringify(ev))
  check('the lock is removed after a green leg', !existsSync(LOCK_PATH), 'lock file still present')
}
{
  const failDotnet = "node -e \"console.log('Failed! - Failed: 2, Passed: 3, Skipped: 0, Total: 5'); process.exit(1)\""
  const r = run('run-scoped-tests.mjs',
    ['--root', T, target, '--kind', 'red', '--filter', 'Foo.Baz', '--cmd', failDotnet])
  check('a failing command exits non-zero (the runner\'s own code)', r.code === 1, `exit ${r.code}: ${r.out.trim()}`)
  const ev = lastEvent(T, target)
  check('the stamped event records the parsed counts on a red run',
    !!ev && ev.kind === 'red' && ev.passed === 3 && ev.failed === 2, JSON.stringify(ev))
  check('the lock is removed after a red leg', !existsSync(LOCK_PATH), 'lock file still present')
}
{
  const unparsable = "node -e \"console.log('nothing of note here')\""
  const r = run('run-scoped-tests.mjs',
    ['--root', T, target, '--kind', 'baseline', '--filter', 'Foo.Qux', '--cmd', unparsable])
  check('unparsable output still exits with the runner\'s own code', r.code === 0, `exit ${r.code}: ${r.out.trim()}`)
  const ev = lastEvent(T, target)
  check('unparsable output stamps null counts with a note',
    !!ev && ev.passed === null && ev.failed === null && ev.note === 'unparsed runner output', JSON.stringify(ev))
}
{
  const passVitest = "node -e \"console.log('Test Files  1 passed (1)'); console.log(' Tests  1 failed | 5 passed (6)')\""
  const r = run('run-scoped-tests.mjs',
    ['--root', T, target, '--kind', 'final', '--ui', '--include', 'WidgetComponent', '--cmd', passVitest])
  check('a vitest-style summary exits with the runner\'s own code', r.code === 0, `exit ${r.code}: ${r.out.trim()}`)
  const ev = lastEvent(T, target)
  check('the stamped event records the parsed vitest counts under "include"',
    !!ev && ev.kind === 'final' && ev.include === 'WidgetComponent' && ev.passed === 5 && ev.failed === 1, JSON.stringify(ev))

  const allPassVitest = "node -e \"console.log('Tests  6 passed (6)')\""
  const r2 = run('run-scoped-tests.mjs',
    ['--root', T, target, '--kind', 'final', '--ui', '--include', 'WidgetComponent', '--cmd', allPassVitest])
  const ev2 = lastEvent(T, target)
  check('a vitest summary with no failed group defaults failed to 0, not null',
    r2.code === 0 && !!ev2 && ev2.passed === 6 && ev2.failed === 0, JSON.stringify(ev2))
}
{
  const before = backupLock()
  try {
    writeFileSync(LOCK_PATH, JSON.stringify({ pid: process.pid, started: new Date().toISOString() }), { flag: 'wx' })
    const marker = join(T, 'lock-marker.txt')
    try { unlinkSync(marker) } catch { /* not created yet */ }
    const cmd = `node -e "require('fs').writeFileSync(${JSON.stringify(marker)},'ran')"`
    const r = run('run-scoped-tests.mjs', ['--root', T, target, '--kind', 'green', '--filter', 'Foo.Locked', '--cmd', cmd])
    check('a live lock holder makes the run exit 3', r.code === 3, `exit ${r.code}: ${r.out.trim()}`)
    check('the exit-3 message names the holder pid and the machine-wide rule',
      r.out.includes(`another test process is running (pid ${process.pid}) — the machine takes one at a time`), r.out.trim())
    check('a run refused by the lock never executes the command', !existsSync(marker), 'the marker file was created despite the lock')
  } finally {
    restoreLock(before)
  }
}
{
  const before = backupLock()
  try {
    writeFileSync(LOCK_PATH, JSON.stringify({ pid: 999999, started: new Date().toISOString() }), { flag: 'wx' })
    const passDotnet = "node -e \"console.log('Passed! - Failed: 0, Passed: 1')\""
    const r = run('run-scoped-tests.mjs', ['--root', T, target, '--kind', 'green', '--filter', 'Foo.Stolen', '--cmd', passDotnet])
    check('a dead pid\'s stale lock is stolen and the run proceeds', r.code === 0, `exit ${r.code}: ${r.out.trim()}`)
    check('the lock is removed after the stolen run completes', !existsSync(LOCK_PATH), 'lock file still present')
  } finally {
    restoreLock(before)
  }
}
{
  const passDotnet = "node -e \"console.log('Passed! - Failed: 0, Passed: 1')\""
  const r = run('run-scoped-tests.mjs',
    ['--root', T, target, '--kind', 'green', '--filter', 'Foo.DryRun', '--cmd', passDotnet, '--dry-run'])
  check('--dry-run exits 0 and prints the command without running it', r.code === 0 && r.out.trim() === passDotnet, r.out.trim())
  check('--dry-run never touches the lock', !existsSync(LOCK_PATH), 'lock file present after a dry run')
}
{
  const before = lastEvent(T, target)
  const passDotnet = "node -e \"console.log('Passed! - Failed: 0, Passed: 9')\""
  const r = run('run-scoped-tests.mjs',
    ['--root', T, target, '--kind', 'green', '--filter', 'Foo.NoEvents', '--cmd', passDotnet, '--no-events'])
  check('--no-events still runs and exits 0', r.code === 0, `exit ${r.code}: ${r.out.trim()}`)
  check('--no-events suppresses the worklog stamp', JSON.stringify(lastEvent(T, target)) === JSON.stringify(before), 'a new event was appended despite --no-events')
}
{
  const passDotnet = "node -e \"console.log('Passed! - Failed: 0, Passed: 2')\""
  const r = run('run-scoped-tests.mjs',
    ['--root', T, target, '--kind', 'green', '--filter', 'Foo.Meta', '--cluster', 'c3', '--round', '2', '--note', 'triage note', '--cmd', passDotnet])
  const ev = lastEvent(T, target)
  check('--cluster, --round and --note pass through to the stamped event',
    r.code === 0 && !!ev && ev.cluster === 'c3' && ev.round === 2 && ev.note === 'triage note', JSON.stringify(ev))
}
{
  let r = run('run-scoped-tests.mjs', ['--root', T, '--kind', 'green', '--filter', 'x'])
  check('missing <target> is a usage error (exit 2)', r.code === 2, `exit ${r.code}: ${r.out.trim()}`)
  r = run('run-scoped-tests.mjs', ['--root', T, target, '--filter', 'x'])
  check('missing --kind is a usage error (exit 2)', r.code === 2, `exit ${r.code}: ${r.out.trim()}`)
  r = run('run-scoped-tests.mjs', ['--root', T, target, '--kind', 'green'])
  check('missing --filter (api mode, no --cmd) is a usage error (exit 2)', r.code === 2, `exit ${r.code}: ${r.out.trim()}`)
  r = run('run-scoped-tests.mjs', ['--root', T, target, '--kind', 'green', '--ui'])
  check('missing --include (ui mode, no --cmd) is a usage error (exit 2)', r.code === 2, `exit ${r.code}: ${r.out.trim()}`)
}

rmSync(T, { recursive: true, force: true })
