// Shared helpers for the reviews/lib test files: the plain check(name, ok, detail) assert,
// the child-process run(script, args) wrapper, the fixture-root constants, fixtureGit() for
// throwaway repos, and verify/git.mjs's env scrub. The runner (run-tests.mjs) reads/resets the
// tally via currentTally()/resetTally() between test files, and compares gitIdentity() at the end
// against IDENTITY_AT_LOAD.
//
// Usage: import { check, run, ... } from './lib.mjs' in a <script>.test.mjs file.
import { spawnSync } from 'node:child_process'
import { join, dirname } from 'node:path'
import { fileURLToPath } from 'node:url'
import { REVIEWS, REPO } from '../records/schema.mjs'
import { scrubbedGitEnv } from '../verify/git.mjs'

const FIXTURES = join(dirname(fileURLToPath(import.meta.url)), 'fixtures')
export const GOOD_ROOT = join(FIXTURES, 'repo')
export const BAD_STATE_ROOT = join(FIXTURES, 'bad-state')
export const SPEED_FIXTURE = join(FIXTURES, 'speed-report')

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

// A fixture's `git config` write reaching the real repository rewrites who authors every later
// commit, and nothing in a green run would say so: the runner compares this against the identity
// left behind once every test file has run.
export function gitIdentity() {
  const get = key => spawnSync('git', ['-C', REPO, 'config', '--get', key],
    { encoding: 'utf8', env: scrubbedGitEnv() }).stdout?.trim() ?? ''
  return { name: get('user.name'), email: get('user.email') }
}
export const IDENTITY_AT_LOAD = gitIdentity()

// Throwaway-repo git. A swallowed nonzero on a setup call — a missing -C target, a refused config
// write — leaves the fixture unbuilt and the assertions measuring the wrong tree, so those throw.
const SETUP = new Set(['init', 'config', 'add', 'commit'])
export function fixtureGit(dir) {
  return (...a) => {
    const r = spawnSync('git', ['-C', dir, ...a], { encoding: 'utf8', env: scrubbedGitEnv() })
    if (SETUP.has(a[0]) && r.status !== 0) {
      throw new Error(`fixture git ${a.join(' ')} in ${dir} exited ${r.status ?? r.error?.code}: ${`${r.stdout ?? ''}${r.stderr ?? ''}`.trim() || r.error?.message || '(no output)'}`)
    }
    return r
  }
}

export { scrubbedGitEnv }
