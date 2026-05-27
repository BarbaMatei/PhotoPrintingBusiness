---
unit: 003-email-infrastructure
bolt: 003-email-infrastructure
stage: model
status: complete
updated: 2026-05-05T16:40:00Z
---

# Static Model - Email Infrastructure

## Bounded Context

The **Email** bounded context owns the reliable delivery of transactional HTML emails to customers and operators. It is infrastructure — no business domain logic lives here. It provides two capabilities:

1. **Email Delivery Abstraction** — `IEmailService` that hides the difference between SMTP (dev/MailHog) and SendGrid (prod)
2. **Reliable Delivery** — an `EmailQueue` aggregate that persists failed sends and retries them with exponential backoff

This context is consumed by all other contexts (auth, orders, admin) but does not consume any other domain context. Its only infrastructure dependency is `PhotoPrintDbContext` and `Serilog` (from unit 001).

---

## Domain Entities

| Entity | Properties | Business Rules |
|--------|------------|----------------|
| **EmailQueue** | `Id` (Guid), `To` (string), `Subject` (string), `HtmlBody` (string), `Status` (enum: Pending/Sent/Failed), `Attempts` (int), `NextRetryAt` (DateTimeOffset), `CreatedAt` (DateTimeOffset), `SentAt` (DateTimeOffset?), `LastError` (string?) | Status transitions: Pending → Sent (on success) or Pending → Pending (backoff, attempt < 3) or Pending → Failed (attempt = 3); `Attempts` is monotonically increasing; `NextRetryAt` must always be a future time on failure |

---

## Value Objects

| Value Object | Properties | Constraints |
|--------------|------------|-------------|
| **EmailStatus** | Enum: `Pending`, `Sent`, `Failed` | Immutable states; terminal states are `Sent` and `Failed` — once in terminal state, record is never updated again |
| **EmailSettings** | `Provider` (string: "Smtp"/"SendGrid"), `FromAddress` (string), `FromName` (string), `OperatorBcc` (string), `Smtp` (SmtpSettings), `SendGrid` (SendGridSettings) | `Provider` must be exactly "Smtp" or "SendGrid"; if "SendGrid", `SendGrid.ApiKey` must be non-empty; if "Smtp", `Smtp.Host` must be non-empty |
| **SmtpSettings** | `Host` (string), `Port` (int), `UseSsl` (bool), `Username` (string?), `Password` (string?) | `Port` must be in range 1–65535 |
| **SendGridSettings** | `ApiKey` (string) | Must be non-empty in production |
| **RetryPolicy** | Fixed: maxAttempts=3, delays=[1s, 4s, 16s] | Backoff formula: `delay = 1s × 4^(attempt-1)`. Immutable — not configurable at runtime |

---

## Aggregates

| Aggregate Root | Members | Invariants |
|----------------|---------|------------|
| **EmailQueue** | `Status`, `Attempts`, `NextRetryAt`, `SentAt`, `LastError` | Cannot transition out of `Sent` or `Failed`; `Attempts` cannot exceed 3; `SentAt` is only set on `Sent` transition; `LastError` is set on each failed attempt and cleared on `Sent` |

---

## Domain Events

| Event | Trigger | Payload |
|-------|---------|---------|
| **EmailSendSucceeded** | `IEmailService.SendAsync` completes without exception | `To`, `Subject`, `Provider` |
| **EmailSendFailed** | `IEmailService.SendAsync` throws | `To`, `Subject`, `Provider`, `ExceptionType`, `Message`, `WillRetry` (bool) |
| **EmailQueuedForRetry** | Failed send is written to `EmailQueue` | `EmailQueueId`, `To`, `Subject`, `Attempts` |
| **EmailRetrySucceeded** | Retry send completes | `EmailQueueId`, `To`, `Attempts` |
| **EmailRetryExhausted** | Attempt count reaches 3 and still failing | `EmailQueueId`, `To`, `LastError` |

*All events are logged via Serilog (structured log entries), not published to an event bus.*

---

## Domain Services

| Service | Operations | Dependencies |
|---------|------------|--------------|
| **IEmailService** | `SendAsync(string to, string subject, string htmlBody): Task` — delivers email immediately via configured provider; throws on failure | Provider implementation (MailKit or SendGrid) |
| **SmtpEmailService** | Implements `IEmailService`; connects to SMTP via MailKit; adds BCC + `List-Unsubscribe` header | `IOptions<EmailSettings>`, `ILogger` |
| **SendGridEmailService** | Implements `IEmailService`; sends via SendGrid REST API; adds BCC + `List-Unsubscribe` header | `IOptions<EmailSettings>`, `ILogger` |
| **IRazorTemplateService** | `RenderAsync<T>(string templateName, T model): Task<string>` — renders a named Razor template with a typed model, returning HTML | RazorLight engine, file system templates |
| **RazorTemplateService** | Implements `IRazorTemplateService`; wraps `RazorLightEngine`; templates loaded from `EmailTemplates/` directory | `RazorLightEngine` |
| **EmailRetryJob** | `BackgroundService`; polls every 10s for `EmailQueue` records where `Status = Pending AND NextRetryAt <= now`; processes in batches of 10; increments `Attempts`, applies backoff, marks `Sent` or `Failed` | `IServiceScopeFactory` (for scoped `DbContext`), `IEmailService`, `ILogger` |

---

## Repository Interfaces

| Repository | Entity | Methods |
|------------|--------|---------|
| **IEmailQueueRepository** *(implicit via DbContext)* | `EmailQueue` | `AddAsync(EmailQueue)`, `GetPendingAsync(DateTimeOffset now, int batch): Task<List<EmailQueue>>`, `SaveChangesAsync()` |

*No separate repository class — `EmailRetryJob` uses `PhotoPrintDbContext` directly via `IServiceScopeFactory`. This is intentional for simplicity at MVP.*

---

## Ubiquitous Language

| Term | Definition |
|------|------------|
| **IEmailService** | The domain interface for sending a single email. Only concern is delivery — does not know about retry, templates, or queue. |
| **EmailQueue** | A persisted record of an email that failed to send and must be retried. Not every email goes through the queue — only failed ones. |
| **EmailRetryJob** | A long-running background service (`BackgroundService`) that continuously polls `EmailQueue` for pending items and attempts re-delivery. |
| **Provider** | The concrete email delivery mechanism — either "Smtp" (MailKit, for dev/MailHog) or "SendGrid" (for production). |
| **Exponential Backoff** | A retry delay strategy where each failed attempt waits longer than the last: 1s → 4s → 16s. Prevents hammering a failing mail service. |
| **OperatorBcc** | A configurable email address that receives a silent blind copy of every outgoing email. Used for operational monitoring. |
| **RazorTemplateService** | A service that renders `.cshtml` files with a typed model using RazorLight. Returns rendered HTML, which is then passed to `IEmailService.SendAsync`. |
| **List-Unsubscribe** | An RFC 2369 email header that allows compliant mail clients to offer a one-click unsubscribe button. Reduces spam reports. |
| **MailHog** | A local SMTP trap that captures emails in development without delivering them. Accessed via Docker Compose. |

---

## Provider Selection Logic

```text
Config: Email:Provider
  ├── "Smtp"     → register SmtpEmailService as IEmailService
  ├── "SendGrid" → register SendGridEmailService as IEmailService
  └── anything else → throw InvalidOperationException at startup
```

---

## EmailQueue State Machine

```text
[Created] ──► Pending
                │
                ├── send succeeds ──► Sent (terminal)
                │
                ├── send fails, attempts < 3 ──► Pending (with new NextRetryAt)
                │
                └── send fails, attempts = 3 ──► Failed (terminal)
```

---

## Template Rendering Flow

```text
caller
  │
  ▼
IEmailService.SendTemplatedAsync<T>(to, subject, templateName, model)
  │
  ▼
IRazorTemplateService.RenderAsync<T>(templateName, model)
  │  loads EmailTemplates/{templateName}.cshtml
  │  wraps in _Layout.cshtml
  │  renders with RazorLight
  ▼
string htmlBody
  │
  ▼
IEmailService.SendAsync(to, subject, htmlBody)
  │
  ▼
Provider (SmtpEmailService / SendGridEmailService)
```
