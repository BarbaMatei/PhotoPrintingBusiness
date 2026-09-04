---
type: owner-summary
target: 054-dependency-hardening
pass: 3
pass-type: delta-discovery
commit: a33b39d
date: 2026-09-05
decisions-needed: 2
---

# Owner summary — 054-dependency-hardening v3

Four blinded lenses re-read the whole fix diff since v1 (`e1febe5..a33b39d`, 23 files) and eight
skeptics tried to disprove what they claimed. Nothing the fix round repaired came back broken:
17 candidates returned, one was refuted outright, and 11 are new — 2 🟠 and 9 🟡, verdict
`approve-with-followups` ([review-v3.md](review-v3.md)). Both 🟠 are missing tests, not broken
behaviour: nothing proves the two promises §16 of `docs/DEPLOYMENT.md` makes about this bolt.

## Needs your decision

1. **Two missing tests, or let them wait for the pre-certification sweep** — 🟠
   [PPW-747](ledger.md) and [PPW-748](ledger.md). PPW-747: the whole point of trusting Caddy is
   that each visitor gets their own rate-limit budget, and that holds only because
   `src/PhotoPrint.API/Program.cs:375` runs before `:380`. Swap those two lines and the site is
   back to one bucket for everyone, with every test still green. PPW-748: the metrics-scrape
   exception is only proven in a test file outside this bolt's scope, whose assertion never
   mentions forwarded headers. One test each, about an hour together — or they queue and drain
   before any certification.
2. **The deferred auth rate limits were re-raised and re-affirmed** — 🟠
   [PPW-712](ledger.md). Two lenses argued this is now urgent because the round switched proxy
   trust on. It is not: `UseRateLimiter()` still runs before `UseRouting()`
   (`src/PhotoPrint.API/Extensions/SecurityExtensions.cs:122`,
   `src/PhotoPrint.API/Program.cs:393`), so those named policies still never execute — the same
   reason the deferral to intent 029 / bolt 063 was recorded in the first place. Nothing to do
   unless you want PPW-711 and PPW-712 brought forward as one change (~2h); they may not ship
   apart.

## Reasons to doubt

- Ten of the eleven new rows were read by a lens and never handed to a skeptic — the delta
  budget allows skeptics on serious candidates only ([metrics.jsonl](metrics.jsonl)). They are
  recorded as reported, not proven. Six sub-claims were checked by hand during synthesis and
  hold; they are marked as such on their ledger rows.
- Eight of the eleven are defects inside the round's own fixes ([review-v3.md](review-v3.md)),
  so this pass measures the fix round as much as the bolt: a seed rate the router can now use,
  but on one round only.
- `race`, `db-parity` and `frontend-ux` have still never run on this target. `race` is the one
  that matters: [PPW-749](ledger.md) and [PPW-739](ledger.md) are both about concurrent state in
  a process-wide singleton, and only the correctness and observability lenses have looked at it.
- `Dockerfile` and `.github/workflows/deploy.yml` reached no lens in either pass, which is part
  of what [PPW-757](ledger.md) reports: the new dependency-audit gate is asserted as a command
  string and never executed, and the image build restores without it.
- One candidate was refuted outright — a claim that 12 of the 23 changed files reached no lens.
  They did, through a second diff artifact the finder did not see ([review-v3.md](review-v3.md),
  "Refuted").

## Filed automatically

Nine 🟡 rows went to the backlog on [ledger.md](ledger.md). Four of them —
[PPW-751](ledger.md), [PPW-753](ledger.md), [PPW-754](ledger.md), [PPW-755](ledger.md) — are all
in the logging configuration the round rewrote to fix the 🔴 that hid production logs, and they
are best fixed in one sitting. [PPW-754](ledger.md) is the one to note: the new production log
file is written into the container's throwaway layer, so the 30-day retention the tests assert
does not survive a redeploy.

## State

The ledger holds 47 rows: 11 verified, 2 deferred, 34 open. This run is soft-stopped after this
pass's records on your word, so nothing is routed next; the loop resumes from the records, where
the queued 🟠 (PPW-747, PPW-748) and the never-run `race` lens both stand between this target
and any certification.
