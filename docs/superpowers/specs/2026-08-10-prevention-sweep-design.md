---
type: design-spec
topic: prevention sweep — ledger miner + ranked builder self-sweep
date: 2026-08-10
status: approved (owner, 2026-08-10, in conversation)
owner: Matei Barba
implements: docs/prevention-sweep-idea.md
---

# Prevention sweep — mine the ledgers, rank the mistake classes, gate construction on them

Goal: every new bolt starts from a short, self-updating, **data-ranked** list of the mistake
classes that historically cost the most — checked by the builder before requesting review,
instead of re-billed to the review loop (~300k discovery tokens + fix + verification per
serious finding).

## Pieces

**1. Class map sidecar — `reviews/state/defect-classes.jsonl`** (new). One JSON line per canonical
defect: `{"target", "d", "sev": high|medium|low|cleanup, "class": <slug>, "area": <slug>,
"title": <≤80 chars>}`. The miner reads only this file — never the ledgers (five generations
of prose formats). A later line for the same `(target, d)` supersedes the earlier one.
Area slugs: `storage · payments · orders · checkout · auth · sameday · observability ·
tests · frontend · docs · infra`.

**2. Taxonomy = definition-of-done.md's classes**, as slugs (the numbers are DoD's):
`caller-sweep(1) · second-path-symmetry(2) · bounded-resources(3) ·
multi-store-atomicity(4) · test-vacuity(5) · observability-floor(6) ·
artifact-lifecycle(7) · doc-sync(8) · one-constant-one-home(9) ·
error-contract-mapping(10) · frontend-auth-state(11) · recovery-liveness(12) · other`.
A new class may be proposed only with ≥3 members across ≥2 targets; otherwise `other`.
The taxonomy stays owned by definition-of-done.md.

**3. `reviews/lib/ledger-miner.mjs`** (new, plain node, sibling of the auditor). Aggregates
the sidecar per class: count by severity, weighted score (🔴 5 · 🟠 3 · 🟡 1 · ⚪ 0.5),
targets seen, up to 3 example `D#`s. Writes the ranked table into definition-of-done.md
between `<!-- miner:ranked-classes:start -->` / `:end` markers (render-records precedent);
`--area <slug>` prints a per-area ranking to stdout for bolt targeting; `--dry-run` prints
without writing.

**4. The feed stays mechanical.**
- `reconcile-findings` SKILL (ledger mode): when adding a NEW `D#` row, also assign its
  class from the taxonomy and append the sidecar line. Re-finds that change severity
  re-append with the new severity.
- `runbook-discovery.md` step 6: the hand-maintained "class seen in ≥2 targets gets a
  definition-of-done line" edge is replaced by the sidecar + miner.
- `bolt-process.md` Stage 4: hand-back names the **current top-5 ranked classes** swept and
  the test pinning each (definition-of-done's gate, made concrete and ranked).
- `records-auditor.mjs`: if the sidecar exists, every line parses and carries valid
  `target/d/sev/class` (warn-level for unknown class slugs).

**5. One-time backfill** (~290 rows; ~150–250k tokens, owner-approved). Four parallel agents,
one per ledger-bearing target (042-thumbnail-cache [archive], 043-cloud-storage-provider,
015-sameday-shipping, 044-045-observability), each classifying every `D#` row against the
taxonomy and returning raw JSONL. Main agent merges, spot-checks ~20 rows against the
ledgers, runs the miner, commits. **035 has no ledger.md** (predates the standard) — its
findings stay represented by DoD's existing prose evidence; noted, not backfilled.

## Success measure

`new-serious-per-v1-pass` (already recorded per discovery pass, the bolt-process KPI):
compare bolts built with the ranked sweep against 035/042/043/015/044-045 v1 counts
(7 · 11 · 7 · 19 · 23 serious).

## Out of scope

No new review passes or loop changes; no re-classification agents at miner runtime (script
stays plain); no DoD prose rewrites — the ranked table is additive, between markers.
