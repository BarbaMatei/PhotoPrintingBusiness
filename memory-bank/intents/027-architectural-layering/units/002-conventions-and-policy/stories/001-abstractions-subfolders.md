---
id: 001-abstractions-subfolders
unit: 002-conventions-and-policy
intent: 027-architectural-layering
status: draft
priority: should
created: 2026-06-05T09:30:00Z
assigned_bolt: 060-conventions-and-policy
implemented: false
---

# Story: 001-abstractions-subfolders

## User Story

**As a** developer navigating a feature folder
**I want** interfaces in an `Abstractions/` subfolder, separate from implementations
**So that** the folder listing stops being a noisy interleave of `IFoo.cs`/`Foo.cs`

## Acceptance Criteria

- [ ] **Given** each `Application/<Feature>/`, **When** refactored, **Then** all `I*.cs` move to `Abstractions/` and implementations stay at the feature root
- [ ] **Given** the move, **When** namespaces update to `...<Feature>.Abstractions`, **Then** cross-feature consumers reference the Abstractions namespace
- [ ] **Given** DI registrations, **When** updated, **Then** only `using` directives change
- [ ] **Given** the change, **When** built/tested, **Then** CI green per batch

## Technical Notes

- Chosen over status-quo and consumer-side DIP — there is no second implementation to justify the indirection.

## Dependencies

### Requires
- 027/001/005-application-feature-promotion

### Enables
- 002-no-repository-policy-and-analyzer; 027/003 handlers

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Interface used by many features | Lives in its owning feature's Abstractions; others reference it |

## Out of Scope

- The no-repo policy (next story).
