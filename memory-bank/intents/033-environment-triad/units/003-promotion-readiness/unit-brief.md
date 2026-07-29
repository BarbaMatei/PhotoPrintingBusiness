---
unit: 003-promotion-readiness
intent: 033-environment-triad
phase: inception
status: stories-defined
unit_type: frontend
default_bolt_type: simple-construction-bolt
created: 2026-06-05T12:20:00Z
updated: 2026-06-05T12:30:00Z
---

# Unit Brief: Promotion Readiness

## Purpose

Tie the triad together with a repeatable dev→prod promotion runbook written as **readiness documentation** — config swap, secret swap (test→live), the existing image-tag flow, migration apply, seed policy, smoke verification — and an explicit note that executing it is deferred to roadmap Phase 6. This unit writes *how a future promotion would go*; it does not perform one.

## Scope

### In Scope
- `docs/environments/promotion-path.md`: ordered, repeatable promotion steps cross-referencing unit-001 config map, unit-002 secrets matrix + seeding policy, the existing `deploy.yml` image-tag flow, and the DEPLOYMENT.md §7 migration caveat.
- An explicit **Phase-6 deferral note** (deployment is out of scope now), cross-linked from DEPLOYMENT.md.

### Out of Scope
- Performing any deployment, provisioning, or cutover.
- Defining the tier/secrets/seeding (units 001/002 — referenced, not restated).
- Modifying `deploy.yml` (referenced as the mechanism only).

---

## Assigned Requirements

| FR | Requirement | Priority |
|----|-------------|----------|
| FR-5 | Documented dev→prod promotion path (readiness runbook) | Should |

---

## Domain Concepts

### Key Operations
| Operation | Description | Inputs | Outputs |
|-----------|-------------|--------|---------|
| Document promotion | Sequence the dev→prod steps | units 001/002 outputs + deploy.yml | runbook |
| Defer execution | State Phase-6 boundary | roadmap | deferral note |

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
| 001-promotion-path-runbook | dev→prod promotion runbook (readiness) | Should | Planned |
| 002-deployment-deferral-note | Explicit Phase-6 deferral note | Should | Planned |

---

## Dependencies

### Depends On
| Unit | Reason |
|------|--------|
| 001-config-tiers-and-compose | Runbook references the config map |
| 002-secrets-and-seeding | Runbook references the secrets matrix + seeding policy |

### Depended By
| Unit | Reason |
|------|--------|
| None | Terminal unit |

### External Dependencies
| System | Purpose | Risk |
|--------|---------|------|
| `deploy.yml` (existing) | Referenced image-tag flow | Low (not modified) |
| DEPLOYMENT.md §7 | Migration caveat cross-link | Low |

---

## Technical Context

### Suggested Technology
Markdown runbook under `docs/environments/`; cross-links to existing `deploy.yml` and DEPLOYMENT.md.

---

## Constraints

- Readiness only — no deployment-pressure language; execution explicitly deferred to Phase 6.
- References, does not restate, units 001/002 outputs.

---

## Success Criteria

### Functional
- [ ] Runbook sequences the promotion as repeatable readiness steps.
- [ ] Cross-references config map, secrets matrix, seeding policy, image-tag flow, migration caveat.
- [ ] Phase-6 deferral note present and cross-linked from DEPLOYMENT.md.

### Non-Functional
- [ ] No instruction reads as "deploy now".

### Quality
- [ ] No duplication of unit-001/002 content (links only).

---

## Bolt Suggestions

| Bolt | Type | Stories | Objective |
|------|------|---------|-----------|
| 075-promotion-readiness | simple | 001–002 | Promotion runbook + Phase-6 deferral note |

---

## Notes

Smallest unit. Its whole value is making a *future* deployment safe and repeatable while keeping deployment firmly out of the present scope.
