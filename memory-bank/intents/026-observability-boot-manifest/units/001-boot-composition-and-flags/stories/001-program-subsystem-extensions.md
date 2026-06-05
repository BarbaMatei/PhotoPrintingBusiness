---
id: 001-program-subsystem-extensions
unit: 001-boot-composition-and-flags
intent: 026-observability-boot-manifest
status: draft
priority: should
created: 2026-06-05T09:30:00Z
assigned_bolt: 055-boot-composition-and-flags
implemented: false
---

# Story: 001-program-subsystem-extensions

## User Story

**As a** developer reviewing the boot script
**I want** Sameday/ANAF/Invoicing/Payments/Sentry DI moved into extension methods
**So that** `Program.cs` is reviewable and each subsystem is testable in isolation

## Acceptance Criteria

- [ ] **Given** the inline DI blocks, **When** extracted into `Add{Sameday,Anaf,Invoicing,Payments,Sentry}`, **Then** `Program.cs` becomes a ~120-LOC fluent chain
- [ ] **Given** each extension, **When** its flag is `Enabled=false`, **Then** a unit test asserts no background service is registered
- [ ] **Given** the ordering requirement, **When** the host boots, **Then** `AddInvoicing` runs before `AddAnaf` (a boot test guards this)
- [ ] **Given** `QuestPDF.Settings.License`, **When** moved into `AddInvoicing`, **Then** it is co-located with its only consumer

## Technical Notes

- Follow the existing `AddSocialAuth`/`AddObservability` precedent. Conditional `if (xEnabled)` moves inside each extension.

## Dependencies

### Requires
- None

### Enables
- 002-typed-feature-gate (extensions read flags through the gate)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Extension order swapped | Boot test fails (ANAF before Invoicing) |

## Out of Scope

- The manifest endpoint (unit 002).
