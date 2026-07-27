---
name: reconcile-findings
description: >-
  Match review findings to the target's canonical ledger (F# → D#) or match two parallel
  passes' finding lists for an overlap measurement. Use after a discovery pass's synthesis
  ("reconcile the findings", "map F# to the ledger", "update the ledger for <target>") or when
  measuring pass overlap ("compute the A/B overlap"). Decides same-problem vs new with a
  confidence and a one-line reason per match, links fix-residuals as lineage, attaches prior
  decisions to re-finds (never suppresses them), and splits when unsure. Must pass the
  bolt-035 ground-truth scoring gate before being trusted.
---

# Reconcile findings

Decide which findings are the *same problem* and which are *new* — the memory that lets the
review loop measure overlap, stop re-arguing settled decisions, and keep one canonical `D#`
per real defect per target.

## Modes

- **Ledger mode** (after a discovery pass): match the pass's `F#` findings onto
  `reviews/<target>/ledger.md`, then update the ledger.
- **Overlap mode** (two parallel passes on one frozen commit): match pass A's list against
  pass B's; report N_A, N_B, M (shared) per severity stratum. Only meaningful for passes
  against the same frozen commit — sequential audits on moving code have an open problem
  population and cannot feed overlap math.

## Inputs

1. The finding list(s): id, severity, file/line, one-line claim (from `review-v<n>.md` /
   `findings-v<n>.md`). If an item lacks an ID (a test-gap row, an aside, a fix-note
   observation), mint one before matching — ID-less findings escape any ledger.
2. `reviews/<target>/ledger.md` — every known `D#`, status, and prior decisions (ledger mode).
3. The code at the reviewed commit, read only to adjudicate close calls.

## Matching rules

- **Same problem = same defect mechanism at the same site.** Same file, same theme, same ID
  string, or same underlying code fact is NOT enough. Two different claims about one code fact
  are distinct problems (e.g. "the doc misstates this behavior" vs "the behavior is a bug" —
  they have different fixes).
- **Fix-residual ≠ re-find.** A new defect living inside the fix of an earlier finding is NEW,
  linked as lineage (`residual-of: D#`), never merged — chains run generations deep.
- **A defect inside code an earlier pass explicitly cleared or judged benign** is distinct,
  and the match note must say it re-opens that judgment.
- **A re-find of a decided item** (wont-fix / deferred / disputed / false-positive): match it
  and attach the prior decision verbatim — never suppress it. The record shows re-raises
  overturn the prior call more often than not (3 of 5).
- **When unsure, SPLIT.** A wrong merge inflates overlap → the loop stops early → a bug
  ships. A wrong split only costs another look. Prefer "NEW — possible remainder of D#" over
  a merge; that flag is more useful than either verdict.
- Severity never blocks a match (severity is mutable); mechanism + site decide.

## Output

One row per new finding:

| newId | D# / NEW | confidence (high·med·low) | reason (one line) |

plus `residual-of` lineage links, and the prior decision attached to every matched decided
item. Then:

- **Ledger mode:** add a row per NEW (next free `D#`), append this pass to each matched row's
  provenance, carry each finding's convergence count and `hinted` flag into the row.
- **Overlap mode:** N_A / N_B / M per stratum (serious = 🔴+🟠, minor = 🟡+⚪); exclude
  `hinted` findings from independence claims; list unknown-provenance items honestly instead
  of guessing.

## Trust gate

Before first trusted use — and after any material change to the rules above — run **blind**
against the eval set at
`reviews/archive/035-payment-idempotency/overlap-ground-truth.md` (scoring guide at its
bottom; the runner must NOT read that file or any text derived from it) and record the score
below. Any over-merge of the eval set's hard cases 1–7 is a failing run, whatever else scores
well.

## Scores

- **2026-07-27 — PASS** (blind run over the three 035 audits + resolutions, scored against
  `overlap-ground-truth.md`): identity recall 2/2 — the P01b three-way group including the
  composite-BUG-5 nuance, and P11↔INFO-2 across the resolution boundary — plus the P02
  test-gap match via a minted ID (v1:T8 ↔ v8:DB-1). **0 over-merges across hard cases 1–7**
  (case 7 produced the "possible remainder of P41" flag the key rates better than its own
  label); 0 over-splits of P01b. Stretch goals met: residual chains linked as lineage
  (P04→P05→P06, P08→P09, P12→P49, P34→P50, P19→P21) and all 5 decided-item re-raises carried
  their prior decision (P07, P10, P11, P24, P28). One cheap-direction miss: v5:QUAL-5's
  ID-less aside split from P02 where the key says same-gap.
