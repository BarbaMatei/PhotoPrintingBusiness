---
id: 001-base-api-service
unit: 002-ui-scaling-and-e2e-ui
intent: 030-ui-scaling-and-e2e
status: draft
priority: should
created: 2026-06-05T09:30:00Z
assigned_bolt: 067-ui-scaling-and-e2e-ui
implemented: false
---

# Story: 001-base-api-service

## User Story

**As a** frontend developer
**I want** a shared `BaseApiService`
**So that** the 14 services stop hand-rolling `withCredentials`, error translation, and idempotency-key threading

## Acceptance Criteria

- [ ] **Given** `core/services/api/base-api.service.ts`, **When** created, **Then** it exposes typed `get/post/put/delete` with `withCredentials: true`, `catchError` translation, and optional `Idempotency-Key`
- [ ] **Given** the base, **When** services migrate (start with `order.service.ts`), **Then** they route through it
- [ ] **Given** each migration, **When** Vitest runs, **Then** it passes

## Technical Notes

- Migrate one service at a time; keep an escape hatch for non-standard calls.

## Dependencies

### Requires
- 030/001 (CI gates verify migrations)

### Enables
- 002-home-page-breakup, 003-account-pages-breakup, 004-delivery-step-locker-selector

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Bespoke call shape | Use the escape hatch; document |

## Out of Scope

- Page breakups (later stories).
