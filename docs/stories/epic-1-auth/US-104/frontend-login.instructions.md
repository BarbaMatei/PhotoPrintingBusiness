# US-104 — Login — Email & Password (Frontend)

## Story
**As a** registered user  
**I want to** sign in with email and password to access my account and order history

## Type
FRONTEND — Angular

## Epic
EPIC-1 | Autentificare & Conturi

## Dependencies
- US-105 (Backend login endpoint must exist)
- US-804 (Angular App Shell & Routing)

## Acceptance Criteria

1. **Form**: Email, Password, `Ține-mă minte` toggle; show/hide password icon
2. **On success**: redirect to originally requested URL or homepage
3. **On failure**: `Email sau parolă incorectă` — no field-level leakage
4. **`Am uitat parola`** link → `/reset-password`; `Nu ai cont? Înregistrează-te` link
5. **Spinner + disabled submit** during in-flight request

## Technical Notes

### Component Location
`src/app/features/auth/login/login.component.ts`

### Implementation Details
- Reactive form: Email (required, email format), Password (required)
- `Ține-mă minte` toggle: when checked, store JWT refresh preference (cookie expiry extended)
- Call `POST /api/auth/login` via `AuthService`
- On 200: store access token in memory (not localStorage), refresh token arrives as HttpOnly cookie
- On 401: show generic error message `Email sau parolă incorectă`
- On 423: show `Contul este blocat temporar. Încearcă din nou mai târziu.`
- Redirect: use `returnUrl` query param if present, otherwise navigate to `/`
- Merge guest cart on login if guest token exists in localStorage (`POST /api/cart/merge`)

### UI/UX
- Password visibility toggle (eye icon)
- Link: `Am uitat parola` → `/auth/forgot-password`
- Link: `Nu ai cont? Înregistrează-te` → `/auth/register`
- `Continuă cu Google` button (links to US-106)
- All text in Romanian

## Files to Create/Modify
- `src/app/features/auth/login/login.component.ts`
- `src/app/features/auth/login/login.component.html`
- `src/app/features/auth/login/login.component.scss`
- `src/app/core/auth/auth.service.ts` (add `login()`, `storeToken()` methods)

## Testing
- Unit test: form validation
- Unit test: successful login → redirect
- Unit test: failed login → error message
- Unit test: account locked → locked message
- E2E: login flow happy path
