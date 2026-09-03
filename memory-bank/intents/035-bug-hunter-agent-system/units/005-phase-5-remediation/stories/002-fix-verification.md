---
id: 002-fix-verification
unit: 005-phase-5-remediation
intent: 035-bug-hunter-agent-system
status: ready
priority: should
created: 2026-06-10T10:40:14Z
assigned_bolt: 093-phase-5-remediation
implemented: false
---

# Story: 002-fix-verification (guide Prompt 31 — the loop's verification GATE; extends bug-lifecycle)

**Status:** satisfied by `reviews/lib/verify/verify-fixes.mjs` (2026-09)

## User Story

**As** the authority on closing bugs
**I want** closure granted only when the bug's harvested test passes against current code in the sandbox
**So that** bugs close on proof, not appearance — and the AI-DLC fix loop resolves on evidence

## Acceptance Criteria

- [ ] **Given** Prompt 31, **When** built, **Then** skill `fix-verification` exists AND `bug-lifecycle`'s "mark Fixed" step is **extended to call it first** (via skill-creator), and the brief's three test prompts pass (passing fix → Fixed + verified-fixed; failing "fix" → stays Confirmed, no signal; no test → fallback marked "unverified")
- [ ] **Given** triggers, **When** invoked, **Then** both fire it: `bug-lifecycle` about to mark `Fixed`, and AI-DLC signaling a fix-bolt done for a `correlation_id` — discovered via the **run-open mailbox scan** of `bug-hunting/fix-requests/` for `fix_status: open | fix-reported` records, checking each correlated bug-bolt's `bolt.md` status (Integration Contract §4, v3.2)
- [ ] **Given** the "fix done" signal is consumed (v3.2), **When** verification has not yet run, **Then** **`fix_status: fix-reported`** is written onto the fix-request record first — a fix exists but is unproven (this is the state the knowledge builder's correlation test observes)
- [ ] **Given** a harvested test, **When** gating, **Then** it runs in the sandbox against the current commit (sandbox-vs-commit + flaky-test guards apply): pass → confirm `Fixed`; fail → keep `Confirmed`, note the fix didn't work; no test → `git-revision-tracking` heuristic fallback, closure marked **"unverified"** + terminal **`fix_status: closed-unverified`** written so the run-open scan stops rescanning it (v3.3)
- [ ] **Given** a failing proving test (v3.4, review I11), **When** judging, **Then** `fix-failed` is written only if the bug-bolt's completion commit is an **ancestor of the commit under analysis** (via `git-revision-tracking`); if the fix isn't merged yet, the record stays `fix-reported` ("merge pending") and the scan re-picks it
- [ ] **Given** a verified pass, **When** closing, **Then** **`fix_status: verified-fixed`** (+ `verified_at`, `proof_test_ref`) is written onto the bug's fix-request record — the mailbox the knowledge builder watches (Integration Contract §4); a failed fix writes `fix_status: fix-failed` — and a bug is **never closed on AI-DLC's word alone** (AI-DLC's "done" = its bug-bolt's `bolt.md` reaching `status: complete`)
- [ ] **Given** NFR-2, **Then** Prompt 26's original tests still pass

## Technical Notes

- ⚠️ Build by pasting **Prompt 31** from `docs/agent-systems/bug-hunter-build-guide.md` into the
  **skill-creator** skill (`Skill` tool → `skill-creator:skill-creator`) — it creates
  `fix-verification` AND re-opens `bug-lifecycle` at its planned seam. Run the
  brief's tests + re-run Prompt 26's. STOP and report if skill-creator is unavailable.

## Dependencies

### Requires
- 001-regression-harvest; bug-lifecycle (bolt 092); the sandbox (bolt 087 asset)

### Enables
- The closed bug→fix→verified loop with AI-DLC; knowledge-builder re-distillation

## Out of Scope

- Producing fixes (AI-DLC's job; fix-proposal drafts only).
