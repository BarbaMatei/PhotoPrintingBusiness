---
unit: 003-email-infrastructure
intent: 001-foundation-infrastructure
phase: inception
status: draft
created: 2026-05-05T15:24:00Z
updated: 2026-05-05T15:24:00Z
---

# Unit Brief: Email Infrastructure

## Purpose

Provide a switchable email delivery abstraction (IEmailService) with MailKit for development and SendGrid for production, Razor template rendering, and a database-backed retry queue for reliable delivery.

## Scope

### In Scope
- IEmailService interface with SendAsync and SendTemplatedAsync<T>
- SmtpEmailService (MailKit) for development with MailHog
- SendGridEmailService for production
- Provider switching via config (Email:Provider)
- Operator BCC on all emails
- Razor template rendering (RazorLight) with shared _Layout.cshtml
- EmailQueue table in PostgreSQL for persistent retry
- EmailRetryJob (IHostedService) with exponential backoff (1s, 4s, 16s), max 3 attempts
- EmailSettings configuration POCO

### Out of Scope
- Specific email templates (welcome, order confirmed, etc.) — created in Epic 6
- Background jobs for guest cleanup, orphan files — deferred to US-803
- Email content/copy — defined per email story

---

## Assigned Requirements

| FR | Requirement | Priority |
|----|-------------|----------|
| FR-15 | IEmailService abstraction (MailKit ↔ SendGrid) | Must |
| FR-16 | Email Razor templates with shared layout | Must |
| FR-17 | Database-backed email retry queue | Must |

---

## Domain Concepts

### Key Entities
| Entity | Description | Attributes |
|--------|-------------|------------|
| EmailMessage | Queued email record | Id, To, Subject, HtmlBody, Status, Attempts, NextRetryAt, CreatedAt |
| EmailSettings | Configuration POCO | Provider, FromAddress, FromName, OperatorBcc, Smtp.*, SendGrid.* |

### Key Operations
| Operation | Description | Inputs | Outputs |
|-----------|-------------|--------|---------|
| SendAsync | Send plain email | to, subject, htmlBody | success/queued for retry |
| SendTemplatedAsync | Render Razor template + send | to, subject, templateName, model | success/queued for retry |
| RetryFailedEmails | Background job: process retry queue | EmailQueue records | delivered or max-retries-exceeded |

---

## Story Summary

| Metric | Count |
|--------|-------|
| Total Stories | 3 |
| Must Have | 3 |
| Should Have | 0 |
| Could Have | 0 |

### Stories

| Story ID | Title | Priority | Status |
|----------|-------|----------|--------|
| 001-email-service-abstraction | IEmailService with MailKit and SendGrid implementations | Must | Planned |
| 002-razor-template-rendering | Razor template engine with shared layout | Must | Planned |
| 003-email-retry-queue | Database-backed retry queue with exponential backoff | Must | Planned |

---

## Dependencies

### Depends On
| Unit | Reason |
|------|--------|
| 001-error-handling-logging | Uses Serilog logging for send failures and retry tracking |

### Depended By
None within this intent. Epic 6 (Email Notifications) depends on this infrastructure.
