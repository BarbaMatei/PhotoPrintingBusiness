# US-106 — Google Social Login (Frontend)

## Story
**As a** user  
**I want to** sign in with my Google account without creating a separate password

## Type
FRONTEND — Angular

## Epic
EPIC-1 | Autentificare & Conturi

## Dependencies
- US-107 (Backend Google auth endpoint)
- US-804 (Angular App Shell)

## Acceptance Criteria

1. **`Continuă cu Google`** button on login and register pages using Google Identity Services SDK
2. **On success callback**: sends `idToken` to BE; receives platform JWT
3. **If email exists with password account**: accounts are linked; toast `Contul tău Google a fost conectat`
4. **Error from Google**: friendly message `Autentificarea Google a eșuat. Încearcă din nou`

## Technical Notes

### Implementation Details
- Load Google Identity Services library via script tag or npm package
- Initialize with `google.accounts.id.initialize({ client_id: environment.googleClientId, callback: handleCredentialResponse })`
- On callback: extract `credential` (id_token), send to `POST /api/auth/google`
- On success: store access token, handle refresh token cookie, redirect to returnUrl or home
- On account linking: show toast notification
- Handle errors gracefully — never expose Google error details to user
- Button rendered using `google.accounts.id.renderButton()` for consistent branding

### Environment Config
- `environment.ts`: `googleClientId: 'your-client-id.apps.googleusercontent.com'`

## Files to Create/Modify
- `src/app/features/auth/login/login.component.ts` (add Google button)
- `src/app/features/auth/register/register.component.ts` (add Google button)
- `src/app/core/auth/auth.service.ts` (add `googleLogin()` method)
- `src/app/core/auth/google-auth.service.ts` (Google SDK wrapper)
- `src/index.html` (add Google SDK script if not using npm)

## Testing
- Unit test: Google callback sends token to backend
- Unit test: error handling for failed Google auth
- E2E: Google login button renders correctly
