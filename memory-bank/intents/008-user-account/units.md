---
intent: 008-user-account
phase: inception
status: complete
created: 2026-05-22T12:00:00Z
---

# Units: User Account

## Decomposition

| Unit | Type | Stories | Default Bolt Type |
|------|------|---------|-------------------|
| 001-account-api | backend | US-702, US-703 (addresses API) | ddd-construction-bolt |
| 002-account-ui | frontend | US-701, US-703 (UI), US-704 (cookie banner) | simple-construction-bolt |

## Rationale

The account API covers the `User` and `SavedAddress` aggregates — profile PATCH, password change, deletion, and address CRUD. These are backend concerns that share FluentValidation and the existing `PhotoPrintDbContext`. The account UI covers the Profile page, Saved Addresses page, and the cookie consent banner (last outstanding FE piece from US-704).
