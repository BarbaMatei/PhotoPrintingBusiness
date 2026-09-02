#!/usr/bin/env node
// Runner for the reviews/lib test suite: discovers every *.test.mjs under tests/ (unit/ per module
// or command, flows/ per flow that crosses several) and runs them in four parallel lanes, one child
// process per file, then aggregates the per-file counts into one final summary line. Nearly every
// assertion here costs a child process of its own, so the files are the unit of parallelism; the
// report is still emitted in path order, and the git identity guard still runs last, in this
// process, once every file has finished. `--only` matches the file's own name, whichever folder
// holds it, and `--file` is how a lane names the one file it was given.
//
// Usage: node reviews/lib/tests/run-tests.mjs [--only <name>]
// Exit: 0 all assertions passed · 1 one or more failed · 2 --only matched no test file.
import { mkdtempSync, readdirSync, readFileSync, rmSync, writeFileSync } from 'node:fs'
import { tmpdir } from 'node:os'
import { dirname, basename, join } from 'node:path'
import { fileURLToPath, pathToFileURL } from 'node:url'
import { spawn } from 'node:child_process'
import { check, currentTally, resetTally, gitIdentity, IDENTITY_AT_LOAD } from './lib.mjs'

const SELF = fileURLToPath(import.meta.url)
const TESTS_DIR = dirname(SELF)
const arg = name => {
  const i = process.argv.indexOf(name)
  return i === -1 ? null : process.argv[i + 1]
}

// ---------- a lane: import the one file it was given, leave its tally where the parent asked ----
// The tally goes to a file rather than the pipe because the test file owns stdout, and a lane never
// calls process.exit: an exit would cut off output the parent has not read yet.
const oneFile = arg('--file')
if (oneFile !== null) {
  let threw = null
  try {
    await import(pathToFileURL(join(TESTS_DIR, oneFile)).href)
  } catch (e) {
    threw = e
  }
  const { count, failures } = currentTally()
  writeFileSync(arg('--result'), JSON.stringify({ count, failures, threw: threw ? String(threw?.message ?? threw) : null }))
} else {
  const discover = (dir, prefix = '') => readdirSync(dir, { withFileTypes: true })
    .filter(e => e.name !== 'fixtures')
    .flatMap(e => (e.isDirectory() ? discover(join(dir, e.name), `${prefix}${e.name}/`)
      : e.name.endsWith('.test.mjs') ? [`${prefix}${e.name}`] : []))

  const only = arg('--only')
  let files = discover(TESTS_DIR).sort()
  if (only) {
    files = files.filter(f => basename(f, '.test.mjs').includes(only))
    if (files.length === 0) {
      console.log(`--only ${only} matched no test file under reviews/lib/tests/`)
      process.exit(2)
    }
  }

  const LANES = Math.max(1, Math.min(4, files.length))
  const scratch = mkdtempSync(join(tmpdir(), 'review-lib-tests-'))
  const done = new Array(files.length).fill(null)

  const runFile = i => new Promise(resolve => {
    const resultPath = join(scratch, `${i}.json`)
    const lane = spawn(process.execPath, [SELF, '--file', files[i], '--result', resultPath], { stdio: ['ignore', 'pipe', 'pipe'] })
    let out = '', errOut = ''
    lane.stdout.on('data', d => { out += d })
    lane.stderr.on('data', d => { errOut += d })
    lane.on('close', code => {
      let tally = null
      if (code === 0) { try { tally = JSON.parse(readFileSync(resultPath, 'utf8')) } catch { /* reported as a throw below */ } }
      done[i] = tally
        ? { ...tally, out, errOut }
        : { count: 0, failures: [], out, errOut, threw: `the lane running it exited ${code} without leaving a tally${errOut.trim() ? `: ${errOut.trim().split('\n').slice(-3).join(' | ')}` : ''}` }
      resolve()
    })
  })

  let total = 0
  const failures = []
  // A test file that throws (a fixtureGit setup call refusing, say) must not take the identity
  // guard down with it: the throw becomes a failed assertion of its own and the run carries on to
  // the guard. Lanes finish out of order, so a file's block is only printed once every earlier
  // file has printed — the report reads the same whatever order the lanes came home in.
  let printed = 0
  const report = () => {
    while (printed < files.length && done[printed]) {
      const f = files[printed], r = done[printed]
      if (r.out) process.stdout.write(r.out)
      if (r.errOut) process.stderr.write(r.errOut)
      resetTally()
      if (r.threw) check(`${f} ran to completion`, false, `threw: ${r.threw}`)
      const { count: own, failures: ownFailures } = currentTally()
      console.log(`  ${f}: ${r.count + own}${r.threw ? ' (threw)' : ''}`)
      total += r.count + own
      failures.push(...r.failures, ...ownFailures)
      printed++
    }
  }

  let next = 0
  const lane = async () => { while (next < files.length) { const i = next++; await runFile(i); report() } }
  await Promise.all(Array.from({ length: LANES }, lane))
  rmSync(scratch, { recursive: true, force: true })

  resetTally()
  {
    const before = IDENTITY_AT_LOAD
    const after = gitIdentity()
    const unreadable = before.error ?? after.error
    check("the suite left the repository's git identity untouched",
      !unreadable && after.name === before.name && after.email === before.email,
      unreadable
        ? `the repository's git config was unreadable, so the guard cannot clear the run: ${unreadable}`
        : `git config now reads "${after.name} <${after.email}>", was "${before.name} <${before.email}>" — a fixture repo's config write leaked into the real repository; restore it before committing`)
    const { count, failures: guard } = currentTally()
    console.log(`  (git identity guard): ${count}`)
    total += count
    failures.push(...guard)
  }

  if (failures.length) {
    console.log(`FAIL: ${failures.length} of ${total} assertion(s) failed:\n`)
    for (const f of failures) console.log(`  - ${f}`)
    process.exit(1)
  }
  console.log(`${total} assertions, all passed`)
}
