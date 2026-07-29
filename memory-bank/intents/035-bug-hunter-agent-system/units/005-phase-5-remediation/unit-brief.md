---
unit: 005-phase-5-remediation
intent: 035-bug-hunter-agent-system
phase: inception
status: ready
unit_type: tooling
default_bolt_type: simple-construction-bolt
created: 2026-06-10T10:40:14Z
updated: 2026-06-10T10:40:14Z
---

# Unit Brief: Phase 5 — Remediation & Regression Safety

## Purpose

Close the loop on fixing. Keep the Verifier's proving test as a permanent regression
tripwire, verify fixes by **re-running that test** (the gate that authorizes closing
a bug — including in the AI-DLC bug→fix→re-distil loop), draft patches validated
against the surrounding suite (never applied), and hand confirmed bugs to AI-DLC
through an idempotent fix-request store keyed by `correlation_id`.

## Scope

### In Scope — 5 briefs (guide Prompts 30–33 + 31b)
| Component | Brief | Role |
|-----------|-------|------|
| `regression-harvest` | 30 | Keep the proving test (owner-approved suite write — the ONE allowed new-file write) |
| `fix-verification` | 31 | The closure **GATE**; extends `bug-lifecycle`'s "mark Fixed"; emits `verified-fixed` |
| `orchestrator` (ext) | 31b | NEW in v3.3 (review H1): run-open fix-request mailbox scan — how "fix done" is noticed |
| `fix-proposal` | 32 | Draft diff validated vs surrounding suite in the sandbox; proposal only |
| `fix-request-emit` | 33 | Idempotent hand-off store for AI-DLC, keyed by `correlation_id` |

### Out of Scope
- Applying patches to the repository (never); AI-DLC's own bug-bolt mechanics
  (separate flow — this unit only feeds and listens to it).

---

## ⚠️ Construction Method (owner mandate + guide Part I — MUST follow)

**Each component MUST be created with the `skill-creator` skill** (`Skill` tool →
`skill-creator:skill-creator`): paste Prompt N from
`docs/agent-systems/bug-hunter-build-guide.md`, build, **run the brief's test prompts**,
fix, then move on — in order. Prompt 31 also **re-opens** `bug-lifecycle`, and Prompt 31b
**re-opens** the `orchestrator` at its Open seam (planned extensions). If skill-creator is
unavailable, **STOP and report**.

---

## Assigned Requirements

| FR | Requirement | Priority |
|----|-------------|----------|
| FR-7 | Phase 5 remediation (Prompts 30–33) | Should |
| FR-1, FR-2 | Cross-cutting | Must |

## Story Summary

- **Total Stories**: 5 — **Should**: 5

### Stories
- [ ] **001-regression-harvest** — Should — Planned
- [ ] **002-fix-verification** — Should — Planned
- [ ] **003-fix-proposal** — Should — Planned
- [ ] **004-fix-request-emit** — Should — Planned
- [ ] **005-orchestrator-remediation-ext** — Should — Planned (NEW in v3.3, review H1)

## Dependencies

### Depends On
| Unit | Reason |
|------|--------|
| 002-phase-2-trust | Proving tests come from the Verifier; sandbox guards reused |
| 004-phase-4-learn-and-measure | fix-verification extends bug-lifecycle |

### Depended By
| Unit | Reason |
|------|--------|
| (the AI-DLC bug-fix loop) | fix-requests in; verified-fixed signal out |

## Technical Context

- **The loop (binding)**: confirmed bug → `fix-request-emit` writes to
  `bug-hunting/fix-requests/` (requirements D3) with `correlation_id` → AI-DLC
  creates a bug-bolt and fixes → AI-DLC signals "fix done" (noticed by the run-open
  mailbox scan — Integration Contract §4, v3.2) → `fix-verification` writes
  **`fix_status: fix-reported`** (fix exists, unproven — v3.2), then
  re-runs the harvested test in the sandbox (commit-match + flaky guards apply) →
  on pass: bug `Fixed` + **`fix_status: verified-fixed`** written onto the
  fix-request record (the mailbox the knowledge builder watches — Integration
  Contract §4; same `correlation_id`). **Never close on AI-DLC's word alone**
  (AI-DLC's "done" = the bug-bolt's `bolt.md` reaching `status: complete`).
- **No harvested test** ⇒ fall back to the `git-revision-tracking` heuristic and mark
  the closure "unverified" (honest labeling over false confidence).
- `fix-proposal` validation ladder: bug's own test passes AND module's surrounding
  suite shows no new failures ⇒ "validated"; own-test-only ⇒ "passes its own test,
  broader impact unchecked".
- Regression tests land in the repo's real test projects **only with owner approval**
  per test (this is the single sanctioned write into the codebase, and it is
  test-code only).

## Constraints

- Read-only on app source (sandbox-only patch application). Fix-requests must be
  idempotent — update, never duplicate, per `correlation_id`.

## Success Criteria

- [ ] All 5 briefs built via skill-creator; each brief's test prompts passing.
- [ ] Demonstrated discovery: a completed bug-bolt is noticed by the run-open mailbox scan (31b)
      without any manual prompt.
- [ ] Demonstrated gate behavior: a fix that passes its proving test → `Fixed` +
      `verified-fixed`; a fix that doesn't → stays `Confirmed`, no signal.

## Bolt Suggestions

| Bolt | Type | Stories | Objective |
|------|------|---------|-----------|
| 093-phase-5-remediation | simple-construction-bolt | all 5 | The fix loop: harvest → gate → mailbox scan → proposal → hand-off |
