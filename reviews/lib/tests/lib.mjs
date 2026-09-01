// Shared helpers for the reviews/lib test files: the plain check(name, ok, detail) assert,
// the child-process run(script, args) wrapper, the fixture-root constants, and verify/git.mjs's
// env scrub re-exported for throwaway-repo fixtures. The runner (run-tests.mjs) reads/resets the tally
// via currentTally()/resetTally() between test files.
//
// Usage: import { check, run, ... } from './lib.mjs' in a <script>.test.mjs file.
import { spawnSync } from 'node:child_process'
import { join, dirname } from 'node:path'
import { fileURLToPath } from 'node:url'
import { REVIEWS, REPO } from '../records/schema.mjs'

const FIXTURES = join(dirname(fileURLToPath(import.meta.url)), 'fixtures')
export const GOOD_ROOT = join(FIXTURES, 'repo')
export const BAD_STATE_ROOT = join(FIXTURES, 'bad-state')

let count = 0
const failures = []
export function check(name, ok, detail) {
  count++
  if (!ok) failures.push(detail ? `${name}\n      ${detail}` : name)
}
export function currentTally() { return { count, failures: [...failures] } }
export function resetTally() { count = 0; failures.length = 0 }

export function run(script, args) {
  const r = spawnSync(process.execPath, [join(REVIEWS, 'lib', script), ...args], { encoding: 'utf8', cwd: REPO })
  if (r.error) return { code: -1, out: String(r.error) }
  return { code: r.status, out: `${r.stdout ?? ''}${r.stderr ?? ''}` }
}
export const firstLine = out => out.split('\n')[0]

export { scrubbedGitEnv } from '../verify/git.mjs'
