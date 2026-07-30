---
type: review-system
status: active
created: 2026-06-18
updated: 2026-07-24
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
| Discovery pass (full · delta · certification) | [runbook-discovery.md](runbook-discovery.md) |
| Verification pass (after a fix round) | [runbook-verification.md](runbook-verification.md) |
| Fix round | the `/fix-review` skill — **sole owner of the fixer contract** |

Standing instruction: *"Continue the review loop for `<target>`"* → take the first matching
router row. Before any discovery-scale launch, state the pass type and expected cost in one
line; **certification always waits for an explicit owner go-ahead**.

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
🟡/⚪ enter the ledger as `backlog` — drained deliberately (a groomer sweep, the next bolt in
that area, or re-judged by certification), never silently dropped.

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
- The fixer never sets `verified` — only a re-review can.
- Discovery is **blinded** (best-effort: enforced by prompts, unverified until the blinding
  auditor exists); verification is **anchored** on purpose. Never mix the postures in a pass.
- A review produces findings; fixing is a separate explicit step, verification a third. Never
  auto-apply fixes mid-review.
- Every pass appends its [metrics.jsonl](metrics-schema.md) line and its [index.md](index.md)
  row — at synthesis time, unreconstructable later.
- A target holding a certification is **under watch** ([track-record.md](track-record.md)): a
  later serious finding whose defect existed in the certified code is marked
  `post-cert-escape` and appended there the same day — the reconciler flags it, the
  synthesizer records it. Escapes ÷ certifications is the system's false-certification rate.
- **Rule budget:** a calibration **replaces or deletes** a rule, never stacks an exception on
  top of one; any exception states its expiry (a date, or "next calibration"). The router
  table is the single decision surface for pass selection.

## Files & conventions

- One folder per target: `reviews/<target>/`. Dormant or closed targets move to
  `reviews/archive/<target>/` unchanged.
- `review-v<n>.md` — immutable, one per pass; frontmatter: `version`, `supersedes`, `commit`,
  and required `pass-type: discovery | delta-discovery | verification`.
- `resolution-v<n>.md` — the fixer's answer, living until closed. Frontmatter: `status: open |
  in-progress | resolved`, `fixed_commit`, and a `findings:` map of `{id: {status, commit,
  note}}`; body carries the table + decisions/rationale.
- `findings-v<n>.md` — full per-finding detail (scenario/fix/evidence), so nothing survives
  only in session scratch.
- `summary-v<n>.md` — the one-page owner summary per pass (decisions needed · reasons to
  doubt · evidence links; written via the `owner-summary` skill). The review file is the
  record; this page is what the owner reads.
- **Finding IDs:** pass-local `F1, F2, …` in ranked order; severity and category are columns,
  never encoded in the ID. Older `BUG-`/`SEC-`-style prefixes are grandfathered. `F#` joins
  review↔resolution *within* a pass; the cross-pass key is the ledger's `D#`.
- **Ledger** (`ledger.md`) — one canonical `D#` per real defect, forever; each pass's `F#`
  mapped on *after* the blinded pass. Terminal rows feed the discovery script's
  `decidedFindings`; each deferral row records the commit at which it was last affirmed. A
  re-raise of a decided item gets the prior decision **attached, never suppressed** — the first
  5 recorded re-raises overturned 3 prior calls; the ~55 since mostly re-affirmed.
- Per-finding lifecycle: `open → in-progress → fixed → verified`, or terminal
  `wont-fix | deferred | disputed | false-positive` (rationale required in the resolution).
- Review artifacts ride with the code branch. This README and the runbooks are the system
  spec; matured theory graduates to `analysis/architect-review` as connected concept notes —
  not before it has been stress-tested across several reviews.
