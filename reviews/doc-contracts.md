---
type: doc-contracts
status: active — owner-approved 2026-08-10, enforced by the round-end doc gate
created: 2026-08-10
owner: Matei Barba
---

# Doc contracts — review artifacts

Every review artifact follows a fixed template, a size cap, and the language rules
below. The round-end gate (lint + Haiku judge) enforces this file. It judges and
explains; it never edits. Scope: every per-target artifact. Archived targets are being
retrofitted to this shape by owner order (2026-08-10, newest to oldest; the owner
explicitly lifted review-file immutability for the retrofit — originals live in git
history). `reviews/system/` is out of scope.

## The artifact set

A target folder contains at most: `review-v<n>.md`, `resolution-v<n>.md`,
`summary-v<n>.md`, `ledger.md`, `worklog.jsonl`, `metrics.jsonl`.

- `findings-v<n>.md` no longer exists. Defect detail lives on the ledger row.
- Verification passes write no files. Their record is worklog events, ledger
  status changes, and the index row. The result is reported at the owner gate.
- A summary is written only for passes that can need an owner decision:
  discovery, delta-discovery, certification.
- One-off measurement files are banned. Measurements go to `metrics.jsonl` or
  the worklog.

## Core rules — all files

1. **Templates are law.** Copy the skeleton from `reviews/templates/`, fill it in.
   Headings keep the template's exact wording and order. No extra sections,
   no decorations on headings (no counts, dates, or links in a heading).
2. **Describe once.** A defect's full description lives on its ledger row,
   written when the D# is created. Every other mention is one line plus the D#.
   Re-finds append a history line, never a re-description.
3. **IDs.** `F#` is pass-local and appears only inside its own pass's files,
   always next to its D#. Everywhere else a finding is named by `D#` alone.
4. **Links.** Cross-round references go through the D# on the ledger.
   File-to-file links are allowed only between files of the same round.
5. **Append-only detail.** A ledger detail block never changes after creation,
   except new History lines. Status fields in the table may change.

## Language rules

1. Use only vocabulary from the list below, or everyday English. Coining a new
   label is a violation.
2. Write for a reader who was not in the session. No "as discussed".
3. One idea per sentence. Aim under 25 words. Bullets over paragraphs.
4. Exact facts only: counts, paths, error text, IDs. "Several tests failed" is
   banned; name the number and the tests.
5. One name per concept. The four severity words below are the only ones.
   "Critical", "blocker" and synonyms are banned in prose.
6. Strictness tiers: summaries follow every rule at full strength — the owner
   reads them. Reviews, resolutions and ledger entries may be denser but keep
   the same structure, vocabulary, and caps.

## Size caps

| File | Cap |
|---|---|
| `summary-v<n>.md` | 60 lines of body |
| `review-v<n>.md` | 120 lines of body; table rows are single lines |
| `resolution-v<n>.md` | 200 lines of body (the Findings table rows live here); `Note` cell ≤ 240 characters; each Decision ≤ 15 lines |
| `ledger.md` detail block | 20 lines per defect; table cells one line; Status cell is the status word only |
| `backlog.md` row | 1 table line |

## Vocabulary

Allowed system terms. Anything else must be everyday English.

- **target** — one reviewed feature; one folder under `reviews/`.
- **pass / round** — one numbered run of the loop (v1, v2, …).
- **lens** — one specialized reviewer perspective (security, race, …).
- **discovery** — a blinded pass searching the whole target for defects.
- **delta discovery** — a discovery pass scoped to what changed since the last one.
- **verification** — an anchored pass checking that specific fixes held.
- **certification** — the closing full pass; its verdict can be `approved`.
- **fix round** — the fixer working through a review's findings.
- **blinded / anchored** — finder cannot see prior findings / checker deliberately starts from them.
- **D#** — a defect's permanent id on the ledger.
- **F#** — a finding's pass-local id inside one pass's files.
- **ledger** — `ledger.md`, the permanent per-target defect record.
- **backlog** — `reviews/backlog.md`, unfixed minors from closed targets awaiting drain.
- **inbox** — `reviews/inbox.md`, findings noticed outside any open target.
- **worklog** — `worklog.jsonl`, the per-target append-only event trail.
- **index** — `reviews/index.md`, one row per pass, repo-wide.
- **severity** — 🔴 High · 🟠 Medium · 🟡 Low · ⚪ Cleanup.
- **verdict** — `request-changes` · `approve-with-followups` · `approved`.
- **refuted** — a suspected defect investigated and shown not real.
- **affirmed** — the commit at which a ledger row's status was last checked.
- **quiet / re-arm** — the loop has nothing serious open / a new serious event restarts it.
- **patch-grade / delta-worthy** — a fix round too small to need a delta discovery / big enough to need one.
- **post-cert escape** — a defect found later that existed in certified code.
- **owner gate** — a stop where only the owner's decision continues the loop.
- **approach-check** — an adversarial pre-implementation check of a fix's design (trigger list in the `/fix-review` skill).
- **micro-review** — the anchored per-cluster diff review a fix round dispatches on its own work.
- **reconciliation** — mapping a pass's F#s onto ledger D#s after the blinded pass.

## Per-file contracts

### review-v<n>.md — template `templates/review.md`

Audience: the fixer. Written by a discovery-type pass, finalized after
reconciliation so the table carries both F# and D#. Immutable once the round's
gate passes. The Findings table ranks worst first. Titles reuse the ledger
row's wording. `Notes for the fixer` gives order and traps, never re-describes.

### resolution-v<n>.md — template `templates/resolution.md`

Audience: the verifier and the ledger's historian. One resolution per **fix
round**, numbered by the pass that raised its findings; a clean verification
raises nothing and gets no resolution. Frontmatter carries scalars only. The
`## Findings` body table (D# · Status · Commit · Note, note ≤ 240 chars) is the
machine-read state. Rationale that deserves prose goes under `Decisions`, one
titled block per decision. The fixer never writes `verified`.

### summary-v<n>.md — template `templates/summary.md`

Audience: the owner. Full-strength language rules. Four fixed sections.
`Needs your decision` states each decision with a suggested action, or exactly
"Nothing.". `Reasons to doubt` is computed from the pass's own data. Every
claim links its evidence.

### ledger.md — template `templates/ledger.md`

Audience: everyone; the single home of defect detail. One table row plus one
detail block per D#. The block is written at D# creation and grows only
History lines. Statuses: `open`, `in-progress`, `fixed`, `verified`, and
terminal `wont-fix`, `deferred`, `disputed`, `false-positive`, `backlog`.

### backlog.md — template `templates/backlog.md`

Audience: the owner and bolt-opening agents. One line per row. Rows enter at
target close, before archiving. Rows leave only as fixed-and-verified or
owner-ruled wont-fix, and only after the home ledger row records that state.
