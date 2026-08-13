---
type: owner-summary
target: 044-045-observability
pass: 1
pass-type: discovery
commit: 5cac465
date: 2026-07-31
decisions-needed: 6
---

# Owner summary — 044-045-observability v1

First discovery pass over the tracing, metrics and error-tracking work at `5cac465`. It found 39 defects: 9 High, 14 Medium, 9 Low, 7 Cleanup ([review-v1.md](review-v1.md); detail per D# in ledger.md). Verdict: `request-changes`. Nothing bites today — both features sit behind flags that are `false` in the shipped config, so every defect fires on the flag flip, not now.

## Needs your decision

The 23 High and Medium defects collapse into 6 choices. Counts and D#s are exact.

1. 🔴 The `/metrics` page would be readable by anyone on the internet — PPW-336, PPW-345, PPW-347, PPW-358. The gate checks the caller's network address, but `Caddyfile:13` proxies every path, so every request looks like it comes from Caddy. Allow-listing Caddy — the only way to make scraping work — lets everyone in. Suggested: block `/metrics` at Caddy or move it to a separate internal port, ~1–2h, plus a test that the gate rejects a proxied request. Not a code-only fix.
2. 🔴 Three separate paths send customer emails and live guest session tokens to Sentry — PPW-337, PPW-338, PPW-339, PPW-357. Each was reproduced against the real Sentry SDK by a verifying agent. Performance traces skip the cleaner entirely; the cleaner never looks at the URL query; header matching is case-sensitive, so on HTTP/2 the guest token survives. Suggested: fix as one cluster and invert the approach — strip everything, add back what triage needs, ~3–4h. Fixing them one at a time invites a fourth leak.
3. 🔴 Both headline features do not work, and the tests cannot see it — PPW-340, PPW-341, PPW-343, PPW-344. Per-route sampling can never match a route, so every route traces at 100%. "Errors are always sampled" is dead code. A verifying agent deleted the whole Sentry PII cleaner from `Program.cs:56-57` and the Sentry tests stayed 32/32 green; deleting a metric call site left all 1001 green. Suggested: fix PPW-341 before or with PPW-340 — correcting the sampler makes PPW-341 lose more error traces, not fewer. ~1 day including tests that would catch this.
4. 🔴 A charged customer whose order never becomes Paid is invisible — PPW-342. The uncovered payment-webhook branches record no metric and no log above Debug, so the payment SLO still reads 100%. Suggested: fix now, ~1h; it is a terminal `else` with a warning log and a counter.
5. 🟠 Nine more Medium defects with real operational bite — PPW-346, PPW-348, PPW-349, PPW-350, PPW-351, PPW-352, PPW-353, PPW-355, PPW-356. The worst three: enabling tracing without an OTLP endpoint silently prints full SQL to stdout in production; five of eight Grafana panels query metric names the code never emits, so the dashboard is permanently blank; Sentry's own failures are silent, so an exhausted quota reads as "no errors". Suggested: one fix round together with the High defects, ~1 day.
6. 🟠 The 1461-green test result is not reproducible — PPW-354. The new test factories set process-wide environment variables and never restore them, while xUnit runs test classes in parallel, so which hosts boot the real Sentry SDK changes run to run. Suggested: fix early in the round — it undermines every other test result you will be shown.

## Reasons to doubt

- 2 of 11 manifest lenses did not run: `db-parity` and `frontend-ux`. Justified — no migration, no UI file in scope — but owed, not waived; a certification must fold them in.
- No trend yet. This is the first line in [metrics.jsonl](metrics.jsonl) for this target, so the finding-decay signal that gates the stop rule has no data here.
- 7 findings skipped the adversarial check: PPW-336, PPW-340, PPW-341, PPW-342, PPW-345, PPW-346, PPW-354. Accepted because 3–6 lenses found each independently. The synthesizer rechecked all seven by hand and they held, but that is not an independent agent. Convergence is a precision signal, not proof.
- 2 findings are plausible, not confirmed: PPW-367, PPW-368. The 6 Cleanup findings PPW-369–PPW-374 were never verified at all — Cleanups get no skeptic by design.
- The repo's citation scanner under-reports. It says 0 comment-citation leaks, but its pattern misses the `bolt 044` / `story 003` / `intent 020` forms. There are 77 of those in `src/` comments, two in bolt-045 files. That gap belongs to the `system` target's tooling, and it is why PPW-369 and PPW-370 disagree with the clean scan.
- Blinding is enforced by prompt only; no tool verifies the lenses stayed out of `reviews/`.
- A discovery pass cannot certify. `request-changes` is the strongest statement available here; "the feature is clean" needs a later full pass after the fixes.

## Filed automatically

16 defects (9 🟡, 7 ⚪) went to the ledger backlog as PPW-359–PPW-374 and are not part of the fix round; each is described on its ledger.md row. One deserves your eye anyway: PPW-360 — tracing attaches full SQL text and exception messages to spans sent to the OTLP collector, with no cleaner on that path at all. It is a second data-egress route that this branch's privacy work never looked at.

## State

The ledger now holds PPW-336–PPW-374, all newly minted by this pass: 23 High and Medium rows open, 16 Low and Cleanup rows at backlog. The router proposes a fix round next — open High and Medium defects with no resolution. This is a full-loop target, so it ends at certification: a separate, explicitly gated spend, and not before the nine High defects are fixed and independently verified.
