export const meta = {
  name: 'discovery-review',
  description: 'Reusable multi-lens blinded discovery review: lenses -> in-pass dedup -> convergence-weighted adversarial verify -> return for synthesis',
  phases: [
    { title: 'Discovery', detail: 'blinded isolated lenses over the whole feature' },
    { title: 'Dedup', detail: 'one agent merges same-defect findings across lenses into canonical findings + convergence counts, flagging hint-planted topics' },
    { title: 'Verify', detail: 'adversarial skeptics, tiered by severity AND convergence' },
  ],
}

// ─────────────────────────────────────────────────────────────────────────────
// HOW TO RUN (the main agent scopes, then invokes this script):
//   1. Confirm HEAD == origin/<branch>. Save the source diff(s) to temp files.
//   2. (Token win #4) Optionally assemble a `codePack`: the FULL text of the changed files +
//      their key collaborators (callers, cleanup jobs, middleware, config), read ONCE. Pass it in
//      args so the lenses/skeptics don't each re-read the same files.
//   3. Pick the manifest lenses for the change (LENS_LIBRARY keys below).
//   4. Workflow({ scriptPath: 'reviews/lib/discovery-review.wf.js', args: {
//        target, repoRoot, scope, changedFiles, backendDiff, frontendDiff?, specDocs?, lenses?, codePack?
//      }})
//   Returns [{lens, overall, findings:[...]}] where each finding carries: verdict (confirmed |
//   plausible | refuted | disputed | unverified-cleanup), convergence, hinted, agreeingLenses,
//   guardEvidence, traceEvidence. The '_canonical' overall line reports skeptic-run counts for
//   the metrics entry (cost.agents_by_stage). The main agent then synthesizes.
//
// TOKEN-EFFICIENCY MEASURES BAKED IN (see reviews/README.md "Cost discipline"):
//   #1 Dedup-before-verify: all lenses run, then ONE dedup agent clusters duplicates; each real
//      defect is verified once, not once-per-lens-that-found-it. (Distinct from the cross-pass
//      "ledger reconciler" of the self-driving loop, which does not exist yet.)
//   #2 Convergence-weighted verify: a defect >=3 lenses independently found barely needs a skeptic
//      (agreement IS the precision signal) -> 1 anti-groupthink check; low-convergence findings get
//      the full tier. Hint-planted agreement (hinted=true) does NOT earn the discount — shared
//      prompt context manufactures convergence.
//   #3 Output caps: lenses and skeptics are told to keep prose short (verdict + brief reason), since
//      output tokens dominate cost and synthesis only needs the gist.
//   #4 Read-once codePack: files handed to agents via args instead of re-read 100x (best with prompt
//      caching across the fan-out; measure — its payoff depends on shared caching).
// NOTE: the sandbox has no git/fs access, so scoping (diff, file list, codePack, manifest) is the
// caller's job via `args`. Lens prompts are GENERIC by category; change detail comes from the diff/pack.
// ─────────────────────────────────────────────────────────────────────────────

const a = args || {}
const REPO = a.repoRoot || '.'
const TARGET = a.target || '(unnamed target)'
const SCOPE = a.scope || '(no scope summary supplied)'
const CHANGED = a.changedFiles || '(no changed-file list supplied — search the repo from the diff)'
const BACKEND_DIFF = a.backendDiff || '(none)'
const FRONTEND_DIFF = a.frontendDiff || '(none)'
const SPEC_DOCS = a.specDocs || '(no spec/story docs supplied)'
const CODEPACK = a.codePack || ''

const DEFAULT_LENSES = ['correctness', 'security', 'requirements', 'quality', 'tests-coverage', 'completeness-critic']
const SELECTED = Array.isArray(a.lenses) && a.lenses.length ? a.lenses : DEFAULT_LENSES

const BRIEF = `BE CONCISE: keep each field short. failureScenario <= 60 words, suggestedFix <= 40 words. State the defect, not an essay.`

// #4: hand agents the code once. A stable top-of-prompt prefix maximizes any shared prompt caching.
const PACK = CODEPACK
  ? `\n\nRELEVANT SOURCE (provided so you do NOT re-read these files — grep only for something NOT included here):\n${CODEPACK}\n`
  : ''

const EXPLORE = CODEPACK
  ? `Work from the RELEVANT SOURCE provided above; open/grep additional files only if you need something not included.`
  : `Do NOT limit yourself to the diff — open the full changed files below AND grep the repo for their call sites / collaborators. The highest-value defects live in the interaction between changed and UNCHANGED code.`

// Shared hints seeded into EVERY lens — a recall aid, but agreement on these topics is not
// independent evidence (the dedup agent flags findings these hints planted).
const HINTS = `dual database — SQLite for local/dev/test, PostgreSQL for prod; tests use the EF
InMemory provider (so migration DDL is usually NOT exercised). Storage is behind IStorageService
(local today; a cloud provider is a planned follow-up). Auth supports logged-in users AND anonymous guests.`

const BASE = `You are ONE lens in a multi-lens DISCOVERY code review of the feature branch under review.
Repo root / working dir: "${REPO}". Target: ${TARGET}.

WHAT THE BRANCH CHANGES:
${SCOPE}

Diff(s) for orientation: backend = "${BACKEND_DIFF}", frontend = "${FRONTEND_DIFF}".
${PACK}
DISCOVERY SCOPE — ${EXPLORE}

CHANGED FILES:
${CHANGED}

BLINDING (critical): do NOT read anything under the "reviews/" directory. This is an unbiased blinded pass.

PROJECT CONTEXT: ${HINTS}

OUTPUT CONTRACT: return ONLY via the structured schema. Per finding: file, line, severity, one-line
title, a CONCRETE failure scenario (inputs/state/timing -> wrong result/crash/cost), suggested fix,
confidence 1-10. Severity: high = exploitable / breaks the core promise / data loss; medium = real
impact under specific-but-realistic conditions; low = defence-in-depth / edge / parity; cleanup =
quality only. Report only real, justified issues; empty array if none. ${BRIEF}`

const LENS_LIBRARY = {
  correctness: `CORRECTNESS & concurrency of the changed logic: edge inputs, null/empty paths, boundary
(> vs >=), removed/weakened guards, resource & stream lifetime (disposal, double-dispose, leaks),
broken/unupdated call sites, and check-then-act / TOCTOU races on shared state (DB rows, files, caches).`,

  security: `SECURITY across the change. Confidence 1-10; report anything >=7 that is real. Auth/authz
bypass or mis-scoping, IDOR, cross-user/cross-tenant exposure, cache directives that leak per-user data
to shared caches, injection, secret/PII in logs/responses, replay/double-charge, DoS (unbounded
allocation, missing size/rate/quantity caps). Construct the concrete exploit where one exists.`,

  requirements: `PR / REQUIREMENTS at the contract level. Read the spec/story docs:
${SPEC_DOCS}
Check every acceptance criterion is actually delivered (not silently substituted or descoped),
docs/comments match code, new artifacts are wired into their WHOLE lifecycle (creation AND
cleanup/deletion), and no undocumented scope is bundled with no story/AC/test.`,

  quality: `QUALITY / ALTITUDE — reuse, simplification, efficiency, right-layer. REPORT ONLY. Mostly
"cleanup" unless a real efficiency cost (then low/medium): avoidable hot-path work (redundant queries,
tracking a read, re-reading just-written data, extra round-trips), duplicated logic/constants/strings,
magic numbers, wrong-layer fixes.`,

  'db-parity': `DB / MIGRATION-PARITY (dual SQLite+Postgres). Shared vs per-provider migrations? Does a
new migration hardcode a SQLite type ("TEXT") that diverges on Postgres (unbounded text vs varchar(N),
maxLength unenforced, snapshot drift -> phantom AlterColumn diff)? Compare to sibling migrations. Is the
DDL exercised by any test, or only InMemory/EnsureCreated (which skips migrations)?`,

  'input-validation': `INPUT-VALIDATION depth. For image/file input: per-axis dimension cap (misses
total-megapixel bombs) vs real area budget? Frame count for animated formats bounded? Fail-open vs
fail-closed on unreadable/null identify? Magic-byte checks that over-accept? Trim/length/encoding of new
fields/headers? Construct the crafted input that slips through.`,

  observability: `OBSERVABILITY of new error/side-effecting paths. Is a new log line above the configured
minimum level (a Debug line under an Information floor never emits)? Are distinct incident types (attack
vs benign failure) distinguishable, or collapsed into one message? Are swallowed/caught exceptions logged?
Is a partial-state failure (file written, DB not committed) observable?`,

  race: `RACE / CONCURRENCY / IDEMPOTENCY on new write/cache-fill paths. Concurrent requests for the same
entity: check-then-act windows, non-idempotent writes (fresh random key each time), non-atomic file+DB
writes, missing optimistic-concurrency token (silent last-writer-wins), crash/cancel between steps. State
the concrete leak/lost-update/orphan and whether cleanup can reclaim it. Read-replica hazard if a GET now writes.`,

  'frontend-ux': `FRONTEND correctness + UX (read "${FRONTEND_DIFF}"/full files). Interceptors/guards/auth
tokens: which branch runs per user type (logged-in vs guest vs anon-no-token vs expired-but-present)?
In-flight dedup of async inits? Is a claimed self-heal/retry actually seamless or does it cost a visible
failed attempt? RxJS races, lost retries, dead-end redirects, state wiped on transient errors.`,

  'tests-coverage': `TESTS & VERIFICATION — "green != proven". Do NOT run the suite. For each failure mode
the code can hit, ask: which test goes RED if this bug is injected? Enumerate untested failure modes.
Flag tests that pass for the wrong reason (shared DbContext hiding a missing SaveChanges; the real
component mocked so its guard never runs; asserting presence not value) and any migration/provider path
the suite can't reach. Propose the specific regression test per gap.`,

  'completeness-critic': `COMPLETENESS CRITIC — name what this review is LIKELY to UNDER-review (blind
spots, not obvious bugs): which entry point / provider / unchanged collaborator got less scrutiny than
the happy path? Which asserted claim is unverified? Which manifest lens is missing/shallow for THIS
change? Any changed file no lens owns? Give each a concrete "what to check next".`,
}

// ── Schemas ───────────────────────────────────────────────────────────────────
const FINDINGS_SCHEMA = {
  type: 'object', additionalProperties: false,
  properties: {
    lens: { type: 'string' },
    overall: { type: 'string', description: 'one or two line summary of what this lens concluded' },
    findings: {
      type: 'array',
      items: {
        type: 'object', additionalProperties: false,
        properties: {
          file: { type: 'string' }, line: { type: 'integer' },
          severity: { type: 'string', enum: ['high', 'medium', 'low', 'cleanup'] },
          title: { type: 'string' },
          failureScenario: { type: 'string', description: 'concrete inputs/state/timing -> wrong result; <= 60 words' },
          suggestedFix: { type: 'string', description: '<= 40 words' },
          confidence: { type: 'integer' },
        },
        required: ['file', 'severity', 'title', 'failureScenario', 'suggestedFix', 'confidence'],
      },
    },
  },
  required: ['lens', 'overall', 'findings'],
}
const DEDUP_SCHEMA = {
  type: 'object', additionalProperties: false,
  properties: {
    groups: {
      type: 'array',
      description: 'partition ALL finding ids into groups; same-defect ids share a group; a unique finding is a group of one',
      items: {
        type: 'object', additionalProperties: false,
        properties: {
          memberIds: { type: 'array', items: { type: 'integer' } },
          representativeId: { type: 'integer', description: 'the clearest member to quote' },
          severity: { type: 'string', enum: ['high', 'medium', 'low', 'cleanup'] },
          canonicalTitle: { type: 'string' },
          hinted: { type: 'boolean', description: 'true if the finding topic was planted by the shared PROJECT CONTEXT hints rather than discovered from the code alone' },
        },
        required: ['memberIds', 'representativeId', 'severity', 'canonicalTitle', 'hinted'],
      },
    },
  },
  required: ['groups'],
}
const GUARD_SCHEMA = {
  type: 'object', additionalProperties: false,
  properties: { guardExists: { type: 'boolean' }, evidence: { type: 'string', description: '<= 80 words: the guard (file:line) or why none' } },
  required: ['guardExists', 'evidence'],
}
const TRACE_SCHEMA = {
  type: 'object', additionalProperties: false,
  properties: { traceConstructible: { type: 'boolean' }, trace: { type: 'string', description: '<= 80 words: the concrete failing steps, or why impossible' } },
  required: ['traceConstructible', 'trace'],
}

// PACK leads the prompt so every skeptic shares a stable prefix (prompt-cache friendly — the
// skeptic layer is where most agents run); the varying finding text comes after it.
const findingCtx = (f) => `${PACK ? PACK.trimStart() + '\n' : ''}A code-review finding to adversarially check. Repo root: "${REPO}".
FINDING — ${f.title}
  file: ${f.file}:${f.line ?? '?'}  severity: ${f.severity}  (independently raised by ${f.convergence} lens(es))
  failure scenario: ${f.failureScenario}
Judge whether this is REAL against the code. Do NOT read anything under reviews/. Keep your answer <= 80 words.`

let guardRuns = 0, traceRuns = 0
const guardAgent = (f, i) => {
  guardRuns++
  return agent(findingCtx(f) + `\n\nROLE: skeptic — hunt for an EXISTING guard/check/invariant that already PREVENTS this. guardExists=true only for a genuine guard (with file:line); a partial guard that misses THIS case is false.`,
    { label: `guard#${i}`, phase: 'Verify', schema: GUARD_SCHEMA })
}
const traceAgent = (f, i) => {
  traceRuns++
  return agent(findingCtx(f) + `\n\nROLE: skeptic — try to CONSTRUCT a concrete failing execution from the real code (inputs/state/timing -> the claimed wrong result). traceConstructible=true with the steps if you can; false with the reason if impossible.`,
    { label: `trace#${i}`, phase: 'Verify', schema: TRACE_SCHEMA })
}

// #2 convergence-weighted verification tiers.
async function verifyFinding(f, i) {
  if (f.severity === 'cleanup') {
    return { ...f, verdict: 'unverified-cleanup', guardEvidence: '(cleanup — not verified)', traceEvidence: '(cleanup — not verified)' }
  }
  // Strong cross-lens agreement is itself the precision signal: one anti-groupthink guard-hunt,
  // escalate to a trace only if it surprisingly claims a guard (contradicting the crowd).
  // Hint-planted agreement doesn't count — shared prompt context manufactures convergence.
  if (f.convergence >= 3 && !f.hinted) {
    const guard = await guardAgent(f, i)
    if (!guard?.guardExists) return { ...f, verdict: 'confirmed', guardEvidence: guard?.evidence ?? '(skeptic failed)', traceEvidence: `(spot-check: ${f.convergence} lenses agreed, no guard found)` }
    const trace = await traceAgent(f, i) // surprise: guard claimed despite consensus -> adjudicate
    return { ...f, verdict: trace?.traceConstructible ? 'confirmed' : 'refuted', guardEvidence: guard.evidence, traceEvidence: trace?.trace ?? '(skeptic failed)' }
  }
  if (f.severity === 'low') {
    const trace = await traceAgent(f, i)
    if (trace?.traceConstructible) return { ...f, verdict: 'confirmed', guardEvidence: '(low — guard-hunt skipped; trace built)', traceEvidence: trace.trace }
    const guard = await guardAgent(f, i)
    return { ...f, verdict: guard?.guardExists ? 'refuted' : 'plausible', guardEvidence: guard?.evidence ?? '(skeptic failed)', traceEvidence: trace?.trace ?? '(skeptic failed)' }
  }
  // low-convergence (or hinted) high | medium: full independent pair
  const [guard, trace] = await Promise.all([guardAgent(f, i), traceAgent(f, i)])
  let verdict = 'plausible'
  if (guard?.guardExists && trace?.traceConstructible) verdict = 'disputed' // conflicting evidence — surface it, don't average it
  else if (guard?.guardExists && !trace?.traceConstructible) verdict = 'refuted'
  else if (trace?.traceConstructible && !guard?.guardExists) verdict = 'confirmed'
  return { ...f, verdict, guardEvidence: guard?.evidence ?? '(skeptic failed)', traceEvidence: trace?.trace ?? '(skeptic failed)' }
}

const SEV_RANK = { high: 3, medium: 2, low: 1, cleanup: 0 }

// ── Phase 1: Discovery (barrier so we can dedup across all lenses) ─────────────
const lensDefs = SELECTED.map(key => ({ key, focus: LENS_LIBRARY[key] })).filter(l => l.focus)
if (!lensDefs.length) { log('No valid lenses selected; nothing to do.'); return [] }
log(`Discovery review of ${TARGET}: ${lensDefs.length} lenses -> reconcile -> convergence-weighted verify.`)

const lensResults = await parallel(lensDefs.map(lens => () =>
  agent(`${BASE}\n\nYOUR LENS FOCUS (report ONLY through this lens; others cover the rest):\n${lens.focus}`,
    { label: lens.key, phase: 'Discovery', schema: FINDINGS_SCHEMA }).then(r => ({ lens: lens.key, r }))))

// Flatten + assign stable ids.
const flat = []
for (const lr of lensResults.filter(Boolean)) {
  for (const f of (lr.r?.findings || [])) flat.push({ id: flat.length, lens: lr.lens, ...f })
}
if (!flat.length) { log('No findings.'); return lensResults.filter(Boolean).map(lr => ({ lens: lr.lens, overall: lr.r?.overall || '', findings: [] })) }

// ── Phase 2: Dedup (#1) — one cheap agent clusters same-defect findings ──
const digest = flat.map(f => `#${f.id} [${f.lens}] ${f.severity} ${f.file}:${f.line ?? '?'} — ${f.title}`).join('\n')
const recon = await agent(
  `You are the DEDUP agent for a multi-lens review of ${TARGET}. Below are ${flat.length} raw findings from independent lenses. Group findings that describe the SAME underlying defect (same root cause + location), even if worded differently or a few lines apart. A finding with no duplicate is its own group of one. EVERY id must appear in exactly one group. For each group pick the clearest representativeId, the MAX severity across its members, and a canonical one-line title. Do NOT invent findings.\n\nEvery lens was seeded with these shared project hints:\n"${HINTS}"\nSet hinted=true for a group whose topic those hints directly plant (migration DDL not exercised by tests, SQLite/Postgres parity, the cloud-provider follow-up, guest-vs-logged-in auth branches) — agreement there is prompted, not independent. Otherwise hinted=false.\n\nFINDINGS:\n${digest}`,
  { label: 'dedup', phase: 'Dedup', schema: DEDUP_SCHEMA })

// Build canonical findings from groups; fall back to no-dedup if the dedup agent failed.
let canonical = []
const seen = new Set()
if (recon?.groups?.length) {
  for (const g of recon.groups) {
    const ids = (g.memberIds || []).filter(id => flat[id] && !seen.has(id))
    if (!ids.length) continue
    ids.forEach(id => seen.add(id))
    const rep = flat[g.representativeId] && ids.includes(g.representativeId) ? flat[g.representativeId] : flat[ids[0]]
    const members = ids.map(id => flat[id])
    const sev = [g.severity, ...members.map(m => m.severity)].sort((x, y) => SEV_RANK[y] - SEV_RANK[x])[0]
    const lenses = [...new Set(members.map(m => m.lens))]
    canonical.push({ ...rep, severity: sev, title: g.canonicalTitle || rep.title, hinted: !!g.hinted, convergence: lenses.length, agreeingLenses: lenses, memberCount: members.length })
  }
}
for (const f of flat) if (!seen.has(f.id)) canonical.push({ ...f, hinted: false, convergence: 1, agreeingLenses: [f.lens], memberCount: 1 })
log(`Deduped ${flat.length} raw findings -> ${canonical.length} canonical (max convergence ${Math.max(...canonical.map(c => c.convergence))}).`)

// ── Phase 3: Verify canonical findings (convergence-weighted) ─────────────────
const verified = await parallel(canonical.map((f, i) => () => verifyFinding(f, i)))

const flatTwoPer = 2 * canonical.filter(c => c.severity !== 'cleanup').length
log(`Verify done: ${guardRuns} guard + ${traceRuns} trace skeptic runs (flat 2-per-finding would be ${flatTwoPer}).`)

// Return grouped back under a single synthesized lens bucket + keep per-lens overalls for context.
return [
  { lens: '_canonical', overall: `Deduped ${flat.length} raw findings across ${lensDefs.length} lenses into ${canonical.length}. Skeptic runs: ${guardRuns} guard + ${traceRuns} trace (flat 2-per-finding: ${flatTwoPer}) — record as cost.agents_by_stage.`, findings: verified.filter(Boolean) },
  ...lensResults.filter(Boolean).map(lr => ({ lens: lr.lens, overall: lr.r?.overall || '', findings: [] })),
]
