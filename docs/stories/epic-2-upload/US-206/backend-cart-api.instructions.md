# US-206 — Cart API (Backend)

## Story
**As a** system  
**I want to** store and retrieve cart state server-side for logged-in users

## Type
BACKEND — ASP.NET Core

## Epic
EPIC-2 | Upload Fotografii & Selecție Format

## Dependencies
- US-105/US-109 (Auth — JWT or guest token)
- US-202 (Uploads exist)
- US-204 (Products exist)

## Acceptance Criteria

1. **`POST /api/cart`** — replace full cart: `{productId, finish, items:[{uploadId, quantity}]}`
2. **`GET /api/cart`** — returns cart with computed totals
3. **`DELETE /api/cart`** — clears cart
4. **`POST /api/cart/merge`** — merges guest/localStorage cart into user cart on login
5. **Validates**: uploadId belongs to session/user; productId is active; quantity 1–100
6. **Cart scoped** by `userId` OR `guestSessionId`

## Technical Notes

### Endpoints
```
POST /api/cart
Authorization: Bearer {jwt} OR X-Guest-Token: {uuid}
{
  "productId": "uuid",
  "items": [{ "uploadId": "uuid", "quantity": 3 }]
}
→ 200 { cart object with totals }
```

```
GET /api/cart
→ 200 {
  "productId": "uuid",
  "productName": "10×15 Lucios",
  "items": [{ "uploadId": "uuid", "quantity": 3, "previewUrl": "...", "unitPrice": 0.50, "lineTotal": 1.50 }],
  "subtotal": 15.00,
  "itemCount": 10
}
→ 200 { "items": [], "subtotal": 0, "itemCount": 0 } (empty cart)
```

```
DELETE /api/cart
→ 204 No Content
```

```
POST /api/cart/merge
Authorization: Bearer {jwt}
{ "guestCart": { "productId": "uuid", "items": [{ "uploadId": "uuid", "quantity": 1 }] } }
→ 200 { merged cart }
```

### Implementation Details
- `CartItems` table: `Id`, `UserId?`, `GuestSessionId?`, `UploadId→Uploads`, `ProductId→Products`, `Quantity`, `AddedAt`
- POST /api/cart: replace strategy — delete all existing cart items for user/session, insert new ones (transactional)
- GET /api/cart: join with Products for prices, compute line totals and subtotal
- Merge: on login, combine guest cart items with existing user cart; if same uploadId exists, keep user's version; transfer guest uploads to user
- Validation: uploadId must belong to the requesting user/session; productId must be active; quantity between 1 and 100

## Files to Create/Modify
- `src/PhotoPrint.API/Controllers/CartController.cs`
- `src/PhotoPrint.API/DTOs/Cart/CartRequest.cs`
- `src/PhotoPrint.API/DTOs/Cart/CartResponse.cs`
- `src/PhotoPrint.API/Models/CartItem.cs`
- `src/PhotoPrint.API/Services/ICartService.cs` + `CartService.cs`
- EF Core migration for CartItems

## Testing
- Unit test: set cart replaces existing items
- Unit test: get cart computes totals correctly
- Unit test: clear cart removes all items
- Unit test: merge combines guest and user carts
- Unit test: validation — foreign upload rejected
- Integration test: full cart lifecycle
