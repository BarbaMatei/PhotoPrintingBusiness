# US-306 — Payment — EuPlatesc Backend

## Story
**As a** system  
**I want to** initiate EuPlatesc payment sessions and confirm orders via EuPlatesc IPN callback

## Type
BACKEND — ASP.NET Core

## Epic
EPIC-3 | Checkout & Plată

## Dependencies
- US-305 (Order entity must exist — shared Order model)
- US-206 (Cart)
- US-801 (Error handling)

## Acceptance Criteria

1. **`POST /api/payments/euplatesc/initiate`** — builds EuPlatesc payment request (amount, orderId, returnUrl, cancelUrl, ipnUrl); returns `{redirectUrl}`
2. **EuPlatesc IPN endpoint**: `POST /api/webhooks/euplatesc` — validates HMAC signature using EuPlatesc merchant key
3. **On success IPN**: Order `status=Paid`; fires `OrderConfirmedEmail`
4. **On failure IPN**: Order `status=PaymentFailed`
5. **Stores** `EuPlatescTransactionId` on order for reconciliation
6. **IPN endpoint is public** (no JWT); protected by HMAC validation only

## Technical Notes

### Endpoints
```
POST /api/payments/euplatesc/initiate
Authorization: Bearer {jwt} OR X-Guest-Token: {uuid}
→ 200 { "redirectUrl": "https://secure.euplatesc.ro/tdsprocess/tranzactd.php?...", "orderId": "uuid" }
```

```
POST /api/webhooks/euplatesc
Content-Type: application/x-www-form-urlencoded
(EuPlatesc IPN fields)
→ 200 (plain text response as per EuPlatesc spec)
```

### Implementation Details
- EuPlatesc integration:
  - Build payment form data: `amount`, `curr` (RON), `invoice_id` (orderId), `order_desc`, `merch_id`, `timestamp`, `nonce`
  - Generate HMAC-MD5 signature using merchant key
  - Redirect URL: EuPlatesc hosted payment page with form data as query params
  - Return URL: `{frontendUrl}/comanda/{orderId}/confirmare?processor=euplatesc`
  - IPN URL: `{backendUrl}/api/webhooks/euplatesc`
- IPN handler:
  - Read all form fields from POST body
  - Validate HMAC signature against merchant key
  - Extract `action` field: `0` = success, other = failure
  - On success: find order by invoice_id → set Paid → store `EuPlatescTransactionId` → fire email
  - On failure: set PaymentFailed
  - Return `<epayment>date|hash</epayment>` response as per EuPlatesc spec
- Create PendingOrder with `PaymentProcessor=EuPlatesc` before redirect
- Config: `EuPlatesc:MerchantId`, `EuPlatesc:SecretKey` in environment variables

### Security
- IPN endpoint: no JWT required, but HMAC signature MUST be validated
- Never trust return URL for order status — only trust IPN callback
- Validate that amount in IPN matches order amount

## Files to Create/Modify
- `src/PhotoPrint.API/Controllers/PaymentsController.cs` (EuPlatescInitiate)
- `src/PhotoPrint.API/Controllers/WebhooksController.cs` (EuPlatescIpn)
- `src/PhotoPrint.API/DTOs/Payment/EuPlatescInitiateResponse.cs`
- `src/PhotoPrint.API/Services/IEuPlatescService.cs` + `EuPlatescService.cs`

## Testing
- Unit test: payment request HMAC generation
- Unit test: IPN signature validation
- Unit test: successful IPN → order Paid
- Unit test: failed IPN → order PaymentFailed
- Unit test: amount mismatch rejection
- Integration test: initiate + IPN flow
