// Materializes a review target from a compact spec into a throwaway root, for the tests whose
// fixture is a *routing state* and nothing else: the router and the autonomy policy read a
// ledger's open rows, the metrics lines, the resolution frontmatter and the review-file
// inventory, so a state that used to need four hand-written markdown files under
// fixtures/repo/reviews is six lines of spec beside the assertion that cares about it.
//
// Two things follow from that, and both are the point:
//   - the state a check routes on is readable where the check is, not three files away;
//   - fixtures/repo stays the *deliberately-broken* root — a state parked there for the router
//     also lands in front of the whole-root records auditor, where its every unfilled schema
//     field reads as an error nobody planted.
// A target any other suite reads (the doc gate, the auditor's own fixtures, the renderer) stays
// on disk: those suites check the files themselves, which is the opposite requirement.
//
// The spec states only what a reader routes on; everything else the schema requires is filled in
// here, so a line stays valid without the spec restating it.
//   target       the folder name (also the `target` field of every metrics line)
//   reviews      how many review-v<n>.md exist (their content is never read — only the count)
//   blockers     { <review version>: ['PPW-<n>', …] } — the policy's delta-worthiness read
//   metricsLines pass lines as literal objects, fix-round lines via fixRound() below
//   ledgerRows   [id, severity emoji, status] per row — what "open" means to the loop
//   resolutions  [{ v, status, fixedCommit?, closed?, fixed?: [id…], answers? }]
//
// Every root is removed when the test process exits, so a test never cleans up after itself.
import { mkdirSync, mkdtempSync, rmSync, writeFileSync } from 'node:fs'
import { tmpdir } from 'node:os'
import { join } from 'node:path'

let scratch = null
let built = 0
process.on('exit', () => { if (scratch) rmSync(scratch, { recursive: true, force: true }) })

const zeroTally = { fixed: 0, wont_fix: 0, deferred: 0, disputed: 0, false_positive: 0, open: 0 }

// A fix-round line: the spec states the round, its date, how many findings it fixed and whether
// it ran tests (a round is "substantive" to the convergence rule only if it did both), plus the
// notes cell, which is where a design pass records that it ran.
export const fixRound = ({ round, date, fixed, invocations = null, notes = '' }) => ({
  round, type: 'fix-round', date, base_commit: null, fixed_commit: null,
  findings: { ...zeroTally, fixed },
  tests: invocations === null ? null : { invocations, red_runs: 1, green_runs: 1, final: { passed: 9, failed: 0 } },
  approach_checks: { pre_cleared_consumed: 0, run: 0, tokens: null },
  micro_reviews: { count: 0, follow_up_fixes: 0 }, cost: { agents: 0, tokens: null },
  runtime: { started: `${date}T09:00:00Z`, ended: `${date}T10:00:00Z`, active_s: 3600, blocked_s: 0, idle_s: 0, blocked: [] },
  notes,
})

// A key whose value is empty is left out rather than written blank: `value()` lets the gap after
// a colon cross a newline, so an empty key silently reads the next key's value.
const frontmatter = pairs =>
  `---\n${pairs.filter(([, v]) => v !== '').map(([k, v]) => `${k}: ${v}`).join('\n')}\n---\n`

export function buildTarget({ target, reviews = 1, blockers = {}, metricsLines = [], ledgerRows, resolutions = [] }) {
  scratch ??= mkdtempSync(join(tmpdir(), 'review-built-fixture-'))
  const root = join(scratch, `${++built}-${target}`)
  const dir = join(root, 'reviews', target)
  mkdirSync(dir, { recursive: true })

  for (let v = 1; v <= reviews; v++) {
    writeFileSync(join(dir, `review-v${v}.md`), `${frontmatter([
      ['type', 'review'], ['target', target], ['version', v],
      ['blockers', `[${(blockers[v] ?? []).join(', ')}]`],
    ])}\n# Review v${v} — ${target}\n`)
  }

  if (metricsLines.length) {
    writeFileSync(join(dir, 'metrics.jsonl'),
      metricsLines.map(l => `${JSON.stringify({ target, ...l })}\n`).join(''))
  }

  if (ledgerRows) {
    writeFileSync(join(dir, 'ledger.md'), `${frontmatter([['type', 'review-ledger'], ['target', target]])}
# Ledger — ${target}

## Findings

| ID | Sev | First seen | Title | File | Status | Affirmed |
|---|---|---|---|---|---|---|
${ledgerRows.map(([id, sev, status]) => `| ${id} | ${sev} | v1 | a fixture row | \`Fixture.cs\` | ${status} | — |\n`).join('')}`)
  }

  for (const { v, status, fixedCommit = '', closed = '', fixed = [], answers = `review-v${v}.md` } of resolutions) {
    writeFileSync(join(dir, `resolution-v${v}.md`), `${frontmatter([
      ['type', 'resolution'], ['target', target], ['version', v], ['answers', answers],
      ['status', status], ['fixed_commit', fixedCommit], ['closed', closed],
    ])}
# Resolution v${v} — ${target}

## Findings

| ID | Status | Commit | Note |
|---|---|---|---|
${fixed.map(id => `| ${id} | fixed | \`${fixedCommit}\` | fixed |\n`).join('')}`)
  }

  return root
}
