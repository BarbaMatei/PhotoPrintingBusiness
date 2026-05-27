---
unit: 003-email-infrastructure
bolt: 003-email-infrastructure
stage: design
status: complete
updated: 2026-05-05T16:42:00Z
---

# Technical Design - Email Infrastructure

## Architecture Pattern

**Pattern**: Decorator + Background Service

`IEmailService` (public interface) is implemented by `ReliableEmailService`, a decorator that wraps a raw `IEmailSender` (SMTP or SendGrid). On failure, the decorator silently enqueues to `EmailQueue` (PostgreSQL). `EmailRetryJob` polls the queue and uses the raw `IEmailSender` directly, ensuring no double-queueing.

## Layer Structure

```text
Presentation  → Controllers call IEmailService (e.g., after order)
Application   → ReliableEmailService (decorator: try → queue on failure)
Domain        → EmailQueue entity, EmailStatus enum, RetryPolicy
Infrastructure→ SmtpEmailService, SendGridEmailService, RazorTemplateService, EmailRetryJob
```

## File Structure

```text
src/PhotoPrint.API/
├── Configuration/EmailSettings.cs
├── Services/
│   ├── IEmailSender.cs
│   ├── IEmailService.cs
│   ├── IRazorTemplateService.cs
│   ├── SmtpEmailService.cs
│   ├── SendGridEmailService.cs
│   ├── ReliableEmailService.cs
│   └── RazorTemplateService.cs
├── Models/EmailQueue.cs
├── BackgroundJobs/EmailRetryJob.cs
├── EmailTemplates/_Layout.cshtml
└── Extensions/EmailExtensions.cs
```

## Interfaces

| Interface | Implemented By | Registered As |
|-----------|---------------|---------------|
| `IEmailSender` | SmtpEmailService, SendGridEmailService | Keyed "email-provider-raw" |
| `IEmailService` | ReliableEmailService | Default scoped |
| `IRazorTemplateService` | RazorTemplateService | Singleton |

## DI Registration (keyed, .NET 8)

```csharp
// Provider chosen by Email:Provider config ("Smtp" or "SendGrid")
services.AddKeyedScoped<IEmailSender, SmtpEmailService>("email-provider-raw");
// OR
services.AddKeyedScoped<IEmailSender, SendGridEmailService>("email-provider-raw");

services.AddSingleton<IRazorTemplateService, RazorTemplateService>();
services.AddScoped<IEmailService, ReliableEmailService>();
services.AddHostedService<EmailRetryJob>();
```

## Data Persistence

| Table | Columns | Index |
|-------|---------|-------|
| `email_queue` | Id (uuid PK), To (text), Subject (text), HtmlBody (text), Status (text), Attempts (int), NextRetryAt (timestamptz), CreatedAt (timestamptz), SentAt (timestamptz?), LastError (text?) | (Status, NextRetryAt) for polling |

## Retry Policy

| Attempt | Delay Before | Handled By |
|---------|-------------|------------|
| 0 → 1 | 1 second | ReliableEmailService (initial queue) |
| 1 → 2 | 4 seconds | EmailRetryJob |
| 2 → 3 | 16 seconds | EmailRetryJob |
| 3 (max) | — | Status → Failed |

Formula: `delay = 1s × 4^attempt_index`

## Security Design

| Concern | Approach |
|---------|----------|
| API keys | Never in source; only from `IOptions<EmailSettings>` bound from env vars |
| BCC monitoring | All emails BCC to `Email:OperatorBcc` — configurable, not hardcoded |
| List-Unsubscribe | RFC 2369 header on every email, reducing spam classification risk |
| HTML injection | HtmlBody is already-rendered HTML from Razor templates; callers are responsible for escaping model data in templates |

## NFR Implementation

| Requirement | Approach |
|-------------|---------|
| Reliability | DB-backed queue survives process restarts; max 3 attempts |
| Observability | Serilog structured logs on every send, failure, retry, exhaustion |
| Dev/Prod parity | Provider switch via single config key; MailHog catches SMTP locally |
| Performance | Batch size=10; poll every 10s; RazorLight uses in-memory compilation cache |
