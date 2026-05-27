---
stage: plan
bolt: 027-auth-scss-shared-extraction
created: 2026-05-25T00:00:00Z
---

## Implementation Plan: 027-auth-scss-shared-extraction

### Objective

Extract all shared auth layout styles from `register-page.scss` into a dedicated
`_auth-forms.scss` global partial. Update all 5 consuming SCSS files to import from
the new partial instead of from a sibling component file. Move the `.spinner` button
animation into the same partial since it is shared across all auth forms.

### Discovery Findings (from Stage 1 audit)

**Files that currently `@use '../register/register-page.scss' as *`**  
The problem is wider than the bolt assumed — 4 files import the sibling, not 1:

- `login-page.scss`
- `forgot-password-page.scss`
- `reset-password-page.scss`
- `verify-email-page.scss`

**`.spinner` usage audit**  
The `.spinner` CSS is NOT a page-level loading state — it is a button-level inline
spinner (`<span class="spinner" aria-hidden="true"></span>`) used inside submit
buttons in 5 templates:

- `register-page.html` (line 106)
- `login-page.html` (line 58)
- `forgot-password-page.html` (line 29)
- `verify-email-page.html` (line 33)
- `reset-password-page.html` (line 65)

➡ **Deviation from bolt spec**: The `.spinner` CSS must be moved INTO
`_auth-forms.scss`, NOT deleted. It cannot be replaced by `<app-spinner>` because
it is an inline submit-button spinner, not a standalone page-level loader.
The `guest-checkout-form.scss` has its own duplicate `.spinner` — left out of scope
for this bolt (addressed in bolt 029 global button styles).

**SCSS includePaths confirmed**  
`angular.json` has `"includePaths": ["src"]`, so `@use 'styles/auth-forms' as *`
resolves correctly from any component SCSS file.

### Deliverables

1. `src/PhotoPrint.UI/src/styles/_auth-forms.scss` — new file containing:
   - `.auth-layout`
   - `.auth-card` (with `&__title`, `&__footer` BEM children)
   - `.auth-form`
   - `.form-field` (with `&--checkbox` modifier)
   - `.input-with-toggle`
   - `.toggle-btn`
   - `.field-error`
   - `.spinner` + `@keyframes spin` (button-level spinner, shared across auth forms)

2. `register-page.scss` — keep only:
   - `@use 'styles/variables' as *`
   - `@use 'styles/auth-forms' as *`
   - `.strength-rules` block (register-page-specific, stays here)

3. `login-page.scss` — replace `@use '../register/register-page.scss' as *` with
   `@use 'styles/auth-forms' as *`

4. `forgot-password-page.scss` — same replacement as login

5. `reset-password-page.scss` — same replacement as login

6. `verify-email-page.scss` — same replacement as login

### Dependencies

- `angular.json` `stylePreprocessorOptions.includePaths: ["src"]` — ✅ already set
- `register-page.scss` must remain compilable (cannot delete styles still used by templates)

### Technical Approach

1. Create `_auth-forms.scss` by copying the shared selectors verbatim from `register-page.scss`
2. Verify `styles.scss` — no need to `@forward` the partial; component SCSS files import
   it directly via `@use`
3. Strip the extracted selectors from `register-page.scss`, leaving only the
   `@use 'styles/variables'` import, a new `@use 'styles/auth-forms'` import, and the
   `.strength-rules` block
4. Update all 4 dependent SCSS files (one-line change each)
5. Build check to confirm no missing-selector errors

### Acceptance Criteria

- [ ] `_auth-forms.scss` contains all 8 extracted selectors/blocks
- [ ] `register-page.scss` no longer defines `.auth-layout`, `.auth-card`, `.auth-form`,
      `.form-field`, `.input-with-toggle`, `.toggle-btn`, `.field-error`, or `.spinner`
- [ ] `login-page.scss` imports from `styles/auth-forms`, not from `../register/register-page.scss`
- [ ] Same for `forgot-password-page.scss`, `reset-password-page.scss`, `verify-email-page.scss`
- [ ] `ng build --configuration development` passes with no SCSS errors
- [ ] Visual spot-check: login and register pages render identically before/after
