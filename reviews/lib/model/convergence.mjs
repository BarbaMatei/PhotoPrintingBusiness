// The convergence rule (2026-08-28, accepted fix-round audit) as one implementation: which
// manifest lenses a target still owes, whether the last substantive fix round's seed rate has
// been measured at all, and whether two consecutive rounds have declared a component
// non-convergent. The router and the autonomy policy both refuse certification on the first two
// and both brake a fix round on the third, and every one of those refusals used to be written
// twice — the two copies drifting is the defect this module exists to make impossible.
// Reads nothing: the caller hands in the target's metrics pass/fix-round lines in file order.
// README's note ³ keeps the why and the prose; the numbers are here.
import { MANIFEST_LENSES } from '../records/schema.mjs'

// Seed rate at or above which a round counts as seeding its component, on both of two
// consecutive rounds, before patching is declared non-convergent there.
const SEED_RATE = 0.3

// A blind pass is the only pass that can attribute a new finding to an earlier round's commits.
export const isBlind = l => l.type === 'discovery' || l.type === 'delta-discovery'

// A round is substantive when it fixed at least one finding and ran tests.
export const isSubstantive = l => l.type === 'fix-round' && (l.findings?.fixed ?? 0) > 0 && (l.tests?.invocations ?? 0) > 0

// Manifest lenses that have never run on this target, in manifest order.
export function owedLenses(lines) {
  const union = new Set()
  for (const l of lines) if (Array.isArray(l.lenses)) for (const k of l.lenses) union.add(k)
  return MANIFEST_LENSES.filter(k => !union.has(k))
}

// Index of the newest substantive fix round, or -1 when the target has none.
export function lastSubstantive(lines) {
  for (let i = lines.length - 1; i >= 0; i--) if (isSubstantive(lines[i])) return i
  return -1
}

// A round's seed rate is measured only once a blind pass has run after it; a target with no
// substantive round has nothing to measure, which counts as measured.
export const seedMeasured = (lines, i = lastSubstantive(lines)) =>
  i === -1 || lines.some((l, j) => j > i && isBlind(l))

// round → area → count of the new serious findings a later blind pass attributed to that round.
// A finding with no `seed_round` is not yet measured, never zero, so it is not counted here.
export function seedsByRound(lines) {
  const seeds = new Map()
  for (const l of lines) if (isBlind(l) && Array.isArray(l.findings)) for (const f of l.findings) {
    if (f.new !== true || (f.sev !== 'high' && f.sev !== 'medium') || !Number.isFinite(f.seed_round)) continue
    if (!seeds.has(f.seed_round)) seeds.set(f.seed_round, new Map())
    const m = seeds.get(f.seed_round)
    const a = typeof f.area === 'string' ? f.area : '(unstated)'
    m.set(a, (m.get(a) || 0) + 1)
  }
  return seeds
}

// What the rule says about the two newest substantive rounds, or null when there are not two.
// `notShrinking` is the warning (each round should be strictly smaller than the one before);
// `nonConvergent` is the brake, and `capped` says its one design pass per component per loop
// already ran, which lets the round proceed with a note instead of a gate.
export function convergenceCheck(lines) {
  const subs = lines.filter(isSubstantive)
  if (subs.length < 2) return null
  const [r1, r2] = subs.slice(-2)
  const seeds = seedsByRound(lines)
  const rate = r => {
    const m = seeds.get(r.round)
    let n = 0
    if (m) for (const c of m.values()) n += c
    return { s: n / r.findings.fixed, areas: new Set(m ? m.keys() : []) }
  }
  const a1 = rate(r1), a2 = rate(r2)
  const out = { r1, r2, notShrinking: r2.findings.fixed >= r1.findings.fixed, nonConvergent: false, area: null, s1: a1.s, s2: a2.s, capped: false }
  if (a1.s < SEED_RATE || a2.s < SEED_RATE) return out
  // An "(unstated)" area cannot be named in a design pass, so it never brakes one.
  const common = [...a2.areas].filter(a => a !== '(unstated)' && a1.areas.has(a))
  if (!common.length) return out
  out.nonConvergent = true
  out.area = common[0]
  out.capped = lines.some(l => l.type === 'fix-round' && typeof l.notes === 'string' && l.notes.includes(`design-pass:${out.area}`))
  return out
}

// Why certification may not be launched yet, or null. Same two refusals in both drivers.
export function certificationBlocker(lines) {
  const owed = owedLenses(lines)
  if (owed.length) return { next: `lens-coverage discovery (${owed[0]})`, reason: `certification refused on lens-coverage debt — never run on this target: ${owed.join(', ')} (audit R5)` }
  const i = lastSubstantive(lines)
  if (i !== -1 && !seedMeasured(lines, i))
    return { next: 'delta discovery', reason: `round r${lines[i].round}'s seed rate is unmeasured — no blind pass has run since it; a delta discovery measures it before certification (convergence rule, 2026-08-28)` }
  return null
}
