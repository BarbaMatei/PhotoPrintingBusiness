// Flow: autonomy-policy deciding at the gates over the same kind of ledger states the router
// walks — the medium queue draining before a certification, the ledger guard, the loop-close
// answer. Each state is built from the spec beside its checks (fixture-builder.mjs).
//
// Usage: node reviews/lib/tests/run-tests.mjs --only drive-policy
import { check, run } from '../lib.mjs'
import { buildTarget, fixRound } from '../fixture-builder.mjs'
import { MANIFEST_LENSES } from '../../records/schema.mjs'

// ---------- autonomy-policy: the queue drains before the policy can launch a certification ----------
// The router only meets the sweep on the loop-quiet row; the delta-worthiness gate reaches
// certification by the other road, so the policy has to read the ledger for itself — but only
// on the answers that would launch a certification: a delta-worthy round keeps its delta.
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
  {
    const r = run('drive/autonomy-policy.mjs', ['--root', root, '915-queued-mediums', 'decide', 'delta-worthiness'])
    check('a delta-worthy round keeps its delta discovery over the queued mediums',
      r.code === 0 && r.out.includes('ACTION: auto') && r.out.includes('NEXT: delta discovery') && r.out.includes('PPW-9151'), r.out.trim())
  }
  {
    const r = run('drive/autonomy-policy.mjs', ['--root', root, '915-queued-mediums', 'decide', 'certification-go-ahead'])
    check('policy sweeps the medium queue instead of certifying at the certification-go-ahead gate',
      r.code === 0 && r.out.includes('ACTION: auto') && r.out.includes('NEXT: fix round') && !r.out.includes('NEXT: certification'), r.out.trim())
    check('the certification-go-ahead sweep reason names the count and the ids',
      r.out.includes('sweep before certification — 2 open mediums must drain') && r.out.includes('PPW-9152'), r.out.trim())
  }
}
{
  const root = buildTarget({
    target: '952-patch-grade-queued', reviews: 1,
    metricsLines: [
      { pass: 1, type: 'discovery', date: '2026-08-22', commit: '5555550', verdict: 'request-changes', new_findings: { high: 0, medium: 3, low: 0, cleanup: 0 }, reopened: 0, verified: 0, lenses: MANIFEST_LENSES },
      fixRound({ round: 1, date: '2026-08-22', fixed: 1 }),
      { pass: 1, type: 'verification', date: '2026-08-22', commit: '5555551', verdict: 'approve-with-followups', new_findings: { high: 0, medium: 0, low: 0, cleanup: 0 }, reopened: 0, verified: 1 },
    ],
    ledgerRows: [['PPW-9520', '🟠', 'verified'], ['PPW-9521', '🟠', 'open'], ['PPW-9522', '🟠', 'open']],
    resolutions: [{ v: 1, status: 'resolved', fixedCommit: '5555551', closed: '2026-08-22', fixed: ['PPW-9520'] }],
  })
  const r = run('drive/autonomy-policy.mjs', ['--root', root, '952-patch-grade-queued', 'decide', 'delta-worthiness'])
  check('a patch-grade round with queued mediums sweeps at the delta-worthiness gate',
    r.code === 0 && r.out.includes('NEXT: fix round') && r.out.includes('sweep before certification — 2 open mediums must drain') && r.out.includes('PPW-9521'), r.out.trim())
}
{
  const root = buildTarget({
    target: '952-patch-grade-clean-ledger', reviews: 1,
    metricsLines: [
      { pass: 1, type: 'discovery', date: '2026-08-22', commit: 'eeeeef0', verdict: 'approve-with-followups', new_findings: { high: 0, medium: 0, low: 1, cleanup: 0 }, reopened: 0, verified: 0, lenses: MANIFEST_LENSES },
      { pass: 1, type: 'verification', date: '2026-08-22', commit: 'eeeeef1', verdict: 'approve-with-followups', new_findings: { high: 0, medium: 0, low: 0, cleanup: 0 }, reopened: 0, verified: 1 },
    ],
    ledgerRows: [['PPW-9525', '🟡', 'verified']],
    resolutions: [{ v: 1, status: 'resolved', fixedCommit: 'eeeeef1', closed: '2026-08-22', fixed: ['PPW-9525'] }],
  })
  const r = run('drive/autonomy-policy.mjs', ['--root', root, '952-patch-grade-clean-ledger', 'decide', 'delta-worthiness'])
  check('a patch-grade round with a clean ledger still certifies at the delta-worthiness gate',
    r.code === 0 && r.out.includes('NEXT: certification'), r.out.trim())
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
  const r = run('drive/autonomy-policy.mjs', ['--root', root, '942-resolved-unverified', 'decide', 'certification-go-ahead'])
  check('the ledger guard stands down while a resolved round awaits its verification',
    !r.out.includes('the loop is armed') && !r.out.includes('sweep before certification'), r.out.trim())
}
{
  const root = buildTarget({
    target: '949-discovery-files-mediums', reviews: 1,
    metricsLines: [
      { pass: 1, type: 'discovery', date: '2026-08-22', commit: 'cccccd0', verdict: 'approve-with-followups', new_findings: { high: 0, medium: 2, low: 0, cleanup: 0 }, reopened: 0, verified: 0 },
    ],
    ledgerRows: [['PPW-9491', '🟠', 'open'], ['PPW-9492', '🟠', 'open']],
  })
  const r = run('drive/autonomy-policy.mjs', ['--root', root, '949-discovery-files-mediums', 'decide', 'delta-worthiness'])
  check('the fail-closed stop survives the ledger guard when no resolution exists',
    r.out.includes('ACTION: stop') && r.out.includes('no resolution file'), r.out.trim())
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
  const r = run('drive/autonomy-policy.mjs', ['--root', root, '918-open-blocker', 'decide', 'certification-go-ahead'])
  check('policy answers an open 🔴 with a fix round, not a certification',
    r.code === 0 && r.out.includes('NEXT: fix round') && r.out.includes('the loop is armed — 1 open 🔴') && r.out.includes('PPW-9181'), r.out.trim())
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
  const r = run('drive/autonomy-policy.mjs', ['--root', root, '943-regression-deferred', 'decide', 'certification-go-ahead'])
  check('a ledger with nothing open still certifies as before',
    r.code === 0 && r.out.includes('NEXT: certification (pair)'), r.out.trim())
}
{
  const root = buildTarget({
    target: '945-certified-two-mediums', reviews: 1,
    metricsLines: [
      { pass: 1, type: 'discovery', subtype: 'certification-single', date: '2026-08-22', commit: 'aaaaab1', verdict: 'approved', outcome: 'certified', new_findings: { high: 0, medium: 0, low: 0, cleanup: 0 }, reopened: 0, verified: 0 },
    ],
    ledgerRows: [['PPW-9451', '🟠', 'open'], ['PPW-9452', '🟠', 'open']],
  })
  const r = run('drive/autonomy-policy.mjs', ['--root', root, '945-certified-two-mediums', 'decide', 'loop-close'])
  check('the loop-close gate still closes with mediums open — they roll into the backlog',
    r.code === 0 && r.out.includes('NEXT: close the loop'), r.out.trim())
}
