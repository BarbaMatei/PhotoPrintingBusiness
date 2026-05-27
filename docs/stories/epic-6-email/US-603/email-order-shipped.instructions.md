# US-603 — Email — Order Shipped

## Story
**As a** system  
**I want to** notify the customer with tracking details when the order is dispatched

## Type
BACKEND — ASP.NET Core

## Epic
EPIC-6 | Notificări Email

## Dependencies
- US-605 (IEmailService)
- US-504 (Admin status change triggers this)

## Acceptance Criteria

1. **Triggered by**: order status → `Shipped`
2. **Subject**: `Comanda #FT-XXXX a fost expediată!`
3. **Body**: AWB number, `Urmărește coletul pe Sameday` button with tracking URL, locker address (if Easybox) or `La ușa ta` (if courier), delivery window

## Technical Notes

### Implementation Details
- Triggered in AdminOrderService when status changes to Shipped
- Template: `/EmailTemplates/OrderShipped.cshtml`
- Data model: `{ FirstName, OrderNumber, AwbNumber, TrackingUrl, DeliveryType, DeliveryAddress }`
- Tracking button: links to Sameday tracking URL

### Email Content (Romanian)
```
Subject: Comanda #FT-20260001 a fost expediată!

Bună {FirstName}!

Comanda ta a fost expediată prin Sameday.

AWB: {AwbNumber}
Livrare: {Easybox name + address | La ușa ta: address}
Livrare estimată: 1-2 zile lucrătoare

[Urmărește coletul] (CTA button → trackingUrl)
```

## Files to Create/Modify
- `src/PhotoPrint.API/EmailTemplates/OrderShipped.cshtml`
- `src/PhotoPrint.API/DTOs/Email/OrderShippedEmailModel.cs`
- `src/PhotoPrint.API/Services/AdminOrderService.cs` (queue email on Shipped)

## Testing
- Unit test: email queued on status change to Shipped
- Unit test: tracking URL included in template
- Unit test: delivery type correctly shown (Easybox vs courier)
