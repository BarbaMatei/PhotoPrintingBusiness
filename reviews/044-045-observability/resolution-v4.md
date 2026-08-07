---
type: resolution
target: 044-045-observability
version: 4
answers: review-v4.md
status: resolved
fixed_commit: 52a0cb9
closed: 2026-08-06
findings:
  F1:  { status: fixed, commit: d899559, note: "test-only, and written as a CLASS rule rather than an SLO 3 instance check: for every query in slos.md AND the dashboard, if the numerator (brace groups stripped first, because label values carry their own slashes) contains a `+`, then every `sum(` in it must carry `or vector(0)`. So it also covers SLO 4/5 and any future added term, not just the two copies that had the defect. Red proof: deleting the guards from both copies reddens it, 1 red, no collateral; before this the same deletion left 1133 green. No production change" }
  F2:  { status: fixed, commit: 770f852, note: "test-only, two tests for the two unpinned invariants. (1) The_registered_google_client_keeps_its_backstop_behind_our_own_deadline builds the real AddSocialAuth registration and asserts the registered client's Timeout > RequestDeadline — this is the invariant whose breakage silently restores D75. (2) Our_own_deadline_and_not_the_http_backstop_ends_a_hanging_request bounds the wall clock: only the clock can separate the two, because a deadline that never reaches GetAsync throws the SAME exception type ~15s later. Red proof: both mutations applied together reddened exactly these two, and the timing one took 15 s — the backstop, measured. DELIBERATE SMALL ADDITION recorded rather than hidden: test (1) also resolves IGoogleTokenValidator from the container, closing the zero-coverage gap on the new optional constructor parameter that v4 measured green with a throwaway probe; it is one assertion on the same registration, not a separate finding. THIRD ATTEMPT AT THE TIMING BAR, and CI is what caught it: 5 s was rejected by the micro-review (it equals RequestDeadline), 1 s failed locally on first-call overhead, and 3 s then FAILED ON CI at 4s948ms — the micro-review had asked whether a loaded runner would flake it and I answered that the risk was small, which was wrong. The test now injects its own 30 s backstop instead of borrowing HttpBackstop and bars at 15 s, so the two paths are 15 s apart rather than 12 s and runner noise has to exceed 15 s to false-fail. Red proof at the new numbers: 30s014ms measured against the 15 s bar" }
  F3:  { status: fixed, commit: 9112aa8, note: "three commits: 8b5cb3f (the decided exclusions), b0718d8 (numerator guards + acceptance doc), 9112aa8 (both residuals). Owner chose 'match the prose' — the same call made for SLO 3. SLO 4 excludes `skipped` (AwbCreator returns it when no label was needed at all: the order is not Paid, or it already carries an AWB); SLO 5 excludes `pending` (a submission still in flight, not yet a failure). Applied in BOTH copies — slos.md and the dashboard panel — and both ratios converted to sum() form so the three SLOs read alike. No new test: neither query carries an added term, so F1's guard rule does not apply, and a negative matcher's VALUE is outside the build-check net by design (the status block already states this globally); the label names `result` and `status` are still checked. The 'Two caveats that matter' enumeration needed no edit after all — with these two fixed, the two listed caveats are once again the complete set. FOLLOW-UP after the second micro-review: it caught that I carried over only half of SLO 3's precedent — the exclusion but not the numerator's `or vector(0)` — so a total AWB failure would read 'No Data' instead of a red 0%; guards added to both numerators in both copies, plus the AC story doc amended, which the SLO 3 round did and I had missed. TWO RESIDUALS THE OWNER MUST DECIDE, recorded not fixed: `retry_later` is still on the failure side while `RecordOutcome` fires per ATTEMPT (verified at AwbCreator.cs:42,61), so one order succeeding on attempt 3 scores 1 ok / 3 — the retry loop the 2% budget exists to protect; and `skipped` is overloaded across SIX call sites, one of which (AwbCreator.cs:270) returns Skipped right after an Error-level 'orphaned billable label' log, so excluding all `skipped` makes a genuine failure invisible. BOTH THEN FIXED at the second gate (9112aa8): `retry_later` left the denominator, and the orphaned case got its own `orphaned` result value — a new metric surface (AwbResultValues + All, cardinality 5→6, a metrics.md row, and the `skipped` row rewritten to list all five benign cases). Implemented as `Skipped(string Reason, bool Orphaned = false)` rather than a fifth outcome record so AwbDispatcher's switch and six BeOfType<Skipped>() assertions keep working — the dispatcher still declines to retry, only the metric changed. Red proof: removing the flag reddens An_orphaned_label_records_its_own_outcome_rather_than_skipped, 1 red, no collateral. FIRST RED PROOF WAS INVALID and is recorded as such: my perl pattern contained `$\"`, which perl interpolated as its list-separator variable, so the mutation silently never applied and the suite passed — caught because 0 red on a fix that must be pinned is itself the tell" }
  F4:  { status: fixed, commit: 243625c, note: "FIXED ON THE SECOND ATTEMPT; the first (ab7860f, kept as hygiene) could not work and its commit message states a false cause — both caught by the second micro-review and then confirmed by me. (1) The approach cannot work: gitleaks on a `pull_request` event scans the PR's COMMIT RANGE, and all three commits carrying the literal (44c3e2d, 295a51c, ab7860f) are branch-only — verified with `git merge-base --is-ancestor` against origin/main — while the CI log's own fingerprint is commit-scoped (`44c3e2de...:SentryDataScrubbersTests.cs:generic-api-key:16`). Renaming at HEAD cannot clear a scan that reads history. (2) The stated reason was wrong regardless: Shannon entropy of `guest-token-placeholder` (~3.83/char) is HIGHER than `5f0c-live-guest-guid` (~3.68), and generic-api-key's threshold is 3.5, so if the new value passes at all it is via a stopword ('placeholder'), not via low entropy. The rename is KEPT as hygiene (it stops future HEAD-only flags and the class sweep did find a second copy at SentryOptionsWiringTests.cs:19) but it is not the fix. Owner chose a .gitleaksignore over an allowlist or history rewrite: `243625c` adds the two fingerprints (44c3e2d…:16 and 295a51c…:19, both line numbers read out of the commits themselves) with a header stating when a line may be added, since these never expire. NOT VERIFIABLE LOCALLY — gitleaks is not installed on this machine, so only the next pull_request run proves the gate is green; if it reports a third fingerprint, add it. Also unswept and not mine to rewrite: the same literal still sits in three tracked reviews/** docs, and reviews/** is not in .gitleaks.toml's allowlist" }
  F5:  { status: deferred, commit: null, note: "🟡 — ledger backlog (D106) per the README router" }
  F6:  { status: deferred, commit: null, note: "🟡 — ledger backlog (D107) per the README router" }
  F7:  { status: deferred, commit: null, note: "🟡 — ledger backlog (D108) per the README router" }
  F8:  { status: deferred, commit: null, note: "🟡 — ledger backlog (D109) per the README router" }
  F9:  { status: deferred, commit: null, note: "🟡 — ledger backlog (D110); flagged to the owner in summary-v4 as the one minor worth their eye" }
  F10: { status: deferred, commit: null, note: "🟡 — ledger backlog (D111) per the README router" }
  F11: { status: deferred, commit: null, note: "🟡 — ledger backlog (D112) per the README router" }
  F12: { status: deferred, commit: null, note: "🟡 — ledger backlog (D114) per the README router" }
  F13: { status: deferred, commit: null, note: "🟡 — ledger backlog (D115) per the README router" }
  F14: { status: deferred, commit: null, note: "🟡 — ledger backlog (D120) per the README router" }
  F15: { status: deferred, commit: null, note: "⚪ — ledger backlog (D116) per the README router" }
  F16: { status: deferred, commit: null, note: "⚪ — ledger backlog (D117) per the README router" }
  F17: { status: deferred, commit: null, note: "⚪ — ledger backlog (D118) per the README router" }
  F18: { status: deferred, commit: null, note: "⚪ — ledger backlog (D119) per the README router" }
---

# Resolution v4 — 044-045-observability

Fixer's answer to [review-v4.md](review-v4.md) (immutable). The review named 18 findings;
**the four 🟠 (F1–F4, ledger D103, D104, D105, D113) are this fix round**. The 14 🟡/⚪
(F5–F18) are deferred to the [ledger](ledger.md) backlog per the README router.

**Nothing here is `verified`.** Only `review-v5.md` — a re-review by someone who did not fix —
can set that status.

## Process note the re-reviewer must weigh

The v4 verification pass and this fix round run in **the same session**, the same caveat
[resolution-v3](resolution-v3.md#process-note-the-re-reviewer-must-weigh) recorded, and it bites
harder this time: three of the four findings in scope are ones I raised *and measured myself*, so
there is no independent pressure on either the finding or the fix. Two things offset it partly.
First, every finding in scope was established by a **recorded mutation with a measured result**
(1133 green with the mechanism removed) rather than by argument, so confirming them is re-reading a
measurement. Second, F1, F2 and F4 are all "add the missing guard" work, where the fix either
reddens on revert or it does not — a fixer cannot talk that into being green.

**The v5 re-review should be run from a fresh session**, and should be sceptical in particular
about whether the tests added for F1 and F2 pin the *mechanism* or merely restate it.

## Fix round scope

| Cluster | Findings | Owner file(s) | Approach-check |
|---|---|---|---|
| A — SLO documents and dashboard queries | F1 (D103), F3 (D105) | `memory-bank/operations/slos.md`, `ops/dashboards/fototipar-overview.json`, `Tests/Integration/DashboardMetricNamesTests.cs` | not needed (doc/query + test); **owner gate on F3** |
| B — Google deadline invariants | F2 (D104) | `Tests/Unit/Services/GoogleTokenValidatorTests.cs`, `Tests/Integration/` | not needed (test-only) |
| C — secret scanner gate | F4 (D113) | `.gitleaks.toml`, `hooks/pre-commit`, `Tests/Unit/Configuration/SentryDataScrubbersTests.cs` | not needed; **owner gate** |

**No cluster is trigger-list-shaped.** Nothing here adds a mechanism, changes a key scheme, a
concurrency model, a resource budget or retry semantics: F1, F2 and the test half of C add
assertions over behaviour that already ships, and F3 plus the config half of C are a query/policy
edit. So no adversarial approach-checks were dispatched this round — recorded explicitly because
skipping them is normally the expensive mistake.

Ordering: the two ungated clusters (B, then F1's half of A) first, then the owner gate, then F3 and
C once it is answered. No blocker, so nothing gates the branch.

## Findings

<!-- rendered:findings-table:start -->
| ID | Sev | Title | Status | Commit | How |
|---|---|---|---|---|---|
| F1 |  |  | fixed | `d899559` | test-only, and written as a CLASS rule rather than an SLO 3 instance check: for every query in slos.md AND… |
| F2 |  |  | fixed | `770f852` | test-only, two tests for the two unpinned invariants. (1) The_registered_google_client_keeps_its_backstop_b… |
| F3 |  |  | fixed | `9112aa8` | three commits: 8b5cb3f (the decided exclusions), b0718d8 (numerator guards + acceptance doc), 9112aa8 (both… |
| F4 |  |  | fixed | `243625c` | FIXED ON THE SECOND ATTEMPT; the first (ab7860f, kept as hygiene) could not work and its commit message sta… |
| F5 |  |  | deferred | — | 🟡 — ledger backlog (D106) per the README router |
| F6 |  |  | deferred | — | 🟡 — ledger backlog (D107) per the README router |
| F7 |  |  | deferred | — | 🟡 — ledger backlog (D108) per the README router |
| F8 |  |  | deferred | — | 🟡 — ledger backlog (D109) per the README router |
| F9 |  |  | deferred | — | 🟡 — ledger backlog (D110); flagged to the owner in summary-v4 as the one minor worth their eye |
| F10 |  |  | deferred | — | 🟡 — ledger backlog (D111) per the README router |
| F11 |  |  | deferred | — | 🟡 — ledger backlog (D112) per the README router |
| F12 |  |  | deferred | — | 🟡 — ledger backlog (D114) per the README router |
| F13 |  |  | deferred | — | 🟡 — ledger backlog (D115) per the README router |
| F14 |  |  | deferred | — | 🟡 — ledger backlog (D120) per the README router |
| F15 |  |  | deferred | — | ⚪ — ledger backlog (D116) per the README router |
| F16 |  |  | deferred | — | ⚪ — ledger backlog (D117) per the README router |
| F17 |  |  | deferred | — | ⚪ — ledger backlog (D118) per the README router |
| F18 |  |  | deferred | — | ⚪ — ledger backlog (D119) per the README router |
<!-- rendered:findings-table:end -->

## Decisions

### Owner gate (2026-08-06) — two answers, both as recommended

Asked once after triage, per the fixer contract. **F3/D105:** match the prose, as for SLO 3 — drop
`skipped` from SLO 4's denominator and `pending` from SLO 5's, rather than only documenting a third
caveat. **F4/D113:** clear the gitleaks failure by renaming the fixture constant rather than
allowlisting the file or pinning a fingerprint — it fixes the cause and adds nothing to maintain.

### No approach-checks this round, and why that is not a shortcut

None of the four clusters is trigger-list-shaped: F1, F2 and F4 add or edit tests and a test
constant, and F3 is a query/prose edit under an explicit owner decision. Nothing adds a mechanism,
touches a key scheme, a concurrency model, a resource budget or retry semantics. Recorded
explicitly because skipping checks is normally the expensive mistake — the one place a budget/retry
question *did* surface, it was pushed to the inbox rather than fixed here (see below).

### The first micro-review found three real defects in my own F1 test — repaired in `aca24fd`

This is the round's headline process fact, and it is uncomfortable: the test I shipped to close a
"the mechanism is unpinned" finding was itself weakly pinned.

1. **It could pass while checking nothing.** The rule was `if (!numerator.Contains('+')) continue;`
   with an unconditional `BeEmpty()` terminal, so "every added term is guarded" and "there are no
   added terms" were the same green. And `slos.md:103-104` documents `result=~"ok|duplicate"` as a
   live-but-rejected alternative, so collapsing the `+` away is a realistic edit, not a hypothetical.
   Fixed with a floor: `addedTermsSeen >= 4` (two terms in each of the two copies). **Red-proven** —
   collapsing the doc copy to one `=~` matcher reddens it with "found 2".
2. **It only looked at the numerator.** `Split('/')[0]` threw the denominator away, and
   `anything / empty` blanks a panel exactly as `empty + anything` does. Now every side of the
   division is checked independently — a side with no `+` is skipped, which is why SLO 3's
   deliberately unguarded denominator still passes.
3. **A raw-selector addition demanded zero guards.** The required count keyed on `sum\(`, so
   `a{x} + b{y}` gave `sums=0, guards=0` and passed — and that is exactly the style SLO 4 and SLO 5
   were written in, the two queries F3 was queued to rewrite. The requirement now counts **added
   terms** (`'+' count + 1`), independent of `sum()`.

Two smaller items from the same review, also repaired (`aca24fd`, `3d2f2cf`): the deadline proof's
5 s bar **was `RequestDeadline`'s own value**, so the mutation it most needed to catch (a dropped
`deadline` argument, which produces ~5 s) landed on its boundary — the bar moved to 3 s, after 1 s
proved too tight (first-call handler setup measured ~1 s); and the factory-created client is now
disposed.

**Known residual limit, recorded rather than fixed:** the guard requirement is a count per side, not
a per-term structural check, so `((sum(A) or vector(0) or vector(0)) + sum(B))` would pass with `B`
unguarded. Closing that needs a real PromQL parser in a test helper, which is a worse trade than the
hole.

### The second micro-review broke one of my two remaining fixes, and I confirmed it myself

Ten findings over the F3/F4 diff. Two are serious enough to change a status, and I verified both
rather than taking them on report:

1. **F4's approach cannot work.** gitleaks on a `pull_request` event scans the PR's **commit range**,
   not the merge result. `git log -S"5f0c-live-guest-guid"` names `44c3e2d`, `295a51c` and `ab7860f`,
   and `git merge-base --is-ancestor` shows **none** of them is an ancestor of `origin/main` — so all
   three are inside the PR's own range. The CI log's fingerprint was commit-scoped all along
   (`44c3e2de…:SentryDataScrubbersTests.cs:generic-api-key:16`). Renaming at HEAD cannot clear a scan
   that reads history. **F4 goes back to `open` and the question returns to the owner.**
2. **F4's stated cause was also wrong.** The new value's Shannon entropy (~3.83/char) is *higher*
   than the old one's (~3.68), and `generic-api-key`'s threshold is 3.5, so "low entropy" was never
   the mechanism — if the rename helps at all it is via a stopword (`placeholder`). Corrected in the
   record; the commit message overstates it and cannot be rewritten.
3. **F3 carried over only half of SLO 3's precedent.** SLO 3's round produced an exclusion *and*
   `or vector(0)` guards; I took the exclusion only. On a fresh process where no AWB has yet
   succeeded — expired Sameday credentials is the realistic case — `{result="ok"}` matches nothing and
   the panel reads "No Data" instead of a red 0%. Guards added to both numerators in both copies
   (`b0718d8`). My own F1 guard test cannot catch this: it skips single-term sides, and broadening it
   to demand a guard on every literal-valued `sum()` would red legitimate panels, so the limit is
   recorded rather than papered over.
4. **The acceptance doc was missed.** The SLO 3 round amended the AC line in place; I had not done the
   same for SLO 4/5. Fixed in `b0718d8`.

Two further findings are **verified defects in the shipped exclusion** that need the owner, not me —
both queued below rather than fixed, because each is the same kind of definitional call the gate
answered for SLO 3.

### Second owner gate (2026-08-06) — re-gated after the micro-review, both answered as recommended

**F4:** add a `.gitleaksignore` carrying the two historical fingerprints, rather than allowlisting the
test tree (which would also silence the other high-entropy fixtures the micro-review found) or
rewriting 91 commits of a branch with an open PR. **F3:** fix both residuals — take `retry_later` out
of SLO 4's denominator, and give the orphaned-label case its own result value so it counts as the
failure it is.

**What that second answer cost, recorded because it is more than a query edit.** `orphaned` is a new
label value, so it is a new metric surface: `AwbResultValues.Orphaned` plus its `All` entry, the
cardinality expectation for `awb_creation_total` (5 → 6), a `metrics.md` row, and the `skipped` row
rewritten to list all five benign cases it actually covers. The outcome type gained a flag rather
than a fifth record — `Skipped(string Reason, bool Orphaned = false)` — deliberately: every existing
`new Skipped("…")` call, `AwbDispatcher`'s `case AwbCreationOutcome.Skipped skipped:` and six
`BeOfType<Skipped>()` assertions keep working unchanged, so the dispatcher still declines to retry an
orphaned label exactly as before. Only the metric it reports changed.

**Scoped out on purpose:** SLO 5's `failed` and `rejected` are retriable in the same way
`retry_later` is, and its own text says "accepted on first **or retried** submission" — so it likely
carries the identical per-attempt bias. The owner was asked about the AWB ratio, not this one, and
nothing increments the ANAF counter yet, so it is recorded for the re-reviewer rather than swept in.

### Deliberate deviations

- **F2 test (1) also resolves `IGoogleTokenValidator` from a real container**, which is slightly
  outside D104. It closes the zero-coverage gap on the new optional constructor parameter that the
  v4 pass measured green with a throwaway probe, and it is one assertion on the registration the
  test already builds. Recorded rather than hidden.
- **`BuildServiceProvider(validateScopes: true)` must not be "sharpened" to `ValidateOnBuild`** —
  `AddSocialAuth` also registers `ISocialAuthService`, whose constructor needs `PhotoPrintDbContext`,
  `ITokenService` and `IOptions<JwtSettings>`, none of which that extension registers. A future
  reviewer tightening this would break the test for a reason unrelated to the finding.

### Genuinely new, outside the finding set — NOT fixed

- **The Sameday HTTP client's timeout is shorter than its own retry ladder.** Found by F2's
  micro-review as the true class sibling of D104: `HttpClient.Timeout` (10 s default) wraps the
  resilience handler's ~21 s backoff schedule, so `MaxRetryAttempts = 3` is silently 2 and a vendor
  outage surfaces as a cancellation instead of the vendor's status. Filed to
  [inbox.md](../inbox.md) with its evidence. **Not fixed here on purpose:** it changes a resource
  budget and retry semantics on the AWB path, which is trigger-list work owing its own adversarial
  approach-check, and this is an observability round.
- **Two pre-existing blind spots in the dashboard walker**, noted by the micro-review while checking
  F1's reach: `CollectPanelQueries` never walks `templating.list[]` or `annotations.list[].expr`, and
  `SloQueries()` matches only untagged code fences — a single ```promql tag would drop a block and
  shift the fence pairing. Neither has a live instance today. They belong to D88's family
  (walker reach) and are left there rather than minted as new rows by a fixer.

### Boundaries — not fixed, for the re-reviewer

- **F4's fix cannot be proven locally.** Only the next `pull_request` run shows whether `secret-scan`
  goes green. If gitleaks still flags the new value, the fallback the owner declined (path allowlist)
  is the next option.
- **F1 and F2 add no production change**, so nothing about the *behaviour* of the guarded mechanisms
  improved this round — only the ability to notice their removal.
- **D110 is still open and still on the operator's wall:** the availability panel's dilution figures
  are wrong (5,760/day is `/metrics` alone; the real floor is ~94.5%, not ~99.7%). It is 🟡 backlog,
  flagged in `summary-v4` as the one minor worth the owner's eye, and this round did not touch it.
