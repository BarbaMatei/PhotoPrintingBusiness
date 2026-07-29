---
id: 004-retry-and-promote-handlers
unit: 003-handler-pattern
intent: 027-architectural-layering
status: draft
priority: should
created: 2026-06-05T09:30:00Z
assigned_bolt: 061-handler-pattern
implemented: false
---

# Story: 004-retry-and-promote-handlers

## User Story

**As a** developer
**I want** the admin invoice-retry and photo-promotion use cases extracted into handlers
**So that** the remaining multi-step flows are discoverable and tested in isolation

## Acceptance Criteria

- [ ] **Given** the admin-retry CAS logic in `AdminInvoicesController.RetryAsync`, **When** extracted, **Then** a `RetryInvoiceUploadCommand` + handler owns it and the controller delegates
- [ ] **Given** the cloud-promotion sequence in `OrderPhotoPromoter`, **When** extracted, **Then** a `PromoteOrderPhotosCommand` + handler owns it
- [ ] **Given** both handlers, **When** tested, **Then** each has its own test file
- [ ] **Given** `find Application -name '*Handler.cs'`, **When** run, **Then** it lists the full use-case inventory; CI green

## Technical Notes

- Stop here — do not convert CRUD endpoints (bar: 3+ concerns or 50+ LOC).

## Dependencies

### Requires
- 001-command-handler-abstractions

### Enables
- None

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Retry on a non-retryable invoice state | Same guard behaviour as before |

## Out of Scope

- Further handler proliferation.
