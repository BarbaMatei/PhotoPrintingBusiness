// Flow: route-next-pass over the ledger'd fixture targets — the threshold/queue/sweep rows, the
// reviewed unit, the regression lineage, and the loop-close gate. Spawns the CLI per case.
//
// Usage: node reviews/lib/tests/run-tests.mjs --only drive-routing
import { check, run, GOOD_ROOT } from '../lib.mjs'

// ---------- route-next-pass: the ledger-derived rows — threshold, queue, sweep, reviewed unit ----------
// A fix round and its verification are one reviewed unit, so the ledger — not the metrics tally —
// is what says which findings are still open. Small mediums queue under QUEUE_THRESHOLD instead of
// each spawning a round; the queue must drain before certification.
const REVIEWED_UNIT = 'NEXT: verification (reviewed unit — render records once, after it)'
{
  const r = run('drive/route-next-pass.mjs', ['--root', GOOD_ROOT, '915-queued-mediums'])
  check('router queues two open mediums instead of routing a round', r.out.includes('QUEUED: PPW-9152, PPW-9153 (2 below the threshold of 3)'), `exit ${r.code}: ${r.out.trim()}`)
  check('the queued mediums do not stop the delta-worthiness gate from printing',
    r.code === 3 && r.out.includes('GATE_KIND: delta-worthiness'), `exit ${r.code}: ${r.out.trim()}`)
  check('a verified 🔴 in the ledger does not arm the loop', !r.out.includes('NEXT: fix round'), r.out.trim())
}
{
  const r = run('drive/route-next-pass.mjs', ['--root', GOOD_ROOT, '916-medium-batch'])
  check('router routes a batch of three open mediums to a fix round', r.code === 0 && r.out.includes('NEXT: fix round'), `exit ${r.code}: ${r.out.trim()}`)
  check('the batch reason names the count', r.out.includes('batch of 3 open mediums'), r.out.trim())
  check('an in-progress medium counts toward the batch (2 open + 1 in-progress = 3)', !r.out.includes('QUEUED:'), r.out.trim())
  check('the batch row wins over the clean verification it sits on', !r.out.includes('GATE:'), r.out.trim())
}
{
  const r = run('drive/route-next-pass.mjs', ['--root', GOOD_ROOT, '917-sweep-before-cert'])
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
  const r = run('drive/route-next-pass.mjs', ['--root', GOOD_ROOT, '918-open-blocker'])
  check('router routes an open 🔴 in the ledger straight to a fix round', r.code === 0 && r.out.includes('NEXT: fix round'), `exit ${r.code}: ${r.out.trim()}`)
  check('the armed reason names the open blocker', r.out.includes('PPW-9181'), r.out.trim())
  check('the open blocker outranks the clean verification the metrics show',
    !r.out.includes('GATE_KIND: delta-worthiness'), r.out.trim())
}
{
  const r = run('drive/route-next-pass.mjs', ['--root', GOOD_ROOT, '919-reopened-latest'])
  check('router routes a reopened fix to a fix round', r.code === 0 && r.out.includes('NEXT: fix round'), `exit ${r.code}: ${r.out.trim()}`)
  check('the armed reason names the reopened count', r.out.includes('2 reopened'), r.out.trim())
  check('a reopened fix outranks the medium queue — no QUEUED line prints', !r.out.includes('QUEUED:'), r.out.trim())
}
{
  const r = run('drive/route-next-pass.mjs', ['--root', GOOD_ROOT, '941-fix-caused-medium'])
  check('router routes a fix-caused 🟠 regression to a fix round', r.code === 0 && r.out.includes('NEXT: fix round'), `exit ${r.code}: ${r.out.trim()}`)
  check('the armed reason names the regression and the fix that caused it',
    r.out.includes('fix-caused 🟠 regression') && r.out.includes('PPW-9412') && r.out.includes('PPW-9411'), r.out.trim())
  check('the regression outranks the medium queue — no QUEUED line prints', !r.out.includes('QUEUED:'), r.out.trim())
}
{
  const r = run('drive/route-next-pass.mjs', ['--root', GOOD_ROOT, '943-regression-deferred'])
  check('a lineage entry whose ledger row is settled no longer arms the loop',
    r.code === 2 && !r.out.includes('NEXT: fix round') && r.out.includes('GATE_KIND: certification-go-ahead'), `exit ${r.code}: ${r.out.trim()}`)
  check('a deferred 🟠 is not queued either, so nothing has to be swept', !r.out.includes('QUEUED:') && !r.out.includes('sweep before'), r.out.trim())
}
{
  const r = run('drive/route-next-pass.mjs', ['--root', GOOD_ROOT, '942-resolved-unverified'])
  check('a resolved resolution still routes to verification, open ledger rows and all',
    r.code === 0 && r.out.includes(REVIEWED_UNIT), `exit ${r.code}: ${r.out.trim()}`)
  check('the reviewed unit is not re-armed by the rows its own verification will close',
    !r.out.includes('NEXT: fix round'), r.out.trim())
}
// A round that answers a verification pass leaves the verification as the newest metrics line, so
// routing on that line re-fixes findings the round already fixed: the stand-down reads the records,
// not the line, and row 3 outranks both the ledger rows and the verification-results row.
{
  const r = run('drive/route-next-pass.mjs', ['--root', GOOD_ROOT, '953-round-answers-verification'])
  check('a round answering a verification pass routes to its verification, not another fix round',
    r.code === 0 && r.out.includes(REVIEWED_UNIT) && !r.out.includes('NEXT: fix round'), `exit ${r.code}: ${r.out.trim()}`)
  check('the stale verification line does not arm the loop over the resolved round',
    !r.out.includes('the loop is armed') && r.out.includes('resolution-v2 resolved, not yet re-reviewed (row 3)'), r.out.trim())
}
// A round closed before fix-round lines existed never gets one, so its resolved-no-line window is
// permanent: standing down there would park the loop on a verification that already ran (035's shape).
{
  const r = run('drive/route-next-pass.mjs', ['--root', GOOD_ROOT, '955-pre-cutoff-resolved'])
  check('a resolution closed before the v3 cut-off does not stand the router down',
    r.code === 3 && r.out.includes('GATE_KIND: delta-worthiness') && !r.out.includes('not yet re-reviewed'), `exit ${r.code}: ${r.out.trim()}`)
  const p = run('drive/autonomy-policy.mjs', ['--root', GOOD_ROOT, '955-pre-cutoff-resolved', 'decide', 'certification-go-ahead'])
  check('the policy reads the ledger for a pre-cut-off round instead of standing down',
    p.out.includes('sweep before certification') && p.out.includes('PPW-9552'), p.out.trim())
}
for (const gate of ['certification-go-ahead', 'delta-worthiness']) {
  const r = run('drive/autonomy-policy.mjs', ['--root', GOOD_ROOT, '953-round-answers-verification', 'decide', gate])
  check(`the policy neither arms nor sweeps at the ${gate} gate while the round awaits its verification`,
    !r.out.includes('the loop is armed') && !r.out.includes('sweep before certification'), r.out.trim())
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
  const r = run('drive/route-next-pass.mjs', ['--root', GOOD_ROOT, '948-verification-files-mediums'])
  check('a verification that files two mediums queues them instead of re-arming',
    r.out.includes('QUEUED: PPW-9482, PPW-9483 (2 below the threshold of 3)'), `exit ${r.code}: ${r.out.trim()}`)
  check('the queued mediums do not read as new serious findings on the verification row',
    r.code === 3 && r.out.includes('GATE_KIND: delta-worthiness') && !r.out.includes('NEXT: fix round'), `exit ${r.code}: ${r.out.trim()}`)
  check('a new medium with fix_generated null is not a fix-caused regression', !r.out.includes('fix-caused'), r.out.trim())
}
{
  const r = run('drive/route-next-pass.mjs', ['--root', GOOD_ROOT, '949-discovery-files-mediums'])
  check('a discovery that files two mediums with nothing answering it queues them',
    r.out.includes('QUEUED: PPW-9491, PPW-9492 (2 below the threshold of 3)'), `exit ${r.code}: ${r.out.trim()}`)
  check('and then routes the sweep, not the open-serious row',
    r.code === 0 && r.out.includes('sweep before certification — 2 open mediums must drain') && !r.out.includes('open serious findings'), `exit ${r.code}: ${r.out.trim()}`)
}
{
  const r = run('drive/route-next-pass.mjs', ['--root', GOOD_ROOT, '944-regression-persists'])
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
  const r = run('drive/route-next-pass.mjs', ['--root', GOOD_ROOT, '945-certified-two-mediums'])
  check('two open mediums do not queue over the loop-close gate',
    r.code === 2 && r.out.includes('GATE_KIND: loop-close') && !r.out.includes('QUEUED:'), `exit ${r.code}: ${r.out.trim()}`)
}
{
  const r = run('drive/route-next-pass.mjs', ['--root', GOOD_ROOT, '946-certified-medium-batch'])
  check('a batch of three open mediums does not pre-empt the loop-close gate either',
    r.code === 2 && r.out.includes('GATE_KIND: loop-close') && !r.out.includes('NEXT: fix round'), `exit ${r.code}: ${r.out.trim()}`)
}
{
  const r = run('drive/route-next-pass.mjs', ['--root', GOOD_ROOT, '947-certified-open-blocker'])
  check('an open 🔴 arms the loop even at the loop-close gate',
    r.code === 0 && r.out.includes('NEXT: fix round') && r.out.includes('PPW-9471'), `exit ${r.code}: ${r.out.trim()}`)
  check('the post-certification blocker is not answered with a close gate', !r.out.includes('GATE_KIND: loop-close'), r.out.trim())
}
