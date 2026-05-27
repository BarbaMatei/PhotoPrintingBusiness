---
id: 003-sameday-schema-additions
unit: 001-sameday-api-client
intent: 015-sameday-shipping-integration
status: draft
priority: must
created: 2026-05-25T10:10:00Z
assigned_bolt: 036-sameday-api-client
implemented: false
---

# Story: 003-sameday-schema-additions

## User Story

**As** the application
**I want** persistent storage for the label URL and last tracking sync
**So that** AWB labels can be re-fetched without calling Sameday again and tracking is idempotent

## Acceptance Criteria

- [ ] EF Core migration `20260527_AddSamedayOrderFields`:
  ```sql
  ALTER TABLE "Orders" ADD COLUMN "AwbLabelUrl"        varchar(500) NULL;
  ALTER TABLE "Orders" ADD COLUMN "LastTrackingSyncAt" timestamptz  NULL;
  ```
- [ ] `Order` entity exposes both as nullable properties.
- [ ] EF Core model builder configures column types + max lengths.
- [ ] Migration applies cleanly on Postgres and SQLite (use EF type conventions; `timestamptz` becomes `TEXT` on SQLite — acceptable).

## Technical Notes

- Both columns nullable — existing orders have no AWB label and no tracking sync.
- No backfill required.

## Dependencies

### Requires
- None

### Enables
- 001-sameday-settings-and-typed-client (tests use the new columns)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Migration applied against an older DB | New columns nullable, no constraint failures |

## Out of Scope

- Adding `ShippedAt` — already on `Order`.
