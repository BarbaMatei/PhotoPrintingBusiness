# US-804 — Angular App Shell & Routing (Frontend)

## Story
**As a** developer  
**I want to** establish a clean Angular app structure with lazy-loaded feature modules and route guards

## Type
FRONTEND — Angular

## Epic
EPIC-8 | Platformă & Non-Funcționale

## Dependencies
- None (foundational — should be implemented first)

## Acceptance Criteria

1. **App shell**: header (logo, nav links, cart badge, login/avatar), main `router-outlet`, footer
2. **Lazy-loaded route groups**: `/auth/*`, `/comanda/*`, `/comenzile-mele/*`, `/contul-meu/*`, `/admin/*`
3. **AuthGuard**: redirects unauthenticated users to `/auth/login`; stores `returnUrl`
4. **AdminGuard**: checks `role=Admin` claim in JWT; redirects to home if not admin
5. **GuestOrAuthGuard**: allows both Bearer and guest token for checkout routes
6. **Angular HttpInterceptor**: attaches `Authorization` header (JWT) OR `X-Guest-Token` to all API calls
7. **Global error interceptor**: handles 401 (trigger refresh flow), 403 (show error), 5xx (show error toast)
8. **Environment files**: `environment.ts` (dev API URL, Stripe/Google keys) vs `environment.prod.ts`

## Technical Notes

### Project Structure
```
photo-print-fe/
  src/app/
    core/
      auth/            → AuthService, JwtInterceptor, GuestInterceptor
      guards/          → AuthGuard, AdminGuard, GuestOrAuthGuard
      models/          → TypeScript interfaces (Order, Product, Cart, ...)
    shared/
      components/      → Header, Footer, StatusBadge, PhotoThumbnail, LoadingSpinner
      pipes/           → CurrencyRon, StatusLabel
    features/
      auth/            → login, register, forgot-password, reset-password
      upload/          → photo-upload, format-selector, cart
      checkout/        → delivery-step, review-step, payment-step, confirmation
      orders/          → order-list, order-detail
      account/         → profile, addresses, change-password
      admin/           → dashboard, order-queue, order-detail-panel, products
      legal/           → privacy, terms, cookies
  environments/
    environment.ts
    environment.prod.ts
```

### Implementation Details

#### App Shell
- Header: FotoTipar logo (links to home), nav links (Acasă, Tipărește), cart icon with badge, login/register links OR user avatar dropdown
- Footer: legal links, company info, copyright
- Responsive: hamburger menu on mobile

#### Routing
```typescript
const routes: Routes = [
  { path: '', component: HomeComponent },
  { path: 'auth', loadChildren: () => import('./features/auth/auth.module') },
  { path: 'cos', loadChildren: () => import('./features/upload/upload.module') },
  { path: 'checkout', loadChildren: () => import('./features/checkout/checkout.module'), canActivate: [GuestOrAuthGuard] },
  { path: 'comanda', loadChildren: () => import('./features/orders/orders.module') },
  { path: 'comenzile-mele', loadChildren: () => import('./features/orders/orders.module'), canActivate: [AuthGuard] },
  { path: 'contul-meu', loadChildren: () => import('./features/account/account.module'), canActivate: [AuthGuard] },
  { path: 'admin', loadChildren: () => import('./features/admin/admin.module'), canActivate: [AdminGuard] },
  // Legal pages
  { path: 'politica-de-confidentialitate', component: PrivacyComponent },
  { path: 'termeni-si-conditii', component: TermsComponent },
  { path: 'politica-cookie', component: CookiesComponent },
];
```

#### Guards
- **AuthGuard**: check `AuthService.isAuthenticated$`; if false, store current URL as `returnUrl`, navigate to `/auth/login`
- **AdminGuard**: decode JWT, check `role` claim === `Admin`; if not, navigate to `/`
- **GuestOrAuthGuard**: allow if either JWT token or guest token exists in storage

#### Interceptors
- **JwtInterceptor**: if access token exists, add `Authorization: Bearer {token}` header; on 401, attempt refresh; if refresh fails, logout
- **GuestInterceptor**: if no JWT but guest token exists, add `X-Guest-Token: {token}` header
- **ErrorInterceptor**: catch 403 (show forbidden toast), 5xx (show `Eroare de server. Încearcă din nou.` toast)

#### Environment Config
```typescript
export const environment = {
  production: false,
  apiUrl: 'https://localhost:5001/api',
  stripePublishableKey: 'pk_test_xxx',
  googleClientId: 'xxx.apps.googleusercontent.com'
};
```

## Files to Create/Modify
- Angular CLI: `ng new photo-print-fe --routing --style=scss`
- `src/app/app.component.ts` (shell)
- `src/app/app-routing.module.ts`
- `src/app/core/auth/auth.service.ts`
- `src/app/core/auth/jwt.interceptor.ts`
- `src/app/core/auth/guest.interceptor.ts`
- `src/app/core/auth/error.interceptor.ts`
- `src/app/core/guards/auth.guard.ts`
- `src/app/core/guards/admin.guard.ts`
- `src/app/core/guards/guest-or-auth.guard.ts`
- `src/app/shared/components/header/header.component.ts`
- `src/app/shared/components/footer/footer.component.ts`
- `src/environments/environment.ts`
- `src/environments/environment.prod.ts`

## Testing
- Unit test: AuthGuard redirects unauthenticated users
- Unit test: AdminGuard checks role claim
- Unit test: JwtInterceptor adds Authorization header
- Unit test: GuestInterceptor adds X-Guest-Token header
- Unit test: ErrorInterceptor handles 401 refresh flow
- Unit test: lazy-loaded routes load correctly
- E2E: navigation between routes
