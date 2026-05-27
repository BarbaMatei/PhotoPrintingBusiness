# US-504 — Admin — API (Backend)

## Story
**As a** system  
**I want to** expose admin-only endpoints for full order management, file access, and analytics

## Type
BACKEND — ASP.NET Core

## Epic
EPIC-5 | Panou Admin

## Dependencies
- US-305/US-306 (Orders + Payment infrastructure)
- US-802 (Security — Admin role authorization)
- US-605 (IEmailService for status change notifications)

## Acceptance Criteria

1. **All `/api/admin/*`** endpoints require `[Authorize(Roles='Admin')]`; `403` otherwise
2. **`GET /api/admin/orders`** — paginated, all users, `?status=&search=&page=&pageSize=`
3. **`GET /api/admin/orders/{id}`** — full detail + internalNotes
4. **`PATCH /api/admin/orders/{id}/status {status, awbNumber?}`** — validates transitions: Paid→Printing→Shipped→Delivered; any→Cancelled
5. **`GET /api/admin/orders/{id}/download-zip`** — streams ZIP (`System.IO.Compression`) of all order photo files
6. **`POST /api/admin/orders/{id}/cancel`** — initiates Stripe refund OR EuPlatesc refund based on `PaymentProcessor` field; sets `status=Cancelled`
7. **`PATCH /api/admin/orders/{id}/notes {notes}`** — saves internal note
8. **SignalR `AdminOrderHub`**: broadcasts `NewOrderReceived` and `OrderStatusChanged` to admin clients on order events

## Technical Notes

### Endpoints
```
GET /api/admin/orders?status=Paid&search=FT-2026&page=1&pageSize=20
→ 200 { items: [AdminOrderDto], totalCount, page, pageSize }

GET /api/admin/orders/{id}
→ 200 { AdminOrderDto with internalNotes }

PATCH /api/admin/orders/{id}/status
{ "status": "Printing" }
{ "status": "Shipped", "awbNumber": "SAM123456" }
→ 200 { updated order }
→ 400 { "message": "Tranziție de status invalidă" }

GET /api/admin/orders/{id}/download-zip
→ 200 (application/zip stream)

POST /api/admin/orders/{id}/cancel
{ "reason": "Cererea clientului" }
→ 200 { "refundInitiated": true }

PATCH /api/admin/orders/{id}/notes
{ "notes": "Clientul a solicitat ambalare specială" }
→ 200
```

### Implementation Details
- **Status transitions** (state machine from Appendix D):
  - Valid: AwaitingPayment→Paid (webhook only), Paid→Printing, Printing→Shipped, Shipped→Delivered
  - Cancel: from Paid or Printing → Cancelled
  - All other transitions → 400
- **ZIP download**: use `System.IO.Compression.ZipArchive`; stream directly to response (no temp file); set `Content-Disposition: attachment; filename="order-{number}-photos.zip"`
- **Cancel + refund**: check `PaymentProcessor` field on order; call `StripePaymentService.RefundAsync()` or `EuPlatescService.RefundAsync()` accordingly; set status=Cancelled; send cancellation email
- **SignalR hub**: `AdminOrderHub` — requires Admin role; methods: none (server→client only); broadcasts on order creation and status change
- **Internal notes**: free-text field on Orders table, only returned in admin detail endpoint

### Database
- Add `InternalNotes` (text, nullable) to Orders table if not already present

## Files to Create/Modify
- `src/PhotoPrint.API/Controllers/AdminController.cs`
- `src/PhotoPrint.API/DTOs/Admin/AdminOrderDto.cs`
- `src/PhotoPrint.API/DTOs/Admin/UpdateStatusRequest.cs`
- `src/PhotoPrint.API/DTOs/Admin/CancelOrderRequest.cs`
- `src/PhotoPrint.API/Services/IAdminOrderService.cs` + `AdminOrderService.cs`
- `src/PhotoPrint.API/Hubs/AdminOrderHub.cs`
- `src/PhotoPrint.API/Services/OrderStatusMachine.cs` (state transition validator)

## Testing
- Unit test: status transition validation (all valid + invalid transitions)
- Unit test: ZIP generation with files
- Unit test: cancel + refund flow (Stripe)
- Unit test: cancel + refund flow (EuPlatesc)
- Unit test: SignalR broadcast on status change
- Unit test: admin authorization enforcement
- Integration test: full admin order workflow
