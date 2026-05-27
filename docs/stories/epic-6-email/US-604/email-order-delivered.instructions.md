# US-604 — Email — Order Delivered

## Story
**As a** system  
**I want to** confirm delivery and invite the customer to review or reorder

## Type
BACKEND — ASP.NET Core

## Epic
EPIC-6 | Notificări Email

## Dependencies
- US-605 (IEmailService)
- US-504 (Admin status change triggers this)

## Acceptance Criteria

1. **Triggered by**: order status → `Delivered`
2. **Subject**: `Comanda ta a ajuns!`
3. **Body**: confirmation message, `Comandă din nou` CTA, `Contactează-ne dacă ai probleme` section

## Technical Notes

### Implementation Details
- Triggered in AdminOrderService when status changes to Delivered
- Template: `/EmailTemplates/OrderDelivered.cshtml`
- Data model: `{ FirstName, OrderNumber, ReorderUrl, ContactEmail }`

### Email Content (Romanian)
```
Subject: Comanda ta a ajuns!

Bună {FirstName}!

Comanda #FT-XXXX a fost livrată cu succes. 
Sperăm că ești mulțumit(ă) de fotografiile tale!

[Comandă din nou] (CTA button)

Dacă ai întâmpini probleme, contactează-ne la {contactEmail}.
```

## Files to Create/Modify
- `src/PhotoPrint.API/EmailTemplates/OrderDelivered.cshtml`
- `src/PhotoPrint.API/DTOs/Email/OrderDeliveredEmailModel.cs`
- `src/PhotoPrint.API/Services/AdminOrderService.cs` (queue email on Delivered)

## Testing
- Unit test: email queued on status change to Delivered
- Unit test: template renders with correct data
