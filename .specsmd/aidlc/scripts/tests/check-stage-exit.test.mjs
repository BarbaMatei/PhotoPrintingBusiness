import { test } from 'node:test'
import assert from 'node:assert/strict'
import { mkdtempSync, mkdirSync, writeFileSync } from 'node:fs'
import { join, dirname } from 'node:path'
import { tmpdir } from 'node:os'
import { spawnSync } from 'node:child_process'
import { fileURLToPath } from 'node:url'

const SCRIPT = join(dirname(fileURLToPath(import.meta.url)), '..', 'check-stage-exit.mjs')

function runCheck(root, ...args) {
  const r = spawnSync(process.execPath, [SCRIPT, ...args, '--root', root], { encoding: 'utf8' })
  return { code: r.status, out: `${r.stdout}${r.stderr}` }
}

function bolt(type, files = {}) {
  const root = mkdtempSync(join(tmpdir(), 'check-stage-exit-'))
  const dir = join(root, 'memory-bank', 'bolts', '900-fixture')
  mkdirSync(dir, { recursive: true })
  writeFileSync(join(dir, 'bolt.md'), `---\nid: 900-fixture\nunit: 001-u\nintent: 001-i\ntype: ${type}\nstatus: in_progress\ncurrent_stage: null\nstages_completed: []\n---\n# Bolt\n`)
  for (const [name, content] of Object.entries(files)) {
    writeFileSync(join(dir, name), Array.isArray(content) ? content.map(o => JSON.stringify(o)).join('\n') + '\n' : content)
  }
  return root
}

const unmet = out => out.split(/\r?\n/).filter(l => l.startsWith('- '))
const row = (id, extra = {}) => ({ id, source: 'bolt', fails: 'x fails', expected: 'y happens', test: `Orders.RefundTests.${id}`, log: 'refund_refused', ...extra })
const attackNone = { source: 'attack', none: true, reason: 'the design check found nothing to add' }
const stamp = (t, kind, exit, extra = {}) => ({ t, kind, filter: 'Orders.RefundTests', passed: exit === 0 ? 3 : 2, failed: exit === 0 ? 0 : 1, exit, duration_s: 1, ...extra })
const MUT = { file: 'src/PhotoPrint.API/Orders/RefundService.cs', line: 12, how: 'flip', original: 'a <= b', mutated: 'a > b' }
const fullStamps = [
  stamp('2026-09-04T10:00:00Z', 'red', 1),
  stamp('2026-09-04T10:10:00Z', 'green', 0),
  stamp('2026-09-04T10:20:00Z', 'revert-and-rerun', 1, { mutate: MUT }),
  stamp('2026-09-04T10:30:00Z', 'green', 0),
]

test('--help prints usage and exits 0', () => {
  const r = runCheck(tmpdir(), '--help')
  assert.equal(r.code, 0)
  assert.match(r.out, /check-stage-exit\.mjs <bolt-id> <stage>/)
})

test('an unknown bolt or an unknown stage for the type is a usage error (exit 2)', () => {
  const root = bolt('ddd-construction-bolt')
  assert.equal(runCheck(root, '999-nope', 'implement').code, 2)
  const r = runCheck(root, '900-fixture', 'plan')
  assert.equal(r.code, 2)
  assert.match(r.out, /plan.*ddd-construction-bolt|ddd-construction-bolt.*plan/)
})

test('spike bolts are exempt at every stage', () => {
  const root = bolt('spike-bolt')
  const r = runCheck(root, '900-fixture', 'explore')
  assert.equal(r.code, 0)
  assert.match(r.out, /exempt/)
})

test('stages without conditions (domain-model, adr-analysis) pass with a note', () => {
  const root = bolt('ddd-construction-bolt')
  assert.equal(runCheck(root, '900-fixture', 'domain-model').code, 0)
  assert.equal(runCheck(root, '900-fixture', 'adr-analysis').code, 0)
})

test('the design stage fails when failure-modes.jsonl is missing', () => {
  const root = bolt('ddd-construction-bolt')
  const r = runCheck(root, '900-fixture', 'technical-design')
  assert.equal(r.code, 1)
  assert.equal(unmet(r.out).length, 1)
  assert.match(unmet(r.out)[0], /failure-modes\.jsonl/)
})

test('the design stage reports exactly the rows that break a rule', () => {
  const root = bolt('ddd-construction-bolt', {
    'failure-modes.jsonl': [
      row('FM-1'),
      row('FM-2', { test: '' }),
      row('FM-3', { disposition: 'rejected', test: undefined }),
      row('FM-4', { source: 'attack' }),
    ],
  })
  const r = runCheck(root, '900-fixture', 'technical-design')
  assert.equal(r.code, 1)
  const lines = unmet(r.out)
  assert.equal(lines.length, 3, r.out)
  assert.match(lines.find(l => l.startsWith('- FM-2')), /test/)
  assert.match(lines.find(l => l.startsWith('- FM-3')), /reason/)
  assert.match(lines.find(l => l.startsWith('- FM-4')), /disposition/)
  assert.equal(lines.some(l => l.startsWith('- FM-1')), false)
})

test('the design stage fails when no attack row proves the adversarial check ran', () => {
  const root = bolt('ddd-construction-bolt', { 'failure-modes.jsonl': [row('FM-1')] })
  const r = runCheck(root, '900-fixture', 'technical-design')
  assert.equal(r.code, 1)
  assert.equal(unmet(r.out).length, 1)
  assert.match(unmet(r.out)[0], /attack/)
})

test('a well-formed design-stage file passes for both bolt types', () => {
  for (const [type, stage] of [['ddd-construction-bolt', 'technical-design'], ['simple-construction-bolt', 'plan']]) {
    const root = bolt(type, { 'failure-modes.jsonl': [row('FM-1'), row('FM-2', { source: 'attack', disposition: 'accepted' }), row('FM-3', { source: 'attack', disposition: 'rejected', test: undefined, reason: 'not reachable: the queue is bounded upstream' })] })
    const r = runCheck(root, '900-fixture', stage)
    assert.equal(r.code, 0, r.out)
    assert.match(r.out, /^ok:/m)
  }
})

test('a "none" row with a reason waives the test rules but still needs the attack row', () => {
  const waived = bolt('ddd-construction-bolt', { 'failure-modes.jsonl': [{ none: true, reason: 'docs-only bolt: no code path changes' }, attackNone] })
  assert.equal(runCheck(waived, '900-fixture', 'technical-design').code, 0)
  assert.equal(runCheck(waived, '900-fixture', 'implement').code, 0)
  const unattacked = bolt('ddd-construction-bolt', { 'failure-modes.jsonl': [{ none: true, reason: 'docs-only' }] })
  assert.equal(runCheck(unattacked, '900-fixture', 'technical-design').code, 1)
  const noReason = bolt('ddd-construction-bolt', { 'failure-modes.jsonl': [{ none: true }, attackNone] })
  const r = runCheck(noReason, '900-fixture', 'technical-design')
  assert.equal(r.code, 1)
  assert.match(unmet(r.out)[0], /reason/)
})

test('a malformed line is reported with its line number', () => {
  const root = bolt('ddd-construction-bolt', { 'failure-modes.jsonl': `${JSON.stringify(row('FM-1'))}\n{not json\n${JSON.stringify(attackNone)}\n` })
  const r = runCheck(root, '900-fixture', 'technical-design')
  assert.equal(r.code, 1)
  assert.match(r.out, /line 2/)
})

test('the implement stage passes with red → green → mutated red → green stamps', () => {
  const root = bolt('ddd-construction-bolt', { 'failure-modes.jsonl': [row('FM-1'), attackNone], 'test-stamps.jsonl': fullStamps })
  const r = runCheck(root, '900-fixture', 'implement')
  assert.equal(r.code, 0, r.out)
})

test('a class-level stamp covers every row whose test name contains the filter', () => {
  const root = bolt('ddd-construction-bolt', { 'failure-modes.jsonl': [row('FM-1'), row('FM-2'), attackNone], 'test-stamps.jsonl': fullStamps })
  assert.equal(runCheck(root, '900-fixture', 'implement').code, 0)
})

test('a stamp whose filter is too short to name a test never matches a row', () => {
  const root = bolt('ddd-construction-bolt', { 'failure-modes.jsonl': [row('FM-1'), attackNone], 'test-stamps.jsonl': fullStamps.map(s => ({ ...s, filter: 'e' })) })
  const r = runCheck(root, '900-fixture', 'implement')
  assert.equal(r.code, 1, r.out)
  assert.match(unmet(r.out)[0], /no stamp/)
})

test('UI rows match stamps by their include field', () => {
  const uiRow = { ...row('FM-1'), ui: true, test: 'refund-dialog' }
  const uiStamps = fullStamps.map(s => ({ ...s, filter: undefined, include: 'refund-dialog' }))
  const root = bolt('simple-construction-bolt', { 'failure-modes.jsonl': [uiRow, attackNone], 'test-stamps.jsonl': uiStamps })
  assert.equal(runCheck(root, '900-fixture', 'implement').code, 0)
})

test('the implement stage names each missing proof, one line per row and condition', () => {
  const cases = [
    ['no stamps file', undefined, /test-stamps\.jsonl/],
    ['red after the first green', [stamp('2026-09-04T10:10:00Z', 'green', 0), stamp('2026-09-04T10:00:00Z', 'red', 1), fullStamps[2], fullStamps[3]].map((s, i) => ({ ...s, t: `2026-09-04T1${i}:00:00Z` })), /red.*before.*green/],
    ['red that exited 0', [stamp('2026-09-04T10:00:00Z', 'red', 0), fullStamps[1], fullStamps[2], fullStamps[3]], /red.*before.*green/],
    ['no green at all', [fullStamps[0]], /green/],
    ['no revert-and-rerun', [fullStamps[0], fullStamps[1]], /revert-and-rerun/],
    ['revert-and-rerun that stayed green', [fullStamps[0], fullStamps[1], { ...fullStamps[2], exit: 0, failed: 0 }, fullStamps[3]], /revert-and-rerun.*red|red.*revert-and-rerun/],
    ['revert-and-rerun without a mutate record', [fullStamps[0], fullStamps[1], { ...fullStamps[2], mutate: undefined }, fullStamps[3]], /mutate/],
    ['mutation of a test file', [fullStamps[0], fullStamps[1], { ...fullStamps[2], mutate: { ...MUT, file: 'src/PhotoPrint.Tests/Unit/RefundTests.cs' } }, fullStamps[3]], /test file/],
    ['no green after the mutation', fullStamps.slice(0, 3), /green.*after/],
    ['stamps for another test only', fullStamps.map(s => ({ ...s, filter: 'Cart.Totals' })), /no stamp/],
  ]
  for (const [name, stamps, pattern] of cases) {
    const files = { 'failure-modes.jsonl': [row('FM-1'), attackNone] }
    if (stamps) files['test-stamps.jsonl'] = stamps
    const r = runCheck(bolt('ddd-construction-bolt', files), '900-fixture', 'implement')
    assert.equal(r.code, 1, `${name}: exit ${r.code}\n${r.out}`)
    const lines = unmet(r.out)
    assert.equal(lines.length, 1, `${name}: ${r.out}`)
    assert.match(lines[0], pattern, name)
    if (stamps) assert.match(lines[0], /^- FM-1/, name)
  }
})

test('the test stage requires both gates recorded in the test artifact', () => {
  const files = { 'failure-modes.jsonl': [row('FM-1'), attackNone], 'test-stamps.jsonl': fullStamps }
  const both = bolt('ddd-construction-bolt', { ...files, 'ddd-03-test-report.md': '# Report\n\n## Adversarial design check\n- attacked\n\n## Fresh-eyes micro-review\n- reviewed\n' })
  assert.equal(runCheck(both, '900-fixture', 'test').code, 0)
  const one = bolt('ddd-construction-bolt', { ...files, 'ddd-03-test-report.md': '# Report\n\n### Adversarial design check\n- attacked\n' })
  const r = runCheck(one, '900-fixture', 'test')
  assert.equal(r.code, 1)
  assert.equal(unmet(r.out).length, 1)
  assert.match(unmet(r.out)[0], /fresh-eyes/i)
  const none = bolt('simple-construction-bolt', files)
  const r2 = runCheck(none, '900-fixture', 'test')
  assert.equal(r2.code, 1)
  assert.match(r2.out, /test-walkthrough\.md/)
})

test('the test stage also re-checks the implement conditions', () => {
  const root = bolt('ddd-construction-bolt', { 'failure-modes.jsonl': [row('FM-1'), attackNone], 'test-stamps.jsonl': fullStamps.slice(0, 2), 'ddd-03-test-report.md': '## Adversarial design check\n\n## Fresh-eyes micro-review\n' })
  const r = runCheck(root, '900-fixture', 'test')
  assert.equal(r.code, 1)
  assert.match(r.out, /revert-and-rerun/)
})
