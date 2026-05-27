---
intent: 007-admin-panel
phase: inception
status: complete
created: 2026-05-22T12:00:00Z
---

# Units: Admin Panel

## Decomposition

| Unit | Type | Stories | Default Bolt Type |
|------|------|---------|-------------------|
| 001-admin-api | backend | US-504, US-505 | ddd-construction-bolt |
| 002-admin-ui | frontend | US-501, US-502, US-503, US-506 | simple-construction-bolt |

## Rationale

The admin API (US-504: order management + SignalR + refund; US-505: stats) forms a cohesive backend unit — all endpoints share the `[Authorize(Roles="Admin")]` policy and work on the same `Order` aggregate. The admin UI is a separate frontend unit with 4 pages that consume the API; it is unblocked once bolt 021 is complete.
