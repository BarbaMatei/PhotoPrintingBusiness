// Tests for cli/docs-sync.mjs: the drift alarm. Every generated block must equal what the code
// renders today, each marked block must line up with the data behind it, and a hand edit inside a
// generated block must be caught.
//
// Usage: node reviews/lib/tests/run-tests.mjs --only docs-sync
import { check, run } from './lib.mjs'
import { readFileSync, writeFileSync } from 'node:fs'
import { join } from 'node:path'
import { REVIEWS, REPO, AREAS, CAP_ROWS, CORE_LENSES, ADDED_LENSES, FIXER_EVENTS, HANDBACK_EVENT_DOCS, MANIFEST_LENSES } from '../records/schema.mjs'
import { V2_FIELDS, V3_FIX_FIELDS } from '../records/validate.mjs'
import { ROWS } from '../drive/rows.mjs'
import { GATE_DOCS } from '../drive/gates.mjs'

const SKILLS = join(REPO, '.claude', 'skills')
const body = (file, name) => {
  const lines = readFileSync(file, 'utf8').split(/\r?\n/)
  const open = lines.indexOf(`<!-- generated:${name} -->`)
  const close = lines.indexOf(`<!-- /generated:${name} -->`)
  return open === -1 || close === -1 ? null : lines.slice(open + 1, close)
}
const rowsOf = lines => lines.filter(l => l.startsWith('| ') && !l.startsWith('|---'))

// ---------- the check itself ----------
{
  const r = run('cli/docs-sync.mjs', ['--check'])
  const stale = r.out.split('\n').filter(l => l.startsWith('STALE'))
  const problems = r.out.split('\n').filter(l => l.startsWith('PROBLEM'))
  check('no generated block has drifted from the code', stale.length === 0, r.out.trim())
  check('docs-sync reports no structural problem', problems.length === 0, problems.join('\n'))
  // The link check the old fix-links.mjs owned now runs inside --check.
  check('docs-sync runs the reviews/ link check', /broken link\(s\)|all reviews\/ links resolve/.test(r.out), r.out.trim())
}

// ---------- each block lines up with the data behind it ----------
for (const [file, name, expected] of [
  [join(REVIEWS, 'README.md'), 'router-rows', ROWS.length],
  [join(REVIEWS, 'rules', 'doc-contracts.md'), 'size-caps', CAP_ROWS.length],
  [join(REVIEWS, 'rules', 'doc-contracts.md'), 'areas', AREAS.length],
  [join(REVIEWS, 'rules', 'metrics-schema.md'), 'metrics-v2-fields', V2_FIELDS.length],
  [join(REVIEWS, 'rules', 'metrics-schema.md'), 'metrics-v3-fix-fields', V3_FIX_FIELDS.length],
  [join(REVIEWS, 'runbooks', 'runbook-discovery.md'), 'core-lenses', CORE_LENSES.length],
  [join(REVIEWS, 'runbooks', 'runbook-discovery.md'), 'added-lenses', ADDED_LENSES.length],
  [join(SKILLS, 'fix-review', 'SKILL.md'), 'fixer-events', FIXER_EVENTS.length],
  [join(SKILLS, 'loop-driver', 'SKILL.md'), 'gate-kinds', GATE_DOCS.length],
]) {
  const lines = body(file, name)
  check(`${name} is marked up in ${file.slice(REPO.length + 1)}`, lines !== null)
  if (!lines) continue
  check(`${name} renders one row per data entry`, rowsOf(lines).length === expected + 1,
    `${rowsOf(lines).length - 1} rows vs ${expected} entries`)
}
{
  const routerRows = rowsOf(body(join(REVIEWS, 'README.md'), 'router-rows')).slice(1)
  check('the README router table states the rows in the router\'s own order',
    routerRows[0].startsWith(`| ${ROWS[0].state} |`) && routerRows.at(-1).startsWith(`| ${ROWS.at(-1).state} |`),
    `${routerRows[0]}\n${routerRows.at(-1)}`)
  const handback = body(join(REVIEWS, 'rules', 'doc-contracts.md'), 'handback-events')
  check('the hand-back event list carries one bullet per documented event',
    handback.filter(l => l.startsWith('- ')).length === HANDBACK_EVENT_DOCS.length, handback.join('\n'))
  check('the hand-back bullets carry the stamper\'s required fields, not a hand-typed copy',
    handback.some(l => l.includes('(`round`, `cluster`, `ids`)')), handback.join('\n'))
  const vocab = body(join(REVIEWS, 'README.md'), 'policy-vocabulary')
  check('the README states the policy\'s whole answer vocabulary',
    vocab.join(' ').includes('`fix round`') && vocab.join(' ').includes('`close the loop`'), vocab.join('\n'))
}

// ---------- every manifest lens has a runbook row ----------
{
  const keyed = [...CORE_LENSES, ...ADDED_LENSES].filter(l => l.key).map(l => l.key)
  check('every manifest lens is launched by a runbook row', MANIFEST_LENSES.every(k => keyed.includes(k)),
    MANIFEST_LENSES.filter(k => !keyed.includes(k)).join(', '))
  check('no runbook row launches a lens outside the manifest', keyed.every(k => MANIFEST_LENSES.includes(k)),
    keyed.filter(k => !MANIFEST_LENSES.includes(k)).join(', '))
  check('the prose-only lens row is marked as such, not given an invented key',
    ADDED_LENSES.filter(l => l.key === null).length === 1)
}

// ---------- the alarm fires on a hand edit inside a block ----------
{
  const file = join(REVIEWS, 'rules', 'doc-contracts.md')
  const original = readFileSync(file, 'utf8')
  try {
    writeFileSync(file, original.replace('| `jobs` |', '| `jobsss` |'))
    const r = run('cli/docs-sync.mjs', ['--check'])
    check('a hand edit inside a generated block fails the check', r.code === 1, `exit ${r.code}`)
    check('the check names the stale block and the changed cell',
      r.out.includes('generated:areas') && r.out.includes('jobsss'), r.out.trim())
  } finally {
    writeFileSync(file, original)
  }
  const back = run('cli/docs-sync.mjs', ['--check'])
  check('the file is restored and the check is clean again',
    !back.out.split('\n').some(l => l.startsWith('STALE')), back.out.trim())
}
