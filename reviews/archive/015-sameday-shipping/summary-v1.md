---
type: owner-summary
target: 015-sameday-shipping
pass: 1
pass-type: discovery
commit: 1765918
date: 2026-07-27
decisions-needed: 5
---

# Owner summary — 015-sameday-shipping v1

First full review of the Sameday courier integration, bolts 036 and 037, at `1765918` ([review-v1.md](review-v1.md); detail per D# in ledger.md). It found 41 defects: 5 High, 14 Medium, 16 Low, 6 Cleanup, plus 1 suspicion refuted. Verdict: `request-changes`. Nothing here is live — `Sameday:Enabled` and `Sameday:Jobs:Enabled` are both `false` in `appsettings.json`, so the manual courier fallback is what runs today and every finding is a gate on turning Sameday on. Both suites pass, 862 backend and 448 frontend.

## Needs your decision

1. 🔴 Shipping-label creation is not safe on more than one server — PPW-240, PPW-241, PPW-242, PPW-251, PPW-254. The "do not create a duplicate label" reference sent to Sameday is the same shop-wide value for every order, so retries and two-server races can mint duplicate paid parcels. If Sameday does honour the reference, the second order is handed the first order's label. There is also no database guard on the write, and the delivery poller shares one database connection across parallel work, which faults the whole tick. This is the multi-server safety ADR-015 and ADR-016 claim, unbuilt. Suggested: fix before enabling, about half a day — per-order reference, guarded write, one database scope per order.
2. 🔴 Every locker order would silently get no label — PPW-243, PPW-244, PPW-252, PPW-271. Recipient name and phone reach Sameday as empty, because a guard meant to catch that checks the wrong thing; the locker's own Sameday code is dropped; and the courier service code is hardcoded to the locker value. Suggested: fix before enabling, about half a day — decide once whether recipient validation lives at checkout or in the mapper, then apply it consistently.
3. 🟠 Orders marked paid by an admin, for offline or bank transfer, never get a label — PPW-249, PPW-250, PPW-256. The label trigger lives only in the two payment webhooks, not in the status change, and that path also leaves the paid timestamp empty, so the safety-net retry cannot find the order either. Suggested: decide — route the trigger through the status change, or accept online payments only for now.
4. 🟠 Green tests do not prove the newest wiring — PPW-245, PPW-246, PPW-255, PPW-273. Four tests pass for the wrong reason: the webhook-to-label link, the delivery-race rule, the database save and the request throttle can each be deleted with the suite staying green. Suggested: fix each as its finding's regression test — a fix is not done until its test goes red when the fix is reverted.
5. 🟠 A Sameday outage would be invisible, and the request throttle does not throttle — PPW-248, PPW-253. The commonest tracking failure is swallowed with no log at all, and the rate limiter is rebuilt on every call, so it never limits and leaks a timer each time. Suggested: fix before enabling, one to two hours.

## Reasons to doubt

- All 11 lenses ran, none owed ([metrics.jsonl](metrics.jsonl)). But most findings rest on a single lens: only PPW-240 had six-lens agreement, and PPW-241, PPW-242, PPW-247, PPW-248 and PPW-259 had two or three.
- PPW-240, the headline, got no adversarial check — six-lens agreement is auto-accepted. The synthesizer confirmed it by hand against `Services/Sameday/SamedayClient.cs:104`. That is not an independent agent.
- One suspicion refuted and recorded, one finding plausible with one leg refuted, three topics hinted by the shared project context (PPW-261, PPW-262, PPW-279) — treat their agreement as prompted, not independent.
- Postgres is unproven. Tests run on EF InMemory, so the migrations and the delivery timestamp writes never execute against Postgres, and a real offset write may throw only in production.
- This is discovery, not certification. A full-loop-tier target owes a blinded certification pass before closure, and blinding here is enforced by prompt only — no tool checks it.

## Filed automatically

22 Low and Cleanup findings (PPW-259 to PPW-280) went to the ledger backlog and do not gate the fix round; each is described on its ledger.md row. One deserves your eye anyway: PPW-261 — the label-link migration ships an unbounded column on Postgres instead of a 500-character one, worth grooming before you enable on Postgres.

## State

The ledger now holds PPW-240 to PPW-281, all minted by this pass: 19 High and Medium rows open, 22 Low and Cleanup rows at backlog, 1 false positive. The router proposes a fix round next, worst first: PPW-240 to PPW-244, then the Mediums. New Low and Cleanup rows are not in that scope. After the fixes land with their regression tests, an independent pass re-reviews them.
