---
id: 024-account-ui
unit: 002-account-ui
intent: 008-user-account
type: simple-construction-bolt
status: complete
stories:
  - 001-profile-page
  - 002-saved-addresses-page
  - 003-cookie-consent-banner
created: 2026-05-22T12:00:00Z
started: 2026-05-23T09:00:00Z
completed: 2026-05-23T11:15:00Z
current_stage: null
stages_completed: [1, 2, 3]

requires_bolts: [023-account-api]
enables_bolts: []
requires_units: []
blocks: false

complexity:
  avg_complexity: 2
  avg_uncertainty: 1
  max_dependencies: 1
  testing_scope: 2
---

# Bolt: 024-account-ui

## Overview

Build the Angular account management UI: Profile page, Saved Addresses page, and the global cookie consent banner.

## Objective

By the end of this bolt customers can edit their profile, manage saved delivery addresses, and the cookie consent banner appears on first visit.

## Stories Included

- **001-profile-page**: `/contul-meu/profil` — edit name/phone, change password, delete account (Must)
- **002-saved-addresses-page**: `/contul-meu/adrese` — address list with CRUD (Must)
- **003-cookie-consent-banner**: App-root level first-visit banner (Must)

## Bolt Type

`simple-construction-bolt` — Angular feature with 2 pages, a service, and a root-level component.

## Stage Plan

| Stage | Name | Output |
|-------|------|--------|
| 1 | Plan | `implementation-plan.md` — component tree, AccountService API, localStorage strategy for cookie consent |
| 2 | Implement | Source code: AccountService, ProfilePage, SavedAddressesPage, CookieConsentBanner |
| 3 | Test | Spec files for service and all 3 components |

## Dependencies

- **Requires**: bolt `023-account-api` (all `/api/account/*` endpoints must exist)
- **Enables**: nothing (final phase 7 deliverable)

## Key Technical Notes

- Cookie consent: `localStorage.getItem('cookie-consent')` → `'all'` | `'essential'` | null
- Cookie consent banner: `app-cookie-consent` standalone component added to `app.html` (not lazy-loaded)
- All Angular 21 conventions: `@if`/`@for`, `signal()`, `OnPush`, `vi.fn()` in tests
