---
unit: 003-email-infrastructure
bolt: 003-email-infrastructure
stage: test
status: complete
updated: 2026-05-19T00:00:00Z
---

# Test Report - Email Infrastructure

## Summary

- **Unit Tests**: 15/15 passed — 38/38 project-wide
- **Integration Tests**: 0 written (SMTP/SendGrid require live services; see MailHog note below)
- **Security**: No injectable inputs; API keys from config only
- **Coverage**:

| Component | Coverage | Rationale |
|-----------|----------|-----------|
| `ReliableEmailService` | **100%** | Core business logic — all decorator paths covered |
| `RazorTemplateService` | **100%** | All render/error/init paths covered |
| `EmailRetryJob` | **83.7%** | `ExecuteAsync` hosted-service loop excluded (not unit-testable by design) |
| `SmtpEmailService` | 0% | Infrastructure adapter; requires live SMTP/MailHog — integration-only |
| `SendGridEmailService` | 0% | Infrastructure adapter; requires live SendGrid API — integration-only |

---

## Tests by File

### ReliableEmailServiceTests (5 tests)

| Test | Coverage Target |
|------|----------------|
| `SendAsync_SuccessfulSend_DoesNotQueueToDatabase` | Happy path: send succeeds, no DB write |
| `SendAsync_FailedSend_QueuesEmailToDatabase` | Failed send queued with correct fields |
| `SendAsync_FailedSend_SetsNextRetryToOneSecondFromNow` | `NextRetryAt` precision check |
| `SendTemplatedAsync_TemplateRenderFails_PropagatesException` | Template errors not swallowed |
| `SendTemplatedAsync_RenderSucceedsButSendFails_QueuesRenderedHtml` | Rendered HTML persisted on send failure |

### EmailRetryJobTests (6 tests)

| Test | Coverage Target |
|------|----------------|
| `Processing_SuccessfulSend_MarksEmailAsSent` | Status → Sent, SentAt recorded, LastError cleared |
| `Processing_FailedSend_IncrementsAttemptsAndSetsBackoff` | Attempt++, NextRetryAt = now + 4s backoff |
| `Processing_ThirdFailure_MarksEmailAsFailed` | Attempt 3 → Status = Failed |
| `Processing_FutureNextRetryAt_SkipsEmail` | Not-yet-due email skipped |
| `Processing_CancelledToken_CompletesGracefully` | OperationCanceledException swallowed (shutdown path) |
| `Processing_ScopeFactoryThrows_LogsErrorAndDoesNotPropagate` | General exception logged, not re-thrown |

### RazorTemplateServiceTests (4 tests)

| Test | Coverage Target |
|------|----------------|
| `RenderAsync_SimpleHtmlTemplate_ReturnsHtml` | Basic file system render |
| `RenderAsync_TemplateWithModel_RendersModelProperties` | Dynamic model injection via `ExpandoObject` |
| `RenderAsync_MissingTemplate_ThrowsException` | Error path: missing template logged and thrown |
| `RenderAsync_TemplatesDirectoryCreatedIfMissing` | Constructor creates `EmailTemplates/` if absent |

---

## Acceptance Criteria Validation

### Story 001 — Email Service Abstraction

- ✅ **AC1**: `SmtpEmailService` registered when `Email:Provider = "Smtp"` — verified in `EmailExtensions.cs` switch/case
- ✅ **AC2**: `SendGridEmailService` registered when `Email:Provider = "SendGrid"` — same switch/case
- ✅ **AC3**: BCC to `OperatorBcc` — present in both `SmtpEmailService.BuildMessage` and `SendGridEmailService.SendAsync`
- ✅ **AC4**: `List-Unsubscribe` header — added in both implementations
- ✅ **AC5**: `EmailSettings` binding — `Configure<EmailSettings>(configuration.GetSection("Email"))` in `EmailExtensions`

### Story 002 — Razor Template Rendering

- ✅ **AC1**: `RenderAsync_TemplateWithModel_RendersModelProperties` — model values injected correctly
- ✅ **AC2/3**: `_Layout.cshtml` exists with FotoTipar header (green bar), footer with company info — verified by inspection
- ✅ **AC4**: `RenderAsync_MissingTemplate_ThrowsException` — error thrown and logged at Error level

### Story 003 — Email Retry Queue

- ✅ **AC1**: `SendAsync_FailedSend_QueuesEmailToDatabase` + `SendAsync_FailedSend_SetsNextRetryToOneSecondFromNow`
- ✅ **AC2**: `Processing_SuccessfulSend_MarksEmailAsSent` + `Processing_FutureNextRetryAt_SkipsEmail`
- ✅ **AC3**: `Processing_FailedSend_IncrementsAttemptsAndSetsBackoff` — 4s backoff on attempt 1
- ✅ **AC4**: `Processing_ThirdFailure_MarksEmailAsFailed` — Status = Failed after 3 attempts
- ✅ **AC5**: `Processing_SuccessfulSend_MarksEmailAsSent` — SentAt recorded
- ✅ **AC6**: DB persistence via `20260505131259_AddEmailQueue` migration — `email_queue` table with (Status, NextRetryAt) index

---

## Infrastructure Notes

### SmtpEmailService / SendGridEmailService (0% unit coverage — expected)

These are thin adapters over MailKit and the SendGrid SDK. Unit tests would mock the SDK entirely, providing no meaningful signal. They are validated by:

1. **Code review** — BCC, List-Unsubscribe, error logging, and response status checks are all present
2. **Integration test (manual, dev only)** — Run app with `Email:Provider=Smtp` pointing at MailHog (`localhost:1025`); send a test email; verify receipt in MailHog UI at `localhost:8025`

### EmailRetryJob ExecuteAsync loop (uncovered — by design)

Lines 32–50 comprise the `while (!stoppingToken.IsCancellationRequested)` hosted-service loop. This pattern is standard for `BackgroundService` and its lifecycle is managed by .NET hosting infrastructure. Testing it requires spinning up a full `IHost`, which belongs in integration tests. The loop's constituent operations — `ProcessPendingEmailsAsync` and `Task.Delay` cancellation — are each covered individually.

---

## Issues Found

None. All 38 project tests pass.

## Recommendations

1. **Integration test (future)**: Add a MailHog-based integration test that starts the app, sends a real email via `SmtpEmailService`, and verifies receipt via MailHog's HTTP API — this would cover the 0% SMTP adapter lines.
2. **`PreserveCompilationContext` added to test csproj** — required for RazorLight template compilation in the test runner. This is correctly set and must remain.
