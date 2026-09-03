---
unit: 001-access-hardening
intent: 029-decomposition-and-hardening
phase: inception
status: draft
unit_type: backend
default_bolt_type: simple-construction-bolt
created: 2026-06-05T09:30:00Z
updated: 2026-06-05T09:30:00Z
---

# Unit Brief: Access Hardening

## Purpose

Two independent hardening moves: a global per-IP rate limit on the non-auth API surface, and a centralised `Policies.Admin` constant to kill the string-literal `[Authorize(Roles="Admin")]` footgun.

## Scope

### In Scope
- Global fallback rate limiter (~200 req/min/IP sliding) that auth-specific policies override.
- `Policies.Admin` constant + `AddAuthorization` registration; 6 controllers migrated.

### Out of Scope
- The decompositions (units 002/003).
- ForwardedHeaders itself (intent 025 P05 — a dependency).

---

## Assigned Requirements

| FR | Requirement | Priority |
|----|-------------|----------|
| FR-1 (P08) | Global rate limit + per-endpoint admin role policy constant | Should |

---

## Domain Concepts

### Key Operations
| Operation | Description | Inputs | Outputs |
|-----------|-------------|--------|---------|
| Global limiter | Partition by client IP | request | 429 when over limit |
| Admin policy | Centralised role check | role claim | allow/deny |

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
| 001-global-rate-limit | Global per-IP rate limit | Should | Planned |
| 002-admin-policy-constant | Policies.Admin constant | Should | Planned |

---

## Dependencies

### Depends On
| Unit | Reason |
|------|--------|
| 025/001-dependency-and-boot-hardening | Global limiter keys on the real client IP (P05) |

### Depended By
| Unit | Reason |
|------|--------|
| None | — |

### External Dependencies
| System | Purpose | Risk |
|--------|---------|------|
| Caddy / forwarded headers | Real client IP | Medium |

---

## Technical Context

### Suggested Technology
`AddRateLimiter` + `PartitionedRateLimiter`; `AddAuthorization` policy.

---

## Constraints

- Tune limit during pre-launch load test (admin uploading 30 photos in 10s must not be throttled).

---

## Success Criteria

### Functional
- [ ] Global limiter active; auth policies still stricter.
- [ ] No `Roles="Admin"` literal remains; anonymous → 401 (not 403); over-limit → 429.

### Non-Functional
- [ ] Legitimate bursts not throttled.

### Quality
- [ ] Integration tests for 401/429.

---

## Bolt Suggestions

| Bolt | Type | Stories | Objective |
|------|------|---------|-----------|
| 063-access-hardening | simple | 001, 002 | Rate limit + admin policy |

---

## Notes

Soft pre-launch must-have. Ships first within this intent (independent of the decompositions).
