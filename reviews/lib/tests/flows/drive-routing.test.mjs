// Flow: route-next-pass over the ledger'd routing states — the threshold/queue/sweep rows, the
// reviewed unit, the regression lineage, and the loop-close gate. Spawns the CLI per case.
// Each state is built from the spec beside its checks (fixture-builder.mjs); only 901-good-target,
// which four other suites read as a file, comes from the on-disk fixture root.
//
// Usage: node reviews/lib/tests/run-tests.mjs --only drive-routing
import { check, run, GOOD_ROOT } from '../lib.mjs'
import { buildTarget, fixRound } from '../fixture-builder.mjs'
import { MANIFEST_LENSES } from '../../records/schema.mjs'

// ---------- route-next-pass: the ledger-derived rows — threshold, queue, sweep, reviewed unit ----------
// A fix round and its verification are one reviewed unit, so the ledger — not the metrics tally —
// is what says which findings are still open. Small mediums queue under QUEUE_THRESHOLD instead of
// each spawning a round; the queue must drain before certification.
const REVIEWED_UNIT = 'NEXT: verification (reviewed unit — render records once, after it)'
{
  const root = buildTarget({
    target: '915-queued-mediums', reviews: 1, blockers: { 1: ['PPW-9151'] },
    metricsLines: [
      { pass: 1, type: 'discovery', date: '2026-08-22', commit: '1111110', verdict: 'request-changes', new_findings: { high: 1, medium: 2, low: 0, cleanup: 0 }, reopened: 0, verified: 0 },
      fixRound({ round: 1, date: '2026-08-22', fixed: 1 }),
      { pass: 1, type: 'verification', date: '2026-08-22', commit: '1111111', verdict: 'approve-with-followups', new_findings: { high: 0, medium: 0, low: 0, cleanup: 0 }, reopened: 0, verified: 1 },
    ],
    ledgerRows: [['PPW-9151', '🔴', 'verified'], ['PPW-9152', '🟠', 'open'], ['PPW-9153', '🟠', 'open']],
    resolutions: [{ v: 1, status: 'resolved', fixedCommit: '1111111', closed: '2026-08-22', fixed: ['PPW-9151'] }],
  })
  const r = run('drive/route-next-pass.mjs', ['--root', root, '915-queued-mediums'])
  check('router queues two open mediums instead of routing a round', r.out.includes('QUEUED: PPW-9152, PPW-9153 (2 below the threshold of 3)'), `exit ${r.code}: ${r.out.trim()}`)
  check('the queued mediums do not stop the delta-worthiness gate from printing',
    r.code === 3 && r.out.includes('GATE_KIND: delta-worthiness'), `exit ${r.code}: ${r.out.trim()}`)
  check('a verified 🔴 in the ledger does not arm the loop', !r.out.includes('NEXT: fix round'), r.out.trim())
}
{
  const root = buildTarget({
    target: '916-medium-batch', reviews: 1, blockers: { 1: ['PPW-9161'] },
    metricsLines: [
      { pass: 1, type: 'discovery', date: '2026-08-22', commit: '2222220', verdict: 'request-changes', new_findings: { high: 1, medium: 3, low: 0, cleanup: 0 }, reopened: 0, verified: 0 },
      fixRound({ round: 1, date: '2026-08-22', fixed: 1 }),
      { pass: 1, type: 'verification', date: '2026-08-22', commit: '2222221', verdict: 'approve-with-followups', new_findings: { high: 0, medium: 0, low: 0, cleanup: 0 }, reopened: 0, verified: 1 },
    ],
    ledgerRows: [['PPW-9161', '🔴', 'verified'], ['PPW-9162', '🟠', 'open'], ['PPW-9163', '🟠', 'in-progress'], ['PPW-9164', '🟠', 'open']],
    resolutions: [{ v: 1, status: 'resolved', fixedCommit: '2222221', closed: '2026-08-22', fixed: ['PPW-9161'] }],
  })
  const r = run('drive/route-next-pass.mjs', ['--root', root, '916-medium-batch'])
  check('router routes a batch of three open mediums to a fix round', r.code === 0 && r.out.includes('NEXT: fix round'), `exit ${r.code}: ${r.out.trim()}`)
  check('the batch reason names the count', r.out.includes('batch of 3 open mediums'), r.out.trim())
  check('an in-progress medium counts toward the batch (2 open + 1 in-progress = 3)', !r.out.includes('QUEUED:'), r.out.trim())
  check('the batch row wins over the clean verification it sits on', !r.out.includes('GATE:'), r.out.trim())
}
{
  const root = buildTarget({
    target: '917-sweep-before-cert', reviews: 2, blockers: { 1: ['PPW-9171'] },
    metricsLines: [
      { pass: 1, type: 'discovery', date: '2026-08-22', commit: '3333330', verdict: 'request-changes', new_findings: { high: 1, medium: 2, low: 1, cleanup: 0 }, reopened: 0, verified: 0 },
      { pass: 1, type: 'verification', date: '2026-08-22', commit: '3333331', verdict: 'approve-with-followups', new_findings: { high: 0, medium: 0, low: 0, cleanup: 0 }, reopened: 0, verified: 1 },
      { pass: 2, type: 'delta-discovery', date: '2026-08-22', commit: '3333332', verdict: 'approve-with-followups', new_findings: { high: 0, medium: 0, low: 0, cleanup: 0 }, reopened: 0, verified: 0 },
    ],
    ledgerRows: [['PPW-9171', '🔴', 'verified'], ['PPW-9172', '🟠', 'open'], ['PPW-9173', '🟠', 'deferred'], ['PPW-9174', '🟡', 'open']],
    resolutions: [{ v: 1, status: 'resolved', fixedCommit: '3333331', closed: '2026-08-22', fixed: ['PPW-9171'] }],
  })
  const r = run('drive/route-next-pass.mjs', ['--root', root, '917-sweep-before-cert'])
  check('router sweeps the queue before certification instead of gating', r.code === 0 && r.out.includes('NEXT: fix round'), `exit ${r.code}: ${r.out.trim()}`)
  check('the sweep reason states how many mediums must drain', r.out.includes('sweep before certification — 1 open medium must drain'), r.out.trim())
  check('the sweep counts only open mediums — not a deferred 🟠, not an open 🟡',
    r.out.includes('QUEUED: PPW-9172 (1 below the threshold of 3)'), r.out.trim())
  check('the certification gate does not print while the queue is unswept',
    !r.out.includes('GATE_KIND: certification-go-ahead'), r.out.trim())
  // The seam between the two rows: the queued row answers nothing and writes the ids into the
  // walk's state; the sweep row further down drains that list, not a freshly counted one.
  const queuedIds = (/QUEUED: ([^(]+)/.exec(r.out) ?? [])[1]?.trim()
  const sweptIds = (/must drain \(([^)]+)\)/.exec(r.out) ?? [])[1]?.trim()
  check('the sweep drains exactly the ids the queued row recorded',
    Boolean(queuedIds) && queuedIds === sweptIds, `queued "${queuedIds}" vs swept "${sweptIds}"`)
}
{
  const root = buildTarget({
    target: '918-open-blocker', reviews: 1, blockers: { 1: ['PPW-9181', 'PPW-9182'] },
    metricsLines: [
      { pass: 1, type: 'discovery', date: '2026-08-22', commit: '4444440', verdict: 'request-changes', new_findings: { high: 2, medium: 0, low: 0, cleanup: 0 }, reopened: 0, verified: 0 },
      fixRound({ round: 1, date: '2026-08-22', fixed: 1 }),
      { pass: 1, type: 'verification', date: '2026-08-22', commit: '4444441', verdict: 'approve-with-followups', new_findings: { high: 0, medium: 0, low: 0, cleanup: 0 }, reopened: 0, verified: 1 },
    ],
    ledgerRows: [['PPW-9181', '🔴', 'open'], ['PPW-9182', '🔴', 'verified']],
    resolutions: [{ v: 1, status: 'resolved', fixedCommit: '4444441', closed: '2026-08-22', fixed: ['PPW-9182'] }],
  })
  const r = run('drive/route-next-pass.mjs', ['--root', root, '918-open-blocker'])
  check('router routes an open 🔴 in the ledger straight to a fix round', r.code === 0 && r.out.includes('NEXT: fix round'), `exit ${r.code}: ${r.out.trim()}`)
  check('the armed reason names the open blocker', r.out.includes('PPW-9181'), r.out.trim())
  check('the open blocker outranks the clean verification the metrics show',
    !r.out.includes('GATE_KIND: delta-worthiness'), r.out.trim())
}
{
  const root = buildTarget({
    target: '919-reopened-latest', reviews: 1,
    metricsLines: [
      { pass: 1, type: 'discovery', date: '2026-08-22', commit: '5555550', verdict: 'approve-with-followups', new_findings: { high: 0, medium: 2, low: 0, cleanup: 0 }, reopened: 0, verified: 0 },
      fixRound({ round: 1, date: '2026-08-22', fixed: 2 }),
      { pass: 1, type: 'verification', date: '2026-08-22', commit: '5555551', verdict: 'approve-with-followups', new_findings: { high: 0, medium: 0, low: 0, cleanup: 0 }, reopened: 2, verified: 0 },
    ],
    ledgerRows: [['PPW-9191', '🟠', 'open'], ['PPW-9192', '🟠', 'open']],
    resolutions: [{ v: 1, status: 'resolved', fixedCommit: '5555551', closed: '2026-08-22', fixed: ['PPW-9191', 'PPW-9192'] }],
  })
  const r = run('drive/route-next-pass.mjs', ['--root', root, '919-reopened-latest'])
  check('router routes a reopened fix to a fix round', r.code === 0 && r.out.includes('NEXT: fix round'), `exit ${r.code}: ${r.out.trim()}`)
  check('the armed reason names the reopened count', r.out.includes('2 reopened'), r.out.trim())
  check('a reopened fix outranks the medium queue — no QUEUED line prints', !r.out.includes('QUEUED:'), r.out.trim())
}
{
  const root = buildTarget({
    target: '941-fix-caused-medium', reviews: 1, blockers: { 1: ['PPW-9411'] },
    metricsLines: [
      { pass: 1, type: 'discovery', date: '2026-08-22', commit: '6666660', verdict: 'request-changes', new_findings: { high: 1, medium: 0, low: 0, cleanup: 0 }, reopened: 0, verified: 0 },
      fixRound({ round: 1, date: '2026-08-22', fixed: 1 }),
      {
        pass: 1, type: 'verification', date: '2026-08-22', commit: '6666661', verdict: 'approve-with-followups', new_findings: { high: 0, medium: 1, low: 0, cleanup: 0 },
        findings: [{ d: 'PPW-9412', new: true, sev: 'medium', fix_generated: 'PPW-9411' }], reopened: 0, verified: 1,
      },
    ],
    ledgerRows: [['PPW-9411', '🔴', 'verified'], ['PPW-9412', '🟠', 'open']],
    resolutions: [{ v: 1, status: 'resolved', fixedCommit: '6666661', closed: '2026-08-22', fixed: ['PPW-9411'] }],
  })
  const r = run('drive/route-next-pass.mjs', ['--root', root, '941-fix-caused-medium'])
  check('router routes a fix-caused 🟠 regression to a fix round', r.code === 0 && r.out.includes('NEXT: fix round'), `exit ${r.code}: ${r.out.trim()}`)
  check('the armed reason names the regression and the fix that caused it',
    r.out.includes('fix-caused 🟠 regression') && r.out.includes('PPW-9412') && r.out.includes('PPW-9411'), r.out.trim())
  check('the regression outranks the medium queue — no QUEUED line prints', !r.out.includes('QUEUED:'), r.out.trim())
}
{
  const root = buildTarget({
    target: '943-regression-deferred', reviews: 2, blockers: { 1: ['PPW-9431'] },
    metricsLines: [
      { pass: 1, type: 'discovery', date: '2026-08-22', commit: '8888880', verdict: 'request-changes', new_findings: { high: 1, medium: 0, low: 0, cleanup: 0 }, reopened: 0, verified: 0, lenses: MANIFEST_LENSES },
      {
        pass: 1, type: 'verification', date: '2026-08-22', commit: '8888881', verdict: 'approve-with-followups', new_findings: { high: 0, medium: 1, low: 0, cleanup: 0 },
        findings: [{ d: 'PPW-9432', new: true, sev: 'medium', fix_generated: 'PPW-9431' }], reopened: 0, verified: 1,
      },
      { pass: 2, type: 'delta-discovery', date: '2026-08-22', commit: '8888882', verdict: 'approve-with-followups', new_findings: { high: 0, medium: 0, low: 0, cleanup: 0 }, reopened: 0, verified: 0, lenses: MANIFEST_LENSES },
    ],
    ledgerRows: [['PPW-9431', '🔴', 'verified'], ['PPW-9432', '🟠', 'deferred']],
    resolutions: [{ v: 1, status: 'resolved', fixedCommit: '8888881', closed: '2026-08-22', fixed: ['PPW-9431'] }],
  })
  const r = run('drive/route-next-pass.mjs', ['--root', root, '943-regression-deferred'])
  check('a lineage entry whose ledger row is settled no longer arms the loop',
    r.code === 2 && !r.out.includes('NEXT: fix round') && r.out.includes('GATE_KIND: certification-go-ahead'), `exit ${r.code}: ${r.out.trim()}`)
  check('a deferred 🟠 is not queued either, so nothing has to be swept', !r.out.includes('QUEUED:') && !r.out.includes('sweep before'), r.out.trim())
}
{
  const root = buildTarget({
    target: '942-resolved-unverified', reviews: 1, blockers: { 1: ['PPW-9421'] },
    metricsLines: [
      { pass: 1, type: 'discovery', date: '2026-08-22', commit: '7777770', verdict: 'request-changes', new_findings: { high: 1, medium: 0, low: 0, cleanup: 0 }, reopened: 0, verified: 0 },
    ],
    ledgerRows: [['PPW-9421', '🔴', 'in-progress']],
    resolutions: [{ v: 1, status: 'resolved', fixedCommit: '7777771', closed: '2026-08-22', fixed: ['PPW-9421'] }],
  })
  const r = run('drive/route-next-pass.mjs', ['--root', root, '942-resolved-unverified'])
  check('a resolved resolution still routes to verification, open ledger rows and all',
    r.code === 0 && r.out.includes(REVIEWED_UNIT), `exit ${r.code}: ${r.out.trim()}`)
  check('the reviewed unit is not re-armed by the rows its own verification will close',
    !r.out.includes('NEXT: fix round'), r.out.trim())
}
// A round that answers a verification pass leaves the verification as the newest metrics line, so
// routing on that line re-fixes findings the round already fixed: the stand-down reads the records,
// not the line, and row 3 outranks both the ledger rows and the verification-results row.
{
  const root = buildTarget({
    target: '953-round-answers-verification', reviews: 1,
    metricsLines: [
      { pass: 1, type: 'discovery', lenses: MANIFEST_LENSES, date: '2026-08-22', commit: '5555550', verdict: 'approve-with-followups', new_findings: { high: 0, medium: 2, low: 0, cleanup: 0 }, reopened: 0, verified: 0 },
      fixRound({ round: 1, date: '2026-08-22', fixed: 2 }),
      { pass: 1, type: 'verification', date: '2026-08-22', commit: '5555551', verdict: 'approve-with-followups', new_findings: { high: 0, medium: 0, low: 0, cleanup: 0 }, reopened: 2, verified: 0 },
    ],
    ledgerRows: [['PPW-9531', '🟠', 'open'], ['PPW-9532', '🟠', 'open']],
    resolutions: [
      { v: 1, status: 'resolved', fixedCommit: '5555551', closed: '2026-08-22', fixed: ['PPW-9531', 'PPW-9532'] },
      // Round 2 answers the v1 verification, so it raises no review file of its own.
      { v: 2, status: 'resolved', fixedCommit: '5555552', closed: '2026-08-22', answers: 'the v1 verification (a round answering a verification raises no review file)', fixed: ['PPW-9531', 'PPW-9532'] },
    ],
  })
  const r = run('drive/route-next-pass.mjs', ['--root', root, '953-round-answers-verification'])
  check('a round answering a verification pass routes to its verification, not another fix round',
    r.code === 0 && r.out.includes(REVIEWED_UNIT) && !r.out.includes('NEXT: fix round'), `exit ${r.code}: ${r.out.trim()}`)
  check('the stale verification line does not arm the loop over the resolved round',
    !r.out.includes('the loop is armed') && r.out.includes('resolution-v2 resolved, not yet re-reviewed (row 3)'), r.out.trim())
  for (const gate of ['certification-go-ahead', 'delta-worthiness']) {
    const p = run('drive/autonomy-policy.mjs', ['--root', root, '953-round-answers-verification', 'decide', gate])
    check(`the policy neither arms nor sweeps at the ${gate} gate while the round awaits its verification`,
      !p.out.includes('the loop is armed') && !p.out.includes('sweep before certification'), p.out.trim())
  }
}
// A round closed before fix-round lines existed never gets one, so its resolved-no-line window is
// permanent: standing down there would park the loop on a verification that already ran (035's shape).
{
  const root = buildTarget({
    target: '955-pre-cutoff-resolved', reviews: 1, blockers: { 1: ['PPW-9551'] },
    metricsLines: [
      { pass: 1, type: 'discovery', date: '2026-07-01', commit: '5555560', verdict: 'request-changes', new_findings: { high: 1, medium: 1, low: 0, cleanup: 0 }, reopened: 0, verified: 0 },
      { pass: 1, type: 'verification', date: '2026-07-04', commit: '5555562', verdict: 'approve-with-followups', new_findings: { high: 0, medium: 0, low: 0, cleanup: 0 }, reopened: 0, verified: 1 },
    ],
    ledgerRows: [['PPW-9551', '🔴', 'verified'], ['PPW-9552', '🟠', 'open']],
    resolutions: [{ v: 1, status: 'resolved', fixedCommit: '5555561', closed: '2026-07-04', fixed: ['PPW-9551'] }],
  })
  const r = run('drive/route-next-pass.mjs', ['--root', root, '955-pre-cutoff-resolved'])
  check('a resolution closed before the v3 cut-off does not stand the router down',
    r.code === 3 && r.out.includes('GATE_KIND: delta-worthiness') && !r.out.includes('not yet re-reviewed'), `exit ${r.code}: ${r.out.trim()}`)
  const p = run('drive/autonomy-policy.mjs', ['--root', root, '955-pre-cutoff-resolved', 'decide', 'certification-go-ahead'])
  check('the policy reads the ledger for a pre-cut-off round instead of standing down',
    p.out.includes('sweep before certification') && p.out.includes('PPW-9552'), p.out.trim())
}
{
  const r = run('drive/route-next-pass.mjs', ['--root', GOOD_ROOT, '901-good-target'])
  check('the verification answer names the reviewed unit and keeps its cost line',
    r.out.includes(REVIEWED_UNIT) && r.out.includes('COST: ~60–250k agent tokens'), r.out.trim())
}

// ---------- route-next-pass: queued mediums are absent from the later rows too ----------
// The metrics tally counts a medium as "serious" for the whole life of its line, so a queued
// medium would print QUEUED and then be routed to a fix round two rows later by the same number —
// the router contradicting itself. For a ledger'd target the later rows count the ledger instead.
{
  const root = buildTarget({
    target: '948-verification-files-mediums', reviews: 1, blockers: { 1: ['PPW-9481'] },
    metricsLines: [
      { pass: 1, type: 'discovery', date: '2026-08-22', commit: 'bbbbbc0', verdict: 'request-changes', new_findings: { high: 1, medium: 0, low: 0, cleanup: 0 }, reopened: 0, verified: 0 },
      fixRound({ round: 1, date: '2026-08-22', fixed: 1 }),
      {
        pass: 1, type: 'verification', date: '2026-08-22', commit: 'bbbbbc1', verdict: 'approve-with-followups', new_findings: { high: 0, medium: 2, low: 0, cleanup: 0 },
        findings: [{ d: 'PPW-9482', new: true, sev: 'medium', fix_generated: null }, { d: 'PPW-9483', new: true, sev: 'medium', fix_generated: null }], reopened: 0, verified: 1,
      },
    ],
    ledgerRows: [['PPW-9481', '🔴', 'verified'], ['PPW-9482', '🟠', 'open'], ['PPW-9483', '🟠', 'open']],
    resolutions: [{ v: 1, status: 'resolved', fixedCommit: 'bbbbbc1', closed: '2026-08-22', fixed: ['PPW-9481'] }],
  })
  const r = run('drive/route-next-pass.mjs', ['--root', root, '948-verification-files-mediums'])
  check('a verification that files two mediums queues them instead of re-arming',
    r.out.includes('QUEUED: PPW-9482, PPW-9483 (2 below the threshold of 3)'), `exit ${r.code}: ${r.out.trim()}`)
  check('the queued mediums do not read as new serious findings on the verification row',
    r.code === 3 && r.out.includes('GATE_KIND: delta-worthiness') && !r.out.includes('NEXT: fix round'), `exit ${r.code}: ${r.out.trim()}`)
  check('a new medium with fix_generated null is not a fix-caused regression', !r.out.includes('fix-caused'), r.out.trim())
}
{
  const root = buildTarget({
    target: '949-discovery-files-mediums', reviews: 1,
    metricsLines: [
      { pass: 1, type: 'discovery', date: '2026-08-22', commit: 'cccccd0', verdict: 'approve-with-followups', new_findings: { high: 0, medium: 2, low: 0, cleanup: 0 }, reopened: 0, verified: 0 },
    ],
    ledgerRows: [['PPW-9491', '🟠', 'open'], ['PPW-9492', '🟠', 'open']],
  })
  const r = run('drive/route-next-pass.mjs', ['--root', root, '949-discovery-files-mediums'])
  check('a discovery that files two mediums with nothing answering it queues them',
    r.out.includes('QUEUED: PPW-9491, PPW-9492 (2 below the threshold of 3)'), `exit ${r.code}: ${r.out.trim()}`)
  check('and then routes the sweep, not the open-serious row',
    r.code === 0 && r.out.includes('sweep before certification — 2 open mediums must drain') && !r.out.includes('open serious findings'), `exit ${r.code}: ${r.out.trim()}`)
}
{
  const root = buildTarget({
    target: '944-regression-persists', reviews: 1, blockers: { 1: ['PPW-9441'] },
    metricsLines: [
      { pass: 1, type: 'discovery', date: '2026-08-22', commit: '9999990', verdict: 'request-changes', new_findings: { high: 1, medium: 0, low: 1, cleanup: 0 }, reopened: 0, verified: 0, lenses: MANIFEST_LENSES },
      {
        pass: 1, type: 'verification', date: '2026-08-22', commit: '9999991', verdict: 'approve-with-followups', new_findings: { high: 0, medium: 1, low: 0, cleanup: 0 },
        findings: [{ d: 'PPW-9442', new: true, sev: 'medium', fix_generated: 'PPW-9441' }], reopened: 0, verified: 1,
      },
      fixRound({ round: 2, date: '2026-08-22', fixed: 1 }),
      { pass: 2, type: 'verification', date: '2026-08-22', commit: '9999992', verdict: 'approve-with-followups', new_findings: { high: 0, medium: 0, low: 0, cleanup: 0 }, reopened: 0, verified: 1 },
    ],
    ledgerRows: [['PPW-9441', '🔴', 'verified'], ['PPW-9442', '🟠', 'open'], ['PPW-9443', '🟡', 'verified']],
    resolutions: [
      { v: 1, status: 'resolved', fixedCommit: '9999991', closed: '2026-08-22', fixed: ['PPW-9441'] },
      { v: 2, status: 'resolved', fixedCommit: '9999992', closed: '2026-08-22', answers: 'review-v1.md', fixed: ['PPW-9443'] },
    ],
  })
  const r = run('drive/route-next-pass.mjs', ['--root', root, '944-regression-persists'])
  check('a still-open fix-caused 🟠 keeps arming the loop after a newer clean verification',
    r.code === 0 && r.out.includes('fix-caused 🟠 regression') && r.out.includes('PPW-9442'), `exit ${r.code}: ${r.out.trim()}`)
  check('the regression is read across every verification line, not just the newest',
    r.out.includes('from the fix for PPW-9441'), r.out.trim())
}

// ---------- route-next-pass: the loop-close gate and the ledger rows ----------
// 🟠 still open at certification is the documented norm — they roll into the backlog at close, so
// they must not pre-empt the owner's close decision. A 🔴 that lands after the certification pass
// still has to arm the loop.
const certified = (target, commit, ledgerRows) => buildTarget({
  target, reviews: 1, ledgerRows,
  metricsLines: [
    { pass: 1, type: 'discovery', subtype: 'certification-single', date: '2026-08-22', commit, verdict: 'approved', outcome: 'certified', new_findings: { high: 0, medium: 0, low: 0, cleanup: 0 }, reopened: 0, verified: 0 },
  ],
})
{
  const root = certified('945-certified-two-mediums', 'aaaaab1',
    [['PPW-9451', '🟠', 'open'], ['PPW-9452', '🟠', 'open']])
  const r = run('drive/route-next-pass.mjs', ['--root', root, '945-certified-two-mediums'])
  check('two open mediums do not queue over the loop-close gate',
    r.code === 2 && r.out.includes('GATE_KIND: loop-close') && !r.out.includes('QUEUED:'), `exit ${r.code}: ${r.out.trim()}`)
}
{
  const root = certified('946-certified-medium-batch', 'aaaaab2',
    [['PPW-9461', '🟠', 'open'], ['PPW-9462', '🟠', 'open'], ['PPW-9463', '🟠', 'open']])
  const r = run('drive/route-next-pass.mjs', ['--root', root, '946-certified-medium-batch'])
  check('a batch of three open mediums does not pre-empt the loop-close gate either',
    r.code === 2 && r.out.includes('GATE_KIND: loop-close') && !r.out.includes('NEXT: fix round'), `exit ${r.code}: ${r.out.trim()}`)
}
{
  const root = certified('947-certified-open-blocker', 'aaaaab3',
    [['PPW-9471', '🔴', 'open'], ['PPW-9472', '🟠', 'open']])
  const r = run('drive/route-next-pass.mjs', ['--root', root, '947-certified-open-blocker'])
  check('an open 🔴 arms the loop even at the loop-close gate',
    r.code === 0 && r.out.includes('NEXT: fix round') && r.out.includes('PPW-9471'), `exit ${r.code}: ${r.out.trim()}`)
  check('the post-certification blocker is not answered with a close gate', !r.out.includes('GATE_KIND: loop-close'), r.out.trim())
}
