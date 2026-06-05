---
id: 002-typed-feature-gate
unit: 001-boot-composition-and-flags
intent: 026-observability-boot-manifest
status: draft
priority: should
created: 2026-06-05T09:30:00Z
assigned_bolt: 055-boot-composition-and-flags
implemented: false
---

# Story: 002-typed-feature-gate

## User Story

**As a** developer wiring conditional subsystems
**I want** a typed `IFeatureGate` over a flag enum and registry
**So that** flag reads are compile-checked, testable, and a single source of truth

## Acceptance Criteria

- [ ] **Given** a `FeatureFlag` enum covering all current flags, **When** `IFeatureGate` is implemented, **Then** `IsEnabled(flag)` and `GetAll()` resolve from a static enum→key+default+description table
- [ ] **Given** a missing/malformed config key, **When** `IsEnabled` is called, **Then** it returns the documented default (not a silent `false`)
- [ ] **Given** every former `GetValue<bool>("Enabled")` call site, **When** migrated, **Then** all reads go through the gate
- [ ] **Given** the boot-time binding, **When** documented, **Then** it states the gate is not hot-reloadable

## Technical Notes

- Flags: Sameday, SamedayJobs, Sentry, Observability, Anaf, InvoiceEmailAttachments, PhotoArchive, OldOriginalArchive.
- `GetAll()` is the data source for the P04 manifest (unit 002).

## Dependencies

### Requires
- 001-program-subsystem-extensions

### Enables
- 026/002-system-manifest-and-liveness/001-system-info-endpoint

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Typo'd config key | Resolves to documented default |
| New flag added later | One registry-table row added |

## Out of Scope

- Hot reload / `Microsoft.FeatureManagement`.
