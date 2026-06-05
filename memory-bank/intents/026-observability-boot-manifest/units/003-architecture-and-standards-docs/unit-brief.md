---
unit: 003-architecture-and-standards-docs
intent: 026-observability-boot-manifest
phase: inception
status: draft
unit_type: backend
default_bolt_type: simple-construction-bolt
created: 2026-06-05T09:30:00Z
updated: 2026-06-05T09:30:00Z
---

# Unit Brief: Architecture & Standards Docs

## Purpose

Consolidate the scattered multi-replica reasoning into one doc (P12) and make the standards docs stop lying about the stack, with a known-failures register and a quarterly audit ritual (P19). Documentation unit — `simple-construction-bolt`.

## Scope

### In Scope
- `docs/architecture/multi-replica-readiness.md`; refreshed `tech-stack.md`; `docs/KNOWN_FAILURES.md`; `docs/ARCHITECTURE_AUDIT_CHECKLIST.md`.

### Out of Scope
- Implementing the Redis backplane (bolt 046 deprioritized — doc only).
- Fixing the 7 failing tests (this unit documents them; fixes are separate).

---

## Assigned Requirements

| FR | Requirement | Priority |
|----|-------------|----------|
| FR-5 (P12) | Multi-replica-readiness consolidation doc | Could |
| FR-6 (P19) | Refresh standards + KNOWN_FAILURES + audit checklist | Must |

---

## Domain Concepts

### Key Operations
| Operation | Description | Inputs | Outputs |
|-----------|-------------|--------|---------|
| Consolidate ADRs | Summarise in-process-state reasoning | ADRs 010/013/015/016/023 | one doc |
| Verify stack doc | Match docs to package.json/.csproj | installed deps | corrected tech-stack.md |

---

## Story Summary

| Metric | Count |
|--------|-------|
| Total Stories | 3 |
| Must Have | 2 |
| Should Have | 0 |
| Could Have | 1 |

### Stories

| Story ID | Title | Priority | Status |
|----------|-------|----------|--------|
| 001-multi-replica-readiness-doc | Multi-replica readiness doc | Could | Planned |
| 002-refresh-tech-stack-and-known-failures | Refresh tech-stack + KNOWN_FAILURES | Must | Planned |
| 003-architecture-audit-checklist | Quarterly audit checklist | Must | Planned |

---

## Dependencies

### Depends On
| Unit | Reason |
|------|--------|
| None | Independent (docs) |

### Depended By
| Unit | Reason |
|------|--------|
| None | — |

### External Dependencies
| System | Purpose | Risk |
|--------|---------|------|
| None | — | — |

---

## Technical Context

### Suggested Technology
Markdown under `docs/` and `memory-bank/standards/`; cross-links from `system-architecture.md`.

---

## Constraints

- P12 is documentation only; must not read as a commitment to build Redis.
- Aligns with [[project_bolt_046_deprioritized]].

---

## Success Criteria

### Functional
- [ ] Multi-replica doc covers all 5 concerns, each citing its ADR; linked from `system-architecture.md`.
- [ ] `tech-stack.md` claims match installed deps (Angular 21, Vitest, no phantom libs).
- [ ] `KNOWN_FAILURES.md` lists each of the 7 failing tests with a reason.
- [ ] `ARCHITECTURE_AUDIT_CHECKLIST.md` exists and is referenced.

### Quality
- [ ] No code change; docs reviewed.

---

## Bolt Suggestions

| Bolt | Type | Stories | Objective |
|------|------|---------|-----------|
| 057-architecture-and-standards-docs | simple | 001, 002, 003 | All four docs |

---

## Notes

Pre-launch must-have for P19 (lying docs = onboarding poison). Independent — can run in parallel with units 001/002.
