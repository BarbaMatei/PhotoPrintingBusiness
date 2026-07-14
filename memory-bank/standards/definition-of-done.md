# Definition of Done

**What this is.** A pre-hand-back checklist distilled from ~165 verified review findings across
bolts 035 (payment idempotency), 042 (thumbnail cache), and 043 (cloud storage). Every class
below was found *post-hoc* by multi-lens review at real token cost; this file exists so the next
bolt doesn't pay to rediscover it. Evidence pointers reference the review ledgers
(`reviews/<target>/ledger.md`).

**When to apply.** Before handing back any bolt implementation (stage 4) and any review-fix
round. Scale to the change: a full bolt gets every section; a small fix gets the four rules and
the classes its diff touches.

---

## The four rules (apply to every change)

Generalized from the fixer contract in `reviews/README.md` — they apply to first-hand code for
the same reason they apply to fixes:

1. **Class sweep.** State the defect/change class in one sentence, then search for sibling
   sites — every consumer of a changed contract, the same pattern elsewhere, the same stale
   token in every doc — and handle the class, not the instance.
   *(042: the cleanup job missed the new `ThumbnailPath`; 043: two prod callers still read the
   local tier after cloud landed — both were "the other places" of a change made correctly once.)*
2. **New-mechanism bar.** Any new mechanism (cache, limiter, retry, event, catch/mapping,
   background job) is a mini-feature and ships at feature grade: defaults derived from the real
   constraint and stated; an observability hook; tests for the failure modes the mechanism
   itself introduces; docs updated.
   *(042: the decode limiter shipped with a CPU-count default for a RAM problem, silent
   saturation, and an untested slot-release — three findings from one missing bar.)*
3. **Design check.** A change to a design — key scheme, concurrency model, resource budget,
   retry semantics, storage layout — is not a patch: run one adversarial agent against the
   proposed approach *before implementing* (see `bolt-process.md`, Stage 2).
4. **Fresh-eyes micro-review at hand-back.** Before declaring done, dispatch 1–2 anchored
   Explore agents (fresh context) over the full diff with three questions: class or instance?
   new surface at the rule-2 bar? anything adjacent broken? A self-skim does not count — the
   mind that wrote the code answers "nothing broken" over diffs that reviews then mine for
   findings.

---

## The defect classes

For each class the diff touches: apply the rule, and name the test that would go red if the
class were violated.

**1. Caller sweep on contract change.** Changing or adding a contract (interface, entity field,
key scheme, status code) requires enumerating ALL existing consumers — grep, don't recall — and
updating or explicitly clearing each. *(043 F1/F2 — the only High of the bolt; 035 OrderNumber
collision lived outside the diff.)*

**2. Second-path symmetry.** Anything with two implementations or two user types — SQLite/
Postgres, local/cloud storage, Stripe/EuPlatesc, logged-in/guest — gets every behavior either
verified symmetric or documented divergent. Exception contracts must be uniform across
implementations of one interface. *(043 F3: S3 threw `AmazonS3Exception` where local threw
`FileNotFoundException` → preview 500s; 042 D23: migration DDL diverges by provider.)*

**3. Bounded resources.** Every allocation, queue, concurrency level, and accepted input has a
cap derived from the real constraint (RAM, disk, not CPU count by default), configurable with
the rationale stated; the cap-breach path maps to a proper status code and has a test.
*(042 D2/D33/D61/D96 — the bomb-guard/memory-bound family, four rounds of findings.)*

**4. Multi-store writes declare atomicity.** Any write spanning two stores (file + DB row,
DB + external API) states its ordering, what happens on crash between steps, and who reclaims
orphans (a named sweep, not hope). Check-then-act windows on shared state are named and either
closed or accepted in writing. *(042 D5→D34/D35 chain; 043 F8 TOCTOU; ADR-011 is the model.)*

**5. Failure modes have tests — "green ≠ proven".** For every failure mode the code can hit:
which test goes red if this bug is injected? Mock only at system boundaries (network, external
APIs, clock) — the real component (real ImageSharp, real SQLite) must run in at least one test
of its guards. Each suite states what it *cannot* prove and where that gap is covered.
*(042 D25: the real ImageProcessor was mocked in ALL tests — 490 greens proved nothing about
image handling; 043 F14–F16/F18 were all "branch exists, no test".)*

**6. Observability floor.** Every new error or side-effecting path emits a distinguishable log
at or above the configured minimum level (a `Debug` line under an `Information` floor does not
exist). Swallowed exceptions are logged. Partial state (file written, row not committed) is
observable. New limiters/queues expose saturation. *(042 D15–D17/D62/D68/D84/D88/D89 — the
largest single class in the ledger.)*

**7. Full lifecycle for every artifact.** Everything created — file, DB row, token, browser
object URL, event subscription — has a named deleter and a test proving deletion. Enumerate the
unhappy lifecycle: cancel, refund, expiry, guest-session death. *(042 D4/D40/D54; 043 F17 —
"paid then cancelled" purge was an owner decision nobody had asked.)*

**8. Doc sync is repo-wide.** A change that alters behavior updates every doc stating the old
behavior — sweep the stale token across the repo, not just the file in reach. Bundled scope
(changes beyond the story) is documented with an AC and a test. *(042's stale-doc class took
four rounds to kill: D57–D60/D64/D65/D80/D91.)*

**9. One constant, one home.** Derived values are computed, not restated (a TTL and its
`max-age` must share a source). No magic numbers; no per-provider duplication of the same
config. *(043 F5: presign TTL 30 min vs hardcoded `max-age=3600`; 042 D21/D22.)*

**10. Error-contract mapping.** Every exception type a new dependency can throw is either
mapped to the API error contract (ADR-002 422 / ADR-004 409 / ProblemDetails) or deliberately
propagated — and the unmapped-exception path has a test. *(042 D6: `UnknownImageFormatException`
→ raw 500; D52: the 512 MB backstop threw an unmapped type.)*

**11. Frontend auth/state matrix.** Any interceptor, guard, or session change enumerates the
user-type × token-state matrix — logged-in, guest, anonymous, expired-token — and states what
each branch does to user state (cart, contact info, in-flight uploads). No branch may silently
wipe state the user typed. In-flight request dedup for shared async init. *(042's guest
self-heal family — D11–D14, D48, D63, D72–D74, D86, D94 — the single most re-found cluster.)*

**12. Recovery liveness.** Recovery/sweep mechanisms state whether they run boot-only or
periodically, and the staleness window is a written decision, not an accident. *(043 F4: the
purge scanner ran at boot only — a stuck order waited for the next deploy.)*

---

## Hand-back gate

Done means: the four rules applied · every touched class above checked · the ddd-02
failure-mode table carried into ddd-03 with real test names · micro-review (rule 4) ran and its
findings are fixed or recorded. Then hand back — review still runs, but it should be finding
the subtle 30%, not this list.
