// Tests for autonomy-policy.mjs: the decide subcommand across gate kinds.
//
// Usage: node reviews/lib/tests/run-tests.mjs --only autonomy-policy
import { check, run, GOOD_ROOT } from './lib.mjs'

// ---------- autonomy-policy: decide ----------
{
  const r = run('autonomy-policy.mjs', ['--root', GOOD_ROOT, '910-delta-worthy', 'decide', 'delta-worthiness'])
  check('policy auto-routes a blocker-fixing round to delta discovery', r.code === 0 && r.out.includes('ACTION: auto') && r.out.includes('NEXT: delta discovery'), r.out.trim())
  check('policy names the fixed blocker in its reason', r.out.includes('PPW-9910'), r.out.trim())
}
{
  const r = run('autonomy-policy.mjs', ['--root', GOOD_ROOT, '911-patch-grade', 'decide', 'delta-worthiness'])
  check('policy routes a patch-grade round to a first certification pair', r.code === 0 && r.out.includes('ACTION: auto') && r.out.includes('NEXT: certification (pair)'), r.out.trim())
}
{
  const r = run('autonomy-policy.mjs', ['--root', GOOD_ROOT, '912-recert', 'decide', 'delta-worthiness'])
  check('policy routes a re-certification as a single pass', r.code === 0 && r.out.includes('NEXT: certification (single)'), r.out.trim())
}
{
  const r = run('autonomy-policy.mjs', ['--root', GOOD_ROOT, '913-loop-quiet', 'decide', 'certification-go-ahead'])
  check('policy answers a clean discovery loop-quiet gate with a first certification pair', r.code === 0 && r.out.includes('ACTION: auto') && r.out.includes('NEXT: certification (pair)'), r.out.trim())
}
{
  const r = run('autonomy-policy.mjs', ['--root', GOOD_ROOT, '912-recert', 'decide', 'certification-go-ahead'])
  check('policy answers a loop-quiet gate for a re-certified target with a single pass', r.code === 0 && r.out.includes('NEXT: certification (single)'), r.out.trim())
}
{
  const r = run('autonomy-policy.mjs', ['--root', GOOD_ROOT, '914-resolution-above-review', 'decide', 'delta-worthiness'])
  check('policy judges the newest resolution, not the one paired with the newest review', r.code === 0 && r.out.includes('NEXT: certification (pair)'), r.out.trim())
  check('policy calls a round with no review file of its own patch-grade', r.out.includes('patch-grade'), r.out.trim())
}
{
  const r = run('autonomy-policy.mjs', ['--root', GOOD_ROOT, '909-certified-target', 'decide', 'loop-close'])
  check('policy closes the loop under the standing approval', r.out.includes('ACTION: auto') && r.out.includes('NEXT: close the loop'), r.out.trim())
}
{
  const r = run('autonomy-policy.mjs', ['--root', GOOD_ROOT, '909-certified-target', 'decide', 'mystery-gate'])
  check('policy fails closed on an unknown gate kind', r.out.includes('ACTION: stop'), r.out.trim())
}

{
  const r = run('autonomy-policy.mjs', ['--root', GOOD_ROOT, '915-lens-debt', 'decide', 'certification-go-ahead'])
  check('policy refuses auto-certification on lens debt and routes the owed lens', r.out.includes('ACTION: auto') && r.out.includes('NEXT: lens-coverage discovery (frontend-ux)'), r.out.trim())
}

{
  const r = run('autonomy-policy.mjs', ['--root', GOOD_ROOT, '916-unmeasured-seed', 'decide', 'delta-worthiness'])
  check('policy routes an unmeasured final round to a measuring delta discovery', r.out.includes('ACTION: auto') && r.out.includes('NEXT: delta discovery') && r.out.includes('unmeasured'), r.out.trim())
}

{
  const r = run('autonomy-policy.mjs', ['--root', GOOD_ROOT, '917-non-convergent', 'decide', 'design-pass'])
  check('policy stops on a design-pass gate', r.out.includes('ACTION: stop'), r.out.trim())
}

{
  const r = run('autonomy-policy.mjs', ['--root', GOOD_ROOT, '919-override-stop', 'decide', 'loop-close'])
  check('policy stops when a gate override was logged after the run started', r.out.includes('ACTION: stop') && r.out.includes('COMMENTS_OK'), r.out.trim())
}

{
  const r = run('autonomy-policy.mjs', ['--root', GOOD_ROOT, '919-override-clean', 'decide', 'loop-close'])
  check('policy ignores overrides logged before the run started', r.out.includes('ACTION: auto'), r.out.trim())
}

// A mis-stamped run-start would push the override cut-off past a real override and hide it.
{
  const r = run('autonomy-policy.mjs', ['--root', GOOD_ROOT, '956-override-voided-run-start', 'decide', 'loop-close'])
  check('a voided run-start does not set the override cut-off', r.out.includes('ACTION: stop') && r.out.includes('2026-08-28T11:00:00Z'), r.out.trim())
}
