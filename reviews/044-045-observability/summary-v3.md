---
type: owner-summary
target: 044-045-observability
pass: 3
pass-type: verification
commit: 7e28317
date: 2026-08-05
decisions-needed: 6
---

# v3 verification — what you need to know

Nine of the ten fixes hold, each proven by breaking it and watching the right tests fail
([the mutation table](review-v3.md#how-each-fix-was-proven)). Nothing was reopened. But the fix
round left the branch's CI red, and two of its fixes protect less than their records claim.

## Needs your decision

1. **🔴 CI has been red on this branch since the fix round, and nobody noticed.**
   A test the round added passes on Windows and fails on Linux, because .NET parses a
   unix-socket address differently on each. CI was green at `8daa977` and has failed all six runs
   since `e791c40`; the round's records report "1120 passed, 0 failed", which was only ever true
   on Windows. The same cause stops the new scrape-port guard's second safety rule from firing on
   the platform you deploy to. → **Fix now, ~30 min, no decision really needed except priority:**
   count a port only when the address actually has one. [D74](findings-v3.md#f1--🔴--d74--the-scrape-guard-mis-parses-socketpipe-listeners-off-windows-and-its-own-test-fails-on-ci)
2. **🟠 The Google sign-in fix has a guard that does nothing, and a test that hides it.**
   The fix stopped a user closing their tab from looking like a Google outage — that part works.
   The extra condition meant to keep *real* outages visible is dead code: measured on .NET 8, it
   can never be true. So when Google is slow and the user gives up (the common case in an
   outage), the request now returns **200** and reaches neither the availability number nor
   Sentry. → **Decide the posture:** accept that an abandoned slow login is invisible, or count it.
   Either way the misleading test should go. ~1–2h. [D75](findings-v3.md#f2--🟠--d75--f4s-timeout-carve-out-is-dead-code-and-its-test-passes-on-a-shape-net-never-produces)
3. **🟠 Sentry has the same hole you just closed for tracing.** Anyone can set one HTTP header and
   make every request a fully-sampled Sentry transaction (or none), burning the quota your alert
   rules depend on. It is the same "a caller's header decides our sampling" problem as
   [D41](ledger.md), one layer over, and it is dormant only because Sentry is currently off. →
   **Same decision you already made for D41** (ignore the caller's decision), ~1h. Not executed —
   settled from the Sentry SDK's own docs. [D77](findings-v3.md#f4--🟠--d77--sentry-honours-an-inbound-sentry-trace-ahead-of-tracessamplerate)
4. **🟠 SLO 3 scores correct behaviour as failure.** Its written definition says a duplicate
   webhook handled correctly counts as success; its query counts it as failure — and invalid
   signatures too, on an endpoint anyone can POST to, so a stranger can drive that SLO to zero.
   → **Decide what the ratio should measure**, then fix query or prose, ~1h. [D80](findings-v3.md#f7--🟠--d80--slo-3s-query-contradicts-slo-3s-own-definition)
5. **🟠 The SLO document now points readers the wrong way about the number you parked.** You
   deliberately deferred [D46](ledger.md) (availability can't read below ~99.7% because it counts
   the site's own health checks). The file still says SLOs 1–4 are measured, names only SLO 3 as
   doubtful, and offers SLO 1 as the *reliable* cross-check. → **Cheap doc fix, ~15 min** — worth
   doing precisely because the defect is parked, not fixed. [D81](findings-v3.md#f8--🟠--d81--slosmd-still-asserts-slo-1-is-measured--and-now-offers-it-as-slo-3s-cross-check)
6. **🟠 One order can ship with no shipping label and no alarm — needs confirming first.** The
   retry sweep that is the safety net for label creation only looks at orders in `Paid`, so an
   order an admin advances to `Printing` before its label exists falls out of the net silently.
   Verified: both filters and the alarm. Not verified: the dispatcher leg. → **Confirm, then fix
   (~1h).** [D82](findings-v3.md#f9--🟠--d82--awbretryjobs--paid-filter-silences-the-only-never-got-a-label-alarm)

**Five more 🟠 need fixing but no decision from you:** the unmapped-500 log level is still
unpinned (the same gap as D49, one branch over), the nested-capture guard the last round added has
no test, nothing pins the sampler's production call site, and two documentation promises are false
— [D76, D78, D79, D83, D84](ledger.md#v3-findings-d74d102).

## Reasons to doubt this pass

- **Two manifest lenses are still owed, not waived** — `db-parity` and `frontend-ux`, unrun since
  [v1](review-v1.md). Nothing has yet checked this feature against PostgreSQL.
- **No decay curve exists for this feature.** Only one full blinded pass has ever run (v1, 37
  agents). v2 and v3 are anchored verifications with 6 and 4 agents, so the 39 → 34 → 29
  new-finding counts in [metrics.jsonl](metrics.jsonl) are **not** comparable.
- **A four-agent anchored pass found a 🔴.** That means cost was not the limit here — it is
  evidence that more remains, not that the code is nearly clean.
- **A verification pass cannot certify** — it can only say "these fixes held". This one says
  `request-changes`.
- **Three findings rest on reading, not running**: D77 (Sentry docs), D82 (one leg), D93/D94
  (topology). Everything else in the serious list was measured on this machine or in your CI log.
- **Two of my own measurements were invalidated and redone** — a text replacement corrupted a
  Romanian message and produced a misleading failure; caught because it broke a test outside the
  mutation. Recorded in [review-v3.md](review-v3.md#the-three-fix-diff-questions).
- Verification is deliberately **anchored, not blinded** — it looked where the fixes were.

## Filed automatically

18 minor findings (14 🟡, 4 ⚪) went to the [ledger backlog](ledger.md#v3-findings-d74d102).
One deserves your eye anyway: [D97](findings-v3.md) — nothing tests the new boot guard's abort
path at all, including the log line `DEPLOYMENT.md` §14.10 now tells operators to grep for.

## State

Loop re-armed by a 🔴 → **next pass is a fix round**; the quiet counter resets, and the branch
should not merge while CI is red.
