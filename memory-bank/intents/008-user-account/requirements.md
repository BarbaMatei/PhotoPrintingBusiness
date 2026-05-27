---
intent: 008-user-account
phase: inception
status: complete
created: 2026-05-22T12:00:00Z
updated: 2026-05-22T12:00:00Z
---

# Requirements: User Account

## Intent Overview

Implement the customer account self-service area: a backend Account API for profile management and account lifecycle, an Angular Profile page, and a Saved Addresses page. Legal static pages are already implemented (bolt 004-angular-app-shell). Cookie consent banner is included here as it's the last missing FE-only piece in the account/legal scope.

## Business Goals

| Goal | Success Metric | Priority |
|------|----------------|----------|
| Customers can update their personal details | Profile PATCH works without re-login | Must |
| Customers can change their password securely | Change password invalidates all other sessions | Must |
| Customers can save delivery addresses for reuse | Saved address pre-selected at checkout | Must |
| Customers can delete their account | GDPR-compliant soft-delete with 30-day grace period | Must |

---

## Functional Requirements

### FR-1: Account API
- **Description**: Authenticated endpoints for reading/updating profile, changing password, requesting account deletion.
- **Acceptance Criteria**: `GET /api/account` → `{firstName, lastName, email, phone, hasPassword, linkedProviders[]}`; `PATCH /api/account` updates name/phone; `POST /api/account/change-password` requires currentPassword, invalidates all refresh tokens; `DELETE /api/account` sets `DeletionRequestedAt` (processed by AccountDeletionJob in bolt 025).
- **Priority**: Must
- **Related Stories**: US-702

### FR-2: Saved Addresses API
- **Description**: CRUD for saved delivery addresses, max 5 per user.
- **Acceptance Criteria**: `GET /api/account/addresses`; `POST /api/account/addresses`; `PUT /api/account/addresses/{id}`; `DELETE /api/account/addresses/{id}`; `PATCH /api/account/addresses/{id}/default`; max 5 addresses enforced.
- **Priority**: Must
- **Related Stories**: US-703

### FR-3: User Profile Page (Frontend)
- **Description**: Angular page at `/contul-meu/profil`. Editable first/last name and phone; read-only email; change-password section; social connections section; account deletion link.
- **Priority**: Must
- **Related Stories**: US-701

### FR-4: Saved Addresses Page (Frontend)
- **Description**: Angular page at `/contul-meu/adrese`. List with Edit/Delete/Set Default. Add address form (same fields as checkout home delivery). Max 5.
- **Priority**: Must
- **Related Stories**: US-703

### FR-5: Cookie Consent Banner (Frontend)
- **Description**: First-visit banner with "Acceptă toate" / "Doar esențiale"; consent stored in localStorage.
- **Priority**: Must
- **Related Stories**: US-704

---

## Non-Functional Requirements

| Category | Requirement |
|----------|-------------|
| Security | Change-password invalidates all refresh tokens; account deletion is soft (30-day grace) |
| Validation | FluentValidation on all PATCH/POST; Angular Reactive Forms client-side |
| Max addresses | 5 per user — enforced server-side (400 if exceeded) |

---

## Out of Scope

- 2FA / TOTP authentication
- Export personal data (GDPR SAR)
- Email preference management
- Order management from account (handled by bolt 019)
