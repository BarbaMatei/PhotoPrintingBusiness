---
intent: 024-order-photo-archive
created: 2026-05-27T13:00:00Z
last_updated: 2026-05-27T13:10:00Z
---

# Inception Log: 024-order-photo-archive

## Origin

Created mid-construction of bolt 043 (intent 019). While designing the cloud-storage
provider, the user chose a **two-tier "promote-on-payment"** model: photos stay on the
local server until an order is paid, then are promoted to cloud and removed from the
server. Clarifications established that the **~2000 px large preview + thumbnail** are the
12-month customer-facing archive; the **original is purged after printing completes**.

## Decisions

| Date | Decision |
|------|----------|
| 2026-05-27 | Two-tier storage (local staging → cloud on Paid). Data-minimization driver. |
| 2026-05-27 | Full-view source = generated ~2000 px large preview (not the original). |
| 2026-05-27 | Original purged when order reaches **Shipped** (configurable). |
| 2026-05-27 | Archive retention default **12 months** from order completion (configurable). |
| 2026-05-27 | Order-history viewing limited to registered users + claimed guest orders. |
| 2026-05-27 | Supersedes intent-019 story 003 / bolt 050 (migrate-all → paid-order backfill). |

## Checkpoints

- ✅ Checkpoint 1 (clarifying questions) — retention, full-view source, viewers.
- ✅ Checkpoint 2 (requirements review) — approved.
- ⏳ Checkpoint 3 (artifacts review) — units + stories + bolt plan generated.
