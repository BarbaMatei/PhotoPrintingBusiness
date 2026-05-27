---
stage: plan
bolt: 004-angular-app-shell
created: 2026-05-20T00:00:00Z
---

## Implementation Plan: Angular App Shell

### Objective

Scaffold the Angular 17+ SPA foundation at `src/PhotoPrint.UI/`: a standalone-component app shell (header, router-outlet, footer), lazy-loaded feature routes, functional route guards, functional HTTP interceptors, stub core services, and environment configuration for dev and prod.

No feature logic is implemented here — only the skeleton that all future feature epics will render inside.

---

### Deliverables

**Project Scaffold**
- `src/PhotoPrint.UI/` — Angular 17 CLI project (package.json, angular.json, tsconfig*, eslint, .gitignore)
- `src/PhotoPrint.UI/src/index.html` — Entry HTML, `<app-root>` host, Inter/Roboto font link
- `src/PhotoPrint.UI/src/main.ts` — `bootstrapApplication(AppComponent, appConfig)`
- `src/PhotoPrint.UI/src/styles.scss` — Global reset, typography, SCSS token imports
- `src/PhotoPrint.UI/src/styles/_variables.scss` — Design tokens (colors, spacing, typography)
- `src/PhotoPrint.UI/src/styles/_mixins.scss` — Responsive breakpoint mixins

**Story 001 — App Shell Layout**
- `src/app/app.component.ts/html/scss` — Root standalone component: `<app-header>`, `<router-outlet>`, `<app-footer>`
- `src/app/layout/header/header.component.ts/html/scss` — Header with logo, nav, cart badge, auth state switcher
- `src/app/layout/footer/footer.component.ts/html/scss` — Footer with legal links (Romanian text)
- `src/app/core/services/auth.service.ts` — Stub: `isAuthenticated$`, `currentUser$`, `isAdmin()`, `setReturnUrl()`, `getReturnUrl()`
- `src/app/core/services/cart.service.ts` — Stub: `itemCount$` (BehaviorSubject seeded to 0)

**Story 002 — Lazy-Loaded Routes**
- `src/app/app.routes.ts` — Root route config with `provideRouter()`, all lazy feature routes
- `src/app/features/auth/auth.routes.ts` + placeholder page component
- `src/app/features/upload/upload.routes.ts` + placeholder page component
- `src/app/features/cart/cart.routes.ts` + placeholder page component
- `src/app/features/checkout/checkout.routes.ts` + placeholder page component
- `src/app/features/orders/orders.routes.ts` + placeholder page component
- `src/app/features/account/account.routes.ts` + placeholder page component
- `src/app/features/admin/admin.routes.ts` + placeholder page component
- `src/app/features/legal/privacy-policy.component.ts` — Eager, simple static component
- `src/app/features/legal/terms.component.ts` — Eager, simple static component
- `src/app/features/legal/cookie-policy.component.ts` — Eager, simple static component
- `src/app/shared/components/not-found/not-found.component.ts` — 404 "Pagina nu a fost găsită"

**Story 003 — Route Guards**
- `src/app/core/guards/auth.guard.ts` — `CanActivateFn`: checks `AuthService.isAuthenticated()`, stores `returnUrl`, redirects to `/auth/login`
- `src/app/core/guards/admin.guard.ts` — `CanActivateFn`: checks `AuthService.isAdmin()`, redirects to `/` if false
- `src/app/core/guards/guest-or-auth.guard.ts` — `CanActivateFn`: allows if JWT OR guest token present in storage

**Story 004 — HTTP Interceptors**
- `src/app/core/interceptors/jwt.interceptor.ts` — `HttpInterceptorFn`: attaches `Authorization: Bearer` for API calls; handles 401 stub (logs out)
- `src/app/core/interceptors/guest.interceptor.ts` — `HttpInterceptorFn`: attaches `X-Guest-Token` if no JWT and guest token present; only for API base URL
- `src/app/core/interceptors/error.interceptor.ts` — `HttpInterceptorFn`: catches 401/403/5xx/network errors, shows toast
- `src/app/shared/services/toast.service.ts` — `BehaviorSubject<Toast[]>`, `show()`, `dismiss()` methods
- `src/app/shared/components/toast/toast.component.ts/html/scss` — Toast container rendered in AppComponent
- `src/app/app.config.ts` — `ApplicationConfig` with `provideRouter()`, `provideHttpClient(withInterceptors([...]))`, `provideAnimations()`

**Story 005 — Environment Config**
- `src/environments/environment.ts` — Dev: `apiUrl`, `stripePublishableKey`, `googleClientId`, `production: false`
- `src/environments/environment.prod.ts` — Prod: same shape, production values, `production: true`
- `angular.json` `fileReplacements` for production configuration

---

### Dependencies

- **Angular CLI 17+**: `npm install -g @angular/cli` to scaffold, or use `npx @angular/cli@17`
- **No Angular Material / third-party UI** — custom SCSS per UX guide
- **No NgRx** — `BehaviorSubject` in services for state
- **@angular/common/http**: built-in, provides `HttpClient` and interceptors
- **`@angular/router`**: built-in, provides `provideRouter`, `CanActivateFn`

---

### Technical Approach

**Project creation**: `ng new photoPrint-ui --standalone --routing --style=scss --strict` inside `src/PhotoPrint.UI/`

**Standalone-only**: No `NgModule` anywhere. All components are `standalone: true`. `bootstrapApplication()` in `main.ts`.

**Routing**: `provideRouter(routes, withComponentInputBinding())` in `app.config.ts`. Feature routes use `loadComponent` (single page) or `loadChildren` (sub-routes file) with dynamic imports.

**Guards**: All use `CanActivateFn` pattern (inject via `inject()`). No class-based guards.

**Interceptors**: All use `HttpInterceptorFn` pattern. Registered via `withInterceptors([jwt, guest, error])` — order matters: jwt first, then guest, then error.

**Auth stub**: `isAuthenticated()` returns `false`, `isAdmin()` returns `false`. Token methods read/write `localStorage`. Full implementation deferred to Epic 1.

**URL scoping for interceptors**: Interceptors check `request.url.startsWith(environment.apiUrl)` before attaching headers; external URLs (Stripe, Google) pass through unmodified.

**SCSS tokens**: Defined as CSS custom properties in `_variables.scss` and also as SCSS variables for use in component SCSS files.

---

### Acceptance Criteria

**Story 001 — App Shell**
- [ ] Header shows logo, nav links ("Acasă", "Tipărește"), cart badge, login/register links on any route
- [ ] Header swaps to avatar/dropdown for authenticated users
- [ ] Header shows "Admin" nav link for admin users
- [ ] Footer renders on every route with legal links (Romanian text)
- [ ] Mobile hamburger collapses nav below 768px
- [ ] All components are `standalone: true`

**Story 002 — Lazy Routes**
- [ ] `/auth/*`, `/cos/*`, `/checkout/*`, `/comenzile-mele/*`, `/contul-meu/*`, `/admin/*`, `/tipareste/*` are lazy-loaded
- [ ] Legal routes render static placeholder text
- [ ] `**` wildcard renders "Pagina nu a fost găsită" (404)
- [ ] No NgModule references in route definitions

**Story 003 — Guards**
- [ ] `authGuard`: unauthenticated → redirect to `/auth/login` with `returnUrl`
- [ ] `adminGuard`: non-admin → redirect to `/`
- [ ] `guestOrAuthGuard`: allows if JWT or guest token present; otherwise → `/auth/login`
- [ ] All guards are `CanActivateFn` functions

**Story 004 — Interceptors**
- [ ] JWT bearer header attached for API calls when access token present
- [ ] Guest token header attached when no JWT but guest token present
- [ ] 401/403/5xx errors show toast notifications with Romanian text
- [ ] External URLs skip auth header attachment
- [ ] Interceptors registered via `withInterceptors()` in `app.config.ts`

**Story 005 — Environment**
- [ ] `environment.ts` exports `apiUrl`, `stripePublishableKey`, `googleClientId`, `production: false`
- [ ] `environment.prod.ts` exports same shape with `production: true`
- [ ] `ng build --configuration=production` swaps to prod environment file
</content>
