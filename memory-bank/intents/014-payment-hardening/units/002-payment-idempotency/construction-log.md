---
unit: 002-payment-idempotency
intent: 014-payment-hardening
created: 2026-05-25T13:10:00Z
last_updated: 2026-05-25T14:15:00Z
---

# Construction Log: 002-payment-idempotency

## Original Plan

**From Inception**: 1 bolt planned
**Planned Date**: 2026-05-25T10:05:00Z

| Bolt ID | Stories | Type |
|---------|---------|------|
| 035-payment-idempotency | 3 stories | ddd-construction-bolt |

## Replanning History

| Date | Action | Change | Reason | Approved |
|------|--------|--------|--------|----------|

*(None.)*

## Current Bolt Structure

| Bolt ID | Stories | Status | Changed |
|---------|---------|--------|---------|
| 035-payment-idempotency | 3 | ✅ completed | - |

## Execution History

| Date | Bolt | Event | Details |
|------|------|-------|---------|
| 2026-05-25T13:10:00Z | 035 | started | Stage 1: Domain Model |
| 2026-05-25T13:20:00Z | 035 | stage-complete | Domain Model → Technical Design |
| 2026-05-25T13:30:00Z | 035 | stage-complete | Technical Design → ADR Analysis |
| 2026-05-25T13:36:00Z | 035 | stage-complete | ADR Analysis (ADR-004, ADR-005 created) → Implement |
| 2026-05-25T13:55:00Z | 035 | stage-complete | Implement → Test |
| 2026-05-25T14:10:00Z | 035 | stage-complete | Test (457/457 passed) |
| 2026-05-25T14:15:00Z | 035 | completed | All 5 stages done |

## Execution Summary

| Metric | Value |
|--------|-------|
| Original bolts planned | 1 |
| Current bolt count | 1 |
| Bolts completed | 1 |
| Bolts in progress | 0 |
| Bolts remaining | 0 |
| Replanning events | 0 |
| ADRs created | 2 (ADR-004, ADR-005) |

## Notes

- Two ADRs created at Stage 3: **ADR-004** (state conflict → 409, distinct from validation's 422) and **ADR-005** (`LogicalRequest` excludes `ShippingAddress`). Decision index bumped 3 → 5.
- Two deviations from the Stage-2 design, both documented in the implementation walkthrough:
  1. EuPlatesc redirect URL is **persisted** (`Order.EuPlatescRedirectUrl`) and returned verbatim on replay — `BuildInitiateUrl` is not deterministic (timestamp + nonce). This is the option story 003 left open.
  2. Idempotency resolution lives in `OrderService.CreateFromCartAsync` (returning `OrderCreationResult`), not split into the controller, because the `TotalRon` comparison needs the server-resolved total.
- Migration `20260527075359_AddOrderIdempotencyKey` is SQLite-flavoured (`TEXT`, plain unique index), consistent with the project's entire SQLite-generated migration history; behaviourally equivalent on Postgres (multiple NULLs allowed in a unique index on both providers).
- One Stage-5 test bug fixed (false conflict from `MakeRequest()` randomizing `EasyboxLockerId`); production unaffected.
- Honest coverage gap: the multi-instance DB-arbitrated race is not automatically tested (EF InMemory doesn't enforce unique indexes); deferred to intent 021 with a real Postgres test container.
- Full suite: **457 / 457 passed**.

## Intent-level note

Intent 014 (payment-hardening) had two units. With unit 001 (bolt 034) and unit 002 (bolt 035) both complete, **intent 014 is fully complete**.
