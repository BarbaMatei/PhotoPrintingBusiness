---
unit: 001-boot-composition-and-flags
intent: 026-observability-boot-manifest
phase: inception
status: draft
unit_type: backend
default_bolt_type: simple-construction-bolt
created: 2026-06-05T09:30:00Z
updated: 2026-06-05T09:30:00Z
---

# Unit Brief: Boot Composition & Feature Flags

## Purpose

Make the boot script readable and per-subsystem testable (P07) and replace string-typed feature flags with a typed `IFeatureGate` registry (P10) that doubles as the data source for the system-info manifest. Refactor work — `simple-construction-bolt`.

## Scope

### In Scope
- Extract `AddSameday`/`AddAnaf`/`AddInvoicing`/`AddPayments`/`AddSentry` extensions; slim `Program.cs` to ~120 LOC.
- `FeatureFlag` enum + `IFeatureGate` (`IsEnabled`, `GetAll`) bound from config at boot; migrate all flag readers.

### Out of Scope
- The manifest endpoint + UI (units 002/004).
- Hot-reloadable flags (boot-time only; consistent with bolt-046 deprioritization).

---

## Assigned Requirements

| FR | Requirement | Priority |
|----|-------------|----------|
| FR-1 (P07) | Extract Program.cs subsystem composition into 5 extension methods | Should |
| FR-2 (P10) | Centralise feature flags via typed `IFeatureGate` | Should |

---

## Domain Concepts

### Key Entities
| Entity | Description | Attributes |
|--------|-------------|------------|
| FeatureFlag (enum) | All flags | Sameday, SamedayJobs, Sentry, Observability, Anaf, InvoiceEmailAttachments, PhotoArchive, OldOriginalArchive |
| FeatureFlagInfo | Registry entry | Flag, ConfigKey, Enabled, Default, Description |

### Key Operations
| Operation | Description | Inputs | Outputs |
|-----------|-------------|--------|---------|
| AddX extension | Register a subsystem DI graph behind its flag | IServiceCollection, IConfiguration | services |
| IFeatureGate.GetAll | Enumerate flags for the manifest | — | dict of FeatureFlagInfo |

---

## Story Summary

| Metric | Count |
|--------|-------|
| Total Stories | 2 |
| Must Have | 0 |
| Should Have | 2 |
| Could Have | 0 |

### Stories

| Story ID | Title | Priority | Status |
|----------|-------|----------|--------|
| 001-program-subsystem-extensions | Program.cs subsystem extensions | Should | Planned |
| 002-typed-feature-gate | Typed IFeatureGate registry | Should | Planned |

---

## Dependencies

### Depends On
| Unit | Reason |
|------|--------|
| None | — |

### Depended By
| Unit | Reason |
|------|--------|
| 002-system-manifest-and-liveness | Manifest derives flags from `IFeatureGate.GetAll()` |
| 004-...-ui | Renders flag state |

### External Dependencies
| System | Purpose | Risk |
|--------|---------|------|
| IConfiguration | Flag binding at boot | Low |

---

## Technical Context

### Suggested Technology
ASP.NET Core 8 DI extension methods; options binding; static enum→key registry table.

### Integration Points
| Integration | Type | Protocol |
|-------------|------|----------|
| Program.cs | DI composition | in-process |

---

## Constraints

- Composition order: `AddInvoicing` before `AddAnaf` (ANAF depends on Invoicing).
- Flags are boot-time only; document no hot reload.

---

## Success Criteria

### Functional
- [ ] `Program.cs` ≈ 120 LOC fluent chain; per-extension "Enabled=false registers nothing background-y" test.
- [ ] All flag readers migrated to `IFeatureGate`; typo'd key → documented default, not silent false.

### Non-Functional
- [ ] No behaviour change; host boots in the same order.

### Quality
- [ ] Unit tests for the gate against missing/malformed config.

---

## Bolt Suggestions

| Bolt | Type | Stories | Objective |
|------|------|---------|-----------|
| 055-boot-composition-and-flags | simple | 001, 002 | Extensions + IFeatureGate |

---

## Notes

Foundational for units 002 and 004. P07 → P10 internal order.
