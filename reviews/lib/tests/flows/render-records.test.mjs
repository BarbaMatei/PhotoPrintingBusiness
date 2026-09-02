// Flow: render-records writing a fix round's and a verification's records into a throwaway tree —
// metrics line, index row, ledger status flips — and the doc gate reading back what it wrote.
//
// Usage: node reviews/lib/tests/run-tests.mjs --only render-records
import { check, run } from '../lib.mjs'
import { mkdtempSync, mkdirSync, writeFileSync, readFileSync, existsSync, rmSync } from 'node:fs'
import { join } from 'node:path'
import { tmpdir } from 'node:os'

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
    const r = run('records/render-records.mjs', ['--root', T, target])
    check('renderer refuses to append records with no --outcome', r.code === 1 && r.out.includes('--outcome'), `exit ${r.code}: ${r.out.trim().slice(0, 200)}`)
    check('the refused render wrote no metrics line, no index row and no ledger flip', untouched(), 'one of metrics.jsonl / index.md / ledger.md changed')
  }
  {
    const long = Array.from({ length: 51 }, (_, i) => `w${i}`).join(' ')
    const r = run('records/render-records.mjs', ['--root', T, target, '--outcome', long])
    check('renderer refuses an --outcome over the 50-word index cap', r.code === 1 && r.out.includes('51 words') && r.out.includes('50'), `exit ${r.code}: ${r.out.trim().slice(0, 200)}`)
    check('the over-cap refusal wrote nothing either', untouched(), 'one of metrics.jsonl / index.md / ledger.md changed')
  }
  // A "|" or a newline in the outcome breaks the pipe-delimited row, which only surfaces at the doc gate.
  for (const [label, text] of [['a "|"', 'Both rows closed | and the queue drained'], ['a newline', 'Both rows closed\nand the queue drained']]) {
    const r = run('records/render-records.mjs', ['--root', T, target, '--outcome', text])
    check(`renderer refuses an --outcome carrying ${label}`, r.code === 1 && r.out.includes('one pipe-delimited line'), `exit ${r.code}: ${r.out.trim().slice(0, 200)}`)
    check(`the ${label} refusal wrote nothing`, untouched(), 'one of metrics.jsonl / index.md / ledger.md changed')
  }
  {
    const r = run('records/render-records.mjs', ['--root', T, target, '--outcome'])
    check('--outcome with no value prints the usage line, not a stack trace', r.code === 1 && r.out.includes('usage: render-records.mjs') && !r.out.includes('TypeError'), `exit ${r.code}: ${r.out.trim().slice(0, 200)}`)
    const flag = run('records/render-records.mjs', ['--root', T, target, '--outcome', '--dry-run'])
    check('an --outcome that swallowed the next flag is refused', flag.code === 1 && flag.out.includes('another flag'), `exit ${flag.code}: ${flag.out.trim().slice(0, 200)}`)
  }
  {
    const r = run('records/render-records.mjs', ['--root', T, target, '--outcome', OUTCOME, '--dry-run'])
    check('dry-run prints the index row it would insert', r.code === 0 && r.out.includes('| 2026-08-21 | 938 | v1 fix round (2 clusters, 1 approach-check, 1 micro-review) |'), `exit ${r.code}: ${r.out.split('\n').find(l => l.startsWith('| 2026')) ?? r.out.trim().slice(0, 200)}`)
    check('dry-run prints the ledger flips it would make', r.out.includes('PPW-9381 → fixed at `def5678`') && r.out.includes('PPW-9382 → deferred'), r.out.split('\n').filter(l => l.includes('→')).join(' | '))
    check('dry-run wrote nothing', untouched(), 'one of metrics.jsonl / index.md / ledger.md changed')
  }

  {
    const r = run('records/render-records.mjs', ['--root', T, target, '--outcome', OUTCOME])
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

  // index.md and ledger.md are CRLF here, so an inserted line must match its neighbours.
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
    const r = run('records/render-records.mjs', ['--root', T, target, '--verification', 'v2', '--outcome', V_OUTCOME])
    check('verification refuses while the pass has no pass-records-done', r.code === 1 && r.out.includes('pass-records-done') && r.out.includes('--in-progress'), `exit ${r.code}: ${r.out.trim().slice(0, 250)}`)
    check('the unfinished-pass refusal wrote nothing', vUntouched(), 'index.md, ledger.md or metrics.jsonl changed')
  }
  {
    const r = run('records/render-records.mjs', ['--root', T, target, '--verification', 'v2', '--outcome', V_OUTCOME, '--dry-run'])
    check('an unfinished verification still dry-runs', r.code === 0 && r.out.includes('no pass-records-done yet'), `exit ${r.code}: ${r.out.trim().slice(0, 250)}`)
    check('the verification dry-run wrote nothing', vUntouched(), 'index.md, ledger.md or metrics.jsonl changed')
  }

  wl([...verifyEvents, { t: at('11:20'), ev: 'pass-records-done', pass: 'v2' }])
  {
    const r = run('records/render-records.mjs', ['--root', T, target, '--verification', 'v2', '--outcome', V_OUTCOME, '--new-findings', '0,1,0,0'])
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
    const r = run('records/render-records.mjs', ['--root', T, target, '--verification', 'v2', '--outcome', V_OUTCOME])
    check('a second verification render for the same pass refuses rather than duplicating the line',
      r.code === 1 && r.out.includes('correction line'), `exit ${r.code}: ${r.out.trim().slice(0, 250)}`)
    check('the duplicate refusal left all three files alone',
      read(indexPath) === after.index && read(ledgerPath) === after.ledger && metricsLines().length === after.metrics, 'a file changed')

    wl([...verifyEvents, { t: at('11:30'), ev: 'pass-launch', pass: 'v2', type: 'verification' }])
    const twice = run('records/render-records.mjs', ['--root', T, target, '--verification', '2', '--outcome', V_OUTCOME])
    check('a second pass-launch for an open pass aborts', twice.code === 1 && twice.out.includes(at('11:00')) && twice.out.includes(at('11:30')) && twice.out.includes('wl.mjs'), `exit ${twice.code}: ${twice.out.trim().slice(0, 250)}`)
    check('the unpairable-stamp abort left all three files alone',
      read(indexPath) === after.index && read(ledgerPath) === after.ledger && metricsLines().length === after.metrics, 'a file changed')

    const gate = run('records/doc-gate.mjs', ['--root', T, 'state'])
    check('both generated index rows pass the state doc gate', gate.code === 0, gate.out.trim().slice(0, 400))
  }

  {
    wl([...verifyEvents, { t: at('11:20'), ev: 'pass-records-done', pass: 'v2' },
      { t: at('12:00'), ev: 'pass-launch', pass: 'v3', type: 'verification' },
      { t: at('12:05'), ev: 'verify-result', id: 'PPW-9381', verdict: 'held', commit: 'bbb2222' },
      { t: at('12:10'), ev: 'pass-records-done', pass: 'v3' }])
    const r = run('records/render-records.mjs', ['--root', T, target, '--verification', 'v3', '--outcome', V_OUTCOME, '--commit', 'ccc3333', '--no-index'])
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
    const r = run('records/render-records.mjs', ['--root', T, bare, '--verification', 'v1', '--outcome', 'The one fix held on its own revert.', '--no-index'])
    const written = existsSync(join(T, 'reviews', bare, 'metrics.jsonl')) ? JSON.parse(readFileSync(join(T, 'reviews', bare, 'metrics.jsonl'), 'utf8').trim()) : {}
    check('commit is null, and said to be, when there is no --commit and no resolution to read it from',
      r.code === 0 && written.commit === null && r.out.includes('commit will be null'), `exit ${r.code}: ${JSON.stringify(written).slice(0, 200)}`)
    check('a held row with no commit on its event leaves Affirmed alone', r.out.includes('PPW-9391 → verified') && !r.out.includes('PPW-9391 → verified at'),
      r.out.split('\n').find(l => l.includes('→')) ?? r.out.trim().slice(0, 200))
  }

  rmSync(T, { recursive: true, force: true })
}
