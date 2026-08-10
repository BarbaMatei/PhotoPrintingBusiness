---
type: review-system
status: active
created: 2026-06-18
updated: 2026-08-03
owner: Matei Barba
---

# Multi-Lens Review System

Parallel isolated review lenses, repeated independent passes, and a severity-based stop rule.
One reviewer in one sitting catches a *sample* of what's wrong, not all of it — so the system
samples repeatedly, from designed-in breadth, until no 🔴 survives and every 🟠 carries a
recorded owner-visible decision. **Certified means exactly that — not zero defects.**

- **Why it's built this way** (the 035/042/043 evidence and numbers): [rationale.md](rationale.md)
- **Where it's heading** (autonomy, experiments, tool build order): [self-driving-loop-design.md](self-driving-loop-design.md)
- **Log of every pass**: [index.md](index.md) · **per-pass metrics**: [metrics-schema.md](metrics-schema.md)

## How to run

| Task | Follow |
|---|---|
| Any pass — picked and driven end to end | the **`loop-driver` skill** (mechanical router: [lib/route-next-pass.mjs](lib/route-next-pass.mjs)) |
| Discovery pass (full · delta · certification) | [runbook-discovery.md](runbook-discovery.md) |
| Verification pass (after a fix round) | [runbook-verification.md](runbook-verification.md) |
| Fix round | the `/fix-review` skill — **sole owner of the fixer contract** |

Standing instruction: *"Continue the review loop for `<target>`"* → the **`loop-driver`**
skill: it audits the records, reads the router mechanically, and states the pass type and
expected cost in one line before any discovery-scale launch; **certification always waits
for an explicit owner go-ahead**.

## Entry tiers — does a change get the loop at all?

| Change touches… | Treatment |
|---|---|
| Money, auth, data loss, concurrency, migrations, new external input | **Full loop**, ends at certification |
| Ordinary feature work | One discovery pass + fix rounds + verification; escalate on any serious finding |
| Docs, copy, styling, config with no behavior change | One quick pass, or skip |

Escalate upward on any serious finding; never de-escalate mid-loop.

## The router

The state of `reviews/<target>/` decides the next pass — first matching row wins:

| State | Next pass |
|---|---|
| No `review-v1.md` | **Full discovery** |
| Latest review has open 🔴/🟠 with no `resolved` resolution answering it | **Fix round** — new 🟡/⚪ go to the ledger backlog, not the fix round |
| Resolution `resolved`, not yet re-reviewed | **Verification** |
| Verification clean (0 reopened) + fix round was **delta-worthy**¹ + no delta since | **Delta discovery** |
| Verification clean + fix round patch-grade | Loop **quiet** |
| Loop quiet | **Certification**² |
| Certification quiet | **Certified** — verdict `approved`; loop done |
| A new 🔴 anywhere · a fix-caused 🟠 regression · a reopened fix | **Fix round** (quiet counter resets) |

¹ **Delta-worthy** = the fix round fixed a 🔴, added/converted a mechanism, or changed a
design. Anything else is patch-grade and exits on verification + the fixer's micro-review.

² **Certification** (full-loop tier): the feature's **first** certification attempt is a pair —
two parallel blinded full-manifest passes against one frozen commit, folding in any still-owed
manifest lenses (~2× full-pass cost). **Re-certification** after a fix round that was small and
independently verified is **one** fresh full-manifest pass on the re-frozen commit — the
standard close, not a deviation (calibration 2026-07-29). A full-loop-tier feature never closes
without a fresh full-manifest pass after its last fix round. Lower tiers certify with a
**single** fresh full-manifest pass; with owner sign-off a quiet **lower-tier** loop may close
without one (recorded in the index). Every certification index row records the 🟠 still open at
close. A backlogged minor fixed later needs only normal fix-verification — unless the fix
touches full-loop-tier code.

**What re-arms the loop — exactly three things:** a new 🔴; a fix-caused 🟠 regression; a
reopened fix. New non-regression 🟠 get fixed and verified but do not re-arm a delta. New
🟡/⚪ enter the ledger as `backlog`, never silently dropped: while the loop is open they may
be re-judged by certification; at close every surviving `backlog` row rolls up into
[backlog.md](backlog.md); from there **every new bolt must sweep the rows in its area**
(bolt-process.md) and the **pre-deployment regression phase requires the file empty** —
each row fixed and verified, or owner-ruled wont-fix, written back to its home ledger row
before the queue row is removed.

## Severity & verdicts

- 🔴 **High** — directly exploitable / breaks the core promise / data loss.
- 🟠 **Medium** — real impact under specific-but-realistic conditions.
- 🟡 **Low** — defense-in-depth, edge cases, parity risk.
- ⚪ **Cleanup** — quality only, no behavioral impact.

Review verdicts: `request-changes` (blockers open) · `approve-with-followups` (no blockers,
residuals remain) · `approved` (**certification close only**). Verification and delta passes
are capped at `approve-with-followups` — "this fix held" and "this diff is clean" are not
"the feature is clean".

## Hard rules

- `review-v<n>.md` is **immutable**; fixers respond in `resolution-v<n>.md`.
- **Only the owner opens a target.** A `reviews/<target>/` folder is created solely by
  executing a pass the owner explicitly requested for that target. A defect noticed outside
  any open pass — by a fixer, a driver, anyone — is recorded with its evidence in
  [inbox.md](inbox.md), never in a new folder; it seeds the target's ledger if and when
  the owner opens that loop.
- The fixer never sets `verified` — only a re-review can.
- Discovery is **blinded** (best-effort: enforced by prompts, unverified until the blinding
  auditor exists); verification is **anchored** on purpose. Never mix the postures in a pass.
- A review produces findings; fixing is a separate explicit step, verification a third. Never
  auto-apply fixes mid-review.
- Every pass appends its [metrics.jsonl](metrics-schema.md) line and its [index.md](index.md)
  row — at synthesis time, unreconstructable later. **Fix rounds append theirs too** (schema
  v3, 2026-08-03) — at hand-back, via `reviews/lib/render-records.mjs`, computed from the
  target's worklog.
- A target holding a certification is **under watch** ([track-record.md](track-record.md)): a
  later serious finding whose defect existed in the certified code is marked
  `post-cert-escape` and appended there the same day — the reconciler flags it, the
  synthesizer records it. Escapes ÷ certifications is the system's false-certification rate.
- **Rule budget:** a calibration **replaces or deletes** a rule, never stacks an exception on
  top of one; any exception states its expiry (a date, or "next calibration"). The router
  table is the single decision surface for pass selection — executed mechanically by
  [lib/route-next-pass.mjs](lib/route-next-pass.mjs), hand-read only when it abstains.

## Files & conventions

- One folder per target: `reviews/<target>/`. **Archiving is the last step of recording a
  close**, done by the loop-driver in this order: `closed:` in the ledger frontmatter →
  surviving `backlog` rows copied to [backlog.md](backlog.md) → `archived: <date>` on the
  index row → `git mv` to `reviews/archive/<target>/`, contents unchanged. Dormant targets
  (no pass in 30+ days, nothing serious open) are *offered* for archiving at the next
  loop-driver run, never moved silently. A `post-cert-escape` moves the folder back out.
- `inbox.md` — the single holding pen for findings recorded outside any open target
  (evidence + suggested target per row), untriaged. Rows move into a ledger when the owner
  opens that loop. Distinct from a ledger row's `backlog` status, which is a triaged minor
  deferred within its target.
- `backlog.md` — the cross-target queue of unfixed minors from closed targets (template
  `templates/backlog.md`): one line per row (D#, source target, severity, what, area). Rows
  enter at target close; they leave only as fixed-and-verified or owner-ruled wont-fix, and
  only after the home ledger row records that terminal state.
- `worklog.jsonl` — per-target, append-only, one timestamped JSON event per line: fix-round
  events written by the `/fix-review` skill as work happens, `pass-launch`/`pass-records-done`
  and owner-gate stamps written by the loop-driver. The crash-safe evidence trail; every
  metrics `runtime` value is computed from it, never estimated
  ([schema v3](metrics-schema.md)).
- A closed loop records `closed: <date> — <how>` in the **ledger frontmatter** — the
  router's machine-read terminal state (the index row carries the story).
- **Every artifact follows [doc-contracts.md](doc-contracts.md)** (templates in
  [templates/](templates/), fixed structure, size caps, language rules, vocabulary) —
  enforced by the round-end doc gate. Artifact set (2026-08-10): a target folder holds at
  most `review-v<n>.md`, `resolution-v<n>.md`, `summary-v<n>.md`, `ledger.md`,
  `worklog.jsonl`, `metrics.jsonl`. One-off measurement files are banned (metrics/worklog).
- `review-v<n>.md` — immutable once the round's doc gate passes; one per **discovery-type**
  pass; frontmatter: `version`, `supersedes`, `commit`, and required
  `pass-type: discovery | delta-discovery | certification`. Finalized *after*
  reconciliation, so its table pairs each `F#` with its `D#`; defects are referenced,
  never re-described. **Verification passes write no files** — their record is ledger
  status flips, worklog events, the metrics line and the index row; their outcome is
  reported at the owner gate in chat.
- `resolution-v<n>.md` — the fixer's answer, living until closed; one per **fix round**,
  numbered by the pass that raised its findings (a clean verification raises nothing and
  gets no resolution). Frontmatter: scalars only (`status: open | in-progress | resolved`,
  `fixed_commit`, `answers`); the `## Findings` body table (`| D# | Status | Commit | Note |`,
  note ≤ 240 chars) is the machine-read state; body also carries the scope table +
  decisions/rationale.
- `summary-v<n>.md` — the one-page owner summary, written only for passes that can need an
  owner decision (discovery, delta-discovery, certification; via the `owner-summary` skill).
  The review file is the record; this page is what the owner reads.
- **Finding IDs:** pass-local `F1, F2, …` in ranked order; severity and category are columns,
  never encoded in the ID. Older `BUG-`/`SEC-`-style prefixes are grandfathered. `F#` appears
  only inside its own pass's files, always next to its `D#`; everywhere else a finding is
  named by `D#` alone.
- **Ledger** (`ledger.md`) — one canonical `D#` per real defect, forever; each pass's `F#`
  mapped on *after* the blinded pass. **The single home of defect detail** (describe-once):
  each D# gets one table row plus one detail block — What / Evidence / Suggested fix /
  History — written at creation; the block grows only append-only History lines, and every
  other file references the D# instead of re-describing. Terminal rows feed the discovery
  script's `decidedFindings`; each deferral row records the commit at which it was last
  affirmed. A re-raise of a decided item gets the prior decision **attached, never
  suppressed** — the first 5 recorded re-raises overturned 3 prior calls; the ~55 since
  mostly re-affirmed.
- Per-finding lifecycle: `open → in-progress → fixed → verified`, or terminal
  `wont-fix | deferred | disputed | false-positive` (rationale required in the resolution).
- Review artifacts ride with the code branch. This README and the runbooks are the system
  spec; matured theory graduates to `analysis/architect-review` as connected concept notes —
  not before it has been stress-tested across several reviews.
