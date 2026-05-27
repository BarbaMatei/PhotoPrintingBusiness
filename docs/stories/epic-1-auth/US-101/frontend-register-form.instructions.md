# US-101 — Register — Email & Password (Frontend)

## Story
**As a** new visitor  
**I want to** create a personal account with email and password  
**So that** I can track my orders later

## Type
FRONTEND — Angular

## Epic
EPIC-1 | Autentificare & Conturi

## Dependencies
- US-102 (Backend register endpoint must exist)
- US-804 (Angular App Shell & Routing must be scaffolded)

## Acceptance Criteria

1. **Form fields**: First Name, Last Name, Email, Password, Confirm Password, Phone (optional)
2. **Real-time password validation**: min 8 chars, 1 uppercase, 1 digit, 1 special character — show validation indicators as user types
3. **GDPR consent checkbox** (mandatory) with link to Privacy Policy (`/politica-de-confidentialitate`) — submit button disabled until checked
4. **On success**: show email-verification-pending screen; do NOT auto-login until email confirmed
5. **Duplicate email**: inline field error `Adresa de email este deja folosită`
6. **All labels and messages in Romanian**

## Technical Notes

### Component Location
`src/app/features/auth/register/register.component.ts`

### Implementation Details
- Use Angular Reactive Forms with validators
- Password strength indicator (visual bar or checklist)
- Confirm Password must match Password (cross-field validator)
- Phone field: optional, Romanian format `07xxxxxxxx` if provided
- Call `POST /api/auth/register` via `AuthService`
- Handle 201 (success → navigate to verification-pending page)
- Handle 409 (duplicate email → set field error)
- Handle 422 (validation errors → map to form fields)
- Spinner + disabled submit during in-flight request

### UI/UX
- Responsive layout (mobile-first)
- Link to login page: `Ai deja cont? Conectează-te`
- All error messages in Romanian
- Password visibility toggle (show/hide)

## Files to Create/Modify
- `src/app/features/auth/register/register.component.ts`
- `src/app/features/auth/register/register.component.html`
- `src/app/features/auth/register/register.component.scss`
- `src/app/features/auth/auth-routing.module.ts` (add route)
- `src/app/core/auth/auth.service.ts` (add `register()` method)

## Testing
- Unit test: form validation (all field rules)
- Unit test: password strength validation
- Unit test: GDPR checkbox disables/enables submit
- Unit test: duplicate email error display
- E2E: full registration flow happy path
