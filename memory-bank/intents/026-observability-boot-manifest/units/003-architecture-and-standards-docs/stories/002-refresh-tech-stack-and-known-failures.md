---
id: 002-refresh-tech-stack-and-known-failures
unit: 003-architecture-and-standards-docs
intent: 026-observability-boot-manifest
status: draft
priority: must
created: 2026-06-05T09:30:00Z
assigned_bolt: 057-architecture-and-standards-docs
implemented: false
---

# Story: 002-refresh-tech-stack-and-known-failures

## User Story

**As a** new contributor
**I want** the standards docs to match reality and a register of known-failing tests
**So that** I trust the docs and don't chase phantom dependencies or "broken" tests

## Acceptance Criteria

- [ ] **Given** `tech-stack.md`, **When** refreshed against `package.json`/`.csproj`, **Then** it states Angular 21 (not 17+), Vitest (not Jasmine/Karma), removes `heic2any`/`ng2-charts`, and corrects the config-driven email provider
- [ ] **Given** the 7 consistently-failing tests, **When** `docs/KNOWN_FAILURES.md` is written, **Then** each has a reason and a tracking issue
- [ ] **Given** any doc claim, **When** spot-checked, **Then** it is verifiable against installed dependencies

## Technical Notes

- Audit the 7 failures during this story (likely CI S3-integration skips); an unexplained one becomes a bug ticket.

## Dependencies

### Requires
- None

### Enables
- None

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| A "known failure" is actually a real bug | Promote to a bug ticket rather than documenting as expected |

## Out of Scope

- Fixing the failing tests.
