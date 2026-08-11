---
name: reconcile-findings
description: >-
  Match review findings to the target's canonical ledger and mint a global PPW id for each
  new defect, or match two parallel passes' finding lists for an overlap measurement. Use
  after a discovery pass's synthesis ("reconcile the findings", "map the findings onto the
  ledger", "update the ledger for <target>") or when measuring pass overlap ("compute the A/B
  overlap"). Decides same-problem vs new with a confidence and a one-line reason per match,
  links fix-residuals as lineage, attaches prior decisions to re-finds (never suppresses
  them), and splits when unsure. Must pass the bolt-035 ground-truth scoring gate before
  being trusted.
---

# Reconcile findings

Decide which findings are the *same problem* and which are *new* — the memory that lets the
review loop measure overlap, stop re-arguing settled decisions, and keep one canonical
`PPW-<n>` per real defect.

## Modes

- **Ledger mode** (after a discovery pass): match the pass's finds onto
  `reviews/<target>/ledger.md`, then update the ledger and mint an id per new defect.
- **Overlap mode** (two parallel passes on one frozen commit): match pass A's list against
  pass B's; report N_A, N_B, M (shared) per severity stratum. Only meaningful for passes
  against the same frozen commit — sequential audits on moving code have an open problem
  population and cannot feed overlap math.

## Inputs

1. The finding list(s): id, severity, file/line, one-line claim — from the pass's synthesis
   output (the workflow's deduped canonical list; reconciliation runs *before* `review-v<n>.md`
   is written, so the review can reference the ids). These incoming numbers are the finders'
   own and stay in this session — they are never written to a file. If an item arrives with
   no number at all (a test-gap row, an aside, a fix-note observation), give it one before
   matching: an unnumbered finding escapes any ledger.
2. `reviews/<target>/ledger.md` — every known row, status, and prior decisions (ledger mode).
3. `reviews/state/id-counter` — the next free `PPW-<n>` (ledger mode).
4. The code at the reviewed commit, read only to adjudicate close calls.

## Matching rules

- **Same problem = same defect mechanism at the same site.** Same file, same theme, same ID
  string, or same underlying code fact is NOT enough. Two different claims about one code fact
  are distinct problems (e.g. "the doc misstates this behavior" vs "the behavior is a bug" —
  they have different fixes).
- **Fix-residual ≠ re-find.** A new defect living inside the fix of an earlier finding is NEW,
  linked as lineage (`residual-of: PPW-<n>`), never merged — chains run generations deep.
- **A defect inside code an earlier pass explicitly cleared or judged benign** is distinct,
  and the match note must say it re-opens that judgment.
- **A re-find of a decided item** (wont-fix / deferred / disputed / false-positive): match it
  and attach the prior decision verbatim — never suppress it. Re-raises have overturned prior
  calls (3 of the first 5 recorded); the ~55 since mostly re-affirmed — attach the decision and
  let the synthesizer re-judge either way.
- **When unsure, SPLIT.** A wrong merge inflates overlap → the loop stops early → a bug
  ships. A wrong split only costs another look. Prefer "NEW — possible remainder of
  `PPW-<n>`" over a merge; that flag is more useful than either verdict.
- **Post-cert escape.** Ledger mode, target listed in `reviews/state/track-record.md`: a new 🔴/🟠
  whose mechanism already existed at the certified commit (cited site unchanged since it —
  `git diff <certified>..<reviewed> -- <file>`) is additionally marked `post-cert-escape`,
  and the synthesizer appends the event to track-record.md. Serious findings introduced by
  post-certification changes are not escapes. Unsure → record as escape, doubt stated.
- Severity never blocks a match (severity is mutable); mechanism + site decide.

## Output

One row per new finding:

| finding | `PPW-<n>` / NEW | confidence (high·med·low) | reason (one line) |

plus `residual-of` lineage links, and the prior decision attached to every matched decided
item. Then:

- **Ledger mode:** per NEW finding, mint the next id — read `reviews/state/id-counter`, assign in
  order, and write the incremented number back **in the same change**, so two instances
  minting at once collide in git instead of reusing a number. Add its table row **and its
  detail block**
  per `reviews/templates/ledger.md` — What / Evidence / Suggested fix / History, the defect's
  only full description anywhere (`reviews/rules/doc-contracts.md`, describe-once). The History's
  first line records this pass, the convergence count and the `hinted` flag; serious findings'
  Suggested-fix lines carry the fix brief (see the discovery runbook). For each **matched**
  row, append one History line (this pass, what changed) — never edit existing block text; a
  matched decided item's History line carries the prior decision verbatim.
- **Overlap mode:** N_A / N_B / M per stratum (serious = 🔴+🟠, minor = 🟡+⚪); exclude
  `hinted` findings from independence claims; list unknown-provenance items honestly instead
  of guessing.

## Trust gate

Before first trusted use — and after any material change to the rules above — run **blind**
against the eval set at
`.claude/skills/reconcile-findings/overlap-ground-truth.md` (scoring guide at its
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

The names in that entry are the eval set's own and the finding names 035 used at the time,
kept as recorded; `reviews/archive/id-map.md` translates the pre-2026-08-11 ledger names.
