---
id: 003-renovate-config
unit: 001-dependency-and-boot-hardening
intent: 025-security-dependency-hygiene
status: draft
priority: should
created: 2026-06-05T09:30:00Z
assigned_bolt: 054-dependency-and-boot-hardening
implemented: false
---

# Story: 003-renovate-config

## User Story

**As a** maintainer tired of manual dependency upgrades
**I want** Renovate to open grouped, scheduled upgrade PRs
**So that** CVEs are surfaced automatically and upgrades stay low-noise

## Acceptance Criteria

- [ ] **Given** `.github/renovate.json`, **When** Renovate runs, **Then** it groups `^OpenTelemetry\.`, `^Microsoft\.EntityFrameworkCore`/`^Npgsql`, and `^@angular/` into one PR each
- [ ] **Given** the schedule config, **When** routine updates are due, **Then** they land on the first of the month and majors on Jan/Apr/Jul/Oct
- [ ] **Given** `dependencyDashboard: true`, **When** upgrades are pending, **Then** a single dashboard issue lists them
- [ ] **Given** `vulnerabilityAlerts`, **When** a CVE is published, **Then** the PR is labelled `security` and is not auto-merged

## Technical Notes

- Depends on the central manifest (002) so grouping is meaningful.
- Renovate GitHub App install is a one-time repo-admin action (tracked as an Open Question, not code).

## Dependencies

### Requires
- 002-central-package-management

### Enables
- None (durable ops process)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| PR noise without triage | Dependency dashboard + quarterly review ritual (intent 026 P19) |

## Out of Scope

- Auto-merge of any updates (kept off for a payments codebase).
