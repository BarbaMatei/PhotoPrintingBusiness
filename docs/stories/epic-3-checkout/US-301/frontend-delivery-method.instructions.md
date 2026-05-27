# US-301 — Checkout — Delivery Method (Frontend)

## Story
**As a** customer  
**I want to** choose between Easybox locker pickup or home delivery and provide the address

## Type
FRONTEND — Angular

## Epic
EPIC-3 | Checkout & Plată

## Dependencies
- US-302 (Shipping API backend)
- US-205 (Cart must have items)
- US-108/US-104 (Authentication gate)

## Acceptance Criteria

1. **Step 1 of checkout**: two cards — `Easybox Sameday` (cheaper) and `Livrare la ușă`
2. **Easybox flow**: city search input → list of nearest lockers (name, address, distance from searched city)
3. **Locker list** rendered with **Leaflet.js + OpenStreetMap** map showing pins; selected locker highlighted
4. **Home delivery flow**: form — Stradă, Număr, Bloc/Ap (optional), Oraș, Județ (dropdown), Cod poștal
5. **Saved addresses** shown for logged-in users (select or add new)
6. **Guest users** always fill the form; option to save is hidden
7. **Shipping cost** shown per option (from `/api/shipping/cost`)

## Technical Notes

### Component Location
`src/app/features/checkout/delivery-step/delivery-step.component.ts`

### Implementation Details
- Stepper component: Step 1 (Delivery) → Step 2 (Review) → Step 3 (Payment)
- Two delivery cards with radio selection; show shipping cost on each card
- **Easybox**:
  - City search input with debounce (300ms)
  - Call `GET /api/shipping/lockers?city={query}`
  - Display results in list AND on Leaflet map with pins
  - On pin/list item click: select locker, highlight on map
  - Install `leaflet` + `@types/leaflet` npm packages
  - OpenStreetMap tiles (free, no API key needed)
- **Home delivery**:
  - Reactive form with all address fields
  - Județ dropdown: hardcoded list of Romanian counties (41 + Bucharest)
  - For logged-in users: show saved addresses from `GET /api/account/addresses`; allow selecting one or adding new
- Shipping cost: call `GET /api/shipping/cost?type=Easybox|Courier` and display next to each option
- Store selected delivery method + address/locker in checkout state

### UI/UX
- Map height: ~300px; zoom to show all locker pins in selected city
- Selected locker card: highlighted border + checkmark
- Address form: Romanian field labels
- `Continuă` button to proceed to Step 2; disabled until delivery method + address/locker selected

## Files to Create/Modify
- `src/app/features/checkout/delivery-step/delivery-step.component.ts`
- `src/app/features/checkout/delivery-step/delivery-step.component.html`
- `src/app/features/checkout/delivery-step/delivery-step.component.scss`
- `src/app/features/checkout/locker-map/locker-map.component.ts`
- `src/app/features/checkout/checkout-stepper/checkout-stepper.component.ts`
- `src/app/core/services/shipping.service.ts`
- `src/app/core/models/shipping.model.ts`

## Testing
- Unit test: delivery method selection
- Unit test: locker search and selection
- Unit test: address form validation
- Unit test: saved address selection
- Unit test: shipping cost display
- E2E: select Easybox → pick locker → continue
- E2E: select home delivery → fill form → continue
