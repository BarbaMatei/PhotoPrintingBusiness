# US-601 — Email — Welcome (Registered Users)

## Story
**As a** system  
**I want to** send a welcome email when a new user confirms their email address

## Type
BACKEND — ASP.NET Core

## Epic
EPIC-6 | Notificări Email

## Dependencies
- US-605 (IEmailService infrastructure)
- US-103 (Email confirmation triggers this)

## Acceptance Criteria

1. **Triggered by**: `IsEmailConfirmed` set to `true`
2. **To**: user email; **Subject**: `Bun venit la FotoTipar!`
3. **Body**: first name greeting, brief explanation of service, `Comandă acum` CTA button
4. **Razor HTML template**; plain text fallback auto-generated

## Technical Notes

### Implementation Details
- Hook into email confirmation flow (US-103): after setting `IsEmailConfirmed=true`, queue welcome email
- Template: `/EmailTemplates/Welcome.cshtml`
- Template data model: `{ FirstName, OrderUrl }`
- Use shared layout (`_Layout.cshtml`) with logo header and footer
- Plain text: strip HTML tags for fallback
- Send via `IEmailService.SendAsync()`

### Email Content (Romanian)
```
Subject: Bun venit la FotoTipar!

Bună {FirstName}!

Mulțumim că ți-ai creat cont pe FotoTipar. Acum poți tipări 
fotografiile tale preferate în format profesional.

[Comandă acum] (CTA button)

Cu drag,
Echipa FotoTipar
```

## Files to Create/Modify
- `src/PhotoPrint.API/EmailTemplates/Welcome.cshtml`
- `src/PhotoPrint.API/EmailTemplates/_Layout.cshtml` (shared layout)
- `src/PhotoPrint.API/Services/AuthService.cs` (trigger after confirmation)
- `src/PhotoPrint.API/DTOs/Email/WelcomeEmailModel.cs`

## Testing
- Unit test: welcome email queued after confirmation
- Unit test: template renders with correct data
- Unit test: plain text fallback generated
