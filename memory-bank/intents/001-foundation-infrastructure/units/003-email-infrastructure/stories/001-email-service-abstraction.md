---
id: 001-email-service-abstraction
unit: 003-email-infrastructure
intent: 001-foundation-infrastructure
status: draft
priority: must
created: 2026-05-05T15:26:00Z
assigned_bolt: null
implemented: false
---

# Story: 001-email-service-abstraction

## User Story

**As a** developer
**I want** a switchable email service abstraction with MailKit and SendGrid implementations
**So that** I can test emails locally with MailHog and send via SendGrid in production without code changes

## Acceptance Criteria

- [ ] **Given** `Email:Provider` is set to `Smtp`, **When** the app starts, **Then** `SmtpEmailService` (MailKit) is registered in DI
- [ ] **Given** `Email:Provider` is set to `SendGrid`, **When** the app starts, **Then** `SendGridEmailService` is registered in DI
- [ ] **Given** an email is sent via either implementation, **When** examined, **Then** it includes BCC to the `Email:OperatorBcc` address
- [ ] **Given** an email is sent via either implementation, **When** examined, **Then** it includes `List-Unsubscribe` header
- [ ] **Given** `EmailSettings` config, **When** loaded, **Then** all fields (FromAddress, FromName, OperatorBcc, provider-specific) are bound correctly

## Technical Notes

- `IEmailService` interface with `SendAsync(string to, string subject, string htmlBody)` and `SendTemplatedAsync<T>(string to, string subject, string templateName, T model)`
- `SmtpEmailService`: uses MailKit NuGet; connects to configured SMTP host/port
- `SendGridEmailService`: uses SendGrid NuGet; sends via API with API key
- `EmailSettings` POCO: bind from `Email` config section
- DI registration: conditional based on `Email:Provider` value
- Both implementations add BCC and List-Unsubscribe header

## Dependencies

### Requires
- None (but benefits from Serilog logging from unit 001)

### Enables
- 002-razor-template-rendering (uses IEmailService for final send)
- 003-email-retry-queue (wraps IEmailService with retry logic)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Invalid Email:Provider value | Throw startup exception with clear message |
| SendGrid API key missing in prod | Throw startup exception |
| MailKit connection refused | Throw exception (caught by retry queue) |

## Out of Scope

- Plain text auto-generation from HTML — future enhancement
- Email tracking/analytics
