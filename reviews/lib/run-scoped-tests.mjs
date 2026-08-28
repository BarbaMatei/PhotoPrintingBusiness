#!/usr/bin/env node
// Stamping, locking wrapper around the API and UI test runners: builds the dotnet/npm
// command for a filter or include fragment, holds a machine-global lock so only one test
// process runs on this machine at a time, and stamps exactly one test-run worklog event
// with the parsed pass/fail counts and measured duration.
//
// Usage: node reviews/lib/run-scoped-tests.mjs [--root <repoRoot>] <target> --kind
//   <red|green|final|baseline|revert-and-rerun> (--filter "<FQN fragment>" | --ui --include
//   "<name>") [--cluster <c>] [--round <n>] [--note "<text>"]
//   [--cmd "<template with {filter}/{name}>"] [--dry-run] [--no-events]
// Commands: API `dotnet test src/PhotoPrint.Tests --filter "FullyQualifiedName~{filter}"`;
// UI `npm --prefix src/PhotoPrint.UI test -- --watch=false --include='**/{name}*.spec.ts'`.
// --cmd overrides the built command (fixtures use `node -e` stand-ins) and still accepts
// {filter}/{name} substitution when present.
// Lock: <os.tmpdir()>/photoprint-test.lock, created with 'wx' holding {pid, started}. A
// live holder's pid means exit 3 before running anything; a dead one is stolen. The lock is
// only ever removed by the run that acquired it, always on exit (finally + SIGINT/SIGTERM).
// Output parsing: dotnet's `Failed: n, Passed: n(, Skipped: n)?` summary line, or (--ui)
// vitest's `Tests (n failed )?n passed`. Unparsable output stamps passed/failed as null with
// a note, but the process still exits with the runner's own exit code.
// Exit: N the runner's own exit code (0 on a green run, non-zero on red) · 2 usage error ·
// 3 another test process already holds the lock.
import { writeFileSync, readFileSync, unlinkSync, existsSync } from 'node:fs'
import { spawnSync } from 'node:child_process'
import { join, dirname } from 'node:path'
import { fileURLToPath } from 'node:url'
import { tmpdir } from 'node:os'
import { appendEvent } from './wl.mjs'

const DEFAULT_API_CMD = 'dotnet test src/PhotoPrint.Tests --filter "FullyQualifiedName~{filter}"'
const DEFAULT_UI_CMD = "npm --prefix src/PhotoPrint.UI test -- --watch=false --include='**/{name}*.spec.ts'"
const LOCK_PATH = join(tmpdir(), 'photoprint-test.lock')
const DOTNET_RE = /Failed:\s*(\d+), Passed:\s*(\d+)(?:, Skipped:\s*(\d+))?/
const VITEST_RE = /Tests\s+(?:(\d+) failed[^\d]*)?(\d+) passed/

function usageError(message) {
  console.error(`usage: ${message}`)
  process.exit(2)
}

function parseArgs(argv) {
  const args = { cluster: null, round: null, note: null, cmd: null, dryRun: false, noEvents: false, ui: false }
  const positional = []
  for (let i = 0; i < argv.length; i++) {
    const a = argv[i]
    if (a === '--root') args.root = argv[++i]
    else if (a === '--kind') args.kind = argv[++i]
    else if (a === '--filter') args.filter = argv[++i]
    else if (a === '--include') args.include = argv[++i]
    else if (a === '--ui') args.ui = true
    else if (a === '--cluster') args.cluster = argv[++i]
    else if (a === '--round') args.round = Number(argv[++i])
    else if (a === '--note') args.note = argv[++i]
    else if (a === '--cmd') args.cmd = argv[++i]
    else if (a === '--dry-run') args.dryRun = true
    else if (a === '--no-events') args.noEvents = true
    else positional.push(a)
  }
  args.target = positional[0]
  return args
}

function isAlive(pid) {
  if (!Number.isInteger(pid)) return false
  try { process.kill(pid, 0); return true } catch { return false }
}

// Only the run that successfully creates the lock owns it, so only that run may delete it —
// a run that exits 3 because another pid holds it must never touch that file.
function acquireLock() {
  const payload = JSON.stringify({ pid: process.pid, started: new Date().toISOString() })
  for (;;) {
    try {
      writeFileSync(LOCK_PATH, payload, { flag: 'wx' })
      return
    } catch (e) {
      if (e.code !== 'EEXIST') throw e
      let existing = null
      try { existing = JSON.parse(readFileSync(LOCK_PATH, 'utf8')) } catch { existing = null }
      if (isAlive(existing?.pid)) {
        console.error(`another test process is running (pid ${existing.pid}) — the machine takes one at a time`)
        process.exit(3)
      }
      try { unlinkSync(LOCK_PATH) } catch { /* raced with the dead holder's own cleanup */ }
    }
  }
}

function parseOutput(mode, text) {
  if (mode === 'ui') {
    const m = VITEST_RE.exec(text)
    if (!m) return { passed: null, failed: null, note: 'unparsed runner output' }
    return { passed: Number(m[2]), failed: m[1] !== undefined ? Number(m[1]) : 0 }
  }
  const m = DOTNET_RE.exec(text)
  if (!m) return { passed: null, failed: null, note: 'unparsed runner output' }
  const out = { passed: Number(m[2]), failed: Number(m[1]) }
  if (m[3] !== undefined) out.skipped = Number(m[3])
  return out
}

function main() {
  const args = parseArgs(process.argv.slice(2))
  const REPO = args.root ?? join(dirname(fileURLToPath(import.meta.url)), '..', '..')
  if (!args.target) usageError('missing <target>')
  if (!args.kind) usageError('missing --kind')
  if (!args.cmd && !(args.ui ? args.include : args.filter)) {
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
  const releaseLock = () => { if (lockHeld) { lockHeld = false; try { unlinkSync(LOCK_PATH) } catch { /* already gone */ } } }
  process.on('SIGINT', () => { releaseLock(); process.exit(130) })
  process.on('SIGTERM', () => { releaseLock(); process.exit(143) })

  let exitCode = 1
  try {
    const started = Date.now()
    const result = spawnSync(cmd, { cwd: REPO, shell: true, encoding: 'utf8', timeout: 600000 })
    const duration_s = Math.round(Date.now() - started) / 1000
    if (result.stdout) process.stdout.write(result.stdout)
    if (result.stderr) process.stderr.write(result.stderr)
    exitCode = result.status ?? 1

    const parsed = parseOutput(mode, `${result.stdout ?? ''}${result.stderr ?? ''}`)
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
