---
type: review-resolution
target: bolt-035-payment-idempotency
review_version: 8
status: in-progress          # open | in-progress | resolved
fixed_commit: null           # set to the branch tip when every finding is terminal
opened: 2026-07-04
closed: null
# Per-finding state. status ∈ open | in-progress | fixed | verified | wont-fix | deferred | disputed | false-positive
# `verified` is set ONLY by the re-review (review-v9.md) — a fixer cannot self-verify.
# v8 finding IDs are v8-scoped (clean-room) and do NOT map to the v1–v7 comment IDs.
findings:
  DB-1:   { status: deferred, commit: null, note: "Re-raise of the standing v5→v7 accepted deferral (Postgres/migration test coverage). App uses EnsureCreated(), never Migrate(); a Testcontainers-Postgres fixture belongs to the roadmap's migration/deploy (3-env) phase. Breadcrumb refreshed." }
  OBS-1:  { status: open, commit: null, note: "" }
  BUG-1:  { status: open, commit: null, note: "" }
  SEC-1:  { status: deferred, commit: null, note: "Durable fix = per-tenant composite unique index = new migration + snapshot churn; consistent with the DB/migration deferral it lands in the migration/deploy phase. Accepted-residual threat note added; exploitability LOW (client-chosen GUID keys, self-limiting probe)." }
  BUG-2:  { status: deferred, commit: null, note: "Durable fix (re-read persisted URL under a Postgres row lock / SELECT … FOR UPDATE) needs the not-yet-built Postgres arm; the review's stated minimum (document the Stripe/EuPlatesc gateway-dedupe asymmetry) done now, row-lock deferred to the migration/deploy phase." }
  SEC-2:  { status: open, commit: null, note: "" }
  BUG-3:  { status: open, commit: null, note: "" }
  BUG-4:  { status: open, commit: null, note: "" }
  REQ-1:  { status: open, commit: null, note: "" }
  DB-2:   { status: deferred, commit: null, note: "SQLite-flavored model snapshot → phantom migration diff. Already acknowledged/deferred in the migration's own comment; the fix (per-provider migration assemblies or a CI scaffold-diff guard) is the same migration/deploy-phase item as DB-1. Breadcrumb refreshed." }
  QUAL-2: { status: open, commit: null, note: "" }
  QUAL-1: { status: open, commit: null, note: "" }
  QUAL-3: { status: open, commit: null, note: "" }
  QUAL-4: { status: open, commit: null, note: "" }
  QUAL-5: { status: open, commit: null, note: "" }
  QUAL-6: { status: open, commit: null, note: "" }
  OBS-2:  { status: open, commit: null, note: "" }
  OBS-3:  { status: open, commit: null, note: "" }
---

# Resolution — Bolt 035: Payment Idempotency (review v8)

Fixer's response to [review-v8.md](review-v8.md), the fresh clean-room discovery audit at
`50fc692`. One row per finding ID. The review file is immutable; this file is where the fix
work is recorded. v8 is **approve-with-followups, 0 blockers** — none of these gate merge —
but every finding is driven to a terminal state. When all are terminal the top-level `status`
flips to `resolved` and a re-review → `review-v9.md` sets the surviving fixes to `verified`.

**Scope decided with the owner (2026-07-04):** *full sweep* — fix every tractable finding now
with the regression test the review asked for; **keep the DB/migration/schema findings deferred**
(DB-1, DB-2, SEC-1, BUG-2's row-lock) to the roadmap's migration/deploy phase, consistent with
the standing v5→v7 decision.

`recommended_before_deploy (review): [DB-1, OBS-1]` — OBS-1 fixed here; DB-1 stays deferred
(the same Postgres/migration infra the roadmap parks in the 3-env phase).

| ID | Sev | Status | Fix commit | How / rationale |
|----|-----|--------|-----------|-----------------|
| DB-1  | 🟠 M | **deferred** | — | Re-raise of the standing accepted deferral. App uses `EnsureCreated()` not `Migrate()`; a Testcontainers-Postgres regression belongs to the migration/deploy phase. Breadcrumb refreshed. |
| OBS-1 | 🟠 M | _pending_ | — | |
| BUG-1 | 🟡 L | _pending_ | — | |
| SEC-1 | 🟡 L | **deferred** | — | Per-tenant composite unique index = migration/snapshot churn → migration/deploy phase. Accepted-residual threat note added; LOW exploitability. |
| BUG-2 | 🟡 L | **deferred** | — | Row-lock hardening needs the unbuilt Postgres arm → deferred; documented the gateway-dedupe asymmetry (review's stated minimum) now. |
| SEC-2 | 🟡 L | _pending_ | — | |
| BUG-3 | 🟡 L | _pending_ | — | |
| BUG-4 | 🟡 L | _pending_ | — | |
| REQ-1 | 🟡 L | _pending_ | — | |
| DB-2  | 🟡 L | **deferred** | — | SQLite-flavored snapshot phantom diff; already deferred in the migration comment. Same migration/deploy-phase item as DB-1. Breadcrumb refreshed. |
| QUAL-2| 🟡 L | _pending_ | — | |
| QUAL-1| ⚪ C | _pending_ | — | |
| QUAL-3| ⚪ C | _pending_ | — | |
| QUAL-4| ⚪ C | _pending_ | — | |
| QUAL-5| ⚪ C | _pending_ | — | |
| QUAL-6| ⚪ C | _pending_ | — | |
| OBS-2 | ⚪ C | _pending_ | — | |
| OBS-3 | ⚪ C | _pending_ | — | |

## Decisions for the re-reviewer

- **No blockers existed** — v8 is `approve-with-followups`. This pass drives all 18 findings to
  terminal: the tractable ones fixed with fail-first regression tests, the DB/schema cluster kept
  deferred (below).
- **DB-1, DB-2, SEC-1, BUG-2 (row-lock) deferred** to the migration/deploy phase — see each row.
  DB-1/DB-2 are re-raises of the v5→v7 accepted deferral (the review's own note flags DB-2 as
  "known/deferred"); SEC-1's composite-index fix and BUG-2's `FOR UPDATE` both require the
  not-yet-built Postgres/migration arm, so fixing them now would mean the same premature schema
  churn the owner has consistently parked. Push back if you want the composite index or a
  Testcontainers fixture built in this pass.

_(Per-finding notes below are filled as each fix lands.)_

## Verification (filled by re-review)

Fixer's own checks are recorded at the end of the sweep (NOT a self-verification — the re-review
owns `verified`). Next step after `status: resolved`: a **verification re-review → review-v9.md**
against `fixed_commit`, which flips the surviving fixes to `verified` (or reopens them).
