---
type: doc-contracts
status: active — owner-approved 2026-08-10, enforced by the round-end doc gate
created: 2026-08-10
owner: Matei Barba
---

# Doc contracts — review artifacts

Every review artifact follows a fixed template, a size cap, and the language rules
below. The round-end gate (lint + Sonnet judge) enforces this file. It judges and
explains; it never edits. `doc-gate.mjs <target> <pass>` lints a round's files,
`doc-gate.mjs state` the two cross-target files, and `lib/tests/run-tests.mjs` lints
the gate itself against fixtures. Scope: every per-target artifact, plus the cross-target
`index.md` and `backlog.md`. `reviews/system/` and `track-record.md` have no contract
here — but the system target keeps its own lightweight records: `SF<n>` ids (outside
the `PPW-<n>` sequence), a ledger-style status registry, a worklog, and a metrics
line per meta-pass, grouped per pass under `reviews/system/review-v<n>/`. Archived targets are being
retrofitted to this shape by owner order (2026-08-10, newest to oldest; the owner
explicitly lifted review-file immutability for the retrofit — originals live in git
history).

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
- One folder per target, `reviews/<target>/`, riding with the code branch.
- Archiving is the last step of recording a close, in this order: `closed:` in
  the ledger frontmatter → surviving `backlog` rows copied to `backlog.md` →
  `archived: <date>` on the index row → `git mv` to `reviews/archive/<target>/`,
  contents unchanged. A post-cert escape moves the folder back out.

## Core rules — all files

1. **Templates are law.** Copy the skeleton from `reviews/templates/`, fill it in.
   Headings keep the template's exact wording and order. No extra sections,
   no decorations on headings (no counts, dates, or links in a heading).
2. **Describe once.** A defect's full description lives on its ledger row,
   written when its `PPW-<n>` is minted. Every other mention is one line plus
   that id. Re-finds append a history line, never a re-description.
3. **IDs.** `PPW-<n>` is the only defect id a file may carry. The numbers are
   global: one sequence shared by every target, never reused. The next free
   number is the whole content of `reviews/state/id-counter`; whoever mints ids reads
   it, assigns them in order, and writes the incremented number back in the same
   change — two sessions minting at once collide in git, which is the alarm.
   A blinded finder still numbers its own finds, but those numbers live in the
   running session only: reconciliation mints the `PPW-<n>` before any file of
   that pass is written. Severity and category are columns, never encoded in an
   id. Names used before 2026-08-11 translate through `archive/id-map.md`, the
   only place they survive.
4. **Links.** Cross-round references go through the `PPW-<n>` on the ledger.
   File-to-file links are allowed only between files of the same round.
   Prose never spells out a system file's path — it uses the file's vocabulary
   name ("the backlog", "the ledger"); the vocabulary entry owns the path, and
   `lib/paths.mjs` owns it for scripts. Literal paths appear only in markdown
   links (kept true by `lib/fix-links.mjs`), definitions, and commands.
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
- **`PPW-<n>`** — a defect's permanent id, one global sequence across all targets.
- **id counter** — `reviews/state/id-counter`, holding the next free `PPW-<n>` and nothing else.
- **ledger** — `ledger.md`, the permanent per-target defect record.
- **backlog** — `reviews/state/backlog.md`, the cross-target queue: unfixed minors from closed
  targets, plus defects the owner routed there from outside any pass, all awaiting drain.
- **area** — the one word on a backlog row naming where that row's fix lands. Twelve are
  allowed; the list and the tiebreak rule sit under `backlog.md` below.
- **worklog** — `worklog.jsonl`, the per-target append-only event trail.
- **index** — `reviews/state/index.md`, one row per pass, repo-wide.
- **severity** — 🔴 High · 🟠 Medium · 🟡 Low · ⚪ Cleanup.
- **verdict** — `request-changes` · `approve-with-followups` · `approved`.
- **refuted** — a suspected defect investigated and shown not real.
- **affirmed** — the commit at which a ledger row's status was last checked.
- **quiet / re-arm** — the loop has nothing serious open / a new serious event restarts it.
- **patch-grade / delta-worthy** — a fix round too small to need a delta discovery / big enough to need one.
- **post-cert escape** — a defect found later that existed in certified code.
- **owner gate** — a stop where only the owner's decision continues the loop.
- **unattended run** — one driver run driving the loop to close under the written policy
  (`lib/autonomy-policy.mjs`) and the owner's standing approval, stopping only for a
  question only the owner can answer, broken records, or the no-progress guard.
- **parked** — a gate decision taken by written default during an unattended run,
  awaiting the owner's ruling in the run-end report.
- **approach-check** — an adversarial pre-implementation check of a fix's design (trigger list in the `/fix-review` skill).
- **micro-review** — the anchored per-cluster diff review a fix round dispatches on its own work.
- **reconciliation** — after the blinded pass, matching its finds to ledger rows and
  minting a `PPW-<n>` for each one that is new.
- **class sidecar** — `reviews/state/defect-classes.jsonl`, one line per classified
  ledger row, written by the prevention-sweep backfill, read by the ledger miner.

## Per-file contracts

### review-v<n>.md — template `templates/review.md`

Audience: the fixer. Written by a discovery-type pass, finalized after
reconciliation so every row carries its `PPW-<n>`. Immutable once the round's
gate passes. Frontmatter: `type`, `target`, `version`, `supersedes`, `commit`,
`branch`, `pass-type`, `date`, `lenses`, `lenses-not-run`, `verdict`, `blockers`,
`findings`, `tests`. `pass-type` is `discovery`, `delta-discovery` or
`certification`; `verification` is not a legal value, because those passes write
no review file. The Findings table has one id column, `ID`, and ranks worst first.
Titles reuse the ledger row's wording. `Notes for the fixer` gives order and
traps, never re-describes.

### resolution-v<n>.md — template `templates/resolution.md`

Audience: the verifier and the ledger's historian. One resolution per **fix
round**, numbered by the pass that raised its findings; a clean verification
raises nothing and gets no resolution. It lives until the target closes.
Frontmatter carries scalars only: `type`, `target`, `version`, `answers`,
`fixed_commit`, `status: open | in-progress | resolved`, and `closed:` — the
hand-back date, set when status turns `resolved`.
The `## Findings` body table (ID · Status · Commit ·
Note, note ≤ 240 chars) is the machine-read state, keyed by `PPW-<n>`; the body
also carries the scope table. Rationale that deserves prose goes under
`Decisions`, one titled block per decision — including the owner's ruling on any
defect proposed at this round's gate from outside the finding set. `verified` is
not a legal value in the Status column.

### summary-v<n>.md — template `templates/summary.md`

Audience: the owner. Frontmatter: `type`, `target`, `pass`, `pass-type`,
`commit`, `date`, `decisions-needed`. Full-strength language rules. Four fixed sections.
`Needs your decision` states each decision with a suggested action, or exactly
"Nothing.". `Reasons to doubt` is computed from the pass's own data. Every
claim links its evidence.

### ledger.md — template `templates/ledger.md`

Audience: everyone; the single home of defect detail. One table row plus one
detail block per `PPW-<n>` — What / Evidence / Suggested fix / History. The block
is written when the id is minted and grows only History lines. `First seen` and
every History line name the pass by its number alone (`v1`), with no finder's
number beside it.

A row runs `open → in-progress → fixed → verified`, or ends at terminal
`wont-fix`, `deferred`, `disputed`, `false-positive`, `backlog` — a terminal
status needs its rationale in the resolution. Terminal rows feed the discovery
script's `decidedFindings`, and each deferral row records the commit at which it
was last affirmed. A re-raise of a decided row gets the prior decision attached
to it, never suppressed: of the first 5 recorded re-raises, 3 overturned the
prior call, while the ~55 since mostly re-affirmed it.

Frontmatter carries `closed: <date> — <how>` once the loop closes. That line is
the router's machine-read terminal state; the story of the close lives on the
index row.

### worklog.jsonl — no template

Audience: the auditor and anyone reconstructing a round. Per-target,
append-only, one timestamped JSON event per line, each carrying at least a
string `t` and a string `ev`. Events cover the fix round's work as it happens
plus `pass-launch`, `pass-records-done` and the owner-gate stamps. It is the
crash-safe evidence trail: every metrics `runtime` value is computed from it and
never estimated.

### metrics.jsonl — schema `metrics-schema.md`

Audience: whoever measures the system. Per-target, append-only, one JSON line
per record. `metrics-schema.md` owns the field list and the schema version.

### backlog.md — template `templates/backlog.md`

Audience: the owner and bolt-opening agents. One line per row, keyed by
`PPW-<n>`, with columns ID · Target · Sev · What · Area.

`Area` is one of these twelve words, lowercase, and nothing else:

| Area | Covers |
|---|---|
| `payments` | charging, webhooks, idempotency, invoices |
| `orders` | order lifecycle, admin ops |
| `shipping` | Sameday, AWB, couriers |
| `uploads` | upload handling, originals and thumbnails, storage tiers, S3 and local |
| `gallery` | customer-facing photo UI |
| `auth` | identity, sessions, guest tokens |
| `edge` | proxy, endpoint exposure, rate limiting, health and metrics gating |
| `observability` | metrics, tracing, Sentry, SLOs, dashboards |
| `jobs` | background jobs, retries, sweeps |
| `data` | EF, migrations, dual-provider parity |
| `tests` | test infrastructure: flakes, helpers, coverage gaps whose fix is test-only |
| `records` | docs, memory-bank, process records |

When two areas fit, pick the one where the fix would land, not the one where
the symptom shows. A file path, a line number or a second area in this cell is a
violation: that detail lives on the home ledger row.

This file is the
cross-target queue and is distinct from a ledger row's `backlog` status, which
marks a triaged minor deferred inside its own target. Rows enter at target close,
before archiving, or when the owner routes
a defect noticed outside any pass here at a round's gate — that row takes the
next number from the id counter. Rows leave only as fixed-and-verified or owner-ruled wont-fix,
and only after the home ledger row records that state. An owner-routed row has
no ledger row until a loop opens for its area; until then it leaves on the
owner's ruling alone, recorded in that round's resolution `Decisions` and in this
file's git history.

### index.md — no template

Audience: the owner scanning review history. Frontmatter: `type` and `updated`.
Body: a short paragraph saying what the file is, the two tables below, and
pointer lines to other files — no other prose. `Targets at a glance` holds one row per target; its
State cell is at most 5 short lines — close and archive status with its date and
how, the headline residue by `PPW-<n>`, and what re-arms the loop — and never
narrates pass by pass. `Passes` holds one row per pass, newest first; its
description cell is at most 2 sentences and 50 words: what the pass proved or
found, the worst finding by `PPW-<n>`, anything that re-armed the loop, plus the
surviving links. A pass row carries 5 cells, or 7 when the Outcome and Files
cells apply — those two appear together or not at all. Pass rows are append-only — a row is never rewritten once its
pass closes, and the 2026-08-11 compression is the one owner-ordered exception.
Full-strength language rules, as for a summary. Ids are `PPW-<n>`, except the
`system` target's rows, which carry the `SF<n>` of `system/review-v1/review-v1.md`.
