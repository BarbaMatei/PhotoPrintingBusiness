// Flow: run-tests.mjs itself — discovery, per-file lane spawning, and the parent's report()
// classifying a misbehaving lane. Two committed fixtures back this (tests/fixtures/mw-runner-*),
// kept under fixtures/ so the discovery filter never runs them as part of the real suite; this
// flow copies run-tests.mjs's shallow dependency chain (lib.mjs, records/schema.mjs,
// verify/git.mjs) into a scratch tree with no folder named "fixtures", drops the two fixtures in
// at the top level so the unmodified discovery logic finds them, then drives that copy as a child.
//
// Usage: node reviews/lib/tests/run-tests.mjs --only runner
import { check } from '../lib.mjs'
import { mkdtempSync, mkdirSync, writeFileSync, readFileSync, rmSync } from 'node:fs'
import { join, dirname } from 'node:path'
import { tmpdir } from 'node:os'
import { fileURLToPath } from 'node:url'
import { spawnSync } from 'node:child_process'

const HERE = dirname(fileURLToPath(import.meta.url))
const TESTS_DIR = join(HERE, '..')
const LIB_DIR = join(TESTS_DIR, '..')
const FIXTURES = join(TESTS_DIR, 'fixtures')

const T = mkdtempSync(join(tmpdir(), 'mw-runner-flow-'))
mkdirSync(join(T, 'tests'), { recursive: true })
mkdirSync(join(T, 'records'), { recursive: true })
mkdirSync(join(T, 'verify'), { recursive: true })

for (const f of ['run-tests.mjs', 'lib.mjs']) writeFileSync(join(T, 'tests', f), readFileSync(join(TESTS_DIR, f)))
writeFileSync(join(T, 'records', 'schema.mjs'), readFileSync(join(LIB_DIR, 'records', 'schema.mjs')))
writeFileSync(join(T, 'verify', 'git.mjs'), readFileSync(join(LIB_DIR, 'verify', 'git.mjs')))
for (const f of ['mw-runner-a-throws.test.mjs', 'mw-runner-b-exit7.test.mjs']) writeFileSync(join(T, 'tests', f), readFileSync(join(FIXTURES, f)))

const r = spawnSync(process.execPath, [join(T, 'tests', 'run-tests.mjs'), '--only', 'mw-runner'], { encoding: 'utf8' })
const out = `${r.stdout ?? ''}${r.stderr ?? ''}`

check('a lane that throws mid-import and a lane that exits non-zero fail the run', r.status === 1, `exit ${r.status}: ${out.trim()}`)
check('the file that threw mid-import is reported with (threw)', /mw-runner-a-throws\.test\.mjs: \d+ \(threw\)/.test(out), out)
check('the file that called process.exit(7) is reported as exiting without leaving a tally', /exited 7 without leaving a tally/.test(out), out)
check('the git identity guard runs exactly once, after both lanes are reported', (out.match(/\(git identity guard\):/g) || []).length === 1, out)
{
  const order = [...out.matchAll(/^ {2}(mw-runner-[\w.-]+\.test\.mjs): /gm)].map(m => m[1])
  check('file output is path-ordered regardless of which lane finished first',
    order.length === 2 && order[0] === 'mw-runner-a-throws.test.mjs' && order[1] === 'mw-runner-b-exit7.test.mjs', JSON.stringify(order))
}

rmSync(T, { recursive: true, force: true })
