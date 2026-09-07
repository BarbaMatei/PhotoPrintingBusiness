---
id: 002-bug-documentation
unit: 001-phase-1-skeleton
intent: 035-bug-hunter-agent-system
status: ready
priority: must
created: 2026-06-10T10:40:14Z
assigned_bolt: 087-phase-2-trust
implemented: false
---

# Story: 002-bug-documentation (guide Prompt 2)

**Status:** **not satisfied** — verified by bolt 085-phase-1-skeleton-core (2026-09-03). The loop keeps one canonical describe-once record per defect (What · Evidence · Suggested fix · History), and the doc gate enforces its shape and size — but there is no three-audience record: no per-defect plain-language summary, no reproduction recipe for a tester, no validation of a required-field set, and no structured record at all. Three of five acceptance criteria are absent, so this story is **re-assigned to bolt 087-phase-2-trust** rather than closed here. Evidence: `memory-bank/bolts/085-phase-1-skeleton-core/test-walkthrough.md`.

## User Story

**As** a non-technical stakeholder, a developer, AND a tester
**I want** every bug captured once, in one canonical structured record serving all three audiences
**So that** a bug is understandable, reproducible, and renderable to Markdown (and later SARIF) without rework

## Acceptance Criteria

- [ ] **Given** Prompt 2, **When** built, **Then** skill `bug-documentation` exists, created via skill-creator, and the brief's three test prompts pass (full record for a null-deref; missing-reproduction flagged; contract-cited vs intent-unconfirmed `expected_behavior`) *(2026-09: a component that extends the review loop is a script or skill edit under `reviews/lib` / `.claude/skills`, built and tested there; skill-creator applies only to a new standalone skill — FR-1 as amended)*
- [ ] **Given** a record, **When** validating, **Then** ALL required fields from the brief are enforced (id, signature, title, severity, category from the fixed list, confidence + why, status, risk_score, reachable, commit_sha, **correlation_id**, plain_summary, location[] + flow_position, developer_detail, evidence, reproduction, impact, fix_direction, related) — a record missing required fields is refused
- [ ] **Given** v3 sourcing, **When** writing `expected_behavior`, **Then** an existing knowledge-ledger contract is cited (statement + source ref) when available; otherwise the field is derived from reasoning and tagged **"intent-unconfirmed"**
- [ ] **Given** audience validation, **When** checking, **Then** `plain_summary` is jargon-free, `developer_detail` technical, `reproduction` runnable
- [ ] **Given** a secret-involving defect (v3.2), **When** writing `evidence`, **Then** the record carries the **location + fingerprint** (prefix + length + hash prefix), never the secret's value — the record must be safe in every downstream sink (reports, fix-requests, tickets)

## Technical Notes

- ⚠️ Build by pasting **Prompt 2** from `docs/agent-systems/bug-hunter-build-guide.md` into the
  **skill-creator** skill (`Skill` tool → `skill-creator:skill-creator`); run the
  brief's three test prompts and fix before proceeding. STOP and report if
  skill-creator is unavailable.
- Multi-location support matters later (root-cause-clustering represents N symptoms
  as ONE record with multiple `location` entries).

## Dependencies

### Requires
- (none per the brief)

### Enables
- report-rendering, general-hunter (candidate shape reference), bug-verifier,
  root-cause-clustering, fix-request-emit

## Out of Scope

- Scoring (severity-scoring), rendering (report-rendering).
