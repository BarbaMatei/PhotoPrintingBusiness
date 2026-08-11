---
name: owner-summary
description: >-
  Write the one-page owner summary (summary-v<n>.md) that ends every decision-bearing review
  pass: the serious items needing a decision with suggested actions, a reasons-to-doubt
  section computed from raw pass data, and an evidence link on every claim; minors filed
  automatically. Use at the end of a discovery, delta-discovery, or certification pass
  ("write the owner summary", "summary for <target> v<n>"), or to regenerate one from the
  recorded files. Verification passes write no summary file — their outcome is reported at
  the owner gate in chat.
---

# Owner summary

The page the owner reads **instead of** the review files. The review file is the record; this
is the interface. One per pass: `reviews/<target>/summary-v<n>.md` pairs with `review-v<n>.md`.

## Inputs

`review-v<n>.md` (findings + verdict) · `resolution-v<n>.md` if the round had fixes ·
`ledger.md` (statuses, backlog) · `metrics.jsonl` (trend data) · the pass's lens list vs the
manifest (what was owed but didn't run).

## Output shape

Copy `reviews/templates/summary.md` and fill it in; `reviews/rules/doc-contracts.md` is the
contract (headings verbatim, vocabulary, cap 60 body lines — aim for ~40). Frontmatter:
`type: owner-summary`, `target`, `pass`, `pass-type`, `commit`, `date`,
`decisions-needed: <n>`. Then exactly four sections:

1. **Needs your decision** — 🔴/🟠 items only, numbered. Per item: one plain sentence a cold
   reader understands, a suggested action with a rough cost ("fix now, ~1h, test exists" /
   "defer to bolt-X" / "accept the risk"), and links. If nothing needs a decision, one line
   says so.
2. **Reasons to doubt** — computed from raw data, never from the review's self-assessment:
   manifest lenses owed but not run · the new-findings-per-pass trend (full passes,
   `metrics.jsonl`) · counts of `unverified-*` and `hinted` findings · budget skips ·
   pass-type caps (a verification or delta pass cannot certify) · blinding is best-effort.
3. **Filed automatically** — one line: how many 🟡/⚪ went to the ledger backlog, with a link.
   Flag at most one if it deserves the owner's eye anyway.
4. **State** — one line: where the router stands and what happens next.

## Rules

- **Every claim carries a link** (file, commit, test, `PPW-<n>`, metric). A claim with no link
  does not go on the page — an unverifiable summary forces trust.
- Plain words; short sentences; the reader is technical but cold (spell out what a feature or
  job does; quote UI strings as shipped). Severity dots, `PPW-<n>` and file paths are fine.
- Suggested actions are suggestions — the page never decides, never buries an option the
  owner might prefer.
- Written by the pass runner at synthesis time; the fixer never edits it.
- Don't restate the review: one sentence per item, the link carries the depth.
