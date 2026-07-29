---
id: 003-email-infrastructure
unit: 003-email-infrastructure
intent: 001-foundation-infrastructure
type: ddd-construction-bolt
status: complete
stories:
  - 001-email-service-abstraction
  - 002-razor-template-rendering
  - 003-email-retry-queue
created: 2026-05-05T15:30:00Z
started: 2026-05-05T16:35:00Z
completed: 2026-05-19T00:00:00Z
current_stage: done
stages_completed: [domain-model, technical-design, implement, test]

requires_bolts: [001-error-handling-logging]
enables_bolts: []
requires_units: []
blocks: false

complexity:
  avg_complexity: 2
  avg_uncertainty: 1
  max_dependencies: 2
  testing_scope: 2
---

# Bolt: 003-email-infrastructure

## Overview

Build the email delivery infrastructure: switchable IEmailService (MailKit/SendGrid), Razor template rendering, and database-backed retry queue for reliable transactional email delivery.

## Objective

Implement IEmailService with two providers, Razor template engine with shared layout, EmailQueue entity + migration, and EmailRetryJob background service — producing a reliable email system that persists across restarts and supports both development and production delivery.

## Stories Included

- **001-email-service-abstraction**: IEmailService with MailKit and SendGrid implementations (Must)
- **002-razor-template-rendering**: Razor template engine with shared layout (Must)
- **003-email-retry-queue**: Database-backed retry queue with exponential backoff (Must)

## Bolt Type

**DDD Construction Bolt** — 5 stages: Domain Model → Technical Design → Implementation → Testing → Review

## Dependencies

### Bolt Dependencies (within intent)
- **001-error-handling-logging** (Required): Uses Serilog logging for send failures and retry tracking

### Unit Dependencies (cross-unit)
- None

### Enables (other bolts waiting on this)
- None within this intent. Epic 6 (Email Notifications) depends on this infrastructure.

## Expected Outputs

- `src/PhotoPrint.API/Services/IEmailService.cs`
- `src/PhotoPrint.API/Services/SmtpEmailService.cs`
- `src/PhotoPrint.API/Services/SendGridEmailService.cs`
- `src/PhotoPrint.API/Services/RazorTemplateService.cs`
- `src/PhotoPrint.API/BackgroundJobs/EmailRetryJob.cs`
- `src/PhotoPrint.API/Models/EmailQueue.cs`
- `src/PhotoPrint.API/Configuration/EmailSettings.cs`
- `src/PhotoPrint.API/EmailTemplates/_Layout.cshtml`
- EF Core migration for EmailQueue table
- Unit tests for both email implementations, template rendering, and retry logic
- Integration test with MailHog
