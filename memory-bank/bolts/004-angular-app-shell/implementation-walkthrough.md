# Implementation Walkthrough — Bolt 004 Angular App Shell
## Stage 2: Implement

**Angular version**: 21.2.x (standalone-first, Vitest testing, no zone.js required)
**Build status**: ✅ dev + prod both succeed (0 errors, 0 warnings after budget adjustment)

---

## Files Created

### Environments
| File | Purpose |
|------|---------|
| `src/environments/environment.ts` | Dev config: `apiUrl: https://localhost:5001/api`, Stripe + Google placeholders |
| `src/environments/environment.prod.ts` | Prod config: `apiUrl: https://api.fototipar.ro/api` |

`angular.json` updated with `fileReplacements` in the `production` build configuration.

---

### SCSS Design Tokens
| File | Contents |
|------|---------|
| `src/styles/_variables.scss` | Full token set: colours, typography, spacing (8pt scale), layout, shadows, z-index, transitions, breakpoints |
| `src/styles/_mixins.scss` | Responsive breakpoints (`sm/md/lg/xl/xxl`), container, flex helpers, focus ring, button base mixins |
| `src/styles.scss` | Global reset + base styles; imports tokens via `@use 'styles/variables' as *` |

`angular.json` updated with `stylePreprocessorOptions.includePaths: ["src"]` so all SCSS files import tokens as `@use 'styles/variables' as *` (no fragile relative paths).

---

### Core Services

#### `src/app/core/services/auth.service.ts`
- `BehaviorSubject<boolean>` for `isAuthenticated$` and `isAdmin$`
- `BehaviorSubject<CurrentUser|null>` for `currentUser$`
- `getAccessToken()` → reads `sessionStorage['access_token']`
- `getGuestToken()` → reads `localStorage['guest_token']`
- `setReturnUrl(url)` / `getReturnUrl()` for post-login redirect
- `logout()` clears session state
- All stubs; full OAuth/JWT implementation deferred to Epic 1 (US-101)

#### `src/app/core/services/cart.service.ts`
- `BehaviorSubject<number>(0)` for `itemCount$`
- Seeded to 0; full implementation deferred to Epic 2

#### `src/app/shared/services/toast.service.ts`
- `Toast` interface: `{ id, message, type: 'success'|'error'|'warning'|'info' }`
- `show(message, type)` → appends toast + auto-dismiss after 5 s
- `dismiss(id)` → removes by ID
- Uses `crypto.randomUUID()` for safe ID generation

---

### Route Guards

| File | Guard | Behaviour |
|------|-------|-----------|
| `core/guards/auth.guard.ts` | `authGuard: CanActivateFn` | Redirects to `/auth/login` if not authenticated; saves return URL |
| `core/guards/admin.guard.ts` | `adminGuard: CanActivateFn` | Redirects to `/` if authenticated but not admin |
| `core/guards/guest-or-auth.guard.ts` | `guestOrAuthGuard: CanActivateFn` | Allows authenticated users OR guest token holders (checkout flow) |

Open-redirect protection: `setReturnUrl` only stores paths starting with `/`.

---

### HTTP Interceptors

| File | Interceptor | Behaviour |
|------|-------------|-----------|
| `core/interceptors/jwt.interceptor.ts` | `jwtInterceptor: HttpInterceptorFn` | Attaches `Authorization: Bearer <token>` to own API calls only |
| `core/interceptors/guest.interceptor.ts` | `guestInterceptor: HttpInterceptorFn` | Attaches `X-Guest-Token` header for unauthenticated guests |
| `core/interceptors/error.interceptor.ts` | `errorInterceptor: HttpInterceptorFn` | 401→logout, 403→toast, 5xx→toast, network error→toast |

All interceptors skip external service URLs (Stripe, Google) by checking `req.url.startsWith(environment.apiUrl)`.

---

### Layout Components

#### Header (`src/app/layout/header/header.ts/html/scss`)
- Sticky top bar, `z-index: $z-header`
- Uses `toSignal()` from `@angular/core/rxjs-interop` to convert `AuthService.isAuthenticated$`, `isAdmin$`, and `CartService.itemCount$` into Angular signals
- Responsive: desktop nav hidden on mobile; hamburger + slide-down panel on mobile
- Auth-conditional: guest → login/register links; authenticated → avatar + dropdown
- Cart badge with `cartCount()` count; hidden at zero
- Admin link shown only when `isAdmin()` is truthy
- All text in Romanian

#### Footer (`src/app/layout/footer/footer.ts/html/scss`)
- Dark background bar with logo, legal links, copyright year
- Responsive flex column → row at `md` breakpoint

---

### Toast Component (`src/app/shared/components/toast/toast.ts/html/scss`)
- Subscribes to `ToastService.toasts$` via `toSignal()`
- Fixed position (bottom-right), `z-index: $z-toast` (above all other overlays)
- Type-specific colour coding (`success`, `error`, `warning`, `info`)
- Dismiss button + auto-dismiss via `ToastService.dismiss(id)`
- ARIA live region (`aria-live="polite"`)

---

### Feature Routes (all lazy-loaded)

| Path | Feature | Guard | Route file |
|------|---------|-------|-----------|
| `/auth` | Auth pages | None | `features/auth/auth.routes.ts` |
| `/tipareste` | Upload/print workflow | None | `features/upload/upload.routes.ts` |
| `/cos` | Cart | None | `features/cart/cart.routes.ts` |
| `/checkout` | Checkout | `guestOrAuthGuard` | `features/checkout/checkout.routes.ts` |
| `/comenzile-mele` | My orders | `authGuard` | `features/orders/orders.routes.ts` |
| `/contul-meu` | My account | `authGuard` | `features/account/account.routes.ts` |
| `/admin` | Admin panel | `authGuard` + `adminGuard` | `features/admin/admin.routes.ts` |

Each feature route file exports `routes: Routes` with a single lazy `loadComponent` placeholder.

Legal pages (eager): `/politica-de-confidentialitate`, `/termeni-si-conditii`, `/politica-cookie`
Wildcard `**` → `NotFound` component (inline)

---

### App Root (`src/app/`)

#### `app.routes.ts`
Full route tree with all 7 features + 3 legal pages + `NotFound` wildcard.

#### `app.config.ts`
```typescript
providers: [
  provideBrowserGlobalErrorListeners(),
  provideRouter(routes, withComponentInputBinding()),
  provideHttpClient(withInterceptors([jwtInterceptor, guestInterceptor, errorInterceptor])),
]
```

#### `app.ts` + `app.html` + `app.scss`
Shell: `<app-header />` · `<main><router-outlet/></main>` · `<app-footer />` · `<app-toast />`  
`:host` is a flex column with `min-height: 100dvh`; `main` gets `flex: 1` to push footer down.

---

## Angular 21 Notes

This project scaffolded with Angular **21.2.x** (not 17 as originally targeted). Key differences:

| Feature | Angular 17 (planned) | Angular 21 (actual) |
|---------|---------------------|---------------------|
| File naming | `app.component.ts` | `app.ts` (no `.component` suffix) |
| Class naming | `AppComponent` | `App` |
| standalone default | opt-in | default (no `standalone: true` needed) |
| Test runner | Karma / Jasmine | Vitest + `@angular/build:unit-test` |
| Change detection | Zone.js | zoneless (no `zone.js` in package.json) |
| Signals | available | preferred / default pattern |

All guards and interceptors use `inject()` inside `CanActivateFn` / `HttpInterceptorFn` (functional style — consistent in both Angular 17 and 21).

---

## Build Output

```
Initial bundle (dev):  main.js 564 kB
Lazy chunks:           11 feature route chunks (1.99–2.15 kB each)
Styles:                1.19 kB (global reset)
Build time:            ~4.9 s
```

Production build: ✅ succeeds; environment.prod.ts swapped in via `fileReplacements`.

---

## Acceptance Criteria Status

| AC | Status |
|----|--------|
| `ng build` succeeds with no TypeScript errors | ✅ |
| All routes use standalone components, no NgModules | ✅ |
| Feature routes are lazy-loaded (separate JS chunks) | ✅ |
| Auth/cart/toast services provide `BehaviorSubject` state | ✅ |
| Guards protect `/comenzile-mele`, `/contul-meu`, `/admin` | ✅ |
| JWT + guest + error interceptors registered in `provideHttpClient` | ✅ |
| Header shows cart badge, auth state, admin link | ✅ |
| Footer includes legal links | ✅ |
| `404` wildcard route renders `NotFound` | ✅ |
| Environment files swap in production build | ✅ |
| All text in Romanian | ✅ |
| BEM methodology in SCSS | ✅ |
| Design tokens centralised in `_variables.scss` | ✅ |
