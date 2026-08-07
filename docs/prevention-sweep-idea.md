---
type: idea
status: future — owner-approved in principle, not scheduled
created: 2026-08-03
owner: Matei Barba
---

# Prevention sweep — stop repeat defect classes before they enter the review pipeline

**The idea in one sentence:** mine the review ledgers for the defect classes that keep
recurring, and hand the builder that ranked list as a mandatory self-sweep **before** a bolt
requests review — so known classes get caught at construction time instead of being
re-discovered, re-fixed, and re-verified by the loop.

## Why it pays

Every defect prevented at construction skips its entire downstream chain:

- ~300k discovery tokens to find it (measured cross-target average per serious finding),
- ~25 minutes of fix-round time (044-045-v1 rate),
- a verification slot to prove the fix,
- and the regression risk of the fix itself — fix-caused defects are the system's
  documented dominant cost driver.

This attacks the multiplier the review loop cannot touch from inside: fewer findings per
pass means fewer passes per MR.

## Evidence it would fire today

- The ledgers hold 200+ canonical defects (`reviews/*/ledger.md`) with severity and
  category — the raw material already exists and grows every pass.
- Classes visibly repeat across targets: "the test asserts nothing real" alone was 5 of 23
  serious findings on 044-045; storage-routing misses and guest-state regressions are
  review-history clusters old enough to have their own definition-of-done entries.
- The feedback edge exists but is hand-maintained and anecdotal: `runbook-discovery.md`
  step 6 says a class seen in ≥2 targets gets a `definition-of-done.md` line — kept by
  memory, unranked, and applied after review rather than before it.

## Shape of the tool

1. **Ledger miner** — a plain script (no agent), sibling of `records-auditor.mjs`: rolls up
   recurring defect classes across all `reviews/*/ledger.md` by area and severity, ranked by
   measured downstream cost (count × severity weight).
2. **Builder self-sweep** — the miner's top classes become a checklist the builder must
   sweep before requesting review, wired into the bolt process's hand-back gate; the sweep's
   findings are fixed as construction work, not review findings.
3. **The feed becomes mechanical** — the miner's output replaces the hand-maintained
   definition-of-done edge (same destination file, ranked by data instead of memory).

## Success measure

`new-serious-per-v1-pass` — already the bolt-process KPI recorded per discovery pass —
drops on targets built with the sweep, compared against the pre-sweep targets in
`reviews/*/metrics.jsonl`.

## Status

Owner-rated "really, really good" (2026-08-03) and queued for future implementation —
deliberately **after** the fix-round speed redesign
(`docs/superpowers/specs/2026-08-03-fix-round-speed-design.md`) lands and proves out.
Nothing is scheduled; the owner decides when.
