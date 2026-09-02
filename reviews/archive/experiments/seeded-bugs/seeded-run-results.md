---
type: seeded-experiment-results
status: complete — first run
date: 2026-07-04
model: opus (implanter + all 14 lenses); fable orchestrator/synthesis
seeds: 10 (2 serious, 4 medium, 4 minor) across 6 files
passes: A ("new maintainer cold"), B ("incident post-mortem"); 7 blinded lenses each
inputs: seeded-run-pass-A.md, seeded-run-pass-B.md (synthesized blind), seeded-bugs-key.md (sealed)
---

# Seeded-bug experiment — results (run 1)

## Headline

| Metric | Pass A | Pass B |
|---|---|---|
| Recall, serious (2) | 2/2 | 2/2 |
| Recall, medium (4) | 4/4 | 4/4 |
| Recall, minor (4) | 4/4 | 4/4 |
| **Recall, overall** | **10/10** | **10/10** |

Both differently-framed blinded passes found **all ten** seeds. Every seed → matching finding in
both passes (SEED-01↔A4/B4, 02↔A3/B3, 03↔A1/B1, 04↔A5/B5, 05↔A2/B2, 06↔A8/B8, 07↔A6/B6,
08↔A10/B10, 09↔A9/B9, 10↔A7/B7). Zero misses either pass.

## The sting: 100% recall means this run did NOT test what it was built to test

The experiment's primary target was **assumption 1 — independence** — measured by *miss
correlation*: when two passes miss bugs, do they miss the *same* ones (shared blind spot) or
different ones (real independence)? **With zero misses in either pass, miss correlation is
undefined. This run cannot speak to independence at all.** The seeds were too catchable for a
7-lens Opus fan-out. To test independence, run 2 needs *harder* seeds — subtler, deeper in
untouched collaborators, or in logic no comment contradicts.

Compounding it: **the implanter and the finders are the same model family (Opus).** An implanter
that thinks like the finders plants findable bugs. So 100% is partly a shared-brain artifact —
which is precisely the effect assumption 1 warns about, now working *in our favor* to inflate
recall. A stronger design has a *different* model (or a human) implant, so the finders can't be
primed by a co-thinker. Until then, read 10/10 as "an Opus fan-out reliably finds bugs an Opus
agent considers plausible" — encouraging, but not the independence proof the loop needs.

## What the run DID establish

**Assumption 3 (serious surfaces early/reliably): supported, still small-n.** Both serious seeds
(cross-tenant scope leak, over-broad retry catch) were caught by both passes, each by 5 of 7
lenses, at correct serious severity. Counting bolt-035's original real High, that's 3/3 serious
defects caught on first exposure. Modest, but all evidence points one way so far.

**Assumption 2 (severity calibration): reviewers err HIGH, never low — the safe direction.**
Of 10 seeds, ~4 were rated above their intended tier (SEED-03 and SEED-05 medium→serious;
SEED-07 minor→serious by some lenses; SEED-10 minor→serious/medium). **Zero were rated below
intended.** For a stop rule that files "minor" to a backlog and escalates "serious", under-rating
is the dangerous error (a serious bug silently backlogged) — and it didn't happen. The severity
devil's advocate is therefore *less* urgent than feared; if anything a "why is this serious?"
*deflation* check would cut the noise reaching the human. Caveat: SEED-10 (implanter labeled
"minor" doc-mismatch) was rated a real prod-500 risk by the DB lens — arguably the *reviewers*
were right and the seed label wrong, so "intended severity" is the implanter's intent, not
ground-truth severity.

**Manifest breadth is load-bearing — measured, not asserted.** The two DB/migration seeds
(SEED-06 column width, SEED-10 migration DDL) were reached *only* by the **DB-parity** and
**completeness-critic** lenses. The five "core" lenses (correctness, security-as-logic, quality,
requirements, tests) did not open the migration DDL. A narrow early-bolt-035-style review
(correctness + security only) would have **missed 2 of 10 seeds outright** — both prod-only
Postgres bugs. This is concrete support for the README's characteristic-driven manifest: "change
touches a migration → add the DB-parity lens" is not bureaucracy, it's the only reason those two
were found.

**Convergence within a pass is high for core logic.** Seeds 01–05 (the OrderService/filter
cluster) were each independently raised by 5–6 of 7 lenses per pass. High redundancy there; the
long tail (migration, log level, dead code) rode on 2–4 lenses.

## Free side effect: real non-seed findings

Both passes also surfaced genuine pre-existing issues that are NOT seeds (set aside per protocol,
not counted in recall): the global-unique-index cross-tenant oracle (the real accepted-deferred
P10), decimal truncation in `(long)(TotalRon*100)`, the legacy processor recovery-replay URL rebuild
(deferred BUG-2), `OrderNumber` `HasMaxLength(20)` configured twice, the PostgreSQL order-number
global-count (no per-year reset), and gateway-502 absent from `ProducesResponseType`. A couple
(double HasMaxLength, the 502 doc gap) look genuinely new and worth a real ticket.

## Suite strength (from the implanter's log)

The implanter could NOT plant 5 other intended bugs because existing regression tests catch them
outright (re-widened idempotency catch, broken free+insert atomicity, dropped owner predicate,
dropped GetOrderDetail owner check, Subtotal-vs-Total divergence). The bolt-035 suite genuinely
covers those classes — the seeds that survived are exactly the ones in the suite's blind spots,
which is why review (not CI) is the tool that caught them.

## Actions for run 2 (harder seeds, cleaner independence)

1. **Different implanter model (or human).** Break the shared brain between planter and finder.
2. **Harder seeds** aimed at producing *misses*: subtle logic in unchanged collaborators, bugs
   with no contradicting comment, cross-file invariants. Independence is only measurable once
   something is missed.
3. **Keep the DB-parity + completeness lenses** — they were the sole catchers of 2/10; drop them
   and recall falls to 8/10 on this very set.
4. Consider a severity *deflation* pass (reduce human noise) over the devil's-advocate inflation
   pass (guards a direction the reviewers already err safely on).
