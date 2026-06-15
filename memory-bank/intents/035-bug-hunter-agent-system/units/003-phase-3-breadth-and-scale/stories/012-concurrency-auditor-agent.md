---
id: 012-concurrency-auditor-agent
unit: 003-phase-3-breadth-and-scale
intent: 035-bug-hunter-agent-system
status: ready
priority: should
created: 2026-06-10T10:40:14Z
assigned_bolt: 090-phase-3-specialists-b
implemented: false
---

# Story: 012-concurrency-auditor-agent (guide Prompt 22 — agent-as-skill, CONDITIONAL)

## User Story

**As** the Hunt slot's concurrency specialist
**I want** races, deadlocks, and ordering bugs hunted in the async/shared-state code
**So that** the hardest-to-reproduce bug class gets systematic attention

## Acceptance Criteria

- [ ] **Given** Prompt 22, **When** built, **Then** skill `concurrency-auditor-agent` exists (agent-as-skill), created via skill-creator, and the brief's three test prompts pass (check-then-act races in an inventory decrement; lock-ordering deadlocks; transaction racing the email it sends)
- [ ] **Given** shared mutable state, async/parallel code, locks, transactional regions (via `code-index`/`flow-tracing`), **When** hunting, **Then** checks cover data races, non-atomic check-then-act (TOCTOU), missing/inconsistent locking, lock-ordering deadlocks, transaction-vs-external-effect races, unsafe lazy init
- [ ] **Given** emission, **When** done, **Then** `deduplication` first; candidates only, with the triggering interleaving described as evidence
- [ ] **Given** confirmability, **When** verified later, **Then** it is expected and acceptable that the Verifier marks most of these Medium ("reasoned, not reproduced")

## Technical Notes

- ⚠️ Build by pasting **Prompt 22** from `docs/agent-systems/bug-hunter-build-guide-v3.6.md` into the
  **skill-creator** skill (`Skill` tool → `skill-creator:skill-creator`); run the
  brief's three test prompts and fix before proceeding. STOP and report if
  skill-creator is unavailable.
- **Priority Should (requirements D5)**: the guide marks this optional
  ("skip for strictly single-threaded"), but this stack is async/await throughout,
  with BackgroundServices, EF transactions, webhook concurrency, and SignalR — the
  brief's own trigger conditions. Owner may still defer it within bolt 090.
- Known prior art to seed scope: payment idempotency work (intent 014) addressed some
  TOCTOU surface; webhook handlers + job retries remain.

## Dependencies

### Requires
- 005-flow-tracing, 002-code-index; deduplication, bug-documentation (bolt 085)

### Enables
- orchestrator specialist dispatch (24d)

## Out of Scope

- Stress/load testing (out of system scope entirely).
