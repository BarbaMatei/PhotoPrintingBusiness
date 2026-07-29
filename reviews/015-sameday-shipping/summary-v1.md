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

First full review of the Sameday courier integration (bolts 036 + 037). **Verdict:
`request-changes`** ([review-v1.md](review-v1.md)). Nothing here is live: `Sameday:Enabled` **and**
`Sameday:Jobs:Enabled` are both `false` ([appsettings.json:78,86](../../src/PhotoPrint.API/appsettings.json#L78)),
so the manual-fallback courier is what runs today. These are gates on **turning Sameday on**, with
runway to fix first. Both test suites pass (862 / 448).

## Needs your decision

1. **The AWB (shipping-label) creation isn't safe to run on more than one server.** The "don't create
   a duplicate label" key sent to Sameday is the same shop-wide value for every order, so retries and
   two-server races can mint duplicate paid parcels — or, if Sameday honours the key, hand order #2
   order #1's label. There's also no database guard on the write and the tracking poller shares one DB
   connection across parallel work (crashes the tick). This is the multi-replica safety ADR-015/016
   *claim*, unbuilt. → **Fix before enabling, ~half a day:** per-order reference + guarded write +
   per-order DB scope. [D1–D3](ledger.md), [D12, D15](findings-v1.md#f12-d12--awb-persisted-onto-an-order-cancelled-during-the-sameday-call).
2. **Every Easybox (locker) order would silently get no label.** Recipient name/phone reach Sameday as
   `null` (a guard that was meant to catch this never fires), the locker's Sameday id is dropped, and
   the courier service code is hardcoded to the locker value. → **Fix before enabling, ~half a day:**
   decide where recipient validation lives (checkout vs the mapper) and fix consistently.
   [D4, D5](ledger.md), [D13, D32](findings-v1.md#-medium).
3. **Orders marked paid by an admin (offline / bank transfer) never get a label** — the AWB trigger
   lives only in the two payment webhooks, not the status transition, and that path also blocks the
   safety-net retry from finding them. → **Decide:** route the AWB trigger through the transition
   chokepoint, or accept "online payments only" for now. [D10, D11, D17](ledger.md).
4. **"Green tests" don't prove the newest wiring.** Five tests pass for the wrong reason — the
   webhook→AWB link, the ADR-016 delivery-race invariant, and the DB save can each be deleted with the
   suite staying green. → **Fix as each finding's regression test** (a fix isn't done until its test
   goes red when reverted). [D6, D7, D16](ledger.md), [D34](findings-v1.md#f34-d34--production-resilience-pipeline-rate-limiter-active-never-exercised).
5. **A Sameday outage would be invisible, and the request throttle doesn't throttle.** The commonest
   tracking failure is swallowed with no log, and the rate limiter is rebuilt on every call so it never
   limits (and leaks a timer each time). → **Fix before enabling, ~1–2h:** log the swallowed path;
   build the limiter once. [D14](findings-v1.md#f14-d14--samedayunreachableexception-swallowed-with-no-log--tracking-stalls-silently), [D9](findings-v1.md#f9-d9--rate-limiter-re-created-inside-the-per-execution-delegate--inert--timer-leak).

## Reasons to doubt

- **All 11 manifest lenses ran** — no lens owed ([metrics.jsonl](metrics.jsonl)). But most findings are
  **single-lens** (only D1 had 6-lens agreement; D2/D3/D8/D9/D20 had 2–3), each resting on one skeptic
  trace.
- **The headline D1 got no skeptic** (6-lens agreement is auto-accepted). I confirmed it myself against
  [SamedayClient.cs:104](../../src/PhotoPrint.API/Services/Sameday/SamedayClient.cs#L104) — it's real.
- **1 refuted** (D42, recorded), **1 plausible with one leg refuted** (D27), **3 hinted** by the shared
  dual-DB context (D22/D23/D40 — treat their convergence as prompted, not independent).
- **Postgres is unproven:** tests run on EF InMemory, so the migrations and the `timestamptz` delivery
  writes never execute against Postgres — a real offset-write may throw only in prod ([D23](findings-v1.md#f23-d23--dual-db-parity-migrations--timestamptz-cas-never-run-on-postgres--hinted)).
- This is **discovery, not certification** — full-loop tier owes a blinded certification pair before
  closure. Blinding is best-effort (no auditor yet).

## Filed automatically

22 Low/Cleanup findings (D20–D41) went to the ledger **backlog** — they don't block the fix round
([ledger.md](ledger.md)). Worth an early look regardless: **D22** — the `AwbLabelUrl` migration ships an
unbounded `text` column on Postgres instead of `varchar(500)`, worth grooming before you enable on Postgres.

## State

Router: latest review has open 🔴 → **next pass is a fix round** (the `/fix-review` skill, blocker-first:
D1–D5, then the Mediums). New 🟡/⚪ are backlog, not fix-round scope. After fixes land with their
regression tests, an independent verification pass re-reviews.
