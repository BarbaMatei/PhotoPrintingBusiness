---
id: 002-no-repository-policy-and-analyzer
unit: 002-conventions-and-policy
intent: 027-architectural-layering
status: draft
priority: should
created: 2026-06-05T09:30:00Z
assigned_bolt: 060-conventions-and-policy
implemented: false
---

# Story: 002-no-repository-policy-and-analyzer

## User Story

**As a** maintainer
**I want** the no-repository posture documented and the IQueryable-leak rule enforced
**So that** a future contributor doesn't add repositories/specifications "to be safe"

## Acceptance Criteria

- [ ] **Given** `data-access-conventions.md`, **When** written, **Then** it states: services inject `PhotoPrintDbContext` directly; no `IQueryable<T>` in public signatures; duplicate query shapes extracted only at 3+ call sites; cross-service `SaveChangesAsync` documented per-handler when load-bearing
- [ ] **Given** the analyzer, **When** it runs, **Then** any `IQueryable<T>` return in `Application/.../Services/*.cs` or `Abstractions/I*.cs` is a build error
- [ ] **Given** existing code, **When** the analyzer is added, **Then** it passes (or a discovered leak is fixed)
- [ ] **Given** `system-architecture.md`, **When** updated, **Then** it links the convention doc

## Technical Notes

- Use the `BannedApiAnalyzers` package; rule degrades to CONTRIBUTING.md + review if analyzers are deemed overkill.

## Dependencies

### Requires
- 001-abstractions-subfolders

### Enables
- None

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Analyzer finds a real leak | Fix it; track as a ticket — good outcome |

## Out of Scope

- Introducing repositories (explicitly rejected).
