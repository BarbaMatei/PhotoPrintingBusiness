---
type: owner-summary
target: 015-sameday-shipping
pass: 2
pass-type: verification
commit: 727a018
date: 2026-07-27
decisions-needed: 2
---

# Owner summary — 015-sameday-shipping v2 (verification)

The fixes held. All 21 fixed findings were re-checked against the code and **0 reopened**
([review-v2.md](review-v2.md)); backend `893/893` and frontend `451/451` are green
([metrics.jsonl](metrics.jsonl)). Twelve fixes were proven by putting the bug back and watching the
exact test go red; the other nine were verified by reading the code plus the independent design
reviews done during the fix round. The whole Sameday path is still off behind two `false` flags.

## Needs your decision

1. **Set your real Sameday service codes before switching the jobs on.** The label-booking now
   sends a service code per delivery type, but the actual numbers are specific to your Sameday
   contract and aren't in the code — I left `Sameday:CourierServiceId` / `LockerServiceId` as
   placeholders (`7`). → **Action before go-live: fill both from your Sameday account.** (D5,
   [appsettings.json](../../src/PhotoPrint.API/appsettings.json), [resolution decision](resolution-v1.md).)
2. **Do admin-marked-paid orders need the confirmation email?** When you mark an order paid by hand
   (the cash case), it now gets a shipping label, but — unlike online card payments — it does **not**
   send the customer the "order confirmed" email (the photo-backup step self-heals; the email has no
   backstop). → **Fix now (~1 line) or leave it manual — your call.** ([AdminOrderService Paid path](../../src/PhotoPrint.API/Services/AdminOrderService.cs#L131).)

## Reasons to doubt

- **Same session did the fixing and the checking** — not the intended independent verifier. The
  revert-and-rerun part is bias-proof (a test either reddens when the bug returns or it doesn't), and
  the design-level review came from **independent fresh agents** during the fix round, but a truly
  separate re-review would be stronger.
- **One fix (D3, the tracking-job database-connection split) has no automated red-on-revert test** —
  the in-memory test database can't reproduce that concurrency faithfully. It's verified by the
  independent design check + code reading, not a failing test ([review-v2 method](review-v2.md)).
- **A verification pass cannot certify** — it confirms "the fixes held," never "the feature is
  clean." A **certification pair** (two independent blinded passes on a frozen commit) is still owed
  before this feature is closed, per its full-loop tier.

## Filed automatically

22 Low/Cleanup findings (D20–D31, D33, D35–D41) remain in the ledger **backlog**
([ledger.md](ledger.md)). Worth grooming before you enable on Postgres: **D22/D23/D40** (dual-database
column-type parity).

## State

Router: verification clean (0 reopened) → the remaining pass is **certification** (full-loop tier,
~2× a normal pass, **waits for your explicit go-ahead**). Nothing forces it now — the code is dormant.
Say the word when you want the certification pair, or park it until you're closer to enabling Sameday.
