# US-701 — User Profile Page (Frontend)

## Story
**As a** logged-in customer  
**I want to** view and update my personal information

## Type
FRONTEND — Angular

## Epic
EPIC-7 | Cont Utilizator & Legal

## Dependencies
- US-702 (Account API backend)
- US-804 (AuthGuard)

## Acceptance Criteria

1. **`/contul-meu/profil`**: editable First Name, Last Name, Phone; Email shown read-only
2. **Change password section**: current password + new password (same rules as registration)
3. **Social connections section**: shows linked Google account; `Conectează Google` if not linked
4. **`Șterge contul`** link opens confirmation dialog (type `ȘTERGE` to confirm)

## Technical Notes

### Component Location
`src/app/features/account/profile/profile.component.ts`

### Implementation Details
- Load profile: `GET /api/account` on init
- Edit form: Reactive Forms; First Name, Last Name, Phone editable; Email read-only
- Save: `PATCH /api/account` with changed fields
- Change password: separate form section; current password + new password + confirm
  - Call `POST /api/account/change-password`
  - Same validation rules as registration (8+ chars, uppercase, digit, special)
- Google connection: show linked status; if not linked, show `Conectează Google` button (triggers Google OAuth flow from US-106)
- Delete account: confirmation dialog requiring user to type `ȘTERGE`; call `DELETE /api/account`; logout + redirect to home with toast `Contul va fi șters în 30 de zile`

## Files to Create/Modify
- `src/app/features/account/profile/profile.component.ts`
- `src/app/features/account/profile/profile.component.html`
- `src/app/features/account/profile/profile.component.scss`
- `src/app/features/account/delete-account-dialog/delete-account-dialog.component.ts`

## Testing
- Unit test: profile form loads and displays data
- Unit test: profile update saves changes
- Unit test: change password validation
- Unit test: delete account confirmation flow
- E2E: update profile flow
