---
intent: 006-email-notifications
phase: inception
status: complete
created: 2026-05-22T12:00:00Z
updated: 2026-05-22T12:00:00Z
---

# Requirements: Email Notifications

## Intent Overview

Implement the 4 transactional emails triggered by the FotoTipar order lifecycle: Welcome (on email confirmation), Order Confirmed (on payment success), Order Shipped (on status → Shipped), and Order Delivered (on status → Delivered). The email infrastructure (IEmailService, SmtpEmailService, SendGridEmailService, Razor templates, retry queue) is already implemented in bolt 003-email-infrastructure. This intent wires up the 4 business-level triggers.

## Business Goals

| Goal | Success Metric | Priority |
|------|----------------|----------|
| Customers are informed when their order is confirmed | OrderConfirmed email sent within 30 s of payment webhook | Must |
| Customers receive tracking info when their order ships | OrderShipped email sent on operator status → Shipped | Must |
| Customers receive delivery confirmation | OrderDelivered email sent on operator status → Delivered | Must |
| New users receive a welcoming email | Welcome email sent on email verification | Must |

---

## Functional Requirements

### FR-1: Welcome Email
- **Description**: Triggered when `IsEmailConfirmed` is set to `true`. Sends a personalised welcome to the new user.
- **Acceptance Criteria**: Subject `'Bun venit la FotoTipar!'`; body includes first name, brief service description, "Comandă acum" CTA; Razor HTML template; plain-text fallback.
- **Priority**: Must
- **Related Stories**: US-601

### FR-2: Order Confirmed Email
- **Description**: Triggered by the payment webhook success handler (Stripe or the legacy processor) in the existing `OrderService.CreateFromCartAsync` flow.
- **Acceptance Criteria**: Subject `'Comanda #FT-XXXX a fost primită!'`; items table (format, finish, qty, unit price, line total); delivery address or locker name; total paid; estimated delivery `'2-4 zile lucrătoare'`; guest link `/comanda/{id}?email={email}`; registered user link to order history.
- **Priority**: Must
- **Related Stories**: US-602

### FR-3: Order Shipped Email
- **Description**: Triggered when an admin calls `PATCH /api/admin/orders/{id}/status` with `status=Shipped`.
- **Acceptance Criteria**: Subject `'Comanda #FT-XXXX a fost expediată!'`; AWB number; Sameday tracking URL; locker address (Easybox) or `'La ușa ta'` (courier); delivery window.
- **Priority**: Must
- **Related Stories**: US-603

### FR-4: Order Delivered Email
- **Description**: Triggered when an admin marks order status → Delivered.
- **Acceptance Criteria**: Subject `'Comanda ta a ajuns!'`; confirmation message; "Comandă din nou" CTA; "Contactează-ne" section.
- **Priority**: Must
- **Related Stories**: US-604

---

## Non-Functional Requirements

| Category | Requirement |
|----------|-------------|
| Reliability | Failed sends logged with Serilog; retried up to 3× via existing EmailRetryJob |
| Template | Razor .cshtml in /EmailTemplates/; shared layout with logo and footer |
| BCC | All emails BCC to operator address (config) |
| Deliverability | List-Unsubscribe header on all transactional emails |

---

## Out of Scope

- Email unsubscribe management (preference centre)
- Marketing / promotional emails
- Sameday API integration (AWB auto-generation — Phase 2)
