#!/usr/bin/env node
// Runner for the reviews/lib test suite: discovers every *.test.mjs under tests/ (unit/ per module
// or command, flows/ per flow that crosses several), imports each in path order so its top-level
// check() calls run against lib.mjs's shared tally, then aggregates the per-file counts into one
// final summary line. `--only` matches the file's own name, whichever folder holds it.
//
// Usage: node reviews/lib/tests/run-tests.mjs [--only <name>]
// Exit: 0 all assertions passed · 1 one or more failed · 2 --only matched no test file.
import { readdirSync } from 'node:fs'
import { dirname, basename, join } from 'node:path'
import { fileURLToPath, pathToFileURL } from 'node:url'
import { check, currentTally, resetTally, gitIdentity, IDENTITY_AT_LOAD } from './lib.mjs'

const TESTS_DIR = dirname(fileURLToPath(import.meta.url))

const onlyIndex = process.argv.indexOf('--only')
const only = onlyIndex === -1 ? null : process.argv[onlyIndex + 1]

const discover = (dir, prefix = '') => readdirSync(dir, { withFileTypes: true })
  .filter(e => e.name !== 'fixtures')
  .flatMap(e => (e.isDirectory() ? discover(join(dir, e.name), `${prefix}${e.name}/`)
    : e.name.endsWith('.test.mjs') ? [`${prefix}${e.name}`] : []))

let files = discover(TESTS_DIR).sort()
if (only) {
  files = files.filter(f => basename(f, '.test.mjs').includes(only))
  if (files.length === 0) {
    console.log(`--only ${only} matched no test file under reviews/lib/tests/`)
    process.exit(2)
  }
}

let total = 0
const failures = []
// A test file that throws (a fixtureGit setup call refusing, say) must not take the identity guard
// down with it: the throw becomes a failed assertion of its own and the run carries on to the guard.
for (const f of files) {
  resetTally()
  let threw = null
  try {
    await import(pathToFileURL(join(TESTS_DIR, f)).href)
  } catch (e) {
    threw = e
  }
  if (threw) check(`${f} ran to completion`, false, `threw: ${threw?.message ?? threw}`)
  const { count, failures: fileFailures } = currentTally()
  console.log(`  ${f}: ${count}${threw ? ' (threw)' : ''}`)
  total += count
  failures.push(...fileFailures)
}

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
