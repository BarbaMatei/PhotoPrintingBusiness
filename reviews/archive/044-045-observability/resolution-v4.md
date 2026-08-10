---
type: resolution
target: 044-045-observability
version: 4
answers: review-v4.md
status: resolved
fixed_commit: 52a0cb9
closed: 2026-08-06
findings:
  D103: { status: fixed, commit: d899559, note: "Test-only class rule: every added-term side in slos.md and the dashboard guards each sum() with `or vector(0)`; covers SLO 4/5 and future terms. Red proof: deleting both guards reddens 1 test where 1133 stayed green. Repaired at `aca24fd`." }
  D104: { status: fixed, commit: 770f852, note: "Test-only: two tests pin the invariants — registered client Timeout > RequestDeadline, and the deadline (not the HTTP backstop) ends a hanging GetAsync, wall-clock-proven at 30s014ms against a 15 s bar. Each mutation reddens its own test." }
  D105: { status: fixed, commit: 9112aa8, note: "Three commits: 8b5cb3f exclusions (SLO 4 drops `skipped`, SLO 5 drops `pending`, both copies), b0718d8 numerator guards + AC doc, 9112aa8 residuals: `retry_later` out of the denominator, new `orphaned` result value (cardinality 5→6)." }
  D113: { status: fixed, commit: 243625c, note: ".gitleaksignore with the two commit-pinned fingerprints (44c3e2d…:16, 295a51c…:19); the `ab7860f` rename kept as hygiene only — it cannot clear a commit-range scan. Not provable locally; the next pull_request run is the proof." }
  D106: { status: deferred, commit: null, note: "🟡 — ledger backlog per the README router" }
  D107: { status: deferred, commit: null, note: "🟡 — ledger backlog per the README router" }
  D108: { status: deferred, commit: null, note: "🟡 — ledger backlog per the README router" }
  D109: { status: deferred, commit: null, note: "🟡 — ledger backlog per the README router" }
  D110: { status: deferred, commit: null, note: "🟡 — ledger backlog; flagged to the owner in summary-v4 as the one minor worth their eye" }
  D111: { status: deferred, commit: null, note: "🟡 — ledger backlog per the README router" }
  D112: { status: deferred, commit: null, note: "🟡 — ledger backlog per the README router" }
  D114: { status: deferred, commit: null, note: "🟡 — ledger backlog per the README router" }
  D115: { status: deferred, commit: null, note: "🟡 — ledger backlog per the README router" }
  D120: { status: deferred, commit: null, note: "🟡 — ledger backlog per the README router" }
  D116: { status: deferred, commit: null, note: "⚪ — ledger backlog per the README router" }
  D117: { status: deferred, commit: null, note: "⚪ — ledger backlog per the README router" }
  D118: { status: deferred, commit: null, note: "⚪ — ledger backlog per the README router" }
  D119: { status: deferred, commit: null, note: "⚪ — ledger backlog per the README router" }
---

# Resolution v4 — 044-045-observability

## Scope

| Cluster | Findings | Files | Approach-check |
|---|---|---|---|
| A — SLO documents and dashboard queries | D103, D105 | `memory-bank/operations/slos.md`, `ops/dashboards/fototipar-overview.json`, `Tests/Integration/DashboardMetricNamesTests.cs` | not needed (doc/query + test); owner gate on D105 |
| B — Google deadline invariants | D104 | `Tests/Unit/Services/GoogleTokenValidatorTests.cs`, `Tests/Integration/` | not needed (test-only) |
| C — secret scanner gate | D113 | `.gitleaks.toml`, `hooks/pre-commit`, `Tests/Unit/Configuration/SentryDataScrubbersTests.cs` | not needed (test constant + scanner config); owner gate |
| — | D106–D112, D114–D120 | — | not needed (🟡/⚪ deferred to the ledger backlog per the README router) |

## Decisions

### Same-session round, recorded for the verifier (D103, D104, D105, D113)

The v4 verification pass and this fix round ran in one session — the caveat resolution-v3
recorded, biting harder here: three of the four findings in scope were raised and measured by the
fixer, so no independent pressure sits on either finding or fix. Two partial offsets: every
in-scope finding was established by a recorded mutation with a measured result (1133 green with
the mechanism removed), so confirming one is re-reading a measurement; and D103, D104 and D113
are add-the-missing-guard work that either reddens on revert or does not. The v5 re-review should
run from a fresh session and be sceptical about whether the D103 and D104 tests pin the mechanism
or merely restate it.

### The guard test needed three repairs before it pinned the mechanism (D103)

The first micro-review found three defects in the shipped test; all repaired in `aca24fd`:
1. It could pass while checking nothing — `if (!numerator.Contains('+')) continue;` ended in an
   unconditional `BeEmpty()`, so "every added term is guarded" and "no added terms" were the same
   green. Fixed with a floor, `addedTermsSeen >= 4`; red-proven by collapsing the doc copy to one
   `=~` matcher ("found 2") — a realistic edit, since `slos.md:103-104` documents the
   `result=~"ok|duplicate"` alternative.
2. It only looked at the numerator — `Split('/')[0]` threw the denominator away, and
   `anything / empty` blanks a panel exactly as `empty + anything` does. Now every side of the
   division is checked; a side with no `+` is skipped, which is how SLO 3's deliberately
   unguarded denominator still passes.
3. A raw-selector addition demanded zero guards — the count keyed on `sum\(`, so `a{x} + b{y}`
   passed, and SLO 4/5 were written in exactly that style. Now it counts added terms (`'+' count + 1`).
Residuals recorded, not fixed: the requirement is a count per side, not per-term structure, so
`((sum(A) or vector(0) or vector(0)) + sum(B))` passes with `B` unguarded (a PromQL parser in a
test helper is a worse trade); and single-term sides are skipped — the hole that became D121.

### The timing bar took three attempts, and CI set the final one (D104)

Two tests pin the invariants: `The_registered_google_client_keeps_its_backstop_behind_our_own_deadline`
(registered client's `Timeout > RequestDeadline` — the breakage that silently restores D75) and
`Our_own_deadline_and_not_the_http_backstop_ends_a_hanging_request` (wall clock only, because a
deadline that never reaches `GetAsync` throws the same exception type ~15 s later). Bar history:
5 s equaled `RequestDeadline` itself (micro-review); 1 s failed locally on ~1 s first-call handler
setup; 3 s then failed on CI at 4s948ms. The test now injects its own 30 s backstop instead of
borrowing `HttpBackstop` and bars at 15 s, so the two paths sit 15 s apart. Red proof at the
final numbers: 30s014ms against the 15 s bar. Bar move and client disposal landed in `aca24fd`
and `3d2f2cf`.
Deliberate addition, recorded: the registration test also resolves `IGoogleTokenValidator` from
the container, closing the zero-coverage gap on the new optional constructor parameter. Do not
tighten `BuildServiceProvider(validateScopes: true)` to `ValidateOnBuild` — `AddSocialAuth` also
registers `ISocialAuthService`, whose constructor needs `PhotoPrintDbContext`, `ITokenService`
and `IOptions<JwtSettings>`, none of which that extension registers.

### Owner gates, twice: match the prose, then fix the residuals (D105)

First gate (2026-08-06), answered as recommended: match the prose as SLO 3 did — SLO 4 drops
`skipped` (returned when no label was needed at all), SLO 5 drops `pending` (a submission still
in flight), in both copies, both ratios converted to `sum()` form. The second micro-review caught
that I carried over only half of SLO 3's precedent — the exclusion without the `or vector(0)`
numerator guards — so a fresh process with no successful AWB yet (expired Sameday credentials is
the realistic case) would read "No Data" instead of a red 0%. Guards added to both numerators in
both copies, plus the AC story doc amended (`b0718d8`).
Two verified residuals went back to the owner rather than being swept in: `retry_later` sits on
the failure side while `RecordOutcome` fires per attempt (`AwbCreator.cs:42,61`), so one order
succeeding on attempt 3 scores 1 ok / 3; and `skipped` is overloaded across six call sites, one
of which (`AwbCreator.cs:270`) returns right after an Error-level "orphaned billable label" log.
Second gate (2026-08-06): fix both — `retry_later` left the denominator and the orphaned case
got its own result value (`9112aa8`).

### `orphaned` is a new metric surface, shipped as a flag (D105)

The second gate's cost, more than a query edit: `AwbResultValues.Orphaned` plus its `All` entry,
`awb_creation_total` cardinality 5→6, a `metrics.md` row, and the `skipped` row rewritten to list
the five benign cases it covers. Implemented as `Skipped(string Reason, bool Orphaned = false)`
rather than a fifth outcome record, so `AwbDispatcher`'s switch and six `BeOfType<Skipped>()`
assertions keep working — the dispatcher still declines to retry an orphaned label; only the
metric changed. Red proof: removing the flag reddens
`An_orphaned_label_records_its_own_outcome_rather_than_skipped`, 1 red, no collateral.
The first red proof was invalid and is recorded as such: the perl mutation pattern contained
`$\"`, which perl interpolated as its list-separator variable, so the mutation silently never
applied and the suite passed — caught because 0 red on a fix that must be pinned is itself the tell.
Scoped out on purpose: SLO 5's `failed` and `rejected` are retriable the way `retry_later` is and
its own text says "accepted on first or retried submission", so it likely carries the same
per-attempt bias — but the owner was asked about the AWB ratio only, and nothing increments the
ANAF counter yet. Recorded for the re-reviewer.

### The rename could not clear the scanner; the owner chose a .gitleaksignore (D113)

First attempt `ab7860f` renamed the fixture constant per the first owner gate (2026-08-06); the
second micro-review broke it and I confirmed both halves. gitleaks on a `pull_request` event
scans the PR's commit range, and all three commits carrying the literal (`44c3e2d`, `295a51c`,
`ab7860f`) are branch-only — checked with `git merge-base --is-ancestor` against `origin/main` —
while the CI fingerprint is commit-scoped (`44c3e2de…:SentryDataScrubbersTests.cs:generic-api-key:16`),
so a rename at HEAD cannot clear a scan that reads history. The stated cause was also wrong:
the new value's Shannon entropy (~3.83/char) is higher than the old's (~3.68) against a 3.5
threshold — if the rename helps, it is via the `placeholder` stopword; `ab7860f`'s commit
message overstates this and cannot be rewritten.
Second gate: a `.gitleaksignore` (`243625c`) carrying the two fingerprints (`44c3e2d…:16`,
`295a51c…:19`, line numbers read out of the commits) — over allowlisting the test tree, which
would also silence other high-entropy fixtures, or rewriting 91 commits of a branch with an
open PR. The rename stays as hygiene; the sweep found a second copy at `SentryOptionsWiringTests.cs:19`.
Not provable locally — gitleaks is not installed here; the next `pull_request` run is the proof.
The same literal still sits in three tracked `reviews/**` docs, outside `.gitleaks.toml`'s allowlist.

### Found outside the finding set, filed not fixed (D104, D88)

- The Sameday HTTP client's timeout is shorter than its own retry ladder: `HttpClient.Timeout`
  (10 s default) wraps the resilience handler's ~21 s backoff schedule, so `MaxRetryAttempts = 3`
  is silently 2 and a vendor outage surfaces as a cancellation instead of the vendor's status.
  The true class sibling of D104; filed to `reviews/inbox.md` with its evidence. Not fixed here:
  it changes a resource budget and retry semantics on the AWB path, which owes its own
  adversarial approach-check, and this is an observability round.
- Two dashboard-walker blind spots, D88's family: `CollectPanelQueries` never walks
  `templating.list[]` or `annotations.list[].expr`, and `SloQueries()` matches only untagged code
  fences, so a single promql fence tag would drop a block and shift the fence pairing. Neither
  has a live instance today; left on D88 rather than minted as new rows by a fixer.
