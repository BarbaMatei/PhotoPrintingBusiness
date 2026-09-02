// Materializes one review target from a compact spec into a throwaway root, for the tests whose
// fixture is a routing state and nothing else. A target any other suite reads as files (doc gate,
// records auditor, renderer) stays on disk under fixtures/repo — see that root's README.
// The spec states only what a reader routes on; the rest of the schema is filled in here.
//   target       folder name, and the `target` field of every metrics line
//   reviews      how many review-v<n>.md exist (only the count is ever read)
//   blockers     { <review version>: ['PPW-<n>', …] } — the policy's delta-worthiness read
//   metricsLines pass lines as literal objects, fix-round lines via fixRound()
//   ledgerRows   [id, severity emoji, status] per row
//   resolutions  [{ v, status, fixedCommit?, closed?, fixed?: [id…], answers? }]
//
// Usage: const root = buildTarget({ … }) — pass it as `--root`; every root is removed on exit.
import { mkdirSync, mkdtempSync, rmSync, writeFileSync } from 'node:fs'
import { tmpdir } from 'node:os'
import { join } from 'node:path'

let scratch = null
let built = 0
process.on('exit', () => { if (scratch) rmSync(scratch, { recursive: true, force: true }) })

const zeroTally = { fixed: 0, wont_fix: 0, deferred: 0, disputed: 0, false_positive: 0, open: 0 }

// A round counts as substantive to the convergence rule only if it both fixed and ran tests.
export const fixRound = ({ round, date, fixed, invocations = null, notes = '' }) => ({
  round, type: 'fix-round', date, base_commit: null, fixed_commit: null,
  findings: { ...zeroTally, fixed },
  tests: invocations === null ? null : { invocations, red_runs: 1, green_runs: 1, final: { passed: 9, failed: 0 } },
  approach_checks: { pre_cleared_consumed: 0, run: 0, tokens: null },
  micro_reviews: { count: 0, follow_up_fixes: 0 }, cost: { agents: 0, tokens: null },
  runtime: { started: `${date}T09:00:00Z`, ended: `${date}T10:00:00Z`, active_s: 3600, blocked_s: 0, idle_s: 0, blocked: [] },
  notes,
})

// An empty key is left out, not written blank: `value()` would read the next key's value.
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
