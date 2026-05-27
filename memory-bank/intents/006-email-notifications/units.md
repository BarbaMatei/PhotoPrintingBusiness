---
intent: 006-email-notifications
phase: inception
status: complete
created: 2026-05-22T12:00:00Z
---

# Units: Email Notifications

## Decomposition

| Unit | Type | Stories | Default Bolt Type |
|------|------|---------|-------------------|
| 001-transactional-emails | backend | US-601, US-602, US-603, US-604 | ddd-construction-bolt |

## Rationale

All 4 email triggers live in the same backend domain (order lifecycle → email dispatch). They share the same `IEmailService` dependency and Razor template infrastructure. A single backend unit keeps the trigger logic and template wiring cohesive. No frontend work required — emails are server-initiated.
