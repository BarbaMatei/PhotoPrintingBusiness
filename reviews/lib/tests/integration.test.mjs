// Cross-script seam tests: router/policy agreement on shared fixture state, render-records' generated docs against doc-gate, and verify-fixes driving wl.mjs's appendEvent.
//
// Usage: node reviews/lib/tests/run-tests.mjs --only integration
import { check, run, GOOD_ROOT, scrubbedGitEnv } from './lib.mjs'
import { mkdtempSync, mkdirSync, writeFileSync, readFileSync, existsSync, rmSync } from 'node:fs'
import { join } from 'node:path'
import { tmpdir } from 'node:os'
import { spawnSync } from 'node:child_process'
import { REVIEWS, REPO } from '../paths.mjs'

// ---------- route-next-pass: the ledger-derived rows — threshold, queue, sweep, reviewed unit ----------
// A fix round and its verification are one reviewed unit, so the ledger — not the metrics tally —
// is what says which findings are still open. Small mediums queue under QUEUE_THRESHOLD instead of
// each spawning a round; the queue must drain before certification.
const REVIEWED_UNIT = 'NEXT: verification (reviewed unit — render records once, after it)'
{
  const r = run('route-next-pass.mjs', ['--root', GOOD_ROOT, '915-queued-mediums'])
  check('router queues two open mediums instead of routing a round', r.out.includes('QUEUED: PPW-9152, PPW-9153 (2 below the threshold of 3)'), `exit ${r.code}: ${r.out.trim()}`)
  check('the queued mediums do not stop the delta-worthiness gate from printing',
    r.code === 3 && r.out.includes('GATE_KIND: delta-worthiness'), `exit ${r.code}: ${r.out.trim()}`)
  check('a verified 🔴 in the ledger does not arm the loop', !r.out.includes('NEXT: fix round'), r.out.trim())
}
{
  const r = run('route-next-pass.mjs', ['--root', GOOD_ROOT, '916-medium-batch'])
  check('router routes a batch of three open mediums to a fix round', r.code === 0 && r.out.includes('NEXT: fix round'), `exit ${r.code}: ${r.out.trim()}`)
  check('the batch reason names the count', r.out.includes('batch of 3 open mediums'), r.out.trim())
  check('an in-progress medium counts toward the batch (2 open + 1 in-progress = 3)', !r.out.includes('QUEUED:'), r.out.trim())
  check('the batch row wins over the clean verification it sits on', !r.out.includes('GATE:'), r.out.trim())
}
{
  const r = run('route-next-pass.mjs', ['--root', GOOD_ROOT, '917-sweep-before-cert'])
  check('router sweeps the queue before certification instead of gating', r.code === 0 && r.out.includes('NEXT: fix round'), `exit ${r.code}: ${r.out.trim()}`)
  check('the sweep reason states how many mediums must drain', r.out.includes('sweep before certification — 1 open medium must drain'), r.out.trim())
  check('the sweep counts only open mediums — not a deferred 🟠, not an open 🟡',
    r.out.includes('QUEUED: PPW-9172 (1 below the threshold of 3)'), r.out.trim())
  check('the certification gate does not print while the queue is unswept',
    !r.out.includes('GATE_KIND: certification-go-ahead'), r.out.trim())
}
{
  const r = run('route-next-pass.mjs', ['--root', GOOD_ROOT, '918-open-blocker'])
  check('router routes an open 🔴 in the ledger straight to a fix round', r.code === 0 && r.out.includes('NEXT: fix round'), `exit ${r.code}: ${r.out.trim()}`)
  check('the armed reason names the open blocker', r.out.includes('PPW-9181'), r.out.trim())
  check('the open blocker outranks the clean verification the metrics show',
    !r.out.includes('GATE_KIND: delta-worthiness'), r.out.trim())
}
{
  const r = run('route-next-pass.mjs', ['--root', GOOD_ROOT, '919-reopened-latest'])
  check('router routes a reopened fix to a fix round', r.code === 0 && r.out.includes('NEXT: fix round'), `exit ${r.code}: ${r.out.trim()}`)
  check('the armed reason names the reopened count', r.out.includes('2 reopened'), r.out.trim())
  check('a reopened fix outranks the medium queue — no QUEUED line prints', !r.out.includes('QUEUED:'), r.out.trim())
}
{
  const r = run('route-next-pass.mjs', ['--root', GOOD_ROOT, '941-fix-caused-medium'])
  check('router routes a fix-caused 🟠 regression to a fix round', r.code === 0 && r.out.includes('NEXT: fix round'), `exit ${r.code}: ${r.out.trim()}`)
  check('the armed reason names the regression and the fix that caused it',
    r.out.includes('fix-caused 🟠 regression') && r.out.includes('PPW-9412') && r.out.includes('PPW-9411'), r.out.trim())
  check('the regression outranks the medium queue — no QUEUED line prints', !r.out.includes('QUEUED:'), r.out.trim())
}
{
  const r = run('route-next-pass.mjs', ['--root', GOOD_ROOT, '943-regression-deferred'])
  check('a lineage entry whose ledger row is settled no longer arms the loop',
    r.code === 2 && !r.out.includes('NEXT: fix round') && r.out.includes('GATE_KIND: certification-go-ahead'), `exit ${r.code}: ${r.out.trim()}`)
  check('a deferred 🟠 is not queued either, so nothing has to be swept', !r.out.includes('QUEUED:') && !r.out.includes('sweep before'), r.out.trim())
}
{
  const r = run('route-next-pass.mjs', ['--root', GOOD_ROOT, '942-resolved-unverified'])
  check('a resolved resolution still routes to verification, open ledger rows and all',
    r.code === 0 && r.out.includes(REVIEWED_UNIT), `exit ${r.code}: ${r.out.trim()}`)
  check('the reviewed unit is not re-armed by the rows its own verification will close',
    !r.out.includes('NEXT: fix round'), r.out.trim())
}
// A round that answers a verification pass leaves the verification as the newest metrics line, so
// routing on that line re-fixes findings the round already fixed: the stand-down reads the records,
// not the line, and row 3 outranks both the ledger rows and the verification-results row.
{
  const r = run('route-next-pass.mjs', ['--root', GOOD_ROOT, '953-round-answers-verification'])
  check('a round answering a verification pass routes to its verification, not another fix round',
    r.code === 0 && r.out.includes(REVIEWED_UNIT) && !r.out.includes('NEXT: fix round'), `exit ${r.code}: ${r.out.trim()}`)
  check('the stale verification line does not arm the loop over the resolved round',
    !r.out.includes('the loop is armed') && r.out.includes('resolution-v2 resolved, not yet re-reviewed (row 3)'), r.out.trim())
}
for (const gate of ['certification-go-ahead', 'delta-worthiness']) {
  const r = run('autonomy-policy.mjs', ['--root', GOOD_ROOT, '953-round-answers-verification', 'decide', gate])
  check(`the policy neither arms nor sweeps at the ${gate} gate while the round awaits its verification`,
    !r.out.includes('the loop is armed') && !r.out.includes('sweep before certification'), r.out.trim())
}
{
  const r = run('route-next-pass.mjs', ['--root', GOOD_ROOT, '901-good-target'])
  check('the verification answer names the reviewed unit and keeps its cost line',
    r.out.includes(REVIEWED_UNIT) && r.out.includes('COST: ~60–250k agent tokens'), r.out.trim())
}

// ---------- route-next-pass: queued mediums are absent from the later rows too ----------
// The metrics tally counts a medium as "serious" for the whole life of its line, so a queued
// medium would print QUEUED and then be routed to a fix round two rows later by the same number —
// the router contradicting itself. For a ledger'd target the later rows count the ledger instead.
{
  const r = run('route-next-pass.mjs', ['--root', GOOD_ROOT, '948-verification-files-mediums'])
  check('a verification that files two mediums queues them instead of re-arming',
    r.out.includes('QUEUED: PPW-9482, PPW-9483 (2 below the threshold of 3)'), `exit ${r.code}: ${r.out.trim()}`)
  check('the queued mediums do not read as new serious findings on the verification row',
    r.code === 3 && r.out.includes('GATE_KIND: delta-worthiness') && !r.out.includes('NEXT: fix round'), `exit ${r.code}: ${r.out.trim()}`)
  check('a new medium with fix_generated null is not a fix-caused regression', !r.out.includes('fix-caused'), r.out.trim())
}
{
  const r = run('route-next-pass.mjs', ['--root', GOOD_ROOT, '949-discovery-files-mediums'])
  check('a discovery that files two mediums with nothing answering it queues them',
    r.out.includes('QUEUED: PPW-9491, PPW-9492 (2 below the threshold of 3)'), `exit ${r.code}: ${r.out.trim()}`)
  check('and then routes the sweep, not the open-serious row',
    r.code === 0 && r.out.includes('sweep before certification — 2 open mediums must drain') && !r.out.includes('open serious findings'), `exit ${r.code}: ${r.out.trim()}`)
}
{
  const r = run('route-next-pass.mjs', ['--root', GOOD_ROOT, '944-regression-persists'])
  check('a still-open fix-caused 🟠 keeps arming the loop after a newer clean verification',
    r.code === 0 && r.out.includes('fix-caused 🟠 regression') && r.out.includes('PPW-9442'), `exit ${r.code}: ${r.out.trim()}`)
  check('the regression is read across every verification line, not just the newest',
    r.out.includes('from the fix for PPW-9441'), r.out.trim())
}

// ---------- route-next-pass: the loop-close gate and the ledger rows ----------
// 🟠 still open at certification is the documented norm — they roll into the backlog at close, so
// they must not pre-empt the owner's close decision. A 🔴 that lands after the certification pass
// still has to arm the loop.
{
  const r = run('route-next-pass.mjs', ['--root', GOOD_ROOT, '945-certified-two-mediums'])
  check('two open mediums do not queue over the loop-close gate',
    r.code === 2 && r.out.includes('GATE_KIND: loop-close') && !r.out.includes('QUEUED:'), `exit ${r.code}: ${r.out.trim()}`)
}
{
  const r = run('route-next-pass.mjs', ['--root', GOOD_ROOT, '946-certified-medium-batch'])
  check('a batch of three open mediums does not pre-empt the loop-close gate either',
    r.code === 2 && r.out.includes('GATE_KIND: loop-close') && !r.out.includes('NEXT: fix round'), `exit ${r.code}: ${r.out.trim()}`)
}
{
  const r = run('route-next-pass.mjs', ['--root', GOOD_ROOT, '947-certified-open-blocker'])
  check('an open 🔴 arms the loop even at the loop-close gate',
    r.code === 0 && r.out.includes('NEXT: fix round') && r.out.includes('PPW-9471'), `exit ${r.code}: ${r.out.trim()}`)
  check('the post-certification blocker is not answered with a close gate', !r.out.includes('GATE_KIND: loop-close'), r.out.trim())
}

// ---------- render-records: index rows, ledger flips, verification mode ----------
// The renderer wrote only metrics.jsonl and printed a suggestion line — the index row and the
// ledger status flips were hand-copied, which is where rows went missing or contradicted.
{
  const T = mkdtempSync(join(tmpdir(), 'render-index-'))
  const target = '938-index-rows'
  const dir = join(T, 'reviews', target)
  const stateDir = join(T, 'reviews', 'state')
  mkdirSync(dir, { recursive: true })
  mkdirSync(stateDir, { recursive: true })
  const indexPath = join(stateDir, 'index.md')
  const ledgerPath = join(dir, 'ledger.md')
  const metricsPath = join(dir, 'metrics.jsonl')
  const wlPath = join(dir, 'worklog.jsonl')
  const read = p => existsSync(p) ? readFileSync(p, 'utf8') : ''
  const metricsLines = () => read(metricsPath).split('\n').filter(l => l.trim())
  const at = hhmm => `2026-08-21T${hhmm}:00+03:00`
  const wl = events => writeFileSync(wlPath, `${events.map(e => JSON.stringify(e)).join('\n')}\n`)

  writeFileSync(join(dir, 'review-v1.md'), '---\ntype: review\ntarget: 938-index-rows\nversion: 1\ncommit: abc1234\n---\n\n# Review v1\n')
  writeFileSync(join(dir, 'resolution-v1.md'), `---
type: resolution
target: ${target}
version: 1
answers: review-v1.md
status: resolved
fixed_commit: def5678
closed: 2026-08-21
---

# Resolution v1 — ${target}

## Findings

| ID | Status | Commit | Note |
|---|---|---|---|
| PPW-9381 | fixed | \`abc1234\` | rethrows the last error |
| PPW-9382 | deferred | — | queued behind the log-floor rewrite |
| PPW-9389 | fixed | \`abc1234\` | row the ledger never got |
`)
  const LEDGER_ROW_1 = '| PPW-9381 | 🔴 | v1 | The retry loop drops the last error | `Services/Retry.cs:12` | in-progress | `0000000` |'
  const LEDGER_ROW_2 = '| PPW-9382 | 🟠 | v1 | The sweep logs below the level floor | `Jobs/Sweep.cs:30` | open | `0000000` |'
  const HIST_1 = '  - v1: found by the correctness lens'
  const HIST_2 = '  - v1: found by the observability lens'
  writeFileSync(ledgerPath, `---
type: review-ledger
target: ${target}
updated: 2026-08-21
---

# Ledger — ${target}

## Findings

| ID | Sev | First seen | Title | File | Status | Affirmed |
|---|---|---|---|---|---|---|
${LEDGER_ROW_1}
${LEDGER_ROW_2}

### PPW-9381 — The retry loop drops the last error

- **What:** The last attempt's exception is swallowed and the caller sees success.
- **Evidence:** \`Services/Retry.cs:12-18\`.
- **Suggested fix:** Rethrow the last exception.
- **History:**
${HIST_1}

### PPW-9382 — The sweep logs below the level floor

- **What:** The sweep logs at Debug, under the configured floor, so nothing is recorded.
- **Evidence:** \`Jobs/Sweep.cs:30\`.
- **Suggested fix:** Log at Information.
- **History:**
${HIST_2}
`)
  const INDEX_SEP = '|---|---|---|---|---|---|---|'
  const OLD_PASS_ROW = '| 2026-08-20 | 938 | v1 discovery (2 lenses) | request-changes | 1/1/0/0 | Worst is PPW-9381, a retry loop that drops its last error | [review](../938-index-rows/review-v1.md) · [ledger](../938-index-rows/ledger.md) |'
  writeFileSync(indexPath, `---
type: review-index
updated: 2026-08-21
---

# Review Index

Fixture copy of the two tables the renderer inserts into: one row per target, one row
per pass.

## Targets at a glance

| Target | State |
|---|---|
| 938 index rows | Open: one discovery pass at \`abc1234\`, one fix round resolved. <br> Re-arms on a new 🔴. |

## Passes

| Date | Target | Pass | Verdict | New H/M/L/C | Outcome | Files |
${INDEX_SEP}
${OLD_PASS_ROW}
`)
  writeFileSync(join(stateDir, 'backlog.md'), `---
type: review-backlog
updated: 2026-08-21
---

# Backlog — unfixed minors from closed targets

Fixture copy: one conforming row.

| ID | Target | Sev | What | Area |
|---|---|---|---|---|
| PPW-9382 | 938-index-rows | 🟠 | The sweep logs below the level floor | \`observability\` |
`)
  wl([
    { t: at('10:00'), ev: 'round-start', round: 1 },
    { t: at('10:05'), ev: 'triage-done', round: 1, clusters: 2, pre_cleared: 1 },
    { t: at('10:10'), ev: 'check-dispatched', cluster: 'retry' },
    { t: at('10:15'), ev: 'micro-review-dispatched', cluster: 'retry' },
    { t: at('10:20'), ev: 'test-run', kind: 'final', passed: 12, failed: 0 },
    { t: at('10:25'), ev: 'round-end', round: 1 },
  ])

  const OUTCOME = 'Both named rows closed: the retry loop now rethrows its last error, and the log-floor row is queued behind the rewrite it waits on.'
  const indexBefore = read(indexPath)
  const ledgerBefore = read(ledgerPath)
  const untouched = () => !existsSync(metricsPath) && read(indexPath) === indexBefore && read(ledgerPath) === ledgerBefore

  {
    const r = run('render-records.mjs', ['--root', T, target])
    check('renderer refuses to append records with no --outcome', r.code === 1 && r.out.includes('--outcome'), `exit ${r.code}: ${r.out.trim().slice(0, 200)}`)
    check('the refused render wrote no metrics line, no index row and no ledger flip', untouched(), 'one of metrics.jsonl / index.md / ledger.md changed')
  }
  {
    const long = Array.from({ length: 51 }, (_, i) => `w${i}`).join(' ')
    const r = run('render-records.mjs', ['--root', T, target, '--outcome', long])
    check('renderer refuses an --outcome over the 50-word index cap', r.code === 1 && r.out.includes('51 words') && r.out.includes('50'), `exit ${r.code}: ${r.out.trim().slice(0, 200)}`)
    check('the over-cap refusal wrote nothing either', untouched(), 'one of metrics.jsonl / index.md / ledger.md changed')
  }
  // A "|" or a newline in the outcome would be written straight into the pipe-delimited row, and the
  // broken row only surfaces at the next doc gate — by then it needs hand repair.
  for (const [label, text] of [['a "|"', 'Both rows closed | and the queue drained'], ['a newline', 'Both rows closed\nand the queue drained']]) {
    const r = run('render-records.mjs', ['--root', T, target, '--outcome', text])
    check(`renderer refuses an --outcome carrying ${label}`, r.code === 1 && r.out.includes('one pipe-delimited line'), `exit ${r.code}: ${r.out.trim().slice(0, 200)}`)
    check(`the ${label} refusal wrote nothing`, untouched(), 'one of metrics.jsonl / index.md / ledger.md changed')
  }
  {
    const r = run('render-records.mjs', ['--root', T, target, '--outcome'])
    check('--outcome with no value prints the usage line, not a stack trace', r.code === 1 && r.out.includes('usage: render-records.mjs') && !r.out.includes('TypeError'), `exit ${r.code}: ${r.out.trim().slice(0, 200)}`)
    const flag = run('render-records.mjs', ['--root', T, target, '--outcome', '--dry-run'])
    check('an --outcome that swallowed the next flag is refused', flag.code === 1 && flag.out.includes('another flag'), `exit ${flag.code}: ${flag.out.trim().slice(0, 200)}`)
  }
  {
    const r = run('render-records.mjs', ['--root', T, target, '--outcome', OUTCOME, '--dry-run'])
    check('dry-run prints the index row it would insert', r.code === 0 && r.out.includes('| 2026-08-21 | 938 | v1 fix round (2 clusters, 1 approach-check, 1 micro-review) |'), `exit ${r.code}: ${r.out.split('\n').find(l => l.startsWith('| 2026')) ?? r.out.trim().slice(0, 200)}`)
    check('dry-run prints the ledger flips it would make', r.out.includes('PPW-9381 → fixed at `def5678`') && r.out.includes('PPW-9382 → deferred'), r.out.split('\n').filter(l => l.includes('→')).join(' | '))
    check('dry-run wrote nothing', untouched(), 'one of metrics.jsonl / index.md / ledger.md changed')
  }

  {
    const r = run('render-records.mjs', ['--root', T, target, '--outcome', OUTCOME])
    check('renderer appends the fix round\'s records', r.code === 0 && metricsLines().length === 1, `exit ${r.code}: ${r.out.trim().slice(0, 300)}`)
    check('the renderer warns about a findings row with no ledger row', r.out.includes('PPW-9389 has no ledger row'), r.out.split('\n').find(l => l.includes('9389')) ?? r.out.trim().slice(0, 200))
    const expectedRow = `| 2026-08-21 | 938 | v1 fix round (2 clusters, 1 approach-check, 1 micro-review) | — (resolved) | 0/0/0/0 | ${OUTCOME} | [resolution](../938-index-rows/resolution-v1.md) · [ledger](../938-index-rows/ledger.md) |`
    const expectedIndex = indexBefore.replace(`${INDEX_SEP}\n`, `${INDEX_SEP}\n${expectedRow}\n`)
    check('the index row lands as the newest Passes row and every other byte is unchanged', read(indexPath) === expectedIndex,
      read(indexPath).split('\n').filter(l => l.startsWith('| 2026')).join('\n'))
    const expectedLedger = ledgerBefore
      .replace(LEDGER_ROW_1, '| PPW-9381 | 🔴 | v1 | The retry loop drops the last error | `Services/Retry.cs:12` | fixed | `def5678` |')
      .replace(LEDGER_ROW_2, '| PPW-9382 | 🟠 | v1 | The sweep logs below the level floor | `Jobs/Sweep.cs:30` | deferred | `0000000` |')
      .replace(HIST_1, `${HIST_1}\n  - v1: fix round — fixed at \`abc1234\``)
      .replace(HIST_2, `${HIST_2}\n  - v1: fix round — deferred`)
    check('the ledger flip changes only Status, Affirmed and one appended History line per row', read(ledgerPath) === expectedLedger,
      read(ledgerPath).split('\n').filter(l => l.includes('PPW-938') || l.includes('fix round')).join('\n'))
  }

  // A Windows checkout keeps index.md and ledger.md CRLF, so the verification stage runs against
  // CRLF copies: an inserted line has to match its neighbours, not leave a lone LF behind.
  const crlf = t => t.replace(/\r?\n/g, '\r\n')
  writeFileSync(indexPath, crlf(read(indexPath)))
  writeFileSync(ledgerPath, crlf(read(ledgerPath)))
  const afterRound = { index: read(indexPath), ledger: read(ledgerPath), metrics: metricsLines().length }
  const verifyEvents = [
    { t: at('11:00'), ev: 'pass-launch', pass: 'v2', type: 'verification' },
    { t: at('11:05'), ev: 'verify-result', id: 'PPW-9381', verdict: 'held', commit: 'aaa1111' },
    { t: at('11:10'), ev: 'verify-result', id: 'PPW-9382', verdict: 'test-never-red' },
    { t: at('11:15'), ev: 'test-run', kind: 'final', passed: 20, failed: 0 },
  ]
  const V_OUTCOME = 'The retry fix held on its own revert; the log-floor row reopened because its test stays green with the defect back in place.'
  const vUntouched = () => read(indexPath) === afterRound.index && read(ledgerPath) === afterRound.ledger && metricsLines().length === afterRound.metrics

  wl([...verifyEvents])
  {
    const r = run('render-records.mjs', ['--root', T, target, '--verification', 'v2', '--outcome', V_OUTCOME])
    check('verification refuses while the pass has no pass-records-done', r.code === 1 && r.out.includes('pass-records-done') && r.out.includes('--in-progress'), `exit ${r.code}: ${r.out.trim().slice(0, 250)}`)
    check('the unfinished-pass refusal wrote nothing', vUntouched(), 'index.md, ledger.md or metrics.jsonl changed')
  }
  {
    const r = run('render-records.mjs', ['--root', T, target, '--verification', 'v2', '--outcome', V_OUTCOME, '--dry-run'])
    check('an unfinished verification still dry-runs', r.code === 0 && r.out.includes('no pass-records-done yet'), `exit ${r.code}: ${r.out.trim().slice(0, 250)}`)
    check('the verification dry-run wrote nothing', vUntouched(), 'index.md, ledger.md or metrics.jsonl changed')
  }

  wl([...verifyEvents, { t: at('11:20'), ev: 'pass-records-done', pass: 'v2' }])
  {
    const r = run('render-records.mjs', ['--root', T, target, '--verification', 'v2', '--outcome', V_OUTCOME, '--new-findings', '0,1,0,0'])
    check('verification mode appends its metrics line', r.code === 0 && metricsLines().length === afterRound.metrics + 1, `exit ${r.code}: ${r.out.trim().slice(0, 300)}`)
    const appended = metricsLines().length ? JSON.parse(metricsLines()[metricsLines().length - 1]) : {}
    check('the verification line tallies held as verified and everything else as reopened',
      appended.type === 'verification' && appended.pass === 2 && appended.verified === 1 && appended.reopened === 1,
      JSON.stringify(appended).slice(0, 300))
    check('the verification line names the reopened id, carries the fixed verdict and the span\'s runtime',
      appended.notes === 'reopened: PPW-9382 (test-never-red)' && appended.verdict === 'approve-with-followups'
      && appended.runtime?.started === at('11:00') && appended.runtime?.ended === at('11:20'),
      JSON.stringify({ notes: appended.notes, verdict: appended.verdict, runtime: appended.runtime }))
    check('the verification line carries no outcome or subtype key and takes its counts from --new-findings',
      !('outcome' in appended) && !('subtype' in appended) && appended.new_findings?.medium === 1 && appended.tests?.passed === 20,
      JSON.stringify({ new_findings: appended.new_findings, tests: appended.tests }))
    // records-auditor hard-requires `commit` on every pass line, and a verification is anchored at
    // the commit whose fixes it checks — so with no --commit it reads the newest resolution's.
    check('with no --commit the line falls back to the newest resolution\'s fixed_commit and says so',
      appended.commit === 'def5678' && r.out.includes("resolution-v1.md's fixed_commit def5678"), `commit ${appended.commit}: ${r.out.split('\n').find(l => l.includes('fixed_commit')) ?? ''}`)
    const expectedRow = `| 2026-08-21 | 938 | v2 verification (anchored) | approve-with-followups | 0/1/0/0 | ${V_OUTCOME} | [ledger](../938-index-rows/ledger.md) |`
    check('the verification index row lands newest-first with the anchored pass cell, CRLF and all', read(indexPath) === afterRound.index.replace(`${INDEX_SEP}\r\n`, `${INDEX_SEP}\r\n${expectedRow}\r\n`),
      read(indexPath).split('\n').filter(l => l.startsWith('| 2026')).join('\n'))
    const expectedLedger = afterRound.ledger
      .replace('| `Services/Retry.cs:12` | fixed | `def5678` |', '| `Services/Retry.cs:12` | verified | `aaa1111` |')
      .replace('| `Jobs/Sweep.cs:30` | deferred | `0000000` |', '| `Jobs/Sweep.cs:30` | open | `0000000` |')
      .replace('  - v1: fix round — fixed at `abc1234`', '  - v1: fix round — fixed at `abc1234`\r\n  - v2: verification — held')
      .replace('  - v1: fix round — deferred', '  - v1: fix round — deferred\r\n  - v2: verification — reopened (test-never-red)')
    check('a held row flips to verified at the event\'s commit and a reopened row back to open', read(ledgerPath) === expectedLedger,
      read(ledgerPath).split('\n').filter(l => l.includes('PPW-938') || l.includes('verification')).join('\n'))
  }
  {
    const after = { index: read(indexPath), ledger: read(ledgerPath), metrics: metricsLines().length }
    const r = run('render-records.mjs', ['--root', T, target, '--verification', 'v2', '--outcome', V_OUTCOME])
    check('a second verification render for the same pass refuses rather than duplicating the line',
      r.code === 1 && r.out.includes('correction line'), `exit ${r.code}: ${r.out.trim().slice(0, 250)}`)
    check('the duplicate refusal left all three files alone',
      read(indexPath) === after.index && read(ledgerPath) === after.ledger && metricsLines().length === after.metrics, 'a file changed')

    wl([...verifyEvents, { t: at('11:30'), ev: 'pass-launch', pass: 'v2', type: 'verification' }])
    const twice = run('render-records.mjs', ['--root', T, target, '--verification', '2', '--outcome', V_OUTCOME])
    check('a second pass-launch for an open pass aborts', twice.code === 1 && twice.out.includes(at('11:00')) && twice.out.includes(at('11:30')) && twice.out.includes('wl.mjs'), `exit ${twice.code}: ${twice.out.trim().slice(0, 250)}`)
    check('the unpairable-stamp abort left all three files alone',
      read(indexPath) === after.index && read(ledgerPath) === after.ledger && metricsLines().length === after.metrics, 'a file changed')

    const gate = run('doc-gate.mjs', ['--root', T, 'state'])
    check('both generated index rows pass the state doc gate', gate.code === 0, gate.out.trim().slice(0, 400))
  }

  {
    wl([...verifyEvents, { t: at('11:20'), ev: 'pass-records-done', pass: 'v2' },
      { t: at('12:00'), ev: 'pass-launch', pass: 'v3', type: 'verification' },
      { t: at('12:05'), ev: 'verify-result', id: 'PPW-9381', verdict: 'held', commit: 'bbb2222' },
      { t: at('12:10'), ev: 'pass-records-done', pass: 'v3' }])
    const r = run('render-records.mjs', ['--root', T, target, '--verification', 'v3', '--outcome', V_OUTCOME, '--commit', 'ccc3333', '--no-index'])
    const appended = metricsLines().length ? JSON.parse(metricsLines()[metricsLines().length - 1]) : {}
    check('an explicit --commit wins over the resolution fallback', r.code === 0 && appended.commit === 'ccc3333' && !r.out.includes('fixed_commit def5678'),
      `exit ${r.code}: commit ${appended.commit}`)
  }
  {
    const bare = '939-no-anchor'
    mkdirSync(join(T, 'reviews', bare), { recursive: true })
    writeFileSync(join(T, 'reviews', bare, 'worklog.jsonl'), [
      { t: at('13:00'), ev: 'pass-launch', pass: 'v1', type: 'verification' },
      { t: at('13:05'), ev: 'verify-result', id: 'PPW-9391', verdict: 'held' },
      { t: at('13:10'), ev: 'pass-records-done', pass: 'v1' },
    ].map(e => JSON.stringify(e)).join('\n') + '\n')
    const r = run('render-records.mjs', ['--root', T, bare, '--verification', 'v1', '--outcome', 'The one fix held on its own revert.', '--no-index'])
    const written = existsSync(join(T, 'reviews', bare, 'metrics.jsonl')) ? JSON.parse(readFileSync(join(T, 'reviews', bare, 'metrics.jsonl'), 'utf8').trim()) : {}
    check('commit is null, and said to be, when there is no --commit and no resolution to read it from',
      r.code === 0 && written.commit === null && r.out.includes('commit will be null'), `exit ${r.code}: ${JSON.stringify(written).slice(0, 200)}`)
    check('a held row with no commit on its event leaves Affirmed alone', r.out.includes('PPW-9391 → verified') && !r.out.includes('PPW-9391 → verified at'),
      r.out.split('\n').find(l => l.includes('→')) ?? r.out.trim().slice(0, 200))
  }

  rmSync(T, { recursive: true, force: true })
}

// ---------- autonomy-policy: the queue drains before the policy can launch a certification ----------
// The router only meets the sweep on the loop-quiet row; the delta-worthiness gate reaches
// certification by the other road, so the policy has to read the ledger for itself — but only
// on the answers that would launch a certification: a delta-worthy round keeps its delta.
{
  const r = run('autonomy-policy.mjs', ['--root', GOOD_ROOT, '915-queued-mediums', 'decide', 'delta-worthiness'])
  check('a delta-worthy round keeps its delta discovery over the queued mediums',
    r.code === 0 && r.out.includes('ACTION: auto') && r.out.includes('NEXT: delta discovery') && r.out.includes('PPW-9151'), r.out.trim())
}
{
  const r = run('autonomy-policy.mjs', ['--root', GOOD_ROOT, '915-queued-mediums', 'decide', 'certification-go-ahead'])
  check('policy sweeps the medium queue instead of certifying at the certification-go-ahead gate',
    r.code === 0 && r.out.includes('ACTION: auto') && r.out.includes('NEXT: fix round') && !r.out.includes('NEXT: certification'), r.out.trim())
  check('the certification-go-ahead sweep reason names the count and the ids',
    r.out.includes('sweep before certification — 2 open mediums must drain') && r.out.includes('PPW-9152'), r.out.trim())
}
{
  const r = run('autonomy-policy.mjs', ['--root', GOOD_ROOT, '952-patch-grade-queued', 'decide', 'delta-worthiness'])
  check('a patch-grade round with queued mediums sweeps at the delta-worthiness gate',
    r.code === 0 && r.out.includes('NEXT: fix round') && r.out.includes('sweep before certification — 2 open mediums must drain') && r.out.includes('PPW-9521'), r.out.trim())
}
{
  const r = run('autonomy-policy.mjs', ['--root', GOOD_ROOT, '952-patch-grade-clean-ledger', 'decide', 'delta-worthiness'])
  check('a patch-grade round with a clean ledger still certifies at the delta-worthiness gate',
    r.code === 0 && r.out.includes('NEXT: certification'), r.out.trim())
}
{
  const r = run('autonomy-policy.mjs', ['--root', GOOD_ROOT, '942-resolved-unverified', 'decide', 'certification-go-ahead'])
  check('the ledger guard stands down while a resolved round awaits its verification',
    !r.out.includes('the loop is armed') && !r.out.includes('sweep before certification'), r.out.trim())
}
{
  const r = run('autonomy-policy.mjs', ['--root', GOOD_ROOT, '949-discovery-files-mediums', 'decide', 'delta-worthiness'])
  check('the fail-closed stop survives the ledger guard when no resolution exists',
    r.out.includes('ACTION: stop') && r.out.includes('no resolution file'), r.out.trim())
}
{
  const r = run('autonomy-policy.mjs', ['--root', GOOD_ROOT, '918-open-blocker', 'decide', 'certification-go-ahead'])
  check('policy answers an open 🔴 with a fix round, not a certification',
    r.code === 0 && r.out.includes('NEXT: fix round') && r.out.includes('the loop is armed — 1 open 🔴') && r.out.includes('PPW-9181'), r.out.trim())
}
{
  const r = run('autonomy-policy.mjs', ['--root', GOOD_ROOT, '943-regression-deferred', 'decide', 'certification-go-ahead'])
  check('a ledger with nothing open still certifies as before',
    r.code === 0 && r.out.includes('NEXT: certification (pair)'), r.out.trim())
}
{
  const r = run('autonomy-policy.mjs', ['--root', GOOD_ROOT, '945-certified-two-mediums', 'decide', 'loop-close'])
  check('the loop-close gate still closes with mediums open — they roll into the backlog',
    r.code === 0 && r.out.includes('NEXT: close the loop'), r.out.trim())
}

// ---------- verify-fixes: revert-and-rerun against a throwaway repo ----------
{
  const T = mkdtempSync(join(tmpdir(), 'verify-fixes-'))
  const gitEnv = scrubbedGitEnv()
  const g = (...a) => spawnSync('git', ['-C', T, ...a], { encoding: 'utf8', env: gitEnv })
  g('init', '-q', '-b', 'main')
  g('config', 'user.email', 'fixture@test'); g('config', 'user.name', 'fixture')
  mkdirSync(join(T, 'src', 'app'), { recursive: true })
  mkdirSync(join(T, 'src', 'PhotoPrint.Tests', 'Unit'), { recursive: true })
  writeFileSync(join(T, 'src', 'app', 'calc.txt'), 'buggy\n')
  g('add', '.'); g('commit', '-qm', 'base')
  writeFileSync(join(T, 'src', 'app', 'calc.txt'), 'fixed\n')
  writeFileSync(join(T, 'src', 'PhotoPrint.Tests', 'Unit', 'CalcTests.cs'), 'test body\n')
  g('add', '.'); g('commit', '-qm', 'fix')
  const sha = g('rev-parse', '--short', 'HEAD').stdout.trim()
  mkdirSync(join(T, 'reviews', '950-verify-target'), { recursive: true })
  writeFileSync(join(T, 'reviews', '950-verify-target', 'resolution-v1.md'),
    `---\ntype: resolution\ntarget: 950-verify-target\nversion: 1\nanswers: review-v1.md\nstatus: resolved\nfixed_commit: ${sha}\n---\n\n## Findings\n\n| ID | Status | Commit | Note |\n|---|---|---|---|\n| PPW-9501 | fixed | \`${sha}\` | fixture fix |\n`)
  g('add', '.'); g('commit', '-qm', 'resolution')
  // Prints a failing-test line: since SF39 a red leg counts only when one is named.
  const redGreen = `node -e "if(require('fs').readFileSync('src/app/calc.txt','utf8').includes('buggy')){console.log('Failed CalcTests.Fixture');process.exit(1)}"`

  const wlRel = 'reviews/950-verify-target/worklog.jsonl'
  const wlPath = join(T, wlRel)
  const wlLines = () => existsSync(wlPath) ? readFileSync(wlPath, 'utf8').split(/\r?\n/).filter(Boolean).map(l => JSON.parse(l)) : []
  const statusLines = () => g('status', '--porcelain').stdout.trim().split(/\r?\n/).filter(Boolean)
  const commitWorklog = msg => { g('add', wlRel); g('commit', '-qm', msg) }

  const dry = run('verify-fixes.mjs', ['--root', T, '950-verify-target', '--dry-run'])
  check('verify-fixes dry-run derives the plan', dry.code === 0 && dry.out.includes('calc.txt') && dry.out.includes('PhotoPrint.Tests.Unit.CalcTests'), dry.out.trim())
  check('verify-fixes --dry-run appends no worklog event', wlLines().length === 0, JSON.stringify(wlLines()))

  const live = run('verify-fixes.mjs', ['--root', T, '950-verify-target', '--test-cmd-api', redGreen])
  check('verify-fixes proves red-then-green and reports held', live.code === 0 && live.out.includes('"verdict":"held"') && live.out.includes('SUMMARY: 1/1 held'), live.out.trim())
  check("verify-fixes warns when HEAD has moved past the resolution's fixed_commit",
    live.out.includes(`warning: HEAD is not the resolution's fixed_commit ${sha}`), live.out.trim())
  {
    const verifyResults = wlLines().filter(e => e.ev === 'verify-result')
    check('verify-fixes appends exactly one verify-result event for the held row', verifyResults.length === 1, JSON.stringify(verifyResults))
    check('the verify-result event names PPW-9501 held', verifyResults[0]?.id === 'PPW-9501' && verifyResults[0]?.verdict === 'held', JSON.stringify(verifyResults))
  }
  check('verify-fixes leaves the tree clean except the worklog it just wrote',
    statusLines().length === 1 && statusLines()[0].endsWith(wlRel), g('status', '--porcelain').stdout)
  commitWorklog('worklog')

  const liveNoEvents = run('verify-fixes.mjs', ['--root', T, '950-verify-target', '--test-cmd-api', redGreen, '--no-events'])
  check('verify-fixes --no-events reports held without appending', liveNoEvents.code === 0 && liveNoEvents.out.includes('"verdict":"held"'), liveNoEvents.out.trim())
  check('verify-fixes --no-events appends no additional worklog event', wlLines().filter(e => e.ev === 'verify-result').length === 1, JSON.stringify(wlLines()))

  {
    const decoyT = mkdtempSync(join(tmpdir(), 'decoy-'))
    const dg = (...a) => spawnSync('git', ['-C', decoyT, ...a], { encoding: 'utf8', env: gitEnv })
    dg('init', '-q', '-b', 'main')
    dg('config', 'user.email', 'decoy@test'); dg('config', 'user.name', 'decoy')
    writeFileSync(join(decoyT, 'marker.txt'), 'untouched\n')
    dg('add', '.'); dg('commit', '-qm', 'decoy base')
    const decoyHead = dg('rev-parse', 'HEAD').stdout.trim()
    const leakedEnv = { ...process.env, GIT_DIR: join(decoyT, '.git'), GIT_WORK_TREE: decoyT, GIT_COMMON_DIR: join(decoyT, '.git') }
    const leaked = spawnSync(process.execPath, [join(REVIEWS, 'lib', 'verify-fixes.mjs'), '--root', T, '950-verify-target', '--test-cmd-api', redGreen, '--no-events'], { encoding: 'utf8', cwd: REPO, env: leakedEnv })
    const leakedOut = `${leaked.stdout ?? ''}${leaked.stderr ?? ''}`
    check('verify-fixes ignores a leaked GIT_DIR/GIT_WORK_TREE/GIT_COMMON_DIR and still verifies the real target',
      leaked.status === 0 && leakedOut.includes('"verdict":"held"'), leakedOut.trim())
    check('verify-fixes leaves the real fixture tree clean despite the leaked env', g('status', '--porcelain').stdout.trim() === '', g('status', '--porcelain').stdout)
    check('verify-fixes never touches the decoy repo the leaked env pointed at',
      dg('rev-parse', 'HEAD').stdout.trim() === decoyHead && dg('status', '--porcelain').stdout.trim() === '',
      `decoy HEAD now ${dg('rev-parse', 'HEAD').stdout.trim()} (was ${decoyHead}); status: ${dg('status', '--porcelain').stdout.trim()}`)
    check('the decoy marker file is untouched', readFileSync(join(decoyT, 'marker.txt'), 'utf8') === 'untouched\n', readFileSync(join(decoyT, 'marker.txt'), 'utf8'))
    rmSync(decoyT, { recursive: true, force: true })
  }

  const neverRed = run('verify-fixes.mjs', ['--root', T, '950-verify-target', '--test-cmd-api', 'node -e "process.exit(0)"', '--no-events'])
  check('verify-fixes reopens a fix whose test never goes red', neverRed.code === 1 && neverRed.out.includes('"verdict":"test-never-red"'), neverRed.out.trim())

  writeFileSync(join(T, 'src', 'app', 'calc.txt'), 'dirty\n')
  const dirty = run('verify-fixes.mjs', ['--root', T, '950-verify-target', '--test-cmd-api', redGreen])
  check('verify-fixes refuses a dirty tree', dirty.code === 2, `exit ${dirty.code}: ${dirty.out.trim()}`)
  g('checkout', '--', '.')

  // A fix that took a follow-up commit lists both in the Commit cell; reverting only the first
  // would leave the follow-up's code in place and the test green for the wrong reason.
  writeFileSync(join(T, 'src', 'app', 'calc.txt'), 'fixed and polished\n')
  g('add', '.'); g('commit', '-qm', 'follow-up')
  const sha2 = g('rev-parse', '--short', 'HEAD').stdout.trim()
  writeFileSync(join(T, 'reviews', '950-verify-target', 'resolution-v1.md'),
    `---\ntype: resolution\ntarget: 950-verify-target\nversion: 1\nanswers: review-v1.md\nstatus: resolved\nfixed_commit: ${sha2}\n---\n\n## Findings\n\n| ID | Status | Commit | Note |\n|---|---|---|---|\n| PPW-9501 | fixed | \`${sha}\`, \`${sha2}\` | fixture fix with a follow-up |\n| PPW-9502 | fixed | — | fixture row whose cell names no commit |\n`)
  g('add', '.'); g('commit', '-qm', 'resolution with two commits')
  const multi = run('verify-fixes.mjs', ['--root', T, '950-verify-target', '--test-cmd-api', redGreen, '--no-events'])
  check('verify-fixes covers a row whose Commit cell lists two commits', multi.out.includes('"id":"PPW-9501"') && multi.out.includes('"verdict":"held"'), multi.out.trim())
  check('verify-fixes never skips a fixed row it cannot parse', multi.out.includes('"id":"PPW-9502"') && multi.out.includes('"verdict":"unparsable-commit"'), multi.out.trim())
  check('verify-fixes counts both rows in its summary', multi.out.includes('SUMMARY: 1/2 held') && multi.code === 1, `exit ${multi.code}: ${multi.out.trim()}`)
  check('verify-fixes leaves the tree clean after a multi-commit revert', g('status', '--porcelain').stdout.trim() === '', g('status', '--porcelain').stdout)

  // Two rows each completing a full revert -> red -> restore -> green cycle in one run.
  mkdirSync(join(T, 'src', 'app2'), { recursive: true })
  writeFileSync(join(T, 'src', 'app2', 'a.txt'), 'buggyA\n')
  writeFileSync(join(T, 'src', 'app2', 'b.txt'), 'buggyB\n')
  g('add', '.'); g('commit', '-qm', 'two-row base')
  writeFileSync(join(T, 'src', 'app2', 'a.txt'), 'fixedA\n')
  writeFileSync(join(T, 'src', 'PhotoPrint.Tests', 'Unit', 'ATests.cs'), 'test a\n')
  g('add', '.'); g('commit', '-qm', 'fix a')
  const shaA = g('rev-parse', '--short', 'HEAD').stdout.trim()
  writeFileSync(join(T, 'src', 'app2', 'b.txt'), 'fixedB\n')
  writeFileSync(join(T, 'src', 'PhotoPrint.Tests', 'Unit', 'BTests.cs'), 'test b\n')
  g('add', '.'); g('commit', '-qm', 'fix b')
  const shaB = g('rev-parse', '--short', 'HEAD').stdout.trim()
  writeFileSync(join(T, 'reviews', '950-verify-target', 'resolution-v1.md'),
    `---\ntype: resolution\ntarget: 950-verify-target\nversion: 1\nanswers: review-v1.md\nstatus: resolved\nfixed_commit: ${shaB}\n---\n\n## Findings\n\n| ID | Status | Commit | Note |\n|---|---|---|---|\n| PPW-9510 | fixed | \`${shaA}\` | two-row fixture: row A |\n| PPW-9511 | fixed | \`${shaB}\` | two-row fixture: row B |\n`)
  g('add', '.'); g('commit', '-qm', 'two-row resolution')
  const twoRowTpl = `node -e "const fs=require('fs'); const f=process.argv[1].indexOf('ATests')>=0?'src/app2/a.txt':'src/app2/b.txt'; if(fs.readFileSync(f,'utf8').indexOf('buggy')>=0){console.log('Failed '+process.argv[1]);process.exit(1)}" {filter}`
  const twoRow = run('verify-fixes.mjs', ['--root', T, '950-verify-target', '--test-cmd-api', twoRowTpl])
  check('verify-fixes holds both rows of the two-row run', twoRow.code === 0 && twoRow.out.includes('SUMMARY: 2/2 held'), twoRow.out.trim())
  {
    const twoRowResults = wlLines().filter(e => e.ev === 'verify-result' && (e.id === 'PPW-9510' || e.id === 'PPW-9511'))
    check('both rows of the multi-row run land their own held verify-result event',
      twoRowResults.length === 2 && twoRowResults.every(e => e.verdict === 'held'), JSON.stringify(wlLines()))
  }
  check('verify-fixes leaves the tree clean except the worklog after the two-row run',
    statusLines().length === 1 && statusLines()[0].endsWith(wlRel), g('status', '--porcelain').stdout)
  commitWorklog('worklog after two-row run')

  // A fix commit can itself touch worklog.jsonl, and revert/restore must not corrupt that history.
  writeFileSync(join(T, 'src', 'app2', 'c.txt'), 'buggyC\n')
  g('add', '.'); g('commit', '-qm', 'c base')
  writeFileSync(join(T, 'src', 'app2', 'c.txt'), 'fixedC\n')
  writeFileSync(join(T, 'src', 'PhotoPrint.Tests', 'Unit', 'CTests.cs'), 'test c\n')
  writeFileSync(wlPath, readFileSync(wlPath, 'utf8') + JSON.stringify({ t: '2020-01-01T00:00:00+00:00', ev: 'note', text: 'fixer note committed with the fix' }) + '\n')
  g('add', '.'); g('commit', '-qm', 'fix c (also touches worklog.jsonl)')
  const shaC = g('rev-parse', '--short', 'HEAD').stdout.trim()
  const worklogAtHead = wlLines()
  writeFileSync(join(T, 'reviews', '950-verify-target', 'resolution-v1.md'),
    `---\ntype: resolution\ntarget: 950-verify-target\nversion: 1\nanswers: review-v1.md\nstatus: resolved\nfixed_commit: ${shaC}\n---\n\n## Findings\n\n| ID | Status | Commit | Note |\n|---|---|---|---|\n| PPW-9520 | fixed | \`${shaC}\` | fixture: fix commit also touches worklog.jsonl |\n`)
  g('add', '.'); g('commit', '-qm', 'worklog-in-fix resolution')
  const worklogInFixTpl = `node -e "if(require('fs').readFileSync('src/app2/c.txt','utf8').includes('buggy')){console.log('Failed WorklogTests.Fixture');process.exit(1)}"`
  const wlFix = run('verify-fixes.mjs', ['--root', T, '950-verify-target', '--test-cmd-api', worklogInFixTpl])
  check('verify-fixes holds a row whose fix commit also touches worklog.jsonl', wlFix.code === 0 && wlFix.out.includes('"verdict":"held"'), wlFix.out.trim())
  const worklogAfterFix = wlLines()
  check("the fix commit's committed worklog history survives the revert/restore intact",
    worklogAfterFix.length === worklogAtHead.length + 1 &&
    worklogAtHead.every((e, i) => JSON.stringify(e) === JSON.stringify(worklogAfterFix[i])),
    JSON.stringify({ before: worklogAtHead, after: worklogAfterFix }))
  const lastLine = worklogAfterFix[worklogAfterFix.length - 1]
  check('the run appended its own verify-result on top of, not instead of, that history',
    lastLine?.id === 'PPW-9520' && lastLine?.verdict === 'held', JSON.stringify(worklogAfterFix))
  commitWorklog('worklog after worklog-in-fix run')

  // A frontend spec with no installed dependencies must not be run: the runner would fail to
  // start in both legs, which reads as a red that reddened for the wrong reason.
  mkdirSync(join(T, 'src', 'PhotoPrint.UI', 'src'), { recursive: true })
  writeFileSync(join(T, 'src', 'app', 'widget.txt'), 'buggy\n')
  writeFileSync(join(T, 'src', 'PhotoPrint.UI', 'src', 'widget.spec.ts'), 'spec body\n')
  g('add', '.'); g('commit', '-qm', 'ui fix')
  const uiSha = g('rev-parse', '--short', 'HEAD').stdout.trim()
  writeFileSync(join(T, 'reviews', '950-verify-target', 'resolution-v1.md'),
    `---\ntype: resolution\ntarget: 950-verify-target\nversion: 1\nanswers: review-v1.md\nstatus: resolved\nfixed_commit: ${uiSha}\n---\n\n## Findings\n\n| ID | Status | Commit | Note |\n|---|---|---|---|\n| PPW-9503 | fixed | \`${uiSha}\` | fixture fix carrying a frontend spec |\n`)
  g('add', '.'); g('commit', '-qm', 'ui resolution')
  const ui = run('verify-fixes.mjs', ['--root', T, '950-verify-target', '--test-cmd-api', redGreen, '--no-events'])
  check('verify-fixes refuses a frontend row with no installed dependencies', ui.out.includes('"verdict":"env-missing"') && ui.out.includes('node_modules'), ui.out.trim())
  check('verify-fixes ran no test for the refused frontend row', ui.out.includes('"red_exits":[]'), ui.out.trim())
  rmSync(T, { recursive: true, force: true })
}

// ---------- pre-commit hook: gate overrides leave a trace in the override log ----------
{
  const sh = spawnSync('sh', ['-c', 'true'], { encoding: 'utf8' })
  if (sh.error || sh.status !== 0) {
    console.log('note: sh unavailable — the pre-commit override-log checks were skipped')
  } else {
    const T = mkdtempSync(join(tmpdir(), 'hook-override-'))
    // A git hook's own GIT_DIR/GIT_INDEX_FILE would override -C and land these commits
    // on the real repository, so the throwaway repo gets a scrubbed environment.
    const g = (...a) => spawnSync('git', ['-C', T, ...a], { encoding: 'utf8', env: scrubbedGitEnv() })
    g('init', '-q', '-b', 'main')
    g('config', 'user.email', 'fixture@test'); g('config', 'user.name', 'fixture')
    mkdirSync(join(T, 'src'), { recursive: true })
    writeFileSync(join(T, 'src', 'Fixture.cs'), 'var x = 1; // narrating comment\n')
    g('add', '.')
    const hook = join(REVIEWS, '..', '.githooks', 'pre-commit')
    const blocked = spawnSync('sh', [hook], { cwd: T, encoding: 'utf8', env: { ...scrubbedGitEnv(), COMMENTS_OK: '', DOCGATE_OK: '' } })
    check('hook blocks a staged comment line without an override', blocked.status === 1 && !existsSync(join(T, 'reviews', 'state', 'overrides.jsonl')), `exit ${blocked.status}: ${(blocked.stderr ?? '').trim().slice(0, 200)}`)
    const overridden = spawnSync('sh', [hook], { cwd: T, encoding: 'utf8', env: { ...scrubbedGitEnv(), COMMENTS_OK: '1', DOCGATE_OK: '' } })
    const logPath = join(T, 'reviews', 'state', 'overrides.jsonl')
    const logged = existsSync(logPath) ? readFileSync(logPath, 'utf8') : ''
    check('hook passes with COMMENTS_OK=1 but logs the override', overridden.status === 0 && logged.includes('"var":"COMMENTS_OK"') && logged.includes('src/Fixture.cs'), `exit ${overridden.status}: log=${logged.trim() || '(missing)'}`)
    check('the override-log line parses as JSON with a timestamp', (() => { try { const o = JSON.parse(logged.trim().split('\n')[0]); return Number.isFinite(Date.parse(o.t)) } catch { return false } })(), logged.trim())
    rmSync(T, { recursive: true, force: true })
  }
}
