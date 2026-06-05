---
id: 032-password-requirements-profile
unit: 004-responsive-ux-fixes
intent: 012-ui-polish
type: simple-construction-bolt
status: complete
priority: P3
stories:
  - 001-extract-password-checklist-component
  - 002-wire-checklist-profile-page
created: 2026-05-25T00:00:00Z
started: 2026-05-25T00:00:00Z
completed: 2026-05-25T00:00:00Z
current_stage: null
stages_completed: [plan, implement, test]

requires_bolts: [008-authentication-ui, 024-account-ui]
enables_bolts: []
requires_units: []
blocks: false

complexity:
  avg_complexity: 2
  avg_uncertainty: 1
  max_dependencies: 2
  testing_scope: 2
---

# Bolt: 032-password-requirements-profile

## Overview

The register page shows an always-visible password requirements checklist with live colour feedback. The profile/account "change password" page only shows errors when the field is touched, with no checklist — inconsistent UX. This bolt standardises both pages to use the same always-visible checklist pattern by extracting it into a shared `PasswordChecklistComponent`.

## Objective

By the end of this bolt both the register page and the change-password section of the profile page show the same always-visible password requirements checklist with live rule-pass/rule-fail colour states, driven by a shared `PasswordChecklistComponent`.

## Stories Included

- **001-extract-password-checklist-component**: Extract the password requirements checklist from `register-page` into a new standalone `PasswordChecklistComponent`; accepts `@Input() password: string`; emits `@Output() allValid: EventEmitter<boolean>`; replaces the inline checklist in `register-page` (Must)
- **002-wire-checklist-profile-page**: Add `<app-password-checklist [password]="newPasswordValue">` to the change-password form in the profile/account page; remove the existing touch-only error display for the password strength rules; update component imports (Must)

## Bolt Type

`simple-construction-bolt` — extract an existing inline pattern into a shared component, then wire it into a second page.

## Stage Plan

| Stage | Name | Output |
|-------|------|--------|
| 1 | Plan | `implementation-plan.md` — document the exact rules checked on the register page; define `PasswordChecklistComponent` API; map the profile page's change-password form structure |
| 2 | Implement | `password-checklist.component.ts`; patch `register-page` to use it; patch profile change-password section |
| 3 | Test | Spec for `PasswordChecklistComponent` (all rules pass/fail states); updated spec for `RegisterPage`; updated spec for profile change-password |

## Dependencies

- **Requires**: bolt `008-authentication-ui` (register page must exist with existing checklist — ✅ complete)
- **Requires**: bolt `024-account-ui` (profile/account page with change-password form must exist)
- **Enables**: nothing

## Files Affected

```
src/PhotoPrint.UI/src/app/shared/components/password-checklist/password-checklist.component.ts   ← NEW
src/PhotoPrint.UI/src/app/shared/components/password-checklist/password-checklist.component.html ← NEW
src/PhotoPrint.UI/src/app/shared/components/password-checklist/password-checklist.component.scss ← NEW
src/PhotoPrint.UI/src/app/features/auth/pages/register/register-page.ts
src/PhotoPrint.UI/src/app/features/auth/pages/register/register-page.html
src/PhotoPrint.UI/src/app/features/account/pages/profile/profile-page.ts          (or equivalent)
src/PhotoPrint.UI/src/app/features/account/pages/profile/profile-page.html
```

## Key Technical Notes

### Password rules to implement (based on register page)

| Rule | Check |
|------|-------|
| At least 8 characters | `password.length >= 8` |
| At least one uppercase letter | `/[A-Z]/.test(password)` |
| At least one lowercase letter | `/[a-z]/.test(password)` |
| At least one digit | `/\d/.test(password)` |
| At least one special character | `/[^A-Za-z0-9]/.test(password)` |

### PasswordChecklistComponent API

```typescript
@Component({
  selector: 'app-password-checklist',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PasswordChecklistComponent {
  @Input() password: string = '';
  @Output() allValid = new EventEmitter<boolean>();
}
```

### CSS class pattern (already established in register page)

```scss
.rule { color: $color-text-muted; }          // neutral default
.rule.rule--pass { color: $color-success; }  // ✓ green
.rule.rule--err { color: $color-danger; }    // ✗ red (only after first interaction)
```

The checklist should always be visible (not toggled by `touched`). Show neutral colour on initial empty state; switch to pass/fail as the user types.
