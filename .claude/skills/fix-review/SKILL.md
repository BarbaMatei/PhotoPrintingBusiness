---
name: fix-review
description: >-
  Drive code-review findings to closure — the fixer half of the multi-lens review
  loop. Use whenever the user wants to fix, address, resolve, act on, or "apply" the
  findings of a review: phrases like "fix the review findings", "address the blockers
  in review X", "resolve the open findings", "apply the review feedback", or right after
  a /code-review or multi-lens review produces a review-v<n>.md + resolution-v<n>.md
  under reviews/. Reads the latest review + resolution for a target, fixes findings
  blocker-first WITH the regression tests the review asked for, records each fix in the
  resolution file (status + commit + note), commits per finding referencing its ID, and
  hands back for re-review. Does NOT re-derive findings, does NOT edit the immutable
  review file, and never marks a finding "verified" (only a re-review can).
---

# Fix Review

The fixer half of the review loop documented in `reviews/README.md`. A review produced
findings; your job is to drive each one to a terminal state and record what you did,
without corrupting the reviewer's point-in-time record.

## Where this fits

Three artifacts, three roles (like a GitHub review thread):

```
review-v<n>.md      (reviewer, IMMUTABLE)  — findings with IDs + verdict, at a commit
resolution-v<n>.md  (you, the fixer)       — per-finding: status + commit + note
review-v<n+1>.md    (re-review, IMMUTABLE)  — verifies your fixes, sets "verified"
```

The finding IDs (`BUG-1`, `SEC-1`, `QUAL-3`, …) join the three. You write only in the
resolution file. You hand back for re-review — you do **not** declare anything verified.

If `reviews/README.md` exists, skim it first; it is the source of truth for the
conventions. This skill is the operational checklist.

## Inputs — locate the work

1. Resolve the **target** from the user's request (a bolt id, branch, or folder under
   `reviews/`). If ambiguous, list `reviews/*/` and ask which one.
2. In `reviews/<target>/`, find the **highest** `review-v<n>.md` and its paired
   `resolution-v<n>.md`. If the resolution file is missing, create it from the review's
   finding list (all `status: open`) before starting — mirror the frontmatter shape in
   `reviews/README.md`.
3. Read the review's frontmatter `blockers:` list and the body's findings (ID, severity,
   location, recommended fix). Read the resolution to see what's already done — never
   redo a finding already at a terminal status.

Do not re-run the review or invent new findings. If you spot something genuinely new
while fixing, note it in the resolution's decisions section for the re-reviewer; don't
silently fix outside the finding set.

## The contract

**You MAY** edit source + tests for the finding you're fixing, and edit the resolution
file.

**You MUST NOT**
- modify any `review-v<n>.md` (it's an immutable record — editing it destroys the audit trail);
- mark any finding `verified` (that status belongs to the re-review — a fixer vouching
  for its own fix is exactly the bias the loop exists to prevent);
- change unrelated behavior, or fix things outside the finding set without recording why;
- close a non-trivial bug/security finding without a regression test (see below).

## Workflow

Work findings in this order — fixing the cheap ones first wastes a re-review cycle if a
blocker later forces broad changes:

1. **Blockers first.** Everything in the review's `blockers:` list before anything else.
2. Then remaining 🔴 High and 🟠 Medium by severity.
3. 🟡 Low and ⚪ Cleanup last — batch related ones into a single commit.

For each finding (scale the rigor: 🔴/🟠 get every step; 🟡/⚪ get the class sweep and may
be batched):

1. **Re-read the finding** in the review (its location + recommended fix). Open the cited
   code and confirm the issue still exists at the current commit. If it doesn't (already
   fixed, or you judge it a false positive), set status `false-positive`/`disputed` with a
   one-line rationale instead of changing code.
2. **Name the class, sweep for siblings.** State the defect *class* in one sentence, then
   grep code and docs for other sites of it — the same pattern, the same stale value. Fix
   the class; if you deliberately fix only the instance, say why in the note. For doc
   drift the unit of fix is the stale token repo-wide, never just the file the finding
   cited.
3. **Escalate design changes.** If the fix changes a key scheme, concurrency model,
   resource budget, or retry semantics, it is a design, not a patch: before implementing,
   dispatch one adversarial agent (race/resource lens as fits) against the proposed
   approach and fold in what it finds.
4. **Test-first for behavioral findings.** Follow the repo's test conventions (if a
   test-driven-development skill or `CLAUDE.md` rule applies, obey it). Write the
   regression test the review called for — the concurrency case, the cross-tenant case,
   the edge input — and confirm it FAILS against the current code. This is what turns
   "I think I fixed it" into evidence. Doc-only and pure-cleanup findings need no test.
5. **Implement the fix** at the right altitude (prefer the review's recommended approach;
   if you deviate, say why in the note). If the fix **adds a mechanism** — a new class,
   catch/mapping, event, limit, retry, cache — it is a mini-feature and ships at feature
   grade: defaults/sizing derived from the real constraint (say from what in the note), an
   observability hook, tests for the failure modes the mechanism itself introduces, and
   updates to every doc that states the old behavior. Re-run the test; confirm it now
   passes and you broke nothing adjacent.
6. **Commit** — one focused commit per finding (or per tightly-related cleanup group),
   message referencing the ID and the review version:
   `fix(<area>): <what> (<FINDING-ID>, review <target>-v<n>)`
   e.g. `fix(payments): scope idempotency lookup to caller (SEC-1, review 035-v1)`.
7. **Record it** in the resolution file (next section).

## Recording in the resolution file

After each finding, update its entry in `resolution-v<n>.md`:

- The frontmatter `findings:` map → set the finding's `status`, `commit` (the SHA you
  just made), and a one-line `note` (what you did, or why you won't). A mechanism-adding
  fix's note also names the **new surface** (the mechanism and its failure modes) — that
  is where the re-review points the owning lens.
- The body table row → same status / commit / how.

Status values: `fixed` · `wont-fix` · `deferred` · `disputed` · `false-positive`
(never `verified`). For anything other than `fixed`, write the rationale in the
**decisions** section so the re-reviewer can agree or push back.

When every finding has a terminal status **and all blockers are addressed**, set the
top-level `status: resolved`, `fixed_commit:` (the last/overall fix commit or the branch
tip), and `closed:` date. If you stopped partway, leave `status: in-progress`.

**Example finding entry (frontmatter):**
```yaml
  SEC-1: { status: fixed, commit: a1b2c3d, note: "scoped GetByIdempotencyKeyAsync + stale-free to userId/guestSessionId; added cross-tenant test" }
  QUAL-2: { status: wont-fix, commit: null, note: "DivergentFields payload justifies a distinct type; not worth refactoring ConflictException now" }
```

## Fix-diff micro-review — before hand-back

Your own re-read of the diff does not count: it is the same mind that wrote the fixes, and
it reliably answers "no regressions" over diffs a later discovery pass then mines for a
round of findings. Dispatch 1–2 anchored Explore agents (fresh context, one per fix
cluster) over the full fix diff, asking exactly three questions:

1. **Class or instance** — do sibling sites (code or docs) still carry the defect?
2. **New surface at the bar** — does each added mechanism have sized defaults, a signal,
   failure-mode tests, and doc updates?
3. **Regression** — did the fix change any adjacent behavior?

Fix what they find before handing back; anything you leave open goes in the decisions
section for the re-reviewer. Batched doc/cleanup-only rounds may skip this.

## Hand back — do not self-verify

When done, summarize to the user: which findings are `fixed` / `deferred` / `wont-fix`,
the commits, and that the resolution is `resolved`/`in-progress`. Then state plainly that
the next step is a **re-review** against `fixed_commit` to produce `review-v<n+1>.md`,
which is what flips surviving findings to `verified` (or reopens them). Offer to trigger
it, but don't mark verification yourself.

Update `reviews/index.md`'s Status column for the target (`open → in-progress/resolved`).

## Guardrails recap

- Immutable review file — respond in the resolution, never edit the review.
- Class sweep before every fix; doc drift is fixed token-wide, not file-wide.
- Design-level fixes get an adversarial approach-check before implementation.
- Mechanism-adding fixes ship at feature grade and name their new surface in the note.
- Regression test before claiming a behavioral finding fixed.
- Blocker-first ordering.
- One commit per finding, message names the ID.
- Fresh-eyes micro-review of the fix diff before hand-back — self-review alone doesn't count.
- Never self-mark `verified`; hand back for re-review.
