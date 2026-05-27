---
name: frontend-angular
description: Angular frontend development conventions and patterns for the FotoTipar photo printing website. Use this skill when building Angular components, services, guards, interceptors, or any frontend TypeScript code.
---

## Tech Stack

- **Angular 17+** with standalone components (or NgModules with lazy loading)
- **TypeScript 5.x** strict mode
- **SCSS** for styling
- **Angular Material** or custom component library
- **RxJS** for reactive patterns

## Project Structure

```
photo-print-fe/src/app/
  core/           → singletons: services, guards, interceptors, models
  shared/         → reusable components, pipes, directives
  features/       → lazy-loaded feature modules (auth, upload, checkout, orders, account, admin, legal)
  environments/   → environment config files
```

## Coding Conventions

### Components

- Use standalone components where possible (Angular 17+)
- Use `ChangeDetectionStrategy.OnPush` for all presentational components
- Prefix selectors with `app-` (e.g., `app-status-badge`)
- One component per file; co-locate `.ts`, `.html`, `.scss`, `.spec.ts`
- Template-driven forms only for simple cases; **Reactive Forms** for all data entry

### Services

- All HTTP calls go through Angular services in `core/` — never call `HttpClient` directly from components
- Services are `@Injectable({ providedIn: 'root' })` for singletons
- Return `Observable<T>` from service methods; let components subscribe
- Use interfaces for all API response types in `core/models/`

### State Management

- Simple state: use `BehaviorSubject` in services (CartService, AuthService)
- Do NOT introduce NgRx unless complexity warrants it
- Cart state: localStorage for guests, server-sync for logged-in users

### Routing

- Lazy load all feature modules
- Guards: `AuthGuard`, `AdminGuard`, `GuestOrAuthGuard` — use functional guards (Angular 15+)
- Store `returnUrl` in `AuthService` when redirecting to login

### Interceptors

- `JwtInterceptor`: attach `Authorization: Bearer` header; handle 401 → refresh flow
- `GuestInterceptor`: attach `X-Guest-Token` header when no JWT present
- `ErrorInterceptor`: handle 403, 5xx with toast notifications

### Internationalization

- All UI text in **Romanian** — hardcoded strings (no i18n library at MVP)
- Currency format: `XX,XX RON` (use custom `CurrencyRon` pipe)
- Date format: `dd.MM.yyyy` (Romanian locale)

### Error Handling

- HTTP errors: interceptor shows toast for 5xx; specific error messages for 4xx
- Form validation: inline field errors shown on blur and submit
- Loading states: spinner/disabled button during API calls

## API Communication

- Base URL from `environment.apiUrl`
- All endpoints prefixed with `/api`
- Auth: `Bearer JWT` or `X-Guest-Token` header (never both)
- Handle `ProblemDetails` (RFC 7807) error responses from backend

## Third-Party Libraries

- `@stripe/stripe-js` — Stripe Elements for card payment
- `leaflet` + `@types/leaflet` — maps for Easybox locker selection
- `ng2-charts` (Chart.js) — admin dashboard charts
- `@microsoft/signalr` — real-time admin order updates
- `heic2any` — HEIC image preview conversion

## Performance

- Lazy load all feature modules
- Use `trackBy` in all `*ngFor` loops
- Debounce search inputs (300ms)
- Use `async` pipe in templates to auto-unsubscribe
- Image thumbnails: use client-side resized previews, not full images

## Accessibility

- Semantic HTML elements
- ARIA labels on interactive elements
- Keyboard navigation support
- Focus management after route changes
