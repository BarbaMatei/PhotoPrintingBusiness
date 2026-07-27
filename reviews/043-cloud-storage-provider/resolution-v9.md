---
type: resolution
target: 043-cloud-storage-provider
answers: review-v9.md
status: resolved
fixed_commit: b9af326
closed: 2026-07-27
findings:
  D83: { status: fixed, commit: b9af326, note: "empty-archive copy gated on order lifecycle (d041295 + b9af326): pre-payment/production shows 'se pregătesc … în curând'; the purged copy is reserved for Shipped/Delivered/Cancelled/PaymentFailed — 8 status-matrix specs, red before/green after" }
  D84: { status: wont-fix, commit: null, note: "owner 2026-07-27: the EuPlatesc gateway is slated for removal — no further investment in its coverage" }
  D85: { status: deferred, commit: null, note: "owner 2026-07-27: backfill CLI is an ops tool unused until the 3-env/deployment phase — targeted scrutiny lands there (with D20/D60)" }
---

# Resolution v9 — owner triage of the certification follow-ups

The v9 certification left three open Mediums for owner triage (everything else was already
terminal in the ledger). Owner ruled 2026-07-27 via [summary-v9](summary-v9.md).

| ID | Severity | Status | How |
|----|----------|--------|-----|
| D83 | 🟠 Med | fixed `d041295` | Order-lifecycle gate on the empty-archive message in `order-detail-page.ts`; regression specs cover all six statuses (3 pre-production → "în curând", 3 purge-eligible → "nu mai sunt disponibile"), failing before the fix and passing after. FE suite 444/444. |
| D84 | 🟠 Med | wont-fix | EuPlatesc is planned for removal; testing its IPN→promotion wiring buys nothing durable. The Stripe twin stays tested. |
| D85 | 🟠 Med | deferred → 3-env | Backfill CLI is operator tooling for the deployment phase; its scrutiny (incl. backfill × live-worker concurrency) belongs with the D20/D60 environment work. |

## Decisions

- **D84 (wont-fix):** rationale is strategic, not technical — the owner intends to remove the
  EuPlatesc gateway. Any future EuPlatesc-only coverage/parity finding should cite this
  decision rather than be re-fixed.
- **D85 (deferred):** re-check when the 3-env phase starts or the backfill command is first
  used against a real environment; affirmed at `d041295`.
- **Open doc note (no ruling requested):** v9's D10 row flagged that bolt-053's
  implementation-plan AC says the photos endpoint returns 404 for non-owners while the
  shipped code and upheld convention use 403 — a doc-vs-code reconcile for the next doc
  sweep.
- **Fix-diff micro-review (ran, 1 anchored agent):** class-sweep clean (single site);
  regression clean (error/retry, grid, lightbox, error-vs-empty untouched); **one edge
  caught and fixed** — `PaymentFailed` sits outside the linear status chain and fell into
  the optimistic copy; now treated like `Cancelled` (follow-up commit, `Pending` +
  `PaymentFailed` added to the status matrix, FE 446/446).
