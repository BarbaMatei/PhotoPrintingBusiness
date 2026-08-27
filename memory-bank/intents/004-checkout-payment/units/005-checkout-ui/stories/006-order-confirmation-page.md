---
id: 006-order-confirmation-page
unit: 005-checkout-ui
intent: 004-checkout-payment
status: complete
priority: must
created: 2026-05-21T12:00:00Z
assigned_bolt: 017-checkout-ui
implemented: true
---

# Story: 006-order-confirmation-page

## User Story

**As a** customer who has just paid
**I want** to see a confirmation page with my order number and next steps
**So that** I know my order was received and what to expect next

## Acceptance Criteria

- [ ] **Given** `/comanda/{orderId}/confirmare` is loaded, **When** the page fetches `GET /api/orders/{orderId}`, **Then** the page shows: order number (`FT-YYYYNNNN`), order status stepper (`Plătit → Procesare → Expediat → Livrat`), delivery details, and expected delivery note
- [ ] **Given** the order status is not `Paid`, **When** the page loads (e.g., navigated directly before payment), **Then** the user is redirected to the home page
- [ ] **Given** a guest user is on the confirmation page, **When** the guest CTA is shown, **Then** it displays `"Creează un cont pentru a urmări comanda"` with a link to `/inregistrare?claimOrder={orderId}`
- [ ] **Given** an authenticated user is on the confirmation page, **When** shown, **Then** a `"Vezi toate comenzile"` link to `/contul-meu/comenzi` is displayed instead of the guest CTA
- [ ] **Given** `?processor=legacy-processor` is in the URL, **When** the page loads, **Then** the page polls `GET /api/orders/{orderId}` up to 5 times with 2s interval until status is `Paid` (IPN may not have arrived yet)
- [ ] **Given** the confirmation page loads successfully, **When** shown, **Then** `CheckoutStateService` is cleared and sessionStorage `ft_checkout_state` is removed

## Technical Notes

- Route: `/comanda/:orderId/confirmare` (lazy-loaded, no auth guard — accessible by guests)
- Polling for the legacy processor: `?processor=legacy-processor` triggers `interval(2000).pipe(take(5), switchMap(() => ordersService.getById(orderId)), filter(o => o.status === 'Paid'), take(1))`
- Order status stepper: display-only; current status highlighted; uses `OrderStatus` enum values
- `GET /api/orders/{orderId}` must verify order ownership (UserId or GuestSessionId) — returns 404 if not owner
- Guest claim URL: `/inregistrare?claimOrder={orderId}` (registration page handles guest order claim, future intent)
- After clearing `CheckoutStateService`, navigate guard will prevent back-button return to `/checkout`

## Dependencies

### Requires
- Story 001-checkout-stepper (CheckoutStateService.clear())
- Bolt 016 (payment-backends — `GET /api/orders/{orderId}` endpoint)
- Bolt 004 (angular-app-shell — route registration)

### Enables
- Future intent 005-order-management (order history page linked from confirmation)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| the legacy processor IPN delayed > 10s | Polling ends; show `"Plata este în curs de procesare"` with manual refresh hint |
| `GET /api/orders/{orderId}` returns 404 | Redirect to home page |
| Order `PaymentFailed` on confirmation page | Redirect to `/checkout/plata` with error toast |
| User shares confirmation URL | Other user gets 404 (order ownership check) |

## Out of Scope

- Order receipt PDF download
- Re-order from confirmation page
- Sharing order confirmation (e.g., social share)
