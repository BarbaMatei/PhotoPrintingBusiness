---
id: 020-transactional-emails
unit: 001-transactional-emails
intent: 006-email-notifications
type: ddd-construction-bolt
status: complete
stories:
  - 001-welcome-email
  - 002-order-confirmed-email
  - 003-order-shipped-email
  - 004-order-delivered-email
created: 2026-05-22T12:00:00Z
started: 2026-05-22T14:00:00Z
completed: 2026-05-22T15:30:00Z
current_stage: null
stages_completed: [domain-model, technical-design, implement, test]

requires_bolts: [003-email-infrastructure, 018-orders-api]
enables_bolts: [021-admin-api]
requires_units: []
blocks: false

complexity:
  avg_complexity: 1
  avg_uncertainty: 1
  max_dependencies: 2
  testing_scope: 2
---

# Bolt: 020-transactional-emails

## Overview

Wire the 4 lifecycle email triggers (Welcome, OrderConfirmed, OrderShipped, OrderDelivered) to the existing `IEmailService` infrastructure and create Razor HTML templates for each.

## Objective

By the end of this bolt every key order lifecycle event and user registration event fires a transactional email to the customer with correct Romanian content.

## Stories Included

- **001-welcome-email**: Triggered on `IsEmailConfirmed=true` — greets new user (Must)
- **002-order-confirmed-email**: Triggered on payment webhook success — full order summary (Must)
- **003-order-shipped-email**: Triggered on admin status → Shipped — AWB + tracking link (Must)
- **004-order-delivered-email**: Triggered on admin status → Delivered — delivery confirmation (Must)

## Bolt Type

`ddd-construction-bolt` — backend domain work with service method additions and Razor templates.

## Stage Plan

| Stage | Name | Output |
|-------|------|--------|
| 1 | Domain Model | `ddd-01-domain-model.md` — email trigger points, template data models |
| 2 | Technical Design | `ddd-02-technical-design.md` — service interface extensions, call sites, template structure |
| 3 | Implement | `IEmailService` new methods, Razor templates, call-site wiring |
| 4 | Test | `ddd-03-test-report.md` — unit tests for email trigger logic |

## Dependencies

- **Requires**: bolt `003-email-infrastructure` (IEmailService, SmtpEmailService, _EmailLayout — ✅ complete)
- **Requires**: bolt `018-orders-api` (Order aggregate fully populated — ✅ complete)
- **Enables**: bolt `021-admin-api` (shipped/delivered email called from admin status endpoint)
