---
id: 002-delivery-step
unit: 005-checkout-ui
intent: 004-checkout-payment
status: draft
priority: must
created: 2026-05-21T12:00:00Z
assigned_bolt: 017-checkout-ui
implemented: false
---

# Story: 002-delivery-step

## User Story

**As a** customer
**I want** to choose between Easybox locker pickup and home delivery
**So that** I can select the most convenient delivery option for my photo prints

## Acceptance Criteria

- [ ] **Given** Step 1 (`/checkout/livrare`) is loaded, **When** rendered, **Then** two delivery method cards are shown: `Easybox Locker (20,00 RON)` and `Livrare la domiciliu (25,00 RON)` — prices fetched from `GET /api/shipping/cost`
- [ ] **Given** `Easybox Locker` is selected, **When** the card is clicked, **Then** the locker map component (story 003) is shown below the card
- [ ] **Given** `Livrare la domiciliu` is selected, **When** the card is clicked, **Then** an address form is shown: `Nume complet*`, `Telefon*`, `Stradă*`, `Oraș*`, `Județ* (dropdown)`, `Cod poștal*`
- [ ] **Given** an authenticated user has a saved address, **When** the delivery step loads, **Then** a `Folosește adresa salvată` option pre-fills the address form
- [ ] **Given** the `Continuă` button, **When** no delivery method is selected or the form/locker is incomplete, **Then** the button is disabled
- [ ] **Given** `Continuă` is clicked with valid selection, **When** the selection is saved to `CheckoutStateService`, **Then** the user advances to Step 2

## Technical Notes

- Județe (Romanian counties) dropdown: hardcoded 41 + Bucharest array in a constant file
- Address form: Angular Reactive Forms with `Validators.required` on all marked fields; phone: `Validators.pattern(/^[0-9]{10}$/)`
- Saved address: `GET /api/users/me/address` (if auth); pre-fill form on selection
- Shipping costs fetched on component init from `GET /api/shipping/cost?type=Easybox` and `GET /api/shipping/cost?type=Courier`
- `CheckoutStateService.setDelivery({ method, lockerId?, deliveryAddress? })` called on `Continuă`

## Dependencies

### Requires
- Story 001-checkout-stepper (CheckoutStateService)
- Story 003-locker-map-component (Easybox locker selection)
- Bolt 015 (shipping-and-order-core — `/api/shipping/cost` endpoint)

### Enables
- Story 004-order-review-step (needs delivery selection from state)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Shipping cost API unavailable | Show fallback prices (20 RON / 25 RON) with asterisk note |
| Guest user on delivery step | Address form shown without saved-address option |
| Invalid phone number format | Reactive form shows `"Numărul de telefon trebuie să aibă 10 cifre"` |

## Out of Scope

- Courier delivery time estimation
- Multiple saved addresses
- Address autocompletion (Google Maps Places)
