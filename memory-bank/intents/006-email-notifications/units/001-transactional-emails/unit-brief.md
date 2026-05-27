---
unit: 001-transactional-emails
intent: 006-email-notifications
phase: inception
status: ready
created: 2026-05-22T12:00:00Z
updated: 2026-05-22T12:00:00Z
default_bolt_type: ddd-construction-bolt
---

# Unit Brief: 001-transactional-emails

## Purpose

Wire the 4 business-lifecycle email triggers to `IEmailService` and create Razor templates for each email type.

## Scope

### In Scope
- `IEmailService.SendWelcomeEmailAsync(User user)` — trigger: email confirmed
- `IEmailService.SendOrderConfirmedEmailAsync(Order order, string? email)` — trigger: payment webhook success
- `IEmailService.SendOrderShippedEmailAsync(Order order)` — trigger: status → Shipped
- `IEmailService.SendOrderDeliveredEmailAsync(Order order)` — trigger: status → Delivered
- Razor templates: `Welcome.cshtml`, `OrderConfirmed.cshtml`, `OrderShipped.cshtml`, `OrderDelivered.cshtml`
- Integration with `_EmailLayout.cshtml` shared layout (logo, footer, BCC, List-Unsubscribe)
- Call sites: AuthService (welcome), OrderService (order confirmed), AdminService (shipped/delivered)

### Out of Scope
- Email infrastructure itself (bolt 003-email-infrastructure — complete)
- EmailRetryJob (bolt 025-background-jobs)
- Marketing emails

---

## Domain Concepts

### Key Entities
| Entity | Description | Attributes Used |
|--------|-------------|-----------------|
| `User` | Registered customer | FirstName, Email |
| `Order` | Customer order | OrderNumber, Status, TotalRon, Items, ShippingAddress/Locker, PaymentProcessor, AwbNumber, TrackingUrl |
| `OrderItem` | Line item | ProductSnapshot (Name, Size, Finish), Quantity, UnitPriceRon, LineTotalRon |

### Key Operations
| Operation | Trigger | Service Method |
|-----------|---------|----------------|
| Send welcome | Email confirmation endpoint sets IsEmailConfirmed=true | `IEmailService.SendWelcomeEmailAsync` |
| Send order confirmed | Payment webhook success handler | `IEmailService.SendOrderConfirmedEmailAsync` |
| Send order shipped | Admin PATCH status → Shipped | `IEmailService.SendOrderShippedEmailAsync` |
| Send order delivered | Admin PATCH status → Delivered | `IEmailService.SendOrderDeliveredEmailAsync` |

---

## Technical Constraints

- `IEmailService` already exists (bolt 003) — only add new method signatures
- Razor templates in `src/PhotoPrint.API/EmailTemplates/`
- Shared layout `_EmailLayout.cshtml` already exists (bolt 003)
- All email sends are fire-and-forget (`_ = service.SendAsync(...)`) — do not block request
- Guest orders use stored email from `Order.GuestEmail` (nullable string on Order) or `GuestSession.Email`

## Story Summary

| # | Story | Priority |
|---|-------|----------|
| 001 | `001-welcome-email` | Must |
| 002 | `002-order-confirmed-email` | Must |
| 003 | `003-order-shipped-email` | Must |
| 004 | `004-order-delivered-email` | Must |
