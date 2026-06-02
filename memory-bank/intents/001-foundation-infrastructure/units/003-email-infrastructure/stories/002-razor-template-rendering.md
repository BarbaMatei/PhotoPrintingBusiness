---
id: 002-razor-template-rendering
unit: 003-email-infrastructure
intent: 001-foundation-infrastructure
status: complete
priority: must
created: 2026-05-05T15:26:00Z
assigned_bolt: null
implemented: true
---

# Story: 002-razor-template-rendering

## User Story

**As a** developer
**I want** email content rendered from Razor templates with a shared layout
**So that** all emails have consistent branding and dynamic content is injected from models

## Acceptance Criteria

- [ ] **Given** a template name and model, **When** `SendTemplatedAsync<T>` is called, **Then** the Razor template is rendered to HTML with model data substituted
- [ ] **Given** any rendered email, **When** examined, **Then** it uses the shared `_Layout.cshtml` with FotoTipar logo, header, and footer
- [ ] **Given** the shared layout, **When** rendered, **Then** it includes company info and unsubscribe link in the footer
- [ ] **Given** a missing template name, **When** `SendTemplatedAsync` is called, **Then** a clear error is thrown and logged

## Technical Notes

- Use `RazorLight` NuGet package for template rendering
- `RazorTemplateService` wraps RazorLight engine
- Templates stored in `src/PhotoPrint.API/EmailTemplates/`
- Shared layout: `_Layout.cshtml` with `@RenderBody()` placeholder
- Template receives strongly-typed model via `@model T`
- `IEmailService.SendTemplatedAsync<T>` calls RazorTemplateService, then passes HTML to SendAsync

## Dependencies

### Requires
- 001-email-service-abstraction (provides IEmailService.SendAsync for final delivery)

### Enables
- All specific email templates in Epic 6 (welcome, order confirmed, etc.)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Template compilation error | Throw and log descriptive error at first use |
| Null model property | Render empty string (Razor default behavior) |
| Very long email content | No truncation — render full template |

## Out of Scope

- Specific business email templates (welcome, order, shipping) — Epic 6
- Email preview/testing UI
