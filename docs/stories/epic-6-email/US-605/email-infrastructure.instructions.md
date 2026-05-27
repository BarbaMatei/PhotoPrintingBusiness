# US-605 — Email Infrastructure

## Story
**As a** system  
**I want to** provide a single IEmailService abstraction switchable between SMTP and SendGrid

## Type
BACKEND — ASP.NET Core

## Epic
EPIC-6 | Notificări Email

## Dependencies
- US-801 (Logging infrastructure)
- US-803 (Background service for retry queue)

## Acceptance Criteria

1. **IEmailService**: `Task SendAsync(string to, string subject, string htmlBody)`
2. **SmtpEmailService** (MailKit) for dev; **SendGridEmailService** for production — switched via `appsettings EmailProvider`
3. **All emails BCC to operator** address (config); all include `List-Unsubscribe` header
4. **Failed sends** logged with Serilog; retried up to 3× with exponential backoff via BackgroundService queue
5. **Razor templates** in `/EmailTemplates/*.cshtml`; shared layout with logo and footer

## Technical Notes

### Implementation Details
- `IEmailService` interface:
  ```csharp
  public interface IEmailService
  {
      Task SendAsync(string to, string subject, string htmlBody);
      Task SendTemplatedAsync<T>(string to, string subject, string templateName, T model);
  }
  ```
- **SmtpEmailService**: uses MailKit NuGet package; config: `Email:Smtp:Host`, `Email:Smtp:Port`, `Email:Smtp:Username`, `Email:Smtp:Password`
- **SendGridEmailService**: uses SendGrid NuGet package; config: `Email:SendGrid:ApiKey`
- **Provider switching**: `appsettings.json` → `Email:Provider` = `Smtp` | `SendGrid`; DI registration based on config value
- **BCC**: `Email:OperatorBcc` in config; added to every email
- **Retry queue**: `EmailRetryJob` (IHostedService) — maintains in-memory queue of failed emails; retries with exponential backoff (1s, 4s, 16s); max 3 attempts; logs final failure
- **Razor templates**: use `RazorLight` NuGet package for rendering `.cshtml` templates to HTML strings
- **Shared layout**: `_Layout.cshtml` with FotoTipar logo, header, and footer (unsubscribe link, company info)
- **Plain text**: auto-generate from HTML by stripping tags

### Configuration
```json
{
  "Email": {
    "Provider": "Smtp",
    "FromAddress": "noreply@fototipar.ro",
    "FromName": "FotoTipar",
    "OperatorBcc": "operator@fototipar.ro",
    "Smtp": { "Host": "localhost", "Port": 1025, "Username": "", "Password": "" },
    "SendGrid": { "ApiKey": "SG.xxx" }
  }
}
```

## Files to Create/Modify
- `src/PhotoPrint.API/Services/IEmailService.cs`
- `src/PhotoPrint.API/Services/SmtpEmailService.cs`
- `src/PhotoPrint.API/Services/SendGridEmailService.cs`
- `src/PhotoPrint.API/Services/RazorTemplateService.cs`
- `src/PhotoPrint.API/BackgroundJobs/EmailRetryJob.cs`
- `src/PhotoPrint.API/EmailTemplates/_Layout.cshtml`
- `src/PhotoPrint.API/Configuration/EmailSettings.cs`
- `Program.cs` or `Startup.cs` (DI registration)

## Testing
- Unit test: SmtpEmailService sends via MailKit (mock SMTP)
- Unit test: SendGridEmailService sends via API (mock HttpClient)
- Unit test: BCC added to all emails
- Unit test: retry on failure with exponential backoff
- Unit test: Razor template rendering
- Integration test: full email send flow with MailHog
