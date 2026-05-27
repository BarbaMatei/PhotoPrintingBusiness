---
unit: 002-account-ui
intent: 008-user-account
phase: inception
status: ready
created: 2026-05-22T12:00:00Z
updated: 2026-05-22T12:00:00Z
default_bolt_type: simple-construction-bolt
---

# Unit Brief: 002-account-ui

## Purpose

Build the customer account pages: Profile (edit name/phone, change password, account deletion) and Saved Addresses (CRUD list). Also add the cookie consent banner (last remaining US-704 piece).

## Scope

### In Scope
- `/contul-meu/profil` — Profile page: edit name/phone; change password form; social connections (read-only list); delete account dialog (type 'ȘTERGE' to confirm)
- `/contul-meu/adrese` — Saved Addresses page: list with Edit/Delete/Set Default; add/edit form; max 5 indicator
- Cookie consent banner (app-root level): first-visit; "Acceptă toate" / "Doar esențiale"; localStorage
- `AccountService` — HTTP client for all `/api/account/*` calls
- Route registration under `/contul-meu` with `authGuard`

### Out of Scope
- Google account linking/unlinking UI
- Order history (bolt 019-orders-ui — complete)
- Legal static pages (already implemented)

---

## Domain Concepts

### Key Entities
| Entity | Description |
|--------|-------------|
| `AccountDto` | `{firstName, lastName, email, phone, hasPassword, linkedProviders[]}` |
| `SavedAddressDto` | `{id, recipientName, street, number, block?, city, county, postalCode, phone, isDefault}` |

## Story Summary

| # | Story | Priority |
|---|-------|----------|
| 001 | `001-profile-page` | Must |
| 002 | `002-saved-addresses-page` | Must |
| 003 | `003-cookie-consent-banner` | Must |
