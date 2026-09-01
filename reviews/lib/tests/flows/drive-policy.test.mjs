// Flow: autonomy-policy deciding at the gates over the same ledger fixtures the router walks —
// the medium queue draining before a certification, the ledger guard, the loop-close answer.
//
// Usage: node reviews/lib/tests/run-tests.mjs --only drive-policy
import { check, run, GOOD_ROOT } from '../lib.mjs'

// ---------- autonomy-policy: the queue drains before the policy can launch a certification ----------
// The router only meets the sweep on the loop-quiet row; the delta-worthiness gate reaches
// certification by the other road, so the policy has to read the ledger for itself — but only
// on the answers that would launch a certification: a delta-worthy round keeps its delta.
{
  const r = run('drive/autonomy-policy.mjs', ['--root', GOOD_ROOT, '915-queued-mediums', 'decide', 'delta-worthiness'])
  check('a delta-worthy round keeps its delta discovery over the queued mediums',
    r.code === 0 && r.out.includes('ACTION: auto') && r.out.includes('NEXT: delta discovery') && r.out.includes('PPW-9151'), r.out.trim())
}
{
  const r = run('drive/autonomy-policy.mjs', ['--root', GOOD_ROOT, '915-queued-mediums', 'decide', 'certification-go-ahead'])
  check('policy sweeps the medium queue instead of certifying at the certification-go-ahead gate',
    r.code === 0 && r.out.includes('ACTION: auto') && r.out.includes('NEXT: fix round') && !r.out.includes('NEXT: certification'), r.out.trim())
  check('the certification-go-ahead sweep reason names the count and the ids',
    r.out.includes('sweep before certification — 2 open mediums must drain') && r.out.includes('PPW-9152'), r.out.trim())
}
{
  const r = run('drive/autonomy-policy.mjs', ['--root', GOOD_ROOT, '952-patch-grade-queued', 'decide', 'delta-worthiness'])
  check('a patch-grade round with queued mediums sweeps at the delta-worthiness gate',
    r.code === 0 && r.out.includes('NEXT: fix round') && r.out.includes('sweep before certification — 2 open mediums must drain') && r.out.includes('PPW-9521'), r.out.trim())
}
{
  const r = run('drive/autonomy-policy.mjs', ['--root', GOOD_ROOT, '952-patch-grade-clean-ledger', 'decide', 'delta-worthiness'])
  check('a patch-grade round with a clean ledger still certifies at the delta-worthiness gate',
    r.code === 0 && r.out.includes('NEXT: certification'), r.out.trim())
}
{
  const r = run('drive/autonomy-policy.mjs', ['--root', GOOD_ROOT, '942-resolved-unverified', 'decide', 'certification-go-ahead'])
  check('the ledger guard stands down while a resolved round awaits its verification',
    !r.out.includes('the loop is armed') && !r.out.includes('sweep before certification'), r.out.trim())
}
{
  const r = run('drive/autonomy-policy.mjs', ['--root', GOOD_ROOT, '949-discovery-files-mediums', 'decide', 'delta-worthiness'])
  check('the fail-closed stop survives the ledger guard when no resolution exists',
    r.out.includes('ACTION: stop') && r.out.includes('no resolution file'), r.out.trim())
}
{
  const r = run('drive/autonomy-policy.mjs', ['--root', GOOD_ROOT, '918-open-blocker', 'decide', 'certification-go-ahead'])
  check('policy answers an open 🔴 with a fix round, not a certification',
    r.code === 0 && r.out.includes('NEXT: fix round') && r.out.includes('the loop is armed — 1 open 🔴') && r.out.includes('PPW-9181'), r.out.trim())
}
{
  const r = run('drive/autonomy-policy.mjs', ['--root', GOOD_ROOT, '943-regression-deferred', 'decide', 'certification-go-ahead'])
  check('a ledger with nothing open still certifies as before',
    r.code === 0 && r.out.includes('NEXT: certification (pair)'), r.out.trim())
}
{
  const r = run('drive/autonomy-policy.mjs', ['--root', GOOD_ROOT, '945-certified-two-mediums', 'decide', 'loop-close'])
  check('the loop-close gate still closes with mediums open — they roll into the backlog',
    r.code === 0 && r.out.includes('NEXT: close the loop'), r.out.trim())
}
