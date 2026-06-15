---
id: 004-t4-backend-localization
unit: 001-research-tracks
intent: 034-eu-expansion-architecture-study
status: ready
priority: must
created: 2026-06-05T12:57:50Z
assigned_bolt: 079-research-tracks
implemented: false
---

# Story: 004-t4-backend-localization

## User Story

**As the** team that will build EU readiness
**I want** a backend localization strategy for .NET covering messages, emails, invoices, and culture resolution
**So that** server-generated text reaches each customer in their language/format — including emails sent later by background jobs

## Acceptance Criteria

- [ ] **Given** .NET resource-based localization, **When** T4 reports, **Then** it covers: validation/error messages (ProblemDetails), transactional emails (existing Razor templates), invoice PDFs, and enum/display strings
- [ ] **Given** a request needs a culture, **When** T4 reports, **Then** it recommends a culture-resolution strategy (header vs user preference vs site-of-origin) and justifies it
- [ ] **Given** background jobs send emails later, **When** T4 reports, **Then** it **explicitly flags the deferred-culture trap**: culture must be **stored** on the job/entity at enqueue time, never read from ambient request context at send time
- [ ] **Given** a recommendation, **When** it appears, **Then** it references the actual codebase touchpoints (Razor email templates, ProblemDetails usage, invoice PDF generation)

## Technical Notes

- **Method (FR-1)**: .NET docs research + a repo cross-check of where messages/emails/invoices are generated (light, to ground recommendations — distinct from T7's full audit).
- Output: `docs/analysis/eu-expansion/track-4-backend-localization.md`.
- The deferred-culture trap is the highest-value finding here — make it unmissable.

## Dependencies

### Requires
- None (wave-parallel)

### Enables
- 001-synthesis-options-paper (Unit 2)
- Informs T7 seam audit (which sizes the actual retrofit)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Email enqueued in culture A, sent after user switches to B | Stored culture wins; document the rule |
| Invoice PDF must match the order's country, not the viewer | Culture bound to the order entity |

## Out of Scope

- Frontend i18n (T3), tax content (T5), actual translation of resources.
