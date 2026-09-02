// Tests for cli/docs-sync.mjs: the drift alarm. Every generated block must equal what the code
// renders today, each marked block must line up with the data behind it, the link scan must fail on
// a link nobody has tolerated, and a hand edit inside a generated block must be caught.
//
// The mutation test runs against a throwaway copy of cli/docs-blocks.mjs's marked-file set under
// --root: the live tree is never written by a test, because the pre-commit hook runs this suite.
//
// Usage: node reviews/lib/tests/run-tests.mjs --only docs-sync
import { check, run } from '../lib.mjs'
import { readFileSync, writeFileSync, mkdirSync, mkdtempSync, rmSync } from 'node:fs'
import { join, dirname } from 'node:path'
import { tmpdir } from 'node:os'
import { REVIEWS, REPO, AREAS, CAP_ROWS, CORE_LENSES, ADDED_LENSES, FIXER_EVENTS, HANDBACK_EVENT_DOCS, MANIFEST_LENSES } from '../../records/schema.mjs'
import { V2_FIELDS, V3_FIX_FIELDS } from '../../records/validate.mjs'
import { ROWS } from '../../drive/rows.mjs'
import { GATE_DOCS } from '../../drive/gates.mjs'
import { MARKED_FILES } from '../../cli/docs-blocks.mjs'
// Every link under reviews/ resolves, so this list is empty and the assertions below hold it that
// way in both directions: any new broken link fails, and an entry added here that is no longer
// broken fails too, so a tolerated link cannot outlive its reason.
const TOLERATED_LINKS = []

const SKILLS = join(REPO, '.claude', 'skills')
const body = (file, name) => {
  const lines = readFileSync(file, 'utf8').split(/\r?\n/)
  const open = lines.indexOf(`<!-- generated:${name} -->`)
  const close = lines.indexOf(`<!-- /generated:${name} -->`)
  return open === -1 || close === -1 ? null : lines.slice(open + 1, close)
}
const rowsOf = lines => lines.filter(l => l.startsWith('| ') && !l.startsWith('|---'))
const lines = out => out.split('\n').map(l => l.replace(/\\/g, '/'))
const brokenIn = out => lines(out).filter(l => l.startsWith('BROKEN ')).map(l => l.slice('BROKEN '.length))
const staleIn = out => lines(out).filter(l => l.startsWith('STALE'))
const problemsIn = out => lines(out).filter(l => l.startsWith('PROBLEM'))

// ---------- the drift half: what the hook runs ----------
{
  const r = run('cli/docs-sync.mjs', ['--check', '--no-links'])
  check('no generated block has drifted from the code', staleIn(r.out).length === 0, r.out.trim())
  check('docs-sync reports no structural problem', problemsIn(r.out).length === 0, problemsIn(r.out).join('\n'))
  check('the drift-only check exits 0 and states the block count',
    r.code === 0 && r.out.includes(`generated blocks in step`), `exit ${r.code}: ${r.out.trim()}`)
  check('--no-links runs no link scan', !r.out.includes('broken link(s)') && !r.out.includes('links resolve'), r.out.trim())
}

// ---------- the link half: exactly the tolerated set, nothing more ----------
{
  const r = run('cli/docs-sync.mjs', ['--check'])
  const broken = brokenIn(r.out)
  const unexpected = broken.filter(l => !TOLERATED_LINKS.includes(l))
  const gone = TOLERATED_LINKS.filter(l => !broken.includes(l))
  check('no broken link beyond the tolerated list', unexpected.length === 0,
    `new broken link(s):\n      ${unexpected.join('\n      ')}`)
  check('every tolerated broken link is still the one the list names', gone.length === 0,
    `fixed (drop from TOLERATED_LINKS):\n      ${gone.join('\n      ')}`)
  check('no link is tolerated broken any more', TOLERATED_LINKS.length === 0 && broken.length === 0,
    `${TOLERATED_LINKS.length} tolerated, ${broken.length} broken`)
  check('the link scan states that every link resolves', r.out.includes('all reviews/ links resolve'), r.out.trim())
  check('the full --check exits 0 — drift and links both clean', r.code === 0, `exit ${r.code}: ${r.out.trim()}`)
  check('the block verdict prints alongside the link verdict',
    r.out.includes('generated blocks in step'), r.out.trim())
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
  const marked = body(file, name)
  check(`${name} is marked up in ${file.slice(REPO.length + 1)}`, marked !== null)
  if (!marked) continue
  check(`${name} renders one row per data entry`, rowsOf(marked).length === expected + 1,
    `${rowsOf(marked).length - 1} rows vs ${expected} entries`)
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

// ---------- the alarm fires on a hand edit, on a throwaway copy of the tree ----------
{
  const root = mkdtempSync(join(tmpdir(), 'docs-sync-'))
  try {
    for (const rel of MARKED_FILES) {
      const dest = join(root, rel)
      mkdirSync(dirname(dest), { recursive: true })
      writeFileSync(dest, readFileSync(join(REPO, rel), 'utf8'))
    }
    const clean = run('cli/docs-sync.mjs', ['--root', root, '--check', '--no-links'])
    check('--root reads the copied tree and finds every block in step',
      clean.code === 0 && staleIn(clean.out).length === 0 && problemsIn(clean.out).length === 0, `exit ${clean.code}: ${clean.out.trim()}`)

    const edited = join(root, 'reviews/rules/doc-contracts.md')
    writeFileSync(edited, readFileSync(edited, 'utf8').replace('| `jobs` |', '| `jobsss` |'))
    const r = run('cli/docs-sync.mjs', ['--root', root, '--check', '--no-links'])
    check('a hand edit inside a generated block is reported as stale',
      staleIn(r.out).some(l => l.includes('generated:areas')), r.out.trim())
    check('the stale report names the changed cell on both sides',
      r.out.includes('  - | `jobsss` |') && r.out.includes('  + | `jobs` |'), r.out.trim())
    check('a stale block alone fails the drift-only check', r.code === 1, `exit ${r.code}`)

    const written = run('cli/docs-sync.mjs', ['--root', root, '--write'])
    check('--write restores the block and exits 0', written.code === 0 && written.out.includes('blocks rendered'),
      `exit ${written.code}: ${written.out.trim()}`)
    const after = run('cli/docs-sync.mjs', ['--root', root, '--check', '--no-links'])
    check('the copied tree is in step again after --write', after.code === 0 && staleIn(after.out).length === 0, after.out.trim())
    check('the hand edit never reached the live file',
      !readFileSync(join(REVIEWS, 'rules', 'doc-contracts.md'), 'utf8').includes('jobsss'))

    // The other direction of the empty tolerated list: a real broken link is still caught, while a
    // link quoted inside a code span or fence is a template and is not scanned.
    // The copy carries only the marked files, so its own cross-references are broken; the fixture
    // is judged against that baseline.
    const before = brokenIn(run('cli/docs-sync.mjs', ['--root', root, '--check']).out)
    const note = join(root, 'reviews', 'notes', 'link-fixture.md')
    mkdirSync(dirname(note), { recursive: true })
    writeFileSync(note, ['[gone](./nowhere.md)', '`[template](../<target>/resolution-v<round>.md)`',
      '```', '[fenced](./also-nowhere.md)', '```', ''].join('\n'))
    const links = run('cli/docs-sync.mjs', ['--root', root, '--check'])
    const added = brokenIn(links.out).filter(l => !before.includes(l))
    check('a broken link the fixture adds is reported',
      added.includes('reviews/notes/link-fixture.md: ./nowhere.md'), links.out.trim())
    check('a link inside a code span or a fence is not scanned', added.length === 1, added.join('\n'))
    check('the link half alone fails --check, with no block stale',
      links.code === 1 && staleIn(links.out).length === 0, `exit ${links.code}: ${links.out.trim()}`)
    check('the link scan counts what it printed',
      links.out.includes(`${brokenIn(links.out).length} broken link(s)`), links.out.trim())
  } finally {
    rmSync(root, { recursive: true, force: true })
  }
}
