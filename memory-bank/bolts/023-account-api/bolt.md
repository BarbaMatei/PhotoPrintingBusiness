---
id: 023-account-api
unit: 001-account-api
intent: 008-user-account
type: ddd-construction-bolt
status: complete
stories:
  - 001-account-profile-api
  - 002-saved-addresses-api
created: 2026-05-22T12:00:00Z
started: 2026-05-23T07:00:00Z
completed: 2026-05-23T09:00:00Z
current_stage: null
stages_completed: [1, 2, 3, 4]

requires_bolts: [005-auth-core]
enables_bolts: [024-account-ui, 025-background-jobs]
requires_units: []
blocks: false

complexity:
  avg_complexity: 2
  avg_uncertainty: 1
  max_dependencies: 1
  testing_scope: 2
---

# Bolt: 023-account-api

## Overview

Implement the Account API: profile read/update, password change (with token revocation), account deletion request, and saved addresses CRUD (max 5 per user).

## Objective

By the end of this bolt authenticated customers can manage all aspects of their account via the API, and the `SavedAddress` entity is persisted and retrievable.

## Stories Included

- **001-account-profile-api**: `GET/PATCH /api/account`, `POST /api/account/change-password`, `DELETE /api/account` (Must)
- **002-saved-addresses-api**: Full CRUD `/api/account/addresses` with max-5 enforcement and default flag (Must)

## Bolt Type

`ddd-construction-bolt` — backend with new `SavedAddress` entity, EF migration, validation, and token revocation.

## Stage Plan

| Stage | Name | Output |
|-------|------|--------|
| 1 | Domain Model | `ddd-01-domain-model.md` — SavedAddress entity, profile fields, business rules |
| 2 | Technical Design | `ddd-02-technical-design.md` — controllers, service, EF migration, FluentValidation |
| 3 | Implement | Code: AccountController, AccountAddressesController, IAccountService, SavedAddress entity + migration |
| 4 | Test | `ddd-03-test-report.md` — integration tests |

## Dependencies

- **Requires**: bolt `005-auth-core` (User entity, RefreshTokens table, UserManager — ✅ complete)
- **Enables**: bolt `024-account-ui`, bolt `025-background-jobs` (reads `DeletionRequestedAt`)
