---
unit: 001-layering-foundation
intent: 027-architectural-layering
phase: inception
status: draft
unit_type: backend
default_bolt_type: simple-construction-bolt
created: 2026-06-05T09:30:00Z
updated: 2026-06-05T09:30:00Z
---

# Unit Brief: Layering Foundation

## Purpose

Establish the Presentation / Application / Domain / Infrastructure structure inside the single `PhotoPrint.API` assembly, preceded by the ADR that records why folders-not-projects. Folds in the first-pass Domain extraction (P16) and Services feature-folders (P06). Pure refactor — `simple-construction-bolt`.

## Scope

### In Scope
- ADR "no four-project split" (P22).
- `Domain/` (pure functions + POCO entities), `Infrastructure/` (EF/HttpClient/SDKs), `Web/` (controllers/hubs/middleware/filters/auth), `Application/<Feature>/Services/` (promoted from flat `Services/`).
- Layer-rule doc/checklist.

### Out of Scope
- `Abstractions/` subfolders (unit 002) and handlers (unit 003).
- Any behaviour change or schema change.

---

## Assigned Requirements

| FR | Requirement | Priority |
|----|-------------|----------|
| FR-1 (P22) | No-clean-arch-split ADR | Could |
| FR-2 (P21) | Four-layer folder structure | Should |
| FR-3 (P06, folded) | Services feature folders → Application/<Feature>/Services | Should |
| FR-4 (P16, folded) | Domain layer extraction | Could |

---

## Domain Concepts

### Key Operations
| Operation | Description | Inputs | Outputs |
|-----------|-------------|--------|---------|
| Move + rename namespace | Relocate types into layer folders | current files | layered tree, updated usings |
| Verify no drift | Confirm refactor changes nothing | Add-Migration | empty up/down |

---

## Story Summary

| Metric | Count |
|--------|-------|
| Total Stories | 5 |
| Must Have | 0 |
| Should Have | 3 |
| Could Have | 2 |

### Stories

| Story ID | Title | Priority | Status |
|----------|-------|----------|--------|
| 001-no-split-adr | No four-project split ADR | Could | Planned |
| 002-domain-layer-extraction | Domain/ layer (P16) | Could | Planned |
| 003-infrastructure-layer | Infrastructure/ layer | Should | Planned |
| 004-web-layer | Web/ presentation layer | Should | Planned |
| 005-application-feature-promotion | Application/<Feature>/ (P06) | Should | Planned |

---

## Dependencies

### Depends On
| Unit | Reason |
|------|--------|
| None | — |

### Depended By
| Unit | Reason |
|------|--------|
| 002-conventions-and-policy | Abstractions added to the new layout |
| 003-handler-pattern | Handlers land in Application/<Feature>/Handlers |

### External Dependencies
| System | Purpose | Risk |
|--------|---------|------|
| EF Core migration tool | Zero-drift verification | Medium |
| Roslyn BannedApiAnalyzers | Domain-no-EF rule | Low |

---

## Technical Context

### Suggested Technology
.NET 8 folder+namespace refactor; `BannedApiAnalyzers`; per-PR find/replace scripts.

---

## Constraints

- NO new csproj. Each PR builds + tests green and produces zero migration drift.
- Sequence: PR1 ADR → PR2 Domain → PR3 Infrastructure → PR4 Web → PR5 Application.
- Lockstep with intent 028.

---

## Success Criteria

### Functional
- [ ] Four layers exist by folder + namespace; four controllers no longer inject `PhotoPrintDbContext`.
- [ ] Layer rules codified (doc/checklist + analyzer where chosen).

### Non-Functional
- [ ] Zero behaviour change; zero migration drift after every PR.

### Quality
- [ ] CI green after each PR; mechanical `using static` find/replace verified.

---

## Bolt Suggestions

| Bolt | Type | Stories | Objective |
|------|------|---------|-----------|
| 059-layering-foundation | simple | 001–005 | ADR + four layering PRs |

---

## Notes

Highest churn unit (~200 files). Merge-conflict risk is the dominant cost — schedule a quiet window. Pre-write namespace scripts.
