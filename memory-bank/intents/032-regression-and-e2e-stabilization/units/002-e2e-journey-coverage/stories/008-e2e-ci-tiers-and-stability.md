---
id: 008-e2e-ci-tiers-and-stability
unit: 002-e2e-journey-coverage
intent: 032-regression-and-e2e-stabilization
status: draft
priority: must
created: 2026-06-05T11:35:00Z
assigned_bolt: 071-e2e-journey-coverage
implemented: false
---

# Story: 008-e2e-ci-tiers-and-stability

## User Story

**As a** maintainer
**I want** the full e2e suite integrated into CI as a fast PR tier plus a full run on every merge to main, with flake controls and failure artifacts
**So that** PRs stay fast while the whole application is covered and failures are diagnosable

## Acceptance Criteria

- [ ] **Given** `playwright-e2e.yml` (from bolt 066), **When** extended, **Then** a **fast tier** runs on PR (smoke + highest-value journeys) within the documented budget (~8 min), and the **full suite** runs on every merge to main (~25 min) — owner decision 2026-06-05
- [ ] **Given** the Playwright config, **When** CI runs, **Then** `retries` are bounded (1–2 in CI only), and trace + video + screenshot are captured on failure and uploaded as a CI artifact
- [ ] **Given** the specs, **When** audited, **Then** there are **zero** fixed `sleep`/`waitForTimeout` calls; all waits are condition-based (web-first assertions; SignalR awaited with a bounded timeout)
- [ ] **Given** the full suite, **When** it runs on merge to main, **Then** it is **green across 3 consecutive runs** before the suite is declared stable

## Technical Notes

- Extend, do not replace, bolt 066's workflow; tag specs (project/grep) to split fast vs full tiers.
- Artifact upload uses the standard `actions/upload-artifact` on failure.

## Dependencies

### Requires
- 004-real-postgres-e2e-boot (unit 001)
- All journey specs (001–007) to populate the tiers
- bolt 066 (`playwright-e2e.yml` extended)

### Enables
- 002-execute-regression-baseline (unit 003) reads the e2e result

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| A spec flakes once | Bounded retry masks a transient; persistent flake fails and is triaged |
| Full suite exceeds budget | Parallelise via Playwright workers/shards; document the new budget |

## Out of Scope

- Branch-protection / required-checks config (that is the Rung-0 foundations work, not this intent).
