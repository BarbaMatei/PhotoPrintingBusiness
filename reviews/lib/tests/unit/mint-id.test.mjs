// Tests for mint-id.mjs: counter mint/increment, ledger scaffolding, resolution scaffolding.
// Every case runs against a throwaway mkdtemp tree — the real reviews/state/id-counter is
// read-only for this suite. Scaffold cases seed the tree's reviews/templates/ from the real
// templates (the script reads them at runtime), so a template edit is exercised here too.
//
// Usage: node reviews/lib/tests/run-tests.mjs --only mint-id
import { check, run } from '../lib.mjs'
import { mkdtempSync, mkdirSync, writeFileSync, readFileSync, existsSync, rmSync } from 'node:fs'
import { join } from 'node:path'
import { tmpdir } from 'node:os'
import { TEMPLATES } from '../../records/schema.mjs'

function seedTemplates(T) {
  const dst = join(T, 'reviews', 'templates')
  mkdirSync(dst, { recursive: true })
  for (const f of ['ledger.md', 'resolution.md']) writeFileSync(join(dst, f), readFileSync(join(TEMPLATES, f), 'utf8'))
}
const headingsOf = text => text.replace(/\r\n/g, '\n').split('\n').filter(l => /^#{1,2} /.test(l))
const rowLine = (id, sev, pass, title, file) => `| ${id} | ${sev} | ${pass} | ${title} | \`${file}\` | open | |`
const blockLines = (id, title, pass) => [
  '', `### ${id} — ${title}`, '',
  '- **What:** <fill in>', '- **Evidence:** <fill in>', '- **Suggested fix:** <fill in>',
  '- **History:** <append-only, one line per event>', `  - ${pass}: found by <fill in>`,
]
const today = () => new Date().toISOString().slice(0, 10)

// ---------- mint: increments the counter, refuses garbage, --dry-run writes nothing, CRLF-safe ----------
{
  const T = mkdtempSync(join(tmpdir(), 'mint-id-mint-'))
  mkdirSync(join(T, 'reviews', 'state'), { recursive: true })
  const counterPath = join(T, 'reviews', 'state', 'id-counter')
  writeFileSync(counterPath, '5\n')

  let r = run('review/mint-id.mjs', ['--root', T, 'mint', '--count', '3'])
  check('mint exits 0', r.code === 0, `exit ${r.code}: ${r.out.trim()}`)
  check('mint prints the PPW-<a>..PPW-<b> range', r.out.trim() === 'PPW-5..PPW-7', r.out.trim())
  check('mint writes the incremented counter back', readFileSync(counterPath, 'utf8') === '8\n', readFileSync(counterPath, 'utf8'))

  r = run('review/mint-id.mjs', ['--root', T, 'mint', '--count', '1', '--dry-run'])
  check('mint --dry-run exits 0', r.code === 0, `exit ${r.code}: ${r.out.trim()}`)
  check('mint --dry-run prints the next single id as a range', r.out.trim() === 'PPW-8..PPW-8', r.out.trim())
  check('mint --dry-run leaves the counter untouched', readFileSync(counterPath, 'utf8') === '8\n', readFileSync(counterPath, 'utf8'))

  writeFileSync(counterPath, 'abc\n')
  r = run('review/mint-id.mjs', ['--root', T, 'mint', '--count', '1'])
  check('mint refuses a non-numeric counter', r.code === 2 && r.out.includes('ERROR'), r.out.trim())
  check('mint leaves the garbage counter untouched', readFileSync(counterPath, 'utf8') === 'abc\n', readFileSync(counterPath, 'utf8'))

  r = run('review/mint-id.mjs', ['--root', T, 'mint'])
  check('mint refuses a missing --count', r.code === 2 && r.out.includes('ERROR'), r.out.trim())

  writeFileSync(counterPath, '12\r\n')
  r = run('review/mint-id.mjs', ['--root', T, 'mint', '--count', '2'])
  check('mint parses a CRLF-terminated counter correctly', r.out.trim() === 'PPW-12..PPW-13', r.out.trim())
  check('mint writes a clean counter back after a CRLF-terminated input', readFileSync(counterPath, 'utf8') === '14\n', JSON.stringify(readFileSync(counterPath, 'utf8')))

  rmSync(T, { recursive: true, force: true })
}

// ---------- scaffold-ledger: creates from the real template, appends, refuses a duplicate id ----------
{
  const T = mkdtempSync(join(tmpdir(), 'mint-id-ledger-'))
  seedTemplates(T)
  const target = '960-fixture-target'
  const ledgerPath = join(T, 'reviews', target, 'ledger.md')

  let r = run('review/mint-id.mjs', ['--root', T, 'scaffold-ledger', target,
    '--id', 'PPW-9601', '--sev', '🔴', '--title', 'Fixture defect one', '--file', 'Fixture.cs:10', '--pass', 'v1'])
  check('scaffold-ledger exits 0 creating a fresh ledger', r.code === 0, `exit ${r.code}: ${r.out.trim()}`)
  check('scaffold-ledger creates ledger.md', existsSync(ledgerPath), ledgerPath)

  const afterFirst = readFileSync(ledgerPath, 'utf8')
  check('ledger frontmatter names the target', afterFirst.includes(`target: ${target}`), afterFirst)
  const templateLedgerHeadings = headingsOf(readFileSync(join(TEMPLATES, 'ledger.md'), 'utf8')).map(l => l.replace('<target>', target))
  check('the scaffolded ledger carries every top-level heading of templates/ledger.md, read at test time',
    templateLedgerHeadings.every(h => afterFirst.includes(h)), JSON.stringify(templateLedgerHeadings))
  check('ledger table gets the new row', /\|\s*PPW-9601\s*\|\s*🔴\s*\|\s*v1\s*\|\s*Fixture defect one\s*\|\s*`Fixture\.cs:10`\s*\|\s*open\s*\|\s*\|/.test(afterFirst), afterFirst)
  check('ledger detail block uses <fill in> placeholders', afterFirst.includes('### PPW-9601 — Fixture defect one') &&
    afterFirst.includes('- **What:** <fill in>') && afterFirst.includes('- **Evidence:** <fill in>') && afterFirst.includes('- **Suggested fix:** <fill in>'), afterFirst)
  check('ledger history first line names the pass and <fill in> finder', afterFirst.includes('  - v1: found by <fill in>'), afterFirst)
  check('exactly one blank line separates "## Details" from the first block',
    afterFirst.includes('## Details\n\n### PPW-9601 —') && !afterFirst.includes('## Details\n\n\n### PPW-9601'), afterFirst)

  r = run('review/mint-id.mjs', ['--root', T, 'scaffold-ledger', target,
    '--id', 'PPW-9602', '--sev', '🟠', '--title', 'Fixture defect two', '--file', 'Fixture.cs:20', '--pass', 'v1'])
  check('scaffold-ledger exits 0 appending a second row', r.code === 0, `exit ${r.code}: ${r.out.trim()}`)
  const afterSecond = readFileSync(ledgerPath, 'utf8')
  check('the appended ledger keeps the first block intact', afterSecond.includes('### PPW-9601 — Fixture defect one'), afterSecond)
  check('the appended ledger carries the second row and block', afterSecond.includes('PPW-9602') && afterSecond.includes('### PPW-9602 — Fixture defect two'), afterSecond)
  check('exactly one blank line separates the first block from the second',
    afterSecond.includes('found by <fill in>\n\n### PPW-9602 —') && !afterSecond.includes('found by <fill in>\n\n\n### PPW-9602'), afterSecond)

  r = run('review/mint-id.mjs', ['--root', T, 'scaffold-ledger', target,
    '--id', 'PPW-9601', '--sev', '🟡', '--title', 'Duplicate attempt', '--file', 'Fixture.cs:30', '--pass', 'v2'])
  check('scaffold-ledger refuses a duplicate id', r.code === 2 && r.out.includes('ERROR') && r.out.includes('PPW-9601'), r.out.trim())
  check('the ledger is unchanged after the refused duplicate', readFileSync(ledgerPath, 'utf8') === afterSecond, 'ledger.md changed on a refused duplicate')

  r = run('review/mint-id.mjs', ['--root', T, 'scaffold-ledger', target,
    '--id', 'PPW-9603', '--sev', '⚪', '--title', 'Dry-run only', '--file', 'Fixture.cs:40', '--pass', 'v1', '--dry-run'])
  check('scaffold-ledger --dry-run exits 0', r.code === 0, `exit ${r.code}: ${r.out.trim()}`)
  check('scaffold-ledger --dry-run writes nothing', readFileSync(ledgerPath, 'utf8') === afterSecond, 'ledger.md changed on a dry run')

  rmSync(T, { recursive: true, force: true })
}

// ---------- scaffold-ledger: an existing CRLF ledger stays CRLF and byte-identical elsewhere ----------
{
  const T = mkdtempSync(join(tmpdir(), 'mint-id-crlf-'))
  seedTemplates(T)
  const target = '962-crlf-target'
  const dir = join(T, 'reviews', target)
  mkdirSync(dir, { recursive: true })
  const ledgerPath = join(dir, 'ledger.md')

  const existingRow = rowLine('PPW-9620', '🟠', 'v1', 'Pre-existing finding', 'Existing.cs:5')
  const existingBlock = blockLines('PPW-9620', 'Pre-existing finding', 'v1')
  const originalLF = [
    '---', 'type: review-ledger', `target: ${target}`, 'updated: 2026-08-20', '---', '',
    `# Ledger — ${target}`, '',
    '## Findings', '',
    '| ID | Sev | First seen | Title | File | Status | Affirmed |',
    '|---|---|---|---|---|---|---|',
    existingRow, '',
    '## Details',
    ...existingBlock, '',
  ].join('\n')
  const original = originalLF.replace(/\n/g, '\r\n')
  writeFileSync(ledgerPath, original)

  const r = run('review/mint-id.mjs', ['--root', T, 'scaffold-ledger', target,
    '--id', 'PPW-9621', '--sev', '🔴', '--title', 'New CRLF finding', '--file', 'New.cs:9', '--pass', 'v2'])
  check('scaffold-ledger exits 0 against a CRLF-terminated existing ledger', r.code === 0, `exit ${r.code}: ${r.out.trim()}`)

  const after = readFileSync(ledgerPath, 'utf8')
  check('scaffold-ledger keeps the whole file CRLF (no bare \\n anywhere)', after.length > 0 && !/(?<!\r)\n/.test(after), JSON.stringify(after))

  const newRow = rowLine('PPW-9621', '🔴', 'v2', 'New CRLF finding', 'New.cs:9')
  const newBlockCRLF = blockLines('PPW-9621', 'New CRLF finding', 'v2').join('\n').replace(/\n/g, '\r\n')
  const expected = original
    .replace(`${existingRow}\r\n`, `${existingRow}\r\n${newRow}\r\n`)
    .replace('updated: 2026-08-20', `updated: ${today()}`)
    + newBlockCRLF + '\r\n'
  check('the CRLF ledger matches byte-for-byte against a hand-built original.replace(...) expectation',
    after === expected, `--- actual ---\n${JSON.stringify(after)}\n--- expected ---\n${JSON.stringify(expected)}`)

  rmSync(T, { recursive: true, force: true })
}

// ---------- scaffold-resolution: copies review ids, refuses overwrite, --dry-run writes nothing ----------
{
  const T = mkdtempSync(join(tmpdir(), 'mint-id-resolution-'))
  seedTemplates(T)
  const target = '961-fixture-target'
  const dir = join(T, 'reviews', target)
  mkdirSync(dir, { recursive: true })
  const review = `---
type: review
target: ${target}
version: 1
supersedes: null
commit: abc1234
branch: fixture-branch
pass-type: discovery
date: 2026-08-28
lenses: [correctness]
lenses-not-run: [tests-coverage]
verdict: request-changes
blockers: [PPW-9601, PPW-9602]
findings: { high: 2, medium: 0, low: 0, cleanup: 0 }
tests: { dotnet: "n/a", frontend: "n/a" }
---

# Review v1 — ${target}

## Findings

| ID | Sev | Title | File | Fix now? |
|---|---|---|---|---|
| PPW-9601 | 🔴 | Fixture finding one | \`Fixture.cs:10\` | yes |
| PPW-9602 | 🔴 | Fixture finding two | \`Fixture.cs:20\` | yes |

## Refuted

## Notes for the fixer

Fixture only.
`
  writeFileSync(join(dir, 'review-v1.md'), review)
  writeFileSync(join(dir, 'review-v2.md'), review.replace(/version: 1/, 'version: 2').replace(/Review v1/, 'Review v2'))

  let r = run('review/mint-id.mjs', ['--root', T, 'scaffold-resolution', target, '--version', '1'])
  check('scaffold-resolution exits 0', r.code === 0, `exit ${r.code}: ${r.out.trim()}`)
  const resolutionPath = join(dir, 'resolution-v1.md')
  check('scaffold-resolution creates resolution-v1.md', existsSync(resolutionPath), resolutionPath)
  const resolution = readFileSync(resolutionPath, 'utf8')
  check('resolution frontmatter fills target/version/answers', resolution.includes(`target: ${target}`) && resolution.includes('version: 1') && resolution.includes('answers: review-v1.md'), resolution)
  check('resolution copies one Findings row per review id', /\|\s*PPW-9601\s*\|\s*open\s*\|\s*—\s*\|\s*—\s*\|/.test(resolution) && /\|\s*PPW-9602\s*\|\s*open\s*\|\s*—\s*\|\s*—\s*\|/.test(resolution), resolution)

  const templateResolutionRaw = readFileSync(join(TEMPLATES, 'resolution.md'), 'utf8')
  const templateResolutionHeadings = headingsOf(templateResolutionRaw).map(l => l.replace('<n>', '1').replace('<target>', target))
  check('the scaffolded resolution carries every top-level heading of templates/resolution.md, read at test time',
    templateResolutionHeadings.every(h => resolution.includes(h)), JSON.stringify(templateResolutionHeadings))
  const resolutionLines = resolution.replace(/\r\n/g, '\n').split('\n')
  const protocolHeading = templateResolutionRaw.replace(/\r\n/g, '\n').split('\n').find(l => /^### Protocol — /.test(l))
  check('scaffold-resolution includes the template\'s "### Protocol — <label>" Decisions block verbatim as its own heading line',
    !!protocolHeading && resolutionLines.includes(protocolHeading), protocolHeading)
  const decisionTitleHeading = templateResolutionRaw.replace(/\r\n/g, '\n').split('\n').find(l => /^### <One-line decision title/.test(l))
  check('scaffold-resolution includes the template\'s one-line-decision-title placeholder too, as its own heading line',
    !!decisionTitleHeading && resolutionLines.includes(decisionTitleHeading), decisionTitleHeading)

  r = run('review/mint-id.mjs', ['--root', T, 'scaffold-resolution', target, '--version', '1'])
  check('scaffold-resolution refuses to overwrite an existing file', r.code === 2 && r.out.includes('ERROR'), r.out.trim())
  check('the existing resolution is unchanged after the refused overwrite', readFileSync(resolutionPath, 'utf8') === resolution, 'resolution-v1.md changed on a refused overwrite')

  r = run('review/mint-id.mjs', ['--root', T, 'scaffold-resolution', target, '--version', '2', '--dry-run'])
  check('scaffold-resolution --dry-run exits 0', r.code === 0, `exit ${r.code}: ${r.out.trim()}`)
  check('scaffold-resolution --dry-run writes nothing', !existsSync(join(dir, 'resolution-v2.md')), 'resolution-v2.md should not exist after a dry run')

  // doc-gate-clean in shape: a freshly scaffolded target's ledger + resolution carry no
  // heading or frontmatter violation, even though their still-"open" rows are legitimately
  // flagged for missing Decisions blocks until the fixer works through them.
  run('review/mint-id.mjs', ['--root', T, 'scaffold-ledger', target, '--id', 'PPW-9601', '--sev', '🔴', '--title', 'Fixture finding one', '--file', 'Fixture.cs:10', '--pass', 'v1'])
  run('review/mint-id.mjs', ['--root', T, 'scaffold-ledger', target, '--id', 'PPW-9602', '--sev', '🔴', '--title', 'Fixture finding two', '--file', 'Fixture.cs:20', '--pass', 'v1'])
  const gate = run('records/doc-gate.mjs', ['--root', T, target, '1'])
  const shapeViolation = gate.out.split('\n').some(l => /(missing heading|frontmatter missing|no frontmatter block)/i.test(l) && /(resolution-v1\.md|ledger\.md)/.test(l))
  check('a scaffolded resolution + ledger raise no doc-gate heading or frontmatter violation', !shapeViolation, gate.out)

  rmSync(T, { recursive: true, force: true })
}
