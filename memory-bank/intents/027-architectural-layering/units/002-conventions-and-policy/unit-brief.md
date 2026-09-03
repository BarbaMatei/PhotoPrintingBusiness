---
unit: 002-conventions-and-policy
intent: 027-architectural-layering
phase: inception
status: draft
unit_type: backend
default_bolt_type: simple-construction-bolt
created: 2026-06-05T09:30:00Z
updated: 2026-06-05T09:30:00Z
---

# Unit Brief: Conventions & Policy

## Purpose

Resolve the interface↔implementation interleaving with an `Abstractions/` subfolder per feature (P23), and lock the no-repository posture with a documented policy + analyzer that forbids `IQueryable<T>` leaking from services (P24).

## Scope

### In Scope
- Move all `I*.cs` into `Abstractions/`; `data-access-conventions.md`; banned-API analyzer rule for `IQueryable<T>` return types.

### Out of Scope
- Introducing repositories (explicitly rejected).
- Handlers (unit 003).

---

## Assigned Requirements

| FR | Requirement | Priority |
|----|-------------|----------|
| FR-5 (P23) | Abstractions/ subfolder per feature | Should |
| FR-6 (P24) | No-repository policy + IQueryable analyzer | Should |

---

## Domain Concepts

### Key Operations
| Operation | Description | Inputs | Outputs |
|-----------|-------------|--------|---------|
| Relocate interfaces | Move I*.cs to Abstractions/ | feature folders | Abstractions namespaces |
| Enforce no-IQueryable | Analyzer over service signatures | source | build error on leak |

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
| 001-abstractions-subfolders | Abstractions/ per feature | Should | Planned |
| 002-no-repository-policy-and-analyzer | No-repo policy + IQueryable analyzer | Should | Planned |

---

## Dependencies

### Depends On
| Unit | Reason |
|------|--------|
| 001-layering-foundation | Operates on the new Application/<Feature>/ layout |

### Depended By
| Unit | Reason |
|------|--------|
| 003-handler-pattern | Handlers reference Abstractions/ interfaces |

### External Dependencies
| System | Purpose | Risk |
|--------|---------|------|
| Roslyn BannedApiAnalyzers | IQueryable rule | Low |

---

## Technical Context

### Suggested Technology
`Microsoft.CodeAnalysis.BannedApiAnalyzers`; namespace `using` updates.

---

## Constraints

- If the analyzer surfaces an existing `IQueryable` leak, fix it (good outcome).

---

## Success Criteria

### Functional
- [ ] All `I*.cs` under `Abstractions/`; consumers reference the Abstractions namespace.
- [ ] `data-access-conventions.md` written + linked; analyzer flags any `IQueryable<T>` return.

### Non-Functional
- [ ] No behaviour change.

### Quality
- [ ] CI green; `using` churn verified mechanical.

---

## Bolt Suggestions

| Bolt | Type | Stories | Objective |
|------|------|---------|-----------|
| 060-conventions-and-policy | simple | 001, 002 | Abstractions + no-repo policy |

---

## Notes

After unit 001. The cleanest single intervention against the maintainer's "interfaces and classes in the same place" complaint.
