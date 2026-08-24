# US-602 — Email — Order Confirmed

## Story
**As a** system  
**I want to** confirm to the customer (registered or guest) that payment was received and order is being processed

## Type
BACKEND — ASP.NET Core

## Epic
EPIC-6 | Notificări Email

## Dependencies
- US-605 (IEmailService)
- US-305 (Payment webhook triggers this)

## Acceptance Criteria

1. **Triggered by**: payment webhook success (Stripe)
2. **Subject**: `Comanda #FT-XXXX a fost primită!`
3. **Body**: customer first name (or `Drag client` for guest), order number, items table (format, finish, qty, unit price, line total), delivery address/locker name, shipping cost, TOTAL paid, estimated delivery `2-4 zile lucrătoare`
4. **Guest email includes**: `Urmărește comanda` link `/comanda/{id}?email={email}`

## Technical Notes

### Implementation Details
- Triggered in payment webhook handler after setting order to Paid
- Template: `/EmailTemplates/OrderConfirmed.cshtml`
- Data model: `{ FirstName, OrderNumber, Items[], DeliveryInfo, ShippingCost, Total, TrackingLink? }`
- Items rendered as HTML table with columns: Fotografie, Format, Finisaj, Cant., Preț, Total
- For guests: `TrackingLink = /comanda/{orderId}?email={email}`
- For registered users: `TrackingLink = /comanda/{orderId}`
- BCC: operator email (from config)

### Email Content Structure (Romanian)
```
Subject: Comanda #FT-20260001 a fost primită!

Bună {FirstName}!

Comanda ta a fost înregistrată și plata confirmată.

[Order items table]

Subtotal: XX,XX RON
Livrare: XX,XX RON
Total: XX,XX RON

Livrare: {Easybox name + address | Home address}
Livrare estimată: 2-4 zile lucrătoare

[Urmărește comanda] (CTA button)
```

## Files to Create/Modify
- `src/PhotoPrint.API/EmailTemplates/OrderConfirmed.cshtml`
- `src/PhotoPrint.API/DTOs/Email/OrderConfirmedEmailModel.cs`
- `src/PhotoPrint.API/Services/OrderService.cs` (queue email after payment confirmed)

## Testing
- Unit test: email queued after Stripe payment success
- Unit test: guest tracking link includes email param
- Unit test: template renders order items table
