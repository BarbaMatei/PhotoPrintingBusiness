# US-703 — Saved Addresses (Frontend)

## Story
**As a** logged-in customer  
**I want to** manage my delivery addresses so I don't have to retype them every order

## Type
FRONTEND — Angular

## Epic
EPIC-7 | Cont Utilizator & Legal

## Dependencies
- US-702 (Account API — address endpoints)
- US-804 (AuthGuard)

## Acceptance Criteria

1. **`/contul-meu/adrese`**: list of saved addresses with Edit / Delete / Set as Default
2. **Add address form**: same fields as checkout home delivery form
3. **Max 5 saved addresses**; default address pre-selected in checkout

## Technical Notes

### Component Location
`src/app/features/account/addresses/addresses.component.ts`

### Implementation Details
- Load addresses: `GET /api/account/addresses` on init
- Display as address cards: street, city, county, postal code; default badge; edit/delete buttons
- Add: form with fields: Stradă, Număr, Bloc/Ap (optional), Oraș, Județ (dropdown), Cod poștal
- Max 5: disable `Adaugă adresă` button when 5 addresses exist; show message
- Set default: call `PUT /api/account/addresses/{id}` with `isDefault=true`; unset previous default
- Delete: confirmation dialog; call `DELETE /api/account/addresses/{id}`
- Reuse address form component from checkout (US-301)

## Files to Create/Modify
- `src/app/features/account/addresses/addresses.component.ts`
- `src/app/features/account/addresses/addresses.component.html`
- `src/app/features/account/addresses/addresses.component.scss`
- `src/app/shared/components/address-form/address-form.component.ts` (shared with checkout)

## Testing
- Unit test: address list displays
- Unit test: add address form validation
- Unit test: max 5 enforcement
- Unit test: set default address
- Unit test: delete address
