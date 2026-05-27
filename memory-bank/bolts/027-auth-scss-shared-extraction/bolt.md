---
id: 027-auth-scss-shared-extraction
unit: 001-auth-scss-refactor
intent: 012-ui-polish
type: simple-construction-bolt
status: completed
priority: P2
stories:
  - 001-extract-auth-shared-styles
  - 002-remove-local-spinner-animation
created: 2026-05-25T00:00:00Z
started: 2026-05-25T00:00:00Z
completed: 2026-05-25T00:00:00Z
current_stage: done
stages_completed:
  - name: plan
    completed: 2026-05-25T00:00:00Z
    artifact: implementation-plan.md
  - name: implement
    completed: 2026-05-25T00:00:00Z
    artifact: implementation-walkthrough.md
  - name: test
    completed: 2026-05-25T00:00:00Z
    artifact: test-walkthrough.md

requires_bolts: [008-authentication-ui]
enables_bolts: []
requires_units: []
blocks: false

complexity:
  avg_complexity: 1
  avg_uncertainty: 1
  max_dependencies: 1
  testing_scope: 1
---

# Bolt: 027-auth-scss-shared-extraction

## Overview

Fix the anti-pattern where `login-page.scss` imports `register-page.scss` directly to share auth layout styles. Extract all shared auth styles into a dedicated `_auth-forms.scss` partial, update both pages to import from there, and remove the local `.spinner` CSS animation from `register-page.scss` (replaced by the shared `<app-spinner>` component).

## Objective

By the end of this bolt:
- `login-page.scss` no longer imports `register-page.scss`
- All shared auth styles live in `src/PhotoPrint.UI/src/styles/_auth-forms.scss`
- Both `login-page.scss` and `register-page.scss` import from the global partial
- The local `.spinner` keyframe animation in `register-page.scss` is deleted
- The register page template uses `<app-spinner>` instead of any local spinner markup

## Stories Included

- **001-extract-auth-shared-styles**: Create `_auth-forms.scss` with `.auth-layout`, `.auth-card`, `.auth-form`, `.form-field`, `.input-with-toggle`, `.toggle-btn`, `.field-error`; update both page SCSS files to `@use 'styles/auth-forms' as *` (Must)
- **002-remove-local-spinner-animation**: Remove the local `.spinner` CSS keyframe from `register-page.scss`; confirm `RegisterPageComponent` already uses `<app-spinner>` (Must)

## Bolt Type

`simple-construction-bolt` — pure SCSS extraction and reorganisation; no new Angular components, no backend changes.

## Stage Plan

| Stage | Name | Output |
|-------|------|--------|
| 1 | Plan | `implementation-plan.md` — list every selector to move, identify all `@use` paths, confirm no other files import `register-page.scss` |
| 2 | Implement | Create `_auth-forms.scss`; strip selectors from `register-page.scss`; remove `@use` import from `login-page.scss`; add `@use 'styles/auth-forms' as *` to both pages; remove local spinner CSS |
| 3 | Test | Build check (`ng build --configuration development`); visual spot-check login and register pages; no regressions in `PhotoPrint.Tests` |

## Dependencies

- **Requires**: bolt `008-authentication-ui` (login and register pages must exist — ✅ complete)
- **Enables**: nothing (cleanup bolt)

## Files Affected

```
src/PhotoPrint.UI/src/styles/_auth-forms.scss          ← NEW
src/PhotoPrint.UI/src/styles/styles.scss                ← add @forward '_auth-forms' if needed
src/PhotoPrint.UI/src/app/features/auth/pages/login/login-page.scss
src/PhotoPrint.UI/src/app/features/auth/pages/register/register-page.scss
```

## Key Technical Notes

### Selectors to extract into `_auth-forms.scss`

- `.auth-layout` — outer flex column centering wrapper
- `.auth-card` — white card container with shadow
- `.auth-form` — form element inside the card
- `.form-field` — labelled input wrapper
- `.input-with-toggle` — relative-position wrapper for password inputs
- `.toggle-btn` — absolute-positioned show/hide password button
- `.field-error` — red error message under a field

### SCSS import path

Angular resolves `@use 'styles/auth-forms'` relative to `src/` when `stylePreprocessorOptions.includePaths` contains `src/` in `angular.json`. Verify the `includePaths` setting before implementation.

### Spinner removal

The local `.spinner` class in `register-page.scss` animates a border-based circle. The shared `SpinnerComponent` at `src/app/shared/components/spinner/spinner.component.ts` already encapsulates this. Confirm the register template uses `<app-spinner>` (or add it if not yet done) and delete the local CSS.
