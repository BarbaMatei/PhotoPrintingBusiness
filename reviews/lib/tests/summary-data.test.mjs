// Tests for summary-data.mjs: bullet fragments computed from one pass's metrics.jsonl line
// (owed manifest lenses, the new-serious trend, findings[] counts, budget skips, the
// pass-type-cap reminder, backlog filings) plus the missing-pass usage/exit-2 path.
//
// Usage: node reviews/lib/tests/run-tests.mjs --only summary-data
import { check, run } from './lib.mjs'
import { mkdtempSync, mkdirSync, writeFileSync, rmSync } from 'node:fs'
import { join } from 'node:path'
import { tmpdir } from 'node:os'

const jsonl = lines => lines.map(l => JSON.stringify(l)).join('\n') + '\n'

// ---------- core case: owed lenses, the trend across passes, findings[] counts, backlog ----------
{
  const T = mkdtempSync(join(tmpdir(), 'summary-data-core-'))
  const target = '970-summary-data-core'
  const dir = join(T, 'reviews', target)
  mkdirSync(dir, { recursive: true })
  writeFileSync(join(dir, 'metrics.jsonl'), jsonl([
    {
      target, pass: 1, type: 'discovery', date: '2026-08-20', commit: 'aaaaaa1',
      lenses: ['correctness', 'security', 'requirements', 'quality', 'tests-coverage', 'completeness-critic', 'db-parity', 'input-validation', 'observability', 'race', 'frontend-ux'],
      new_findings: { high: 1, medium: 2, low: 0, cleanup: 0 }, reopened: 0, verified: 0,
    },
    {
      target, pass: 2, type: 'discovery', date: '2026-08-24', commit: 'aaaaaa2',
      lenses: ['correctness', 'db-parity', 'frontend-ux', 'tests-coverage', 'completeness-critic'],
      new_findings: { high: 0, medium: 1, low: 2, cleanup: 1 }, reopened: 0, verified: 0,
      cost: { agents: 10, tokens: 500000, agents_by_stage: { lenses: 5, budget_skipped: 2 } },
      findings: [
        { f: 'F1', d: 'PPW-1', new: true, sev: 'medium', lenses: ['correctness'], conv: 1, hinted: true, verdict: 'confirmed', fix_generated: null, sev_delta: null },
        { f: 'F2', d: 'PPW-2', new: true, sev: 'low', lenses: ['tests-coverage'], conv: 1, hinted: false, verdict: 'unverified-needs-repro', fix_generated: null, sev_delta: null },
        { f: 'F3', d: 'PPW-3', new: true, sev: 'low', lenses: ['db-parity'], conv: 1, hinted: true, verdict: 'unverified-blocked-by-env', fix_generated: null, sev_delta: null },
      ],
    },
    // A pass after the one queried below — must never leak into that pass's trend.
    {
      target, pass: 3, type: 'discovery', date: '2026-08-27', commit: 'aaaaaa3',
      lenses: ['correctness'], new_findings: { high: 5, medium: 5, low: 0, cleanup: 0 }, reopened: 0, verified: 0,
    },
  ]))

  {
    const r = run('summary-data.mjs', ['--root', T, target, '2'])
    check('exits 0 for a pass with a metrics line', r.code === 0, `exit ${r.code}: ${r.out.trim()}`)
    check('reports the 6 lenses this scoped pass did not run, in manifest order',
      r.out.includes('- Owed manifest lenses: security, requirements, quality, input-validation, observability, race'), r.out.trim())
    check('the trend includes this pass and the one before it', r.out.includes('- v1: 1+2') && r.out.includes('- v2: 0+1'), r.out.trim())
    check('the trend excludes a later pass not yet reached', !r.out.includes('v3:'), r.out.trim())
    check('counts both unverified-* findings (F2, F3)', r.out.includes("- 2 unverified-* verdicts in this pass's findings"), r.out.trim())
    check('counts both hinted findings (F1, F3)', r.out.includes("- 2 hinted findings in this pass's findings"), r.out.trim())
    check('reads budget_skipped from cost.agents_by_stage', r.out.includes('- 2 budget-skipped agents this pass'), r.out.trim())
    check('a full discovery pass gets no pass-type-cap line', !r.out.includes('cannot certify'), r.out.trim())
    check('backlog filings sum new low + cleanup (2+1)', r.out.includes('- 3 new low/cleanup findings filed to backlog automatically'), r.out.trim())
  }
  {
    const r = run('summary-data.mjs', ['--root', T, target, '1'])
    check('exits 0 for the earlier pass', r.code === 0, `exit ${r.code}: ${r.out.trim()}`)
    check('a full-manifest pass reports no owed lenses', !r.out.includes('Owed manifest lenses'), r.out.trim())
    check('its trend stops at itself', r.out.includes('- v1: 1+2') && !r.out.includes('v2:'), r.out.trim())
    check('a pass with no findings[] key counts zero, not an error',
      r.out.includes("- 0 unverified-* verdicts in this pass's findings") && r.out.includes("- 0 hinted findings in this pass's findings"), r.out.trim())
    check('a pass with no cost field reports zero budget skips', r.out.includes('- 0 budget-skipped agents this pass'), r.out.trim())
    check('a pass with no new low/cleanup reports zero backlog filings', r.out.includes('- 0 new low/cleanup findings filed to backlog automatically'), r.out.trim())
  }
  rmSync(T, { recursive: true, force: true })
}

// ---------- pass-type cap: delta-discovery and verification lines ----------
{
  const T = mkdtempSync(join(tmpdir(), 'summary-data-captype-'))
  const target = '971-summary-data-captype'
  const dir = join(T, 'reviews', target)
  mkdirSync(dir, { recursive: true })
  writeFileSync(join(dir, 'metrics.jsonl'), jsonl([
    {
      target, pass: 5, type: 'delta-discovery', date: '2026-08-25', commit: 'bbbbbb1',
      lenses: ['correctness', 'security'], new_findings: { high: 0, medium: 0, low: 0, cleanup: 0 }, reopened: 0, verified: 0,
    },
    {
      target, pass: 6, type: 'verification', date: '2026-08-26', commit: 'bbbbbb2',
      lenses: null, new_findings: { high: 0, medium: 0, low: 0, cleanup: 0 }, reopened: 0, verified: 0,
    },
  ]))

  {
    const r = run('summary-data.mjs', ['--root', T, target, '5'])
    check('a delta-discovery pass gets the cap reminder', r.out.includes('- This pass type (delta-discovery) cannot certify.'), r.out.trim())
    check('a delta-discovery pass still counts toward the trend', r.out.includes('- v5: 0+0'), r.out.trim())
    check('owed lenses are still reported for a scoped pass type', r.out.includes('Owed manifest lenses:') && r.out.includes('race'), r.out.trim())
  }
  {
    const r = run('summary-data.mjs', ['--root', T, target, '6'])
    check('a verification pass gets the cap reminder', r.out.includes('- This pass type (verification) cannot certify.'), r.out.trim())
    check('verification is not a discovery-type line, so it never gets its own trend entry', !r.out.includes('v6:'), r.out.trim())
    check("the earlier delta-discovery pass's trend entry still shows through", r.out.includes('- v5: 0+0'), r.out.trim())
    check('null lenses means every manifest lens is owed', r.out.includes('Owed manifest lenses:') && r.out.includes('frontend-ux'), r.out.trim())
  }
  rmSync(T, { recursive: true, force: true })
}

// ---------- archive fallback: a target only present under reviews/archive ----------
{
  const T = mkdtempSync(join(tmpdir(), 'summary-data-archive-'))
  const target = '975-summary-data-archived'
  const dir = join(T, 'reviews', 'archive', target)
  mkdirSync(dir, { recursive: true })
  writeFileSync(join(dir, 'metrics.jsonl'), jsonl([
    { target, pass: 1, type: 'discovery', date: '2026-07-01', commit: 'ccccccc',
      lenses: ['correctness', 'security', 'requirements', 'quality', 'tests-coverage', 'completeness-critic', 'db-parity', 'input-validation', 'observability', 'race', 'frontend-ux'],
      new_findings: { high: 2, medium: 0, low: 0, cleanup: 0 }, reopened: 0, verified: 0 },
  ]))
  const r = run('summary-data.mjs', ['--root', T, target, '1'])
  check('an archived target is found via the archive fallback', r.code === 0 && r.out.includes('- v1: 2+0'), `exit ${r.code}: ${r.out.trim()}`)
  rmSync(T, { recursive: true, force: true })
}

// ---------- usage / exit 2: no metrics line for the requested pass ----------
{
  const T = mkdtempSync(join(tmpdir(), 'summary-data-missing-'))
  const target = '972-summary-data-missing'
  const dir = join(T, 'reviews', target)
  mkdirSync(dir, { recursive: true })
  writeFileSync(join(dir, 'metrics.jsonl'), jsonl([
    { target, pass: 1, type: 'discovery', date: '2026-08-20', commit: 'ddddddd', lenses: [], new_findings: { high: 0, medium: 0, low: 0, cleanup: 0 }, reopened: 0, verified: 0 },
  ]))

  {
    const r = run('summary-data.mjs', ['--root', T, target, '99'])
    check('exits 2 when the requested pass has no metrics line', r.code === 2, `exit ${r.code}: ${r.out.trim()}`)
    check('prints the usage line', r.out.includes('usage: node reviews/lib/summary-data.mjs'), r.out.trim())
  }
  {
    const r = run('summary-data.mjs', ['--root', T])
    check('exits 2 with no target/pass arguments at all', r.code === 2, `exit ${r.code}: ${r.out.trim()}`)
  }
  {
    const r = run('summary-data.mjs', ['--root', T, target, 'not-a-number'])
    check('exits 2 when the pass argument is not numeric', r.code === 2, `exit ${r.code}: ${r.out.trim()}`)
  }
  {
    const r = run('summary-data.mjs', ['--root', T, 'no-such-target', '1'])
    check('exits 2 when the target has no metrics.jsonl at all', r.code === 2, `exit ${r.code}: ${r.out.trim()}`)
  }
  rmSync(T, { recursive: true, force: true })
}
