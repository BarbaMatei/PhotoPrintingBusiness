---
id: 003-account-pages-breakup
unit: 002-ui-scaling-and-e2e-ui
intent: 030-ui-scaling-and-e2e
status: draft
priority: should
created: 2026-06-05T09:30:00Z
assigned_bolt: 067-ui-scaling-and-e2e-ui
implemented: false
---

# Story: 003-account-pages-breakup

## User Story

**As a** frontend developer
**I want** the saved-addresses and profile pages split into container + form components
**So that** these 470–498 LOC pages stop mixing form state, validation, and API calls inline

## Acceptance Criteria

- [ ] **Given** `saved-addresses-page.ts` (498 LOC), **When** split, **Then** it becomes a smart container + `address-form` (dumb) + `address-list-item` (dumb)
- [ ] **Given** `profile-page.ts` (473 LOC), **When** split, **Then** it becomes a container + `personal-info-form` + `email-change-form` + `password-change-form`
- [ ] **Given** the breakups, **When** Vitest runs, **Then** form flows still pass

## Technical Notes

- Profile already calls all three flows — the split mirrors existing logic.

## Dependencies

### Requires
- 001-base-api-service

### Enables
- None

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Shared validation between forms | Extract a shared validator util |

## Out of Scope

- home / delivery-step (other stories).
