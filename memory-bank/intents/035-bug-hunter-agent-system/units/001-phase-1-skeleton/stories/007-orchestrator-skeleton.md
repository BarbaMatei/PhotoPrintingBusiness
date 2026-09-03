---
id: 007-orchestrator-skeleton
unit: 001-phase-1-skeleton
intent: 035-bug-hunter-agent-system
status: ready
priority: must
created: 2026-06-10T10:40:14Z
assigned_bolt: 086-phase-1-skeleton-agents
implemented: false
---

# Story: 007-orchestrator-skeleton (guide Prompt 7 — agent-as-skill)

**Status:** claimed satisfied by `.claude/skills/loop-driver/SKILL.md`, `reviews/lib/drive/route-next-pass.mjs` and `reviews/lib/discovery-review.wf.js` (2026-09) — to be verified by bolt 086-phase-1-skeleton-agents; complete only after that verdict.

## User Story

**As** the owner starting a bug-hunting run
**I want** one coordinator that runs the whole six-slot pipeline end-to-end
**So that** every run flows through the same permanent structure that later phases fill — and Phase 1 output is honestly labeled unverified

## Acceptance Criteria

- [ ] **Given** Prompt 7, **When** built, **Then** skill `orchestrator` exists (agent-as-skill), created via skill-creator, and the brief's three test prompts pass (first run labeled unverified; second run surfaces only new; zero-new-bugs run correct)
- [ ] **Given** the heart of the additive design, **When** defined, **Then** ALL six slots exist now — (1) Open/Map [minimal], (2) Hunt → `general-hunter`, (3) Verify [PASS-THROUGH, candidates tagged `Confidence: Low`/"unverified"], (4) Triage → `deduplication` + rough severity, (5) Report → `report-rendering`, (6) Learn [empty; apply `triage-intake` decisions if present], (7) Close → coverage + run summary via `ledger-io` **single-writer merge** of staged hunter output
- [ ] **Given** the v3 reporting policy, **When** reporting in Phase 1, **Then** the whole report is labeled **"unverified candidates — high false-positive rate until Phase 2"** and the reporting floor applies
- [ ] **Given** discipline rules, **When** running, **Then** a per-run scope + stopping condition are defined; read-only on source; bugs are never invented to avoid an empty run, plausible ones never dropped
- [ ] **Given** the trigger, **When** a run starts, **Then** the description is pushy enough that runs go through the orchestrator rather than calling hunters directly
- [ ] **Given** run mechanics (v3.3, hardened v3.4–v3.5), **When** opening/closing, **Then** Open first checks the **cross-system mutex** (the KB's `knowledge/.run-lock` — refuse/queue while present and fresh) then creates `bug-hunting/.run-lock` (with the stale-lock reclaim rule); Close runs a **two-part audit (v3.5)** — (1) store-scoped diff of `git status -- bug-hunting/` vs the allowed set (a sibling's in-flight files can never trip it), and (2) a **forbidden-ground check** that nothing under application source / `memory-bank/` / `docs/` was touched (except the one owner-approved regression-test file) — then **commits path-scoped** (`git add -- bug-hunting/`, never `-A`, serialized under the mutex; the gitignored code index is never committed), and removes the lock on success or abort
- [ ] **Given** the single-history rule (v3.3, Integration Contract §1), **When** running, **Then** runs happen only in the designated integration worktree on `main`
- [ ] **Given** operating profiles (v3.6, Integration Contract §5.5), **When** triggered/committing, **Then** the skill is **profile-agnostic**: it runs when invoked by the active **TriggerPolicy** and commits per the active **CommitPolicy** (this repo: `solo-local` = `local-hook` + `direct-to-main`) — the hook/CI/branch-PR mechanics are deployment-side adapters, not baked into the skill

## Technical Notes

- ⚠️ Build by pasting **Prompt 7** from `docs/agent-systems/bug-hunter-build-guide.md` into the
  **skill-creator** skill (`Skill` tool → `skill-creator:skill-creator`); run the
  brief's three test prompts and fix before proceeding. STOP and report if
  skill-creator is unavailable.
- The Phase 1 milestone check (unit success criteria): after this story, run the
  system on this repo end-to-end — ledger + labeled report produced.
- Later extensions land at the slots (11b, 24d, 29b) — the SKILL.md should make the
  slot structure explicit so those seams are easy to extend without restructuring.

## Dependencies

### Requires
- All of 001–006 (the brief lists all Phase 1 components)

### Enables
- Every later phase (they fill/extend its slots)

## Out of Scope

- Real verification (P2), specialists/cost control (P3), curation (P4).
