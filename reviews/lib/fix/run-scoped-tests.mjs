#!/usr/bin/env node
// Stamping, locking wrapper around the API and UI test runners: builds the dotnet/npm
// command for a filter or include fragment, holds a machine-global lock so only one test
// process runs on this machine at a time, and stamps exactly one test-run worklog event
// with the parsed pass/fail counts and measured duration.
//
// Usage: node reviews/lib/run-scoped-tests.mjs [--root <repoRoot>] <target> --kind
//   <red|green|final|baseline|revert-and-rerun> (--filter "<FQN fragment>" | --ui --include
//   "<name>") [--cluster <c>] [--round <n>] [--note "<text>"]
//   [--cmd "<template with {filter}/{name}>"] [--dry-run] [--no-events] [--summary]
// Commands: API `dotnet test src/PhotoPrint.Tests --filter "FullyQualifiedName~{filter}"`;
// UI `npm --prefix src/PhotoPrint.UI test -- --watch=false --include=**/{name}*.spec.ts`
// (unquoted — spawnSync's shell on Windows is cmd.exe, which does not strip single quotes,
// so a quoted glob never reaches the runner as a glob). --cmd overrides the built command
// (fixtures use `node -e` stand-ins) and still accepts {filter}/{name} substitution when
// present; --filter/--include are required even then, so every stamped event carries the
// field.
// Lock: <os.tmpdir()>/photoprint-test.lock, created with 'wx' holding {pid, started}. A
// live holder's pid means exit 3 before running anything, and that lock file is left
// untouched. A dead holder's lock is stolen, but only once per attempt — if the retried
// 'wx' create collides again right after a steal, that second collision is treated as a
// live holder (exit 3) rather than stolen again, so two processes racing the same stale
// lock can't unlink each other's fresh lock forever. The lock is released in a finally
// block after the run, and only when the file's own pid still matches ours. SIGINT/SIGTERM
// handlers do the same release on POSIX; Windows does not reliably deliver a targeted kill
// to these handlers, so there the dead-pid steal above is the real recovery path.
// Output parsing: dotnet's `Failed: n, Passed: n(, Skipped: n)?` summary line, or (--ui)
// vitest's `Tests (n failed )?n passed`; either half falls back to `node --test`'s TAP
// totals (`# pass n` / `# fail n`, plus `# skipped n` when present). Unparsable output stamps
// passed/failed as null with a note, but the process still exits with the runner's own exit code.
// --summary replaces the runner's output with one totals line (passed/failed/skipped) plus the
// failing test names: dotnet's `Failed <Name>` and `<Name> [FAIL]` lines, vitest's `FAIL <spec >
// test>` lines (its `×` lines when no FAIL line is printed), TAP's `not ok` lines.
// Exit: N the runner's own exit code (0 on a green run, non-zero on red) · 2 usage error ·
// 3 another test process already holds the lock.
import { writeFileSync, readFileSync, unlinkSync } from 'node:fs'
import { spawnSync } from 'node:child_process'
import { join } from 'node:path'
import { tmpdir } from 'node:os'
import { repoRoot, takeRoot } from '../cli/args.mjs'
import { appendEvent } from '../records/wl.mjs'

const DEFAULT_API_CMD = 'dotnet test src/PhotoPrint.Tests --filter "FullyQualifiedName~{filter}"'
const DEFAULT_UI_CMD = 'npm --prefix src/PhotoPrint.UI test -- --watch=false --include=**/{name}*.spec.ts'
const LOCK_PATH = join(tmpdir(), 'photoprint-test.lock')
const DOTNET_RE = /Failed:\s*(\d+), Passed:\s*(\d+)(?:, Skipped:\s*(\d+))?/
const VITEST_RE = /Tests\s+(?:(\d+) failed[^\d]*)?(\d+) passed/
const TAP_PASS_RE = /^# pass (\d+)\s*$/m
const TAP_FAIL_RE = /^# fail (\d+)\s*$/m
const TAP_SKIP_RE = /^# skipped (\d+)\s*$/m
const DOTNET_FAILED_RE = /^\s*Failed\s+(\S.*?)(?:\s+\[[^\]]*\])?\s*$/gm
const XUNIT_FAIL_RE = /^\s*(\S.*?)\s+\[FAIL\]\s*$/gm
const VITEST_FAIL_RE = /^\s*FAIL\s+(\S.*?)\s*$/gm
const VITEST_X_RE = /^\s*[×✗]\s+(\S.*?)(?:\s+\d+(?:\.\d+)?ms)?\s*$/gm
const TAP_NOT_OK_RE = /^\s*not ok\s+\d+\s*-?\s*(\S.*?)\s*$/gm

function usageError(message) {
  console.error(`usage: ${message}`)
  process.exit(2)
}

function parseArgs(rawArgv) {
  const { root, rest: argv } = takeRoot(rawArgv)
  const args = { cluster: null, round: null, note: null, cmd: null, dryRun: false, noEvents: false, ui: false, root }
  const positional = []
  for (let i = 0; i < argv.length; i++) {
    const a = argv[i]
    if (a === '--kind') args.kind = argv[++i]
    else if (a === '--filter') args.filter = argv[++i]
    else if (a === '--include') args.include = argv[++i]
    else if (a === '--ui') args.ui = true
    else if (a === '--cluster') args.cluster = argv[++i]
    else if (a === '--round') args.round = Number(argv[++i])
    else if (a === '--note') args.note = argv[++i]
    else if (a === '--cmd') args.cmd = argv[++i]
    else if (a === '--dry-run') args.dryRun = true
    else if (a === '--no-events') args.noEvents = true
    else if (a === '--summary') args.summary = true
    else positional.push(a)
  }
  args.target = positional[0]
  return args
}

function isAlive(pid) {
  if (!Number.isInteger(pid)) return false
  try { process.kill(pid, 0); return true } catch { return false }
}

// Only the creating run may delete the lock, and a dead holder's lock is stolen at most once.
function acquireLock() {
  const payload = JSON.stringify({ pid: process.pid, started: new Date().toISOString() })
  let stolenOnce = false
  for (;;) {
    try {
      writeFileSync(LOCK_PATH, payload, { flag: 'wx' })
      return
    } catch (e) {
      if (e.code !== 'EEXIST') throw e
      let existing = null
      try { existing = JSON.parse(readFileSync(LOCK_PATH, 'utf8')) } catch { existing = null }
      if (stolenOnce || isAlive(existing?.pid)) {
        console.error(`another test process is running (pid ${existing?.pid ?? 'unknown'}) — the machine takes one at a time`)
        process.exit(3)
      }
      try { unlinkSync(LOCK_PATH) } catch { /* raced with the dead holder's own cleanup */ }
      stolenOnce = true
    }
  }
}

// `node --test` prints its totals as TAP comment lines, so both halves fall back to it: the
// review machinery's own suites run under that runner and used to stamp nulls.
function parseTap(text) {
  const pass = TAP_PASS_RE.exec(text), fail = TAP_FAIL_RE.exec(text)
  if (!pass || !fail) return null
  const out = { passed: Number(pass[1]), failed: Number(fail[1]) }
  const skipped = TAP_SKIP_RE.exec(text)
  if (skipped) out.skipped = Number(skipped[1])
  return out
}

function parseOutput(mode, text) {
  if (mode === 'ui') {
    const m = VITEST_RE.exec(text)
    if (m) return { passed: Number(m[2]), failed: m[1] !== undefined ? Number(m[1]) : 0 }
    return parseTap(text) ?? { passed: null, failed: null, note: 'unparsed runner output' }
  }
  const m = DOTNET_RE.exec(text)
  if (!m) return parseTap(text) ?? { passed: null, failed: null, note: 'unparsed runner output' }
  const out = { passed: Number(m[2]), failed: Number(m[1]) }
  if (m[3] !== undefined) out.skipped = Number(m[3])
  return out
}

function failingTests(mode, text) {
  const names = (...patterns) => {
    const found = new Set()
    for (const re of patterns) for (const m of text.matchAll(re)) found.add(m[1])
    return [...found]
  }
  if (mode === 'ui') {
    const fromFailLines = names(VITEST_FAIL_RE)
    return fromFailLines.length ? fromFailLines : names(VITEST_X_RE, TAP_NOT_OK_RE)
  }
  return [...names(DOTNET_FAILED_RE).filter(name => name.includes('.')), ...names(XUNIT_FAIL_RE, TAP_NOT_OK_RE)]
}

function printSummary(parsed, failing, exitCode) {
  if (parsed.passed === null) console.log(`totals unparsed (runner exit ${exitCode})`)
  else console.log(`passed ${parsed.passed}, failed ${parsed.failed}${parsed.skipped !== undefined ? `, skipped ${parsed.skipped}` : ''}`)
  for (const name of failing) console.log(`  ${name}`)
}

function main() {
  const args = parseArgs(process.argv.slice(2))
  const REPO = repoRoot(import.meta.url, args.root)
  if (!args.target) usageError('missing <target>')
  if (!args.kind) usageError('missing --kind')
  if (args.ui ? !args.include : !args.filter) {
    usageError(args.ui ? 'missing --include' : 'missing --filter')
  }

  const mode = args.ui ? 'ui' : 'api'
  let cmd = args.cmd ?? (mode === 'ui' ? DEFAULT_UI_CMD : DEFAULT_API_CMD)
  if (args.filter != null) cmd = cmd.replaceAll('{filter}', args.filter)
  if (args.include != null) cmd = cmd.replaceAll('{name}', args.include)

  if (args.dryRun) {
    console.log(cmd)
    process.exit(0)
  }

  acquireLock()
  let lockHeld = true
  const releaseLock = () => {
    if (!lockHeld) return
    lockHeld = false
    try {
      const existing = JSON.parse(readFileSync(LOCK_PATH, 'utf8'))
      if (existing?.pid === process.pid) unlinkSync(LOCK_PATH)
    } catch { /* nothing to release, or already gone */ }
  }
  process.on('SIGINT', () => { releaseLock(); process.exit(130) })
  process.on('SIGTERM', () => { releaseLock(); process.exit(143) })

  let exitCode = 1
  try {
    const started = Date.now()
    const result = spawnSync(cmd, { cwd: REPO, shell: true, encoding: 'utf8', timeout: 600000 })
    const duration_s = Math.round(Date.now() - started) / 1000
    exitCode = result.status ?? 1
    const output = `${result.stdout ?? ''}${result.stderr ?? ''}`
    const parsed = parseOutput(mode, output)
    if (args.summary) printSummary(parsed, failingTests(mode, output), exitCode)
    else {
      if (result.stdout) process.stdout.write(result.stdout)
      if (result.stderr) process.stderr.write(result.stderr)
    }

    const event = { kind: args.kind, passed: parsed.passed, failed: parsed.failed, duration_s }
    if (mode === 'api' && args.filter != null) event.filter = args.filter
    if (mode === 'ui' && args.include != null) event.include = args.include
    if (parsed.skipped !== undefined) event.skipped = parsed.skipped
    if (args.cluster != null) event.cluster = args.cluster
    if (args.round != null && !Number.isNaN(args.round)) event.round = args.round
    const note = [args.note, parsed.note].filter(Boolean).join('; ')
    if (note) event.note = note

    if (!args.noEvents) {
      try { appendEvent(REPO, args.target, { ev: 'test-run', ...event }) }
      catch (e) { console.error(`note: test-run not recorded: ${e.message}`) }
    }
  } finally {
    releaseLock()
  }
  process.exit(exitCode)
}

main()
