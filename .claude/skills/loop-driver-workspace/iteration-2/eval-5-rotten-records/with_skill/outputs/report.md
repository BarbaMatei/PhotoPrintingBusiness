Stopped at an owner gate — no review pass ran. The records were broken first: the auditor flagged that pass 2 (the verification) never wrote its metrics line, so I repaired it before trusting any routing.

Repair: appended one new line to `reviews/096-rotten/metrics.jsonl` in the fixture, rebuilt from `review-v2.md` (verdict `approve-with-followups`, F1+F2 verified by revert-and-rerun, 0 reopened, 0 new findings, suites 12 passed / 0 failed). No past line was edited — append-only, per `metrics-schema.md`. Unknowns stayed `null` rather than guessed: the pass's cost was never recorded, and the line's date is the day I rebuilt it (2026-07-31), not the day the pass ran. The line's `notes` field says both. Auditor now exits clean; the one remaining warning is that this fixture has no `reviews/index.md`, so index pairing was skipped — expected here, and no `ledger.md` either.

Router state: latest review v2 · last pass v2 verification → approve-with-followups · 0 new serious · 0 reopened · resolution v1 resolved @406f0d9. Verification was clean, so the router returns a judgment call (exit 3) and I stopped there rather than pre-answering it.

Your call — was the v1 fix round delta-worthy or patch-grade?
- Facts: F1 was high (widget double-fires on retry), fixed with a per-widget retry token; F2 was medium (unbounded retry count), bounded at 3. Fixing a high-severity finding matches one of the router's three delta-worthy tests.
- Delta-worthy → delta discovery, ~0.6–1.2M tokens (5-lens cap, 600k output budget, both script-enforced).
- Patch-grade → the loop goes quiet and certification is next, which always needs your explicit go-ahead: ~4.0–4.6M tokens for a first-attempt blinded pair, or ~2.9M for a single re-certification pass.

I did not commit the repair to the fixture repo.
