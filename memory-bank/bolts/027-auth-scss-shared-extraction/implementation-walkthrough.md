---
stage: implement
bolt: 027-auth-scss-shared-extraction
created: 2026-05-25T00:00:00Z
---

## Implementation Walkthrough: 027-auth-scss-shared-extraction

### Summary

Created a new `_auth-forms.scss` global partial containing all shared auth layout and form styles. Stripped those styles from `register-page.scss` (which was previously the de-facto source of truth) and updated all 5 auth page SCSS files to import from the new partial. Build passes with zero new errors.

### Structure Overview

Auth page SCSS files now follow a clean two-import pattern: project variables first, then shared auth forms. Each page file only contains its own page-specific styles.

### Completed Work

- [x] `src/PhotoPrint.UI/src/styles/_auth-forms.scss` — new file; contains `.auth-layout`, `.auth-card` (BEM children), `.auth-form`, `.form-field` (checkbox modifier), `.input-with-toggle`, `.toggle-btn`, `.field-error`, `.spinner` button animation, and `@keyframes spin`
- [x] `src/app/features/auth/pages/register/register-page.scss` — now only has the two `@use` declarations and the `.strength-rules` register-specific block
- [x] `src/app/features/auth/pages/login/login-page.scss` — sibling import swapped to `@use 'styles/auth-forms' as *`
- [x] `src/app/features/auth/pages/forgot-password/forgot-password-page.scss` — sibling import swapped
- [x] `src/app/features/auth/pages/reset-password/reset-password-page.scss` — sibling import swapped
- [x] `src/app/features/auth/pages/verify-email/verify-email-page.scss` — sibling import swapped

### Key Decisions

- **`.spinner` moved to `_auth-forms.scss`, not deleted**: The bolt spec assumed the spinner was unused, but it is a button-level inline spinner (`<span class="spinner">`) used inside submit buttons across all 5 auth form templates. It cannot be replaced by `<app-spinner>` (which is a page-level loading indicator). It belongs in the shared partial.
- **No `@forward` in `styles.scss`**: The partial is imported directly by component SCSS files via `@use 'styles/auth-forms'`. Angular's `includePaths: ["src"]` makes this resolve correctly without touching the global stylesheet.
- **Scope is 5 files, not 1**: Discovery found 4 files importing from `register-page.scss` (login, forgot-password, reset-password, verify-email), not just login as the bolt spec stated.

### Deviations from Plan

- `.spinner` and `@keyframes spin` moved to `_auth-forms.scss` rather than deleted — see Key Decisions above.
- 4 files updated instead of the 1 mentioned in the bolt description (all were identified during Stage 1 planning).

### Dependencies Added

None.

### Developer Notes

The pre-existing SCSS deprecation warning in the build output is unrelated to this bolt (it concerns `color-mix()` calls in other files). The `@use` module system used here is the correct modern approach — avoid reverting to `@import`.
