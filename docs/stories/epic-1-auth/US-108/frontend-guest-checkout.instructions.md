# US-108 — Guest Checkout — Info Form (Frontend)

## Story
**As a** visitor who does not want to create an account  
**I want to** place an order without registering by filling in my contact details

## Type
FRONTEND — Angular

## Epic
EPIC-1 | Autentificare & Conturi

## Dependencies
- US-109 (Backend guest session endpoint)
- US-804 (Angular App Shell)

## Acceptance Criteria

1. **On checkout start**: modal or page prompt — `Continuă ca oaspete` | `Conectează-te` | `Creează cont`
2. **Guest form fields**: First Name (required), Last Name (required), Email (required, valid format), Phone (required, Romanian format `07xxxxxxxx`)
3. **Guest session** stored in localStorage as `{guestToken, firstName, lastName, email, phone}`; sent as `X-Guest-Token` header on all subsequent requests
4. **Post-order nudge**: `La final îți vei putea crea un cont pentru a urmări comenzile` — subtle nudge shown after order placed

## Technical Notes

### Component Location
`src/app/features/checkout/guest-form/guest-form.component.ts`

### Implementation Details
- Checkout start interceptor: if not logged in, show modal with three options
- Guest form: Reactive Forms, validators for all required fields
- Phone validation: regex `^07\d{8}$`
- On form submit: call `POST /api/auth/guest`, receive `guestToken`
- Store in localStorage: `{ guestToken, firstName, lastName, email, phone }`
- `GuestInterceptor` (HttpInterceptor): if no Bearer token but guestToken exists, add `X-Guest-Token` header
- After order placed, show banner with link to register (pre-fill email)

## Files to Create/Modify
- `src/app/features/checkout/guest-form/guest-form.component.ts`
- `src/app/features/checkout/guest-form/guest-form.component.html`
- `src/app/features/checkout/checkout-gate/checkout-gate.component.ts` (modal with 3 options)
- `src/app/core/auth/guest.interceptor.ts`
- `src/app/core/auth/guest.service.ts` (localStorage management)

## Testing
- Unit test: form validation rules
- Unit test: guest token stored in localStorage
- Unit test: interceptor adds X-Guest-Token header
- E2E: guest checkout flow
