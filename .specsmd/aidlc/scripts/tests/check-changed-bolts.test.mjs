import { test } from 'node:test'
import assert from 'node:assert/strict'
import { mkdtempSync, mkdirSync, writeFileSync, rmSync } from 'node:fs'
import { join, dirname } from 'node:path'
import { tmpdir } from 'node:os'
import { spawnSync } from 'node:child_process'
import { fileURLToPath } from 'node:url'

const SCRIPT = join(dirname(fileURLToPath(import.meta.url)), '..', 'check-changed-bolts.mjs')
process.env.GIT_DIR = join(tmpdir(), 'bogus-inherited-git-dir')
process.env.GIT_INDEX_FILE = join(tmpdir(), 'bogus-inherited-index')
const GIT_ENV = Object.fromEntries(Object.entries(process.env).filter(([k]) => !k.startsWith('GIT_')))
Object.assign(GIT_ENV, { GIT_CONFIG_GLOBAL: join(tmpdir(), 'no-such-gitconfig'), GIT_CONFIG_NOSYSTEM: '1', GIT_AUTHOR_NAME: 'fixture', GIT_AUTHOR_EMAIL: 'fixture@example.test', GIT_COMMITTER_NAME: 'fixture', GIT_COMMITTER_EMAIL: 'fixture@example.test' })

function git(root, ...args) {
  const r = spawnSync('git', ['-c', 'commit.gpgsign=false', '-c', 'core.autocrlf=false', ...args], { cwd: root, encoding: 'utf8', env: GIT_ENV })
  if (r.status !== 0) throw new Error(`git ${args.join(' ')} failed: ${r.stdout}${r.stderr}`)
  return r.stdout.trim()
}

function driver(root, ...args) {
  const r = spawnSync(process.execPath, [SCRIPT, ...args, '--root', root], { encoding: 'utf8', env: GIT_ENV })
  return { code: r.status, out: `${r.stdout}${r.stderr}` }
}

const BOLT = 'memory-bank/bolts/900-fixture'
const boltMd = stages => `---\nid: 900-fixture\nunit: 001-u\nintent: 001-i\ntype: simple-construction-bolt\nstatus: in_progress\ncurrent_stage: null\nstages_completed:${stages.length ? '\n' + stages.map(s => `  - name: ${s}\n    completed: 2026-09-04T10:00:00Z`).join('\n') : ' []'}\n---\n# Bolt\n`
const write = (root, rel, content) => { mkdirSync(dirname(join(root, rel)), { recursive: true }); writeFileSync(join(root, rel), content) }

function repo() {
  const root = mkdtempSync(join(tmpdir(), 'check-changed-bolts-'))
  git(root, 'init', '-q', '-b', 'main')
  write(root, `${BOLT}/bolt.md`, boltMd([]))
  write(root, 'README.md', '# fixture\n')
  git(root, 'add', '-A')
  git(root, 'commit', '-q', '-m', 'init')
  return root
}

const GOOD_ROWS = [
  { id: 'FM-1', source: 'bolt', fails: 'refund after 14 days accepted', expected: 'refused with 409', test: 'Orders.RefundTests', log: 'refund_refused' },
  { source: 'attack', none: true, reason: 'nothing to add' },
].map(o => JSON.stringify(o)).join('\n') + '\n'

test('--help prints usage and exits 0; no mode is a usage error', () => {
  const r = spawnSync(process.execPath, [SCRIPT, '--help'], { encoding: 'utf8' })
  assert.equal(r.status, 0)
  assert.match(`${r.stdout}${r.stderr}`, /--staged \| --base <ref>/)
  assert.equal(driver(tmpdir()).code, 2)
})

test('a staged change that does not complete a stage passes with nothing to check', () => {
  const root = repo()
  write(root, 'README.md', '# fixture\nchanged\n')
  git(root, 'add', '-A')
  const r = driver(root, '--staged')
  assert.equal(r.code, 0, r.out)
  assert.match(r.out, /nothing to check/)
  rmSync(root, { recursive: true, force: true })
})

test('a staged stage completion with unmet conditions is refused with the check\'s list and the lint\'s lines', () => {
  const root = repo()
  write(root, `${BOLT}/bolt.md`, boltMd(['plan']))
  write(root, `${BOLT}/implementation-plan.md`, '## Deliverables\n\n### Completed Work\n- planned everything\n')
  git(root, 'add', '-A')
  const r = driver(root, '--staged')
  assert.equal(r.code, 1, r.out)
  assert.match(r.out, /900-fixture plan: 1 unmet/)
  assert.match(r.out, /failure-modes\.jsonl is missing/)
  assert.match(r.out, /implementation-plan\.md:4: error/)
  assert.match(r.out, /construction gates: FAILED/)
  rmSync(root, { recursive: true, force: true })
})

test('the same completion passes once the conditions are met, and the artifact lint only warns on numbers', () => {
  const root = repo()
  write(root, `${BOLT}/bolt.md`, boltMd(['plan']))
  write(root, `${BOLT}/failure-modes.jsonl`, GOOD_ROWS)
  write(root, `${BOLT}/implementation-plan.md`, '### Completed Work\n- 12 rows planned in memory-bank/bolts/900-fixture/failure-modes.jsonl\n- 30 more cases listed by `dotnet test --list-tests`\n')
  git(root, 'add', '-A')
  const r = driver(root, '--staged')
  assert.equal(r.code, 0, r.out)
  assert.match(r.out, /ok: 900-fixture plan/)
  assert.match(r.out, /implementation-plan\.md:3: warning/)
  assert.match(r.out, /construction gates: ok \(1 stage check, 1 lint\)/)
  rmSync(root, { recursive: true, force: true })
})

test('--base compares HEAD with the merge base and checks every stage added since', () => {
  const root = repo()
  const base = git(root, 'rev-parse', 'HEAD')
  write(root, `${BOLT}/bolt.md`, boltMd(['plan']))
  write(root, `${BOLT}/failure-modes.jsonl`, GOOD_ROWS)
  git(root, 'add', '-A')
  git(root, 'commit', '-q', '-m', 'plan done')
  assert.equal(driver(root, '--base', base).code, 0)
  write(root, `${BOLT}/bolt.md`, boltMd(['plan', 'implement']))
  git(root, 'add', '-A')
  git(root, 'commit', '-q', '-m', 'implement done without stamps')
  const r = driver(root, '--base', base)
  assert.equal(r.code, 1, r.out)
  assert.match(r.out, /900-fixture implement: 1 unmet/)
  assert.match(r.out, /test-stamps\.jsonl is missing/)
  assert.doesNotMatch(r.out, /900-fixture plan:/)
  assert.equal(driver(root, '--base', 'HEAD').code, 0)
  rmSync(root, { recursive: true, force: true })
})

test('a brand-new bolt.md that already lists completed stages is checked too', () => {
  const root = repo()
  write(root, 'memory-bank/bolts/901-new/bolt.md', boltMd(['plan']).replace('900-fixture', '901-new'))
  git(root, 'add', '-A')
  const r = driver(root, '--staged')
  assert.equal(r.code, 1, r.out)
  assert.match(r.out, /901-new plan: 1 unmet/)
  rmSync(root, { recursive: true, force: true })
})

test('spike bolts pass as exempt and stage artifacts of other bolt folders are not linted', () => {
  const root = repo()
  write(root, 'memory-bank/bolts/902-spike/bolt.md', boltMd(['explore']).replace('900-fixture', '902-spike').replace('simple-construction-bolt', 'spike-bolt'))
  write(root, 'memory-bank/bolts/902-spike/notes.md', '### Completed Work\n- explored things\n')
  git(root, 'add', '-A')
  const r = driver(root, '--staged')
  assert.equal(r.code, 0, r.out)
  assert.match(r.out, /exempt/)
  rmSync(root, { recursive: true, force: true })
})
