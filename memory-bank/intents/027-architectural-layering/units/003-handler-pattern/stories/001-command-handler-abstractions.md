---
id: 001-command-handler-abstractions
unit: 003-handler-pattern
intent: 027-architectural-layering
status: draft
priority: should
created: 2026-06-05T09:30:00Z
assigned_bolt: 061-handler-pattern
implemented: false
---

# Story: 001-command-handler-abstractions

## User Story

**As a** developer
**I want** lightweight `ICommandHandler`/`IEventDispatcher` interfaces (no MediatR)
**So that** multi-step use cases have a home without adding a tracked dependency

## Acceptance Criteria

- [ ] **Given** `Application/Shared/Abstractions/`, **When** added, **Then** it defines `ICommandHandler<TCommand,TResult>` and `IEventDispatcher<TEvent>`
- [ ] **Given** the bar (3+ concerns or 50+ LOC), **When** documented, **Then** single-statement actions are explicitly excluded
- [ ] **Given** the new interfaces, **When** built, **Then** CI green

## Technical Notes

- ~30 LOC total. No MediatR (relicensing + dependency-tracking reasons).

## Dependencies

### Requires
- 027/002 Abstractions convention

### Enables
- 002-create-order-handler, 003-order-paid-event-dispatcher, 004-retry-and-promote-handlers

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Temptation to over-apply | Bar enforced in review |

## Out of Scope

- The concrete handlers (later stories).
