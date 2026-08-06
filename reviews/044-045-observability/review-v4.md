---
type: review
target: 044-045-observability
version: 4
supersedes: review-v3.md
pass-type: verification
commit: dc203c7
code_tip: f0aadd7
answers: resolution-v3.md
verdict: approve-with-followups
date: 2026-08-06
---

# Review v4 — 044-045-observability (verification of the v3 fix round)

Anchored, per-fix verification of [resolution-v3.md](resolution-v3.md) at `dc203c7`. The v3 pass
named 29 findings; the fix round took the 🔴 and the ten 🟠 (D74–D84) and fixed all eleven. This
pass asks one question per fix — *did it hold?* — plus the runbook's three fix-diff questions per
cluster. It is **not** a fresh review of the feature.

**Independence.** Run from a fresh session with no fix-round context, which is what
[resolution-v3.md](resolution-v3.md#process-note-the-re-reviewer-must-weigh) asked for: the v3 pass
and the v3 fix round shared one session. Source at the branch tip `f0aadd7` is byte-identical to
`dc203c7` (only `reviews/**` changed since), so verification ran in place rather than on a detached
checkout.

## Verdict: approve-with-followups

**Eleven of eleven fixes hold.** Every behaviour and test fix was proven by reverting it and
watching the predicted test go red; the two doc-only fixes were verified by reading the shipped text
against the code. **review-v3's blocker is genuinely gone:** the `ci` workflow is green on
`ubuntu-latest` at the branch tip, after six consecutive red runs.

| Fix | Ledger | Outcome |
|---|---|---|
| F1 scrape-listener platform parsing (**was the blocker**) | D74 | **verified** — five mutations, plus CI green on the platform that is the only real proof |
| F2 client abort vs dependency failure | D75 | **verified** for the discriminator; both invariants it rests on are unpinned (F2 below) |
| F3 unmapped-500 log level | D76 | **verified** |
| F4 Sentry inbound trace decision | D77 | **verified** at the wiring; the `-0` direction and the `rate=0` off switch are open (F5, F6) |
| F5 nested metric-capture throw | D78 | **verified** |
| F6 sampler call-site pin | D79 | **verified** |
| F7 SLO 3 query vs prose | D80 | **verified**, including that `ok`/`duplicate` stay build-checked; the `or vector(0)` guard is unpinned (F1) |
| F8 SLO 1 caveat on doc and panel | D81 | **verified** (doc-only); the numbers it states are wrong (F9) |
| F9 AWB give-up sweep coverage | D82 | **verified** for the alarm; the re-enqueue boundary is unpinned (F7) |
| F10 `metrics.md` build-check claim | D83 | **verified** (doc-only) |
| F11 dashboard metric-name parser | D84 | **verified** |

**11 verified · 0 declined · 0 reopened · 3 backlog rows close · 18 new findings (0🔴/4🟠/10🟡/4⚪).**

**The loop re-arms** — not on a blocker, but on fix-caused 🟠. The two that matter most are the same
shape as this round's own headline: **a mechanism that is the entire point of a fix, with nothing to
stop it being undone.** The `or vector(0)` guard that the round's second micro-review added — after
catching that the fixer's own SLO 3 query would have read "No Data" while healthy — can be deleted
with 1133 tests green. So can either of the two invariants F2's new deadline discriminator depends
on, and breaking one of them restores D75 invisibly.

**And a gate nobody was watching, one workflow over.** review-v3's blocker was `ci` red. `ci` is
green now. But `secret-scan` has failed on **every pull-request run of this branch** — nine runs,
back past the v2 review commit — because gitleaks flags a fabricated test token in a bolt-045 test
file that `.gitleaks.toml` does not allowlist. The PR still cannot show all-green, and this was true
through two full review passes and a fix round without being noticed (F4).

## How each fix was proven

Seventeen measurements: thirteen revert-and-rerun proofs, three claim probes, and one positive DI
probe. Each failing set was **predicted before the run**, and every run was wide enough to show
collateral — the scoped observability namespaces, **1133 tests** (`Integration` +
`Unit.{Observability,Middleware,Configuration,Validators,Services,Controllers,BackgroundJobs}`), green
before, between and after every mutation.

| # | Mutation | Predicted | Measured |
|---|---|---|---|
| 1 | D74: `IsSocketOrPipe` always returns false | 0 (thought it redundant) | **1 — the `http://pipe:/metrics` case. Prediction miss, see below** |
| 2 | D74: port-0 exclusion removed | 1 | 1 — `A_dynamic_port_is_not_counted_as_a_listener` |
| 3 | D74: the new "no TCP port" refuse verdict → `return null` | 1 | 1 — `A_host_serving_no_tcp_port_at_all_refuses_a_scrape_port` |
| 4 | D74: TestServer empty-address carve-out removed | ≥1 | 3 — its own unit test plus the two TestServer integration tests that set a non-zero scrape port |
| 5 | D74: `LogCritical` → `LogWarning` on the refusal | 1 | 1 — the real-Kestrel boot test |
| 6 | D75: old `GetBaseException() is not TimeoutException` filter restored | 1 | 1 — `ValidateAsync_DeadlineElapsedThenTheCallerAborted_…` |
| 7 | D75: `ct` passed to `GetAsync` instead of the linked deadline token | 0 (claimed gap) | **0 — 1133 green**, run 38 s vs ~20 s: both deadline tests passed via the 15 s backstop |
| 8 | D76: unmapped-500 `LogError` → `LogWarning` | 1 | 1 |
| 9 | D77: `o.TracesSampler` deleted | 1 | 1 |
| 10 | D78: nested-capture throw made unreachable | 1 | 1 |
| 11 | D79: production call site re-wrapped in `ParentBasedSampler` | 1 | 1 |
| 12 | D82: give-up query narrowed back to `Paid` | 1 | 1 |
| 13 | D84: first-`}` regex restored in `MetricNamesIn` | 1 | 1 |
| 14 | D80: `result="duplicate"` mistyped in `slos.md` | ≥1 | 1 intended (`Every_queried_label_exists_on_the_series_it_filters`) + 1 unrelated flake |
| 15 | D80: both `or vector(0)` guards deleted | 0 (claim probe) | **0 — 1133 green** → F1 |
| 16 | D75: registered `HttpClient.Timeout` back to 5 s | 0 (claim probe) | **0 — 1133 green** → F2 |
| 17 | D82: re-enqueue query widened to `Paid \|\| Printing` | 0 (claim probe) | **0 — 1133 green** → F7 |
| 18 | DI: resolve `IGoogleTokenValidator` from the real registration | green | green — the defaulted `TimeSpan?` resolves; claim settled, probe deleted |

**Sixteen of seventeen predictions matched. One did not, and it is recorded rather than smoothed
over:** I predicted mutation 1 would leave the suite green, on the theory that `BindingAddress.Parse`
already treats socket and pipe addresses as port 0 and the new `port != 0` filter therefore absorbs
them. It does not — a named-pipe address gets the **default port 80**, so without the prefix check
rule 3 never fires and the verdict is `null`. `IsSocketOrPipe` is load-bearing. The tests lens
reached the same wrong conclusion independently and filed it as a finding; the measurement refutes
both of us (see [findings-v4.md](findings-v4.md#corrections-to-lens-claims-on-measurement)).

**Two measurement hygiene notes.** Mutation 14 produced collateral —
`ReliableEmailServiceTests.SendAsync_FailedSend_QueuesEmailToDatabase` — which a typo in a markdown
file cannot cause; it passed in the three later wide runs and in isolation, so it is a flake, filed
to [inbox.md](../inbox.md). It is the **second** flake in the email area, after the one
`resolution-v3` filed, and it surfaced the same way: unexplained collateral in a mutation run.
Mutation 13's first attempt failed to compile because the harness lost a backslash level writing the
pattern; it was redone with an escape-free equivalent regex. Every mutation was applied with
byte-level, CRLF-aware replacement after review-v3's re-encoding incident.

## The three fix-diff questions

Asked by three anchored lenses over the saved fix diff (behaviour; Sentry-SDK/OTel/hosting;
tests-contracts-docs). Their claims are recorded as findings **only where this pass could confirm
them**; four were corrected on measurement or on reading and are recorded as corrected. Neither lens
had the `LSP` tool available this session (`select:LSP` returned no match), so both fell back to
grep plus direct reads — worth knowing when weighing their symbol-level claims.

- **Class or instance** — the `== OrderStatus.Paid` class is genuinely swept: nine other status
  filters checked, all already correct, and the two remaining `Paid`-only sites are the disclosed
  boundaries. But the **cancellation-discrimination** class — the inference F2 removed — was not
  swept, and two sibling sites still make it (F11). F7's class is open on two more SLOs (F3).
- **New surface at the bar** — this is where the round is weakest, and the pattern is consistent:
  the new mechanisms have sized defaults and good documentation, and almost none has a failure-mode
  test. The `or vector(0)` guards, the `HttpBackstop > RequestDeadline` invariant, the deadline
  actually being passed to the request, the re-enqueue boundary, the panel description, the `status=`
  log field and both Caddyfile strips are all deletable with a green suite (F1, F2, F7, F16). Five of
  those seven were measured, not argued.
- **Regression** — three, all bounded: rule 3 now aborts boot on a unix-socket-plus-TCP-metrics
  topology and prints a message that is false for it (F8); `TracesSampleRate=0` no longer switches
  performance monitoring off (F5); and `slos.md`'s own status block now presents an exhaustive-sounding
  pair of caveats that omits a third of the same kind (F3). Ordinary sampling volume is **unchanged** —
  confirmed from the 4.13.0 source: one random draw at the same rate either way.

## Findings

Full detail, evidence and failure scenarios in [findings-v4.md](findings-v4.md); canonical
identities in [ledger.md](ledger.md).

| F# | Sev | D# | Title | Cause |
|---|---|---|---|---|
| F1 | 🟠 | D103 | SLO 3's `or vector(0)` guards are pinned by nothing — the defect the round's own micro-review caught can return green | fix-caused (D80) |
| F2 | 🟠 | D104 | Both invariants F2's discriminator rests on are unpinned; breaking either leaves 1133 green and one of them restores D75 | fix-caused (D75) |
| F3 | 🟠 | D105 | SLO 4 and SLO 5 carry the denominator defect F7 fixed, while `slos.md` now says there are "two caveats that matter" | pre-existing + fix-caused doc half (D81) |
| F4 | 🟠 | D113 | `secret-scan` fails on every pull-request run of this branch — a bolt-045 test token gitleaks flags and `.gitleaks.toml` does not allowlist | pre-existing |
| F5 | 🟡 | D106 | `Sentry:TracesSampleRate=0` no longer switches performance monitoring off, only its output | fix-caused (D77) |
| F6 | 🟡 | D107 | The booted-host sampler test covers only the "caller says sampled" direction; the `-0` blinding half is unpinned | fix-caused (D77) |
| F7 | 🟡 | D108 | The re-enqueue query's `Paid`-only scope — an explicit owner decision — is pinned by nothing, and the new test's second assertion cannot fail | fix-caused (D82) |
| F8 | 🟡 | D109 | Rule 3 now aborts boot on a unix-socket API plus a dedicated TCP metrics port, with a message that is false for that topology | fix-caused (D74) |
| F9 | 🟡 | D110 | The dilution numbers now on the operator-facing panel are wrong: 5,760 is `/metrics` alone, and the real floor is ~94.5%, not ~99.7% | fix-caused (D81) |
| F10 | 🟡 | D111 | SLO 3's documented query has no time window while its heading says "rolling 7 days" and its dashboard twin uses `rate(…[7d])` | pre-existing shape |
| F11 | 🟡 | D112 | F2's class unswept: `AwbCreator` and `ShipmentTrackingJob` still infer "our own timeout" from a token flag | pre-existing |
| F12 | 🟡 | D114 | The new real-Kestrel boot test runs un-collectioned in the parallel pool and behaves differently under `ASPNETCORE_ENVIRONMENT=Development` | fix-caused (D74), extends D51 |
| F13 | 🟡 | D115 | `system-architecture.md:45` still describes the old 5 s `HttpClient` timeout — the standard CLAUDE.md routes readers to | fix-caused (D75) |
| F14 | 🟡 | D120 | The give-up alarm re-pages every order in the window after a restart, over a population F9 enlarged | pre-existing, amplified (D82) |
| F15 | ⚪ | D116 | `DEPLOYMENT.md:949` still reasons from the availability target as if the denominator were customer traffic | fix-caused (incomplete D81) |
| F16 | ⚪ | D117 | The panel `description` and the `status=` log field are unpinned; the description cites "D46", an id operators cannot resolve | fix-caused (D81/D82) |
| F17 | ⚪ | D118 | Comment-rule residue: two two-line narrating comments and a double blank line | fix-caused (D77/D82) |
| F18 | ⚪ | D119 | `resolution-v3.md`'s F11 note overstates the parser unification — three parsers exist, one keeps its own regex | records accuracy |

## Deferrals

All standing terminal decisions re-affirmed — **54 stand, three close.** Rows whose cited files the
fix round did not touch stand unchanged since `7e28317`, verified mechanically from the diff; the
seventeen rows citing a file this round changed were each re-read.

- **D97 closes** — "Nothing exercises `StartedAsync`" is no longer true. The real-Kestrel boot test
  added in `d1ffee7` reads the addresses, asserts the throw and pins the `Critical` line (mutation 5
  proves the last). It was recorded `backlog` **and flagged to the owner in `summary-v3` as the one
  minor worth their eye** — it was already closed when that was written.
- **D100 closes** — `docs/DEPLOYMENT.md:1183` now names the `ASPNETCORE_URLS` prerequisite. Recorded
  `backlog`, shipped in `d1ffee7`.
- **D89 closes** — the seeding obligation is now documented in `metrics.md` step 10, which is exactly
  what D89 asked for.
- **D46 re-affirmed** at `dc203c7`: SLO 1's query still carries no route or host filter and the
  instrumentation still sets no `Filter`. The document no longer misleads a reader about it (D81
  verified), but the numbers it now states are wrong (F9).
- **D88 stands, extended** — the escaped-quote gap is still open in `LabelUsagesIn`, and the two
  query-side parsers now disagree with each other.
- **D51 stands, extended** — this round added a second live `TracerProvider` build plus an
  un-collectioned real-Kestrel boot (F12).
- **D35/D39 stand, extended** — `Program.cs` gained one more inlined Sentry option and a two-line
  comment (F17).
- Line-reference drift corrected in the ledger for D46, D93 and D102, whose cited files moved.

## Tests

- Local, Windows, scoped to the observability namespaces: **1133 passed / 0 failed**, 10 MinIO
  skips, at `dc203c7` with a clean tree — before, between and after every mutation. This matches the
  fix round's own recorded final run exactly.
- **CI, `ubuntu-latest`, at the branch tip `f0aadd7`: `ci` GREEN** on both the push and the
  pull-request run. Green also at `86f4cc1`. This is the proof that D74 held, and the only platform
  that could give it.
- **CI, `secret-scan`, pull-request event: RED at the branch tip and on all nine PR runs of this
  branch** (green on every push run, which scans only new commits). One gitleaks finding, a
  fabricated test token — F4.
- One flake filed to [inbox.md](../inbox.md):
  `ReliableEmailServiceTests.SendAsync_FailedSend_QueuesEmailToDatabase`, failed once under parallel
  load in mutation 14, passed in three later wide runs and in isolation. Not caused by this round.
- Frontend not run — backend-only change, per the repo's scoped-run rule.
- Manifest lenses `db-parity` and `frontend-ux` remain **owed, not waived**.
