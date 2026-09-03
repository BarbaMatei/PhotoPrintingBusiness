---
unit: 003-persistence-config
intent: 029-decomposition-and-hardening
phase: inception
status: draft
unit_type: backend
default_bolt_type: simple-construction-bolt
created: 2026-06-05T09:30:00Z
updated: 2026-06-05T09:30:00Z
---

# Unit Brief: Persistence Config

## Purpose

Move the 437-LOC `OnModelCreating` into per-entity `IEntityTypeConfiguration<T>` files so per-entity diffs are reviewable and a missing index is easy to spot (P15).

## Scope

### In Scope
- One `Data/Configurations/<Entity>Configuration.cs` per entity; `ApplyConfigurationsFromAssembly` in `OnModelCreating`.

### Out of Scope
- Any schema change (must be a no-op refactor).

---

## Assigned Requirements

| FR | Requirement | Priority |
|----|-------------|----------|
| FR-4 (P15) | Per-entity IEntityTypeConfiguration<T> | Could |

---

## Domain Concepts

### Key Operations
| Operation | Description | Inputs | Outputs |
|-----------|-------------|--------|---------|
| Extract config | Move inline lambda to Configure() | OnModelCreating block | <Entity>Configuration.cs |
| Verify no-op | Confirm zero drift | Add-Migration | empty up/down |

---

## Story Summary

| Metric | Count |
|--------|-------|
| Total Stories | 1 |
| Must Have | 0 |
| Should Have | 0 |
| Could Have | 1 |

### Stories

| Story ID | Title | Priority | Status |
|----------|-------|----------|--------|
| 001-per-entity-configurations | Per-entity EF config split | Could | Planned |

---

## Dependencies

### Depends On
| Unit | Reason |
|------|--------|
| 027 | Lands under Infrastructure/Data/Configurations |

### Depended By
| Unit | Reason |
|------|--------|
| None | — |

### External Dependencies
| System | Purpose | Risk |
|--------|---------|------|
| EF Core migration tool | No-op verification | Medium |

---

## Technical Context

### Suggested Technology
`IEntityTypeConfiguration<T>` + `ApplyConfigurationsFromAssembly`.

---

## Constraints

- Easy to drop a `HasIndex`/`HasConversion` — `Add-Migration` must show empty diff.

---

## Success Criteria

### Functional
- [ ] One config file per entity; `OnModelCreating` ≤ 100 LOC.

### Non-Functional
- [ ] Zero schema drift.

### Quality
- [ ] `Add-Migration NoOpRefactorVerify` empty; CI green.

---

## Bolt Suggestions

| Bolt | Type | Stories | Objective |
|------|------|---------|-----------|
| 065-persistence-config | simple | 001 | Per-entity EF config |

---

## Notes

Touches only Data/ — parallelisable with unit 001.
