---
unit: 002-refund-return-flow-ui
intent: 031-refund-return-flow
phase: inception
status: draft
unit_type: frontend
default_bolt_type: simple-construction-bolt
created: 2026-06-05T09:30:00Z
updated: 2026-06-05T09:30:00Z
---

# Unit Brief: Refund UI (Admin)

## Purpose

Give admins a refund action on the order-detail view (full + partial, with reason), backed by the unit-001 endpoint.

## Scope

### In Scope
- Refund button + modal (amount optional, reason required) on the admin order-detail page; show refunded state.

### Out of Scope
- Backend refund logic (unit 001); customer-facing refund initiation (admin-only).

---

## Assigned Requirements

| FR | Requirement | Priority |
|----|-------------|----------|
| FR-4 (P09, UI) | Admin refund action on order detail | Must |

---

## Domain Concepts

### Key Operations
| Operation | Description | Inputs | Outputs |
|-----------|-------------|--------|---------|
| Submit refund | Call admin endpoint | amount?, reason | updated order + refund result |

---

## Story Summary

| Metric | Count |
|--------|-------|
| Total Stories | 1 |
| Must Have | 1 |
| Should Have | 0 |
| Could Have | 0 |

### Stories

| Story ID | Title | Priority | Status |
|----------|-------|----------|--------|
| 001-admin-refund-action | Admin refund action + modal | Must | Planned |

---

## Dependencies

### Depends On
| Unit | Reason |
|------|--------|
| 001-refund-domain-and-api | Consumes the refund endpoint |

### Depended By
| Unit | Reason |
|------|--------|
| None | — |

### External Dependencies
| System | Purpose | Risk |
|--------|---------|------|
| PhotoPrint.API | Refund endpoint | Low |

---

## Technical Context

### Suggested Technology
Angular 21 admin order-detail page; reuse `BaseApiService` (intent 030 P26) if available; Romanian copy for refund states.

---

## Constraints

- Admin-only; show clear confirmation for an irreversible action.

---

## Success Criteria

### Functional
- [ ] Refund action present on order detail; supports full + partial with required reason.
- [ ] Refunded state + amount surfaced; error codes mapped to Romanian copy.

### Non-Functional
- [ ] Within bundle budget (intent 030 P18).

### Quality
- [ ] Vitest spec; covered by an admin e2e path where feasible.

---

## Bolt Suggestions

| Bolt | Type | Stories | Objective |
|------|------|---------|-----------|
| 069-refund-return-flow-ui | simple | 001 | Admin refund action |

---

## Notes

After unit 001 endpoint exists.
