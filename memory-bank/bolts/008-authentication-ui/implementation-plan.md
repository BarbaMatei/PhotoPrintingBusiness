---
stage: plan
bolt: 008-authentication-ui
created: 2026-05-20T18:00:00Z
---

## Implementation Plan: authentication-ui

### Objective

Build all Angular 21 authentication pages and shared components for FotoTipar: registration with password strength and GDPR consent, email verification pending with resend and countdown, login with redirect-back, Google Identity Services button, guest checkout prompt modal, forgot-password, and reset-password. Extend `AuthService` with HTTP methods and create `GuestAuthService`.

---

### Deliverables

#### Services

1 - **`AuthService` extended** (`src/app/core/services/auth.service.ts`)
  - `register(dto)` → `POST /api/auth/register`
  - `login(email, password)` → `POST /api/auth/login` → stores `access_token` in sessionStorage
  - `googleLogin(idToken)` → `POST /api/auth/google`
  - `resendConfirmation(email)` → `POST /api/auth/resend-confirmation`
  - `forgotPassword(email)` → `POST /api/auth/forgot-password`
  - `resetPassword(dto)` → `POST /api/auth/reset-password`
  - `setAuthenticated(user)` helper — updates BehaviorSubjects
  - `isAlreadyAuthenticated()` — used by auth pages to redirect away

2 - **`GuestAuthService`** (new) (`src/app/core/services/guest-auth.service.ts`)
  - `createGuestSession(dto)` → `POST /api/auth/guest` → stores to localStorage key `guestSession`
  - `claimGuestSession(guestToken)` → `POST /api/auth/guest/claim`
  - `getStoredSession()` → reads `guestSession` from localStorage
  - `clearSession()` → removes `guestSession` from localStorage

3 - **`passwordStrengthValidator`** (shared) (`src/app/shared/validators/password-strength.validator.ts`)
  - Rules: min 8 chars, 1 uppercase, 1 digit, 1 special character
  - Returns `ValidationErrors` with which rules failed (for UI rendering)

#### Page Components (under `src/app/features/auth/pages/`)

4 - **`RegisterPage`** (`register/register-page.ts`)
  - Reactive form: firstName, lastName, email, password, confirmPassword, phone (optional), gdprConsent
  - Real-time password strength display using `passwordStrengthValidator`
  - On 201: navigate to `/auth/verify-email` with router state `{email}`
  - On 409: email field error `"Adresa de email este deja folosită"`
  - On 422: map field-level errors from API response to form controls
  - Submit spinner + disabled while in flight

5 - **`EmailVerificationPendingPage`** (`verify-email/verify-email-page.ts`)
  - Reads `email` from router state (navigation extras)
  - Resend button: calls `AuthService.resendConfirmation(email)` → success toast
  - 429 → toast + 60s countdown disabling button (`setInterval`, cleaned up in `ngOnDestroy`)
  - `?confirmed=true` query param → success banner with login link

6 - **`LoginPage`** (`login/login-page.ts`)
  - Reactive form: email, password
  - Show/hide password toggle
  - "Ține-mă minte" checkbox (cosmetic)
  - On 200: store `access_token`, call `setAuthenticated()`, navigate to `getReturnUrl()`
  - On 401: form-level error `"Email sau parolă incorectă"`
  - On 403: error `"Confirmați adresa de email pentru a continua"` + resend link
  - On 423: error with remaining minutes from response body
  - Already authenticated → redirect to `/tipareste`

7 - **`ForgotPasswordPage`** (`forgot-password/forgot-password-page.ts`)
  - Single email field
  - On success (any 200 or error): always show `"Dacă adresa există, vei primi un email cu instrucțiuni"`
  - "Înapoi la autentificare" link → `/auth/login`

8 - **`ResetPasswordPage`** (`reset-password/reset-password-page.ts`)
  - Reads `userId` + `token` from `ActivatedRoute.queryParamMap`
  - If missing: show `"Link invalid"` without form
  - Reactive form: newPassword (strength validator), confirmPassword (must match)
  - On 200: success message + link to `/auth/login`
  - On 400: `"Link invalid sau expirat"` + link to `/auth/forgot-password`

#### Shared Components

9 - **`GoogleAuthButton`** (`src/app/shared/components/google-auth-button/google-auth-button.ts`)
  - Uses `afterNextRender()` to initialize Google Identity Services SDK
  - Calls `google.accounts.id.initialize()` + `google.accounts.id.renderButton()`
  - `clientId` from `environment.googleClientId`
  - On token: calls `AuthService.googleLogin(idToken)` → navigate or toast
  - `accountLinked: true` → extra toast `"Contul tău Google a fost conectat"`
  - Error callback → toast `"Autentificarea Google a eșuat. Încearcă din nou"`

10 - **`GuestCheckoutPromptComponent`** (`src/app/features/auth/components/guest-checkout-prompt/guest-checkout-prompt.ts`)
  - Dialog-based modal using `<dialog>` element
  - 3 options: "Continuă ca oaspete" / "Conectează-te" / "Creează cont"
  - "Conectează-te" → navigate to `/auth/login` with returnUrl `/checkout`
  - "Creează cont" → navigate to `/auth/register`
  - "Continuă ca oaspete" → shows `GuestCheckoutFormComponent` inline
  - Close without selecting → dismiss (emits event to parent)

11 - **`GuestCheckoutFormComponent`** (`src/app/features/auth/components/guest-checkout-form/guest-checkout-form.ts`)
  - Reactive form: firstName, lastName, email, phone (pattern `07[0-9]{8}`)
  - On 200: store session to localStorage, emit success event to close modal
  - Phone error: `"Număr de telefon invalid (ex: 0712345678)"`

#### Routing

12 - **`auth.routes.ts`** — replace placeholder with all 5 routes:
  - `/auth/register` → `RegisterPage`
  - `/auth/login` → `LoginPage`
  - `/auth/verify-email` → `EmailVerificationPendingPage`
  - `/auth/forgot-password` → `ForgotPasswordPage`
  - `/auth/reset-password` → `ResetPasswordPage`

#### `index.html`

13 - Add Google Identity Services script tag:
  ```html
  <script src="https://accounts.google.com/gsi/client" async defer></script>
  ```

#### SCSS

14 - Each page/component has a co-located `.scss` file using `@use 'styles/variables' as *`
  - Auth pages share a card-centered layout (`.auth-card` container)
  - Password strength bar with colour coding per rule
  - Modal overlay for guest checkout prompt

---

### Dependencies

- `HttpClient` (already provided in `appConfig` via `provideHttpClient`)
- `ReactiveFormsModule` — imported in each standalone component
- `RouterModule` / `Router` / `ActivatedRoute` — from `@angular/router`
- `ToastService` — already exists at `src/app/shared/services/toast.service.ts`
- Google Identity Services — `https://accounts.google.com/gsi/client` (loaded via `index.html`)
- `environment.googleClientId` — already defined as placeholder
- `environment.apiUrl` — already defined as `https://localhost:5001/api`
- No new npm packages required

---

### Technical Approach

- All page components are **standalone** (`standalone: true`), `ChangeDetectionStrategy.OnPush`
- HTTP methods return `Observable<T>` — components subscribe with `takeUntilDestroyed()`
- Password strength validator returns a map of rule results so the template can show per-rule feedback
- `GuestCheckoutPromptComponent` is mounted by the checkout route resolver (not by `guestOrAuthGuard` directly) — guard still redirects to `/auth/login` for now; prompt is opened from the checkout feature entry page
- `GoogleAuthButton` declares `window.google` via `declare const google: any` (no @types/google needed)
- Error response parsing: extract `errors` array from `ProblemDetails` 422 bodies to map to form fields

---

### Acceptance Criteria

- [ ] `/auth/register` form validates all fields client-side before submit; maps server errors correctly
- [ ] Password strength indicator shows 4 rules in real time
- [ ] GDPR checkbox gates submission
- [ ] `/auth/verify-email` resend works with 60s cooldown on 429
- [ ] `/auth/verify-email?confirmed=true` shows success banner
- [ ] `/auth/login` redirects to `returnUrl` after success
- [ ] Login handles 401, 403, 423 with correct Romanian messages
- [ ] Google button renders and completes sign-in flow
- [ ] Guest prompt shows 3 options; guest form saves to localStorage
- [ ] `/auth/forgot-password` always shows success message
- [ ] `/auth/reset-password` reads query params; handles missing params, 200, and 400
- [ ] All UI text is in Romanian
- [ ] All new services and components have Vitest/Jasmine unit tests
