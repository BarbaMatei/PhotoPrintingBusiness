// Tests for autonomy-policy.mjs: the decide subcommand across gate kinds.
//
// Usage: node reviews/lib/tests/run-tests.mjs --only autonomy-policy
import { check, run, GOOD_ROOT, DRIVE_STATES } from '../lib.mjs'
import { buildTarget, fixRound } from '../fixture-builder.mjs'
import { mkdtempSync, mkdirSync, writeFileSync, rmSync } from 'node:fs'
import { join } from 'node:path'
import { tmpdir } from 'node:os'
import { fixedRows } from '../../records/resolution.mjs'
import { MANIFEST_LENSES } from '../../records/schema.mjs'

// ---------- autonomy-policy: decide ----------
{
  const r = run('drive/autonomy-policy.mjs', ['--root', GOOD_ROOT, '910-delta-worthy', 'decide', 'delta-worthiness'])
  check('policy auto-routes a blocker-fixing round to delta discovery', r.code === 0 && r.out.includes('ACTION: auto') && r.out.includes('NEXT: delta discovery'), r.out.trim())
  check('policy names the fixed blocker in its reason', r.out.includes('PPW-9910'), r.out.trim())
}
{
  const r = run('drive/autonomy-policy.mjs', ['--root', GOOD_ROOT, '911-patch-grade', 'decide', 'delta-worthiness'])
  check('policy routes a patch-grade round to a first certification pair', r.code === 0 && r.out.includes('ACTION: auto') && r.out.includes('NEXT: certification (pair)'), r.out.trim())
}
{
  const r = run('drive/autonomy-policy.mjs', ['--root', GOOD_ROOT, '912-recert', 'decide', 'delta-worthiness'])
  check('policy routes a re-certification as a single pass', r.code === 0 && r.out.includes('NEXT: certification (single)'), r.out.trim())
}
{
  const r = run('drive/autonomy-policy.mjs', ['--root', DRIVE_STATES, '913-loop-quiet', 'decide', 'certification-go-ahead'])
  check('policy answers a clean discovery loop-quiet gate with a first certification pair', r.code === 0 && r.out.includes('ACTION: auto') && r.out.includes('NEXT: certification (pair)'), r.out.trim())
}
{
  const r = run('drive/autonomy-policy.mjs', ['--root', GOOD_ROOT, '912-recert', 'decide', 'certification-go-ahead'])
  check('policy answers a loop-quiet gate for a re-certified target with a single pass', r.code === 0 && r.out.includes('NEXT: certification (single)'), r.out.trim())
}
{
  const r = run('drive/autonomy-policy.mjs', ['--root', DRIVE_STATES, '914-resolution-above-review', 'decide', 'delta-worthiness'])
  check('policy judges the newest resolution, not the one paired with the newest review', r.code === 0 && r.out.includes('NEXT: certification (pair)'), r.out.trim())
  check('policy calls a round with no review file of its own patch-grade', r.out.includes('patch-grade'), r.out.trim())
}
{
  const r = run('drive/autonomy-policy.mjs', ['--root', DRIVE_STATES, '909-certified-target', 'decide', 'loop-close'])
  check('policy closes the loop under the standing approval', r.out.includes('ACTION: auto') && r.out.includes('NEXT: close the loop'), r.out.trim())
}
{
  const r = run('drive/autonomy-policy.mjs', ['--root', DRIVE_STATES, '909-certified-target', 'decide', 'mystery-gate'])
  check('policy fails closed on an unknown gate kind', r.out.includes('ACTION: stop'), r.out.trim())
}

{
  const root = buildTarget({
    target: '925-lens-debt', reviews: 1,
    metricsLines: [
      { pass: 1, type: 'discovery', lenses: MANIFEST_LENSES.slice(0, -1), date: '2026-08-20', commit: 'eeeee15', verdict: 'approve-with-followups', new_findings: { high: 0, medium: 0, low: 0, cleanup: 0 }, reopened: 0, verified: 0 },
    ],
  })
  const r = run('drive/autonomy-policy.mjs', ['--root', root, '925-lens-debt', 'decide', 'certification-go-ahead'])
  check('policy refuses auto-certification on lens debt and routes the owed lens', r.out.includes('ACTION: auto') && r.out.includes('NEXT: lens-coverage discovery (frontend-ux)'), r.out.trim())
}

{
  const root = buildTarget({
    target: '926-unmeasured-seed', reviews: 1,
    metricsLines: [
      { pass: 1, type: 'discovery', lenses: MANIFEST_LENSES, date: '2026-08-20', commit: 'eeeee16', verdict: 'request-changes', new_findings: { high: 0, medium: 1, low: 0, cleanup: 0 }, reopened: 0, verified: 0 },
      fixRound({ round: 1, date: '2026-08-21', fixed: 2, invocations: 3 }),
      { pass: 1, type: 'verification', date: '2026-08-22', commit: 'eeeee17', verdict: 'approve-with-followups', new_findings: { high: 0, medium: 0, low: 0, cleanup: 0 }, reopened: 0, verified: 2 },
    ],
    resolutions: [{ v: 1, status: 'resolved', fixedCommit: 'eeeee17', fixed: ['PPW-9601', 'PPW-9602'] }],
  })
  const r = run('drive/autonomy-policy.mjs', ['--root', root, '926-unmeasured-seed', 'decide', 'delta-worthiness'])
  check('policy routes an unmeasured final round to a measuring delta discovery', r.out.includes('ACTION: auto') && r.out.includes('NEXT: delta discovery') && r.out.includes('unmeasured'), r.out.trim())
}

const NON_CONVERGENT_ROUNDS = [
  fixRound({ round: 1, date: '2026-07-02', fixed: 3, invocations: 4 }),
  fixRound({ round: 2, date: '2026-07-04', fixed: 2, invocations: 3 }),
]
const nonConvergent = ({ target, reviews, seeds, commits, ledgerRows, tailPass }) => buildTarget({
  target, reviews, ledgerRows,
  metricsLines: [
    { pass: 1, type: 'discovery', lenses: ['correctness'], date: '2026-07-01', commit: commits[0], verdict: 'request-changes', new_findings: { high: 1, medium: 1, low: 0, cleanup: 0 }, reopened: 0, verified: 0 },
    NON_CONVERGENT_ROUNDS[0],
    {
      pass: 2, type: 'delta-discovery', lenses: ['correctness'], date: '2026-07-03', commit: commits[1], verdict: 'request-changes', new_findings: { high: 1, medium: 1, low: 0, cleanup: 0 },
      findings: [{ f: 'F1', d: seeds[0], new: true, sev: 'high', seed_round: 1, area: 'payments' }], reopened: 0, verified: 0,
    },
    NON_CONVERGENT_ROUNDS[1],
    {
      pass: 3, type: 'delta-discovery', lenses: ['correctness'], date: '2026-07-05', commit: commits[2], verdict: 'request-changes', new_findings: { high: 1, medium: 0, low: 0, cleanup: 0 },
      findings: [{ f: 'F1', d: seeds[1], new: true, sev: 'high', seed_round: 2, area: 'payments' }], reopened: 0, verified: 0,
    },
    ...(tailPass ? [tailPass] : []),
  ],
  resolutions: [
    { v: 1, status: 'resolved', fixedCommit: commits[1], fixed: ['PPW-9294', 'PPW-9295', 'PPW-9296'] },
    { v: 2, status: 'resolved', fixedCommit: commits[2], fixed: ['PPW-9297', 'PPW-9298'] },
  ],
})

{
  const root = nonConvergent({
    target: '927-non-convergent', reviews: 3, seeds: ['PPW-9711', 'PPW-9713'],
    commits: ['ccccc17', 'ccccc18', 'ccccc19'],
  })
  const r = run('drive/autonomy-policy.mjs', ['--root', root, '927-non-convergent', 'decide', 'design-pass'])
  check('policy stops on a design-pass gate', r.out.includes('ACTION: stop'), r.out.trim())
}

// The policy answers a fix round of its own at both certification-bound gates, so the brake has to
// guard those answers too (owner ruling 1, 2026-08-28) — and a design pass has no written
// delegation, so the policy fails closed and stops.
{
  const root = nonConvergent({
    target: '931-sweep-non-convergent', reviews: 4, seeds: ['PPW-9312', 'PPW-9313'],
    commits: ['ddd9497', 'ddd9498', 'ddd9499'], ledgerRows: [['PPW-9311', '🟠', 'open']],
    tailPass: { pass: 4, type: 'delta-discovery', lenses: ['correctness'], date: '2026-07-07', commit: 'ddd949a', verdict: 'approve-with-followups', new_findings: { high: 0, medium: 0, low: 0, cleanup: 0 }, reopened: 0, verified: 0 },
  })
  for (const gate of ['delta-worthiness', 'certification-go-ahead']) {
    const r = run('drive/autonomy-policy.mjs', ['--root', root, '931-sweep-non-convergent', 'decide', gate])
    check(`policy stops instead of sweeping a non-convergent component at the ${gate} gate`,
      r.out.includes('ACTION: stop') && !r.out.includes('NEXT: fix round'), r.out.trim())
    check(`the ${gate} stop names the component and the two rounds`,
      r.out.includes('"payments"') && r.out.includes('rounds r1 and r2'), r.out.trim())
  }
}
{
  const root = nonConvergent({
    target: '929-armed-non-convergent', reviews: 3, seeds: ['PPW-9292', 'PPW-9293'],
    commits: ['ddd9297', 'ddd9298', 'ddd9299'], ledgerRows: [['PPW-9291', '🔴', 'open']],
  })
  const r = run('drive/autonomy-policy.mjs', ['--root', root, '929-armed-non-convergent', 'decide', 'certification-go-ahead'])
  check('policy stops rather than arming a fix round on a non-convergent component',
    r.out.includes('ACTION: stop') && r.out.includes('"payments"'), r.out.trim())
  // The stop is the owner's whole report in an unattended run, so it must say what is waiting:
  // an open blocker and one queued medium must not read the same.
  check('the stop carries the work that was waiting behind it, ids and severity included',
    r.out.includes('waiting behind it: the loop is armed — 1 open 🔴 (PPW-9291)'), r.out.trim())
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
  check('a convergent armed ledger still gets the policy fix round, not a stop',
    r.out.includes('ACTION: auto') && r.out.includes('NEXT: fix round'), r.out.trim())
}

{
  const r = run('drive/autonomy-policy.mjs', ['--root', DRIVE_STATES, '919-override-stop', 'decide', 'loop-close'])
  check('policy stops when a gate override was logged after the run started', r.out.includes('ACTION: stop') && r.out.includes('COMMENTS_OK'), r.out.trim())
}

{
  const r = run('drive/autonomy-policy.mjs', ['--root', DRIVE_STATES, '919-override-clean', 'decide', 'loop-close'])
  check('policy ignores overrides logged before the run started', r.out.includes('ACTION: auto'), r.out.trim())
}

// The policy reads the resolution's `fixed` rows through the same reader as the verifier and the
// hand-back gates, so a row whose Commit cell is never closed states no fix for either of them: a
// half-written row must not be the thing that decides a delta discovery.
{
  const T = mkdtempSync(join(tmpdir(), 'policy-truncated-row-'))
  const target = '957-truncated-fixed-row'
  const dir = join(T, 'reviews', target)
  mkdirSync(dir, { recursive: true })
  writeFileSync(join(dir, 'review-v1.md'), `---
type: review
target: ${target}
version: 1
pass-type: discovery
date: 2026-08-30
verdict: request-changes
blockers: [PPW-9570]
---

# Review v1 — ${target}
`)
  const resolution = `---
type: resolution
target: ${target}
version: 1
answers: review-v1.md
status: resolved
fixed_commit: ffffff1
---

# Resolution v1 — ${target}

## Findings

| ID | Status | Commit | Note |
|---|---|---|---|
| PPW-9570 | fixed | \`ffffff1\`
`
  writeFileSync(join(dir, 'resolution-v1.md'), resolution)
  const r = run('drive/autonomy-policy.mjs', ['--root', T, target, 'decide', 'delta-worthiness'])
  check('the policy and the shared reader agree that a truncated row states no fix',
    fixedRows(resolution).length === 0 && !r.out.includes('NEXT: delta discovery') && !r.out.includes('PPW-9570'),
    `${JSON.stringify(fixedRows(resolution))} · ${r.out.trim()}`)
  rmSync(T, { recursive: true, force: true })
}

// A mis-stamped run-start would push the override cut-off past a real override and hide it.
{
  const r = run('drive/autonomy-policy.mjs', ['--root', GOOD_ROOT, '956-override-voided-run-start', 'decide', 'loop-close'])
  check('a voided run-start does not set the override cut-off', r.out.includes('ACTION: stop') && r.out.includes('2026-08-28T11:00:00Z'), r.out.trim())
}
