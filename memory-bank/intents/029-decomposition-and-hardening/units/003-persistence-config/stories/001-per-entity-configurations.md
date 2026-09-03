---
id: 001-per-entity-configurations
unit: 003-persistence-config
intent: 029-decomposition-and-hardening
status: draft
priority: could
created: 2026-06-05T09:30:00Z
assigned_bolt: 065-persistence-config
implemented: false
---

# Story: 001-per-entity-configurations

## User Story

**As a** developer reviewing schema changes
**I want** each entity's EF config in its own file
**So that** per-entity diffs are readable and a missing index is easy to spot

## Acceptance Criteria

- [ ] **Given** the 17 inline `modelBuilder.Entity<X>(...)` blocks, **When** moved to `Data/Configurations/<Entity>Configuration.cs` implementing `IEntityTypeConfiguration<T>`, **Then** `OnModelCreating` becomes `ApplyConfigurationsFromAssembly(...)`
- [ ] **Given** `OnModelCreating`, **When** refactored, **Then** it is ≤ 100 LOC
- [ ] **Given** the refactor, **When** `Add-Migration NoOpRefactorVerify` runs, **Then** it produces empty up/down (no dropped index/conversion)

## Technical Notes

- Lands under `Infrastructure/Data/Configurations/` after intent 027.

## Dependencies

### Requires
- 027 (Infrastructure/Data placement)

### Enables
- None

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| A `HasIndex` accidentally dropped | Add-Migration shows a diff → fix before merge |

## Out of Scope

- Schema changes of any kind.
