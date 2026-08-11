---
type: review-system
status: active
created: 2026-06-18
updated: 2026-08-11
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
| Owner summary ending a decision-bearing pass | the `owner-summary` skill |

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
be re-judged by certification; at close the survivors roll up into
[backlog.md](backlog.md) on the terms doc-contracts.md sets. From there **every new bolt
must sweep the rows in its area** (bolt-process.md), and the **pre-deployment regression
phase requires the file empty**.

## Severity & verdicts

- 🔴 **High** — directly exploitable / breaks the core promise / data loss.
- 🟠 **Medium** — real impact under specific-but-realistic conditions.
- 🟡 **Low** — defense-in-depth, edge cases, parity risk.
- ⚪ **Cleanup** — quality only, no behavioral impact.

Review verdicts: `request-changes` (🔴 open) · `approve-with-followups` (no 🔴, residuals
remain) · `approved` (**certification close only**). Verification and delta passes
are capped at `approve-with-followups` — "this fix held" and "this diff is clean" are not
"the feature is clean".

## Hard rules

- Fixers never edit a review file; they answer in `resolution-v<n>.md`.
- **Only the owner opens a target.** A `reviews/<target>/` folder is created solely by
  executing a pass the owner explicitly requested for that target. A defect noticed outside
  any open pass — by a fixer, a driver, anyone — is **proposed at the round's owner gate**,
  never written into a new folder; serious ones are proposed the moment they are seen. The
  owner then routes it into [backlog.md](backlog.md) as one new row, or drops it. Either
  ruling is written into that round's resolution `Decisions`. A drop on the owner's ruling
  is allowed; a silent drop is not.
- Only a re-review grants `verified`.
- Discovery is **blinded** (best-effort: enforced by prompts, unverified until the blinding
  auditor exists); verification is **anchored** on purpose. Never mix the postures in a pass.
- A review produces findings; fixing is a separate explicit step, verification a third. Never
  auto-apply fixes mid-review.
- Every pass appends its [metrics.jsonl](metrics-schema.md) line and its [index.md](index.md)
  row — at synthesis time, unreconstructable later. **Fix rounds append theirs too**
  (since 2026-08-03) — at hand-back, via `reviews/lib/render-records.mjs`.
- A target holding a certification is **under watch** ([track-record.md](track-record.md)): a
  later serious finding whose defect existed in the certified code is marked
  `post-cert-escape` and appended there the same day — the reconciler flags it, the
  synthesizer records it. Escapes ÷ certifications is the system's false-certification rate.
- **Rule budget:** a calibration **replaces or deletes** a rule, never stacks an exception on
  top of one; any exception states its expiry (a date, or "next calibration"). The router
  table is the single decision surface for pass selection — executed mechanically by
  [lib/route-next-pass.mjs](lib/route-next-pass.mjs), hand-read only when it abstains.

## Files & conventions

- **Layout:** one folder per target under `reviews/`; the cross-target files
  ([index.md](index.md), [backlog.md](backlog.md), [id-counter](id-counter),
  [track-record.md](track-record.md)) at the top level; `archive/` for closed targets.
- **[doc-contracts.md](doc-contracts.md) owns every file's shape** — the artifact set, the
  templates in [templates/](templates/), frontmatter, size caps, id rules, language rules,
  vocabulary, and each file's lifecycle. No file shape is restated here; if the two ever
  disagree, doc-contracts.md is right.
- **Who writes what:** the `/fix-review` skill appends the fix round's worklog events as the
  work happens; the loop-driver appends the pass and owner-gate events, and carries out the
  archive-on-close sequence. Dormant targets (no pass in 30+ days, nothing serious open) are
  *offered* for archiving at the next loop-driver run, never moved silently.
- This README and the runbooks are the system spec; matured theory graduates to
  `analysis/architect-review` as connected concept notes — not before the loop has
  stress-tested it.
