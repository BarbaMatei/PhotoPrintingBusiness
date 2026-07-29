---
id: 005-application-feature-promotion
unit: 001-layering-foundation
intent: 027-architectural-layering
status: draft
priority: should
created: 2026-06-05T09:30:00Z
assigned_bolt: 059-layering-foundation
implemented: false
---

# Story: 005-application-feature-promotion

## User Story

**As a** developer
**I want** the flat `Services/` promoted into `Application/<Feature>/Services/`
**So that** each feature's code is cohesive and the namespace marks a future module boundary

## Acceptance Criteria

- [ ] **Given** the flat 49-file `Services/`, **When** promoted, **Then** each service + DTO lives under `Application/<Feature>/` (Orders, Auth, Invoicing, Sameday, Storage, Cart, Catalog, Account, Payments, Uploads, Email, Admin, VAT→Domain)
- [ ] **Given** existing `Services/Sameday/` and `Services/Invoicing/` precedent, **When** promoted, **Then** they fit the same shape
- [ ] **Given** small per-feature batches, **When** each lands, **Then** git history stays bisectable and CI green

## Technical Notes

- This is first-pass P06, folded into P21 as PR4. DI registrations need only `using` changes.

## Dependencies

### Requires
- 004-web-layer

### Enables
- 027/002 (Abstractions added per Application/<Feature>/)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Cross-feature service (admin orchestrator) | Place under `Application/Admin/` |

## Out of Scope

- `Abstractions/` subfolders (unit 002).
