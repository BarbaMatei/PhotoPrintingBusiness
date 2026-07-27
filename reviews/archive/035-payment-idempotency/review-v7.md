---
type: code-review
target: bolt-035-payment-idempotency
version: 7
supersedes: 6
branch: feat/bolt-035-payment-idempotency
commit: fbb4c7c
base: 3faaae6 (the v6 tip)
reviewed: 2026-06-19
reviewer: Claude (re-review — 1 isolated lens, clean context, blinded to reviews/)
lenses: [quality-altitude]
verdict: approved
findings: { verified: 1, deferred_accepted: 2 }
---

# Review v7 — Bolt 035: Payment Idempotency (verify the QUAL-4 fix)

Narrow re-review of the single change made after v6: the **QUAL-4** test-helper consolidation
(`fbb4c7c`). v6 had accepted QUAL-4 as deferred; on request the fixer instead consolidated the
duplicated payment-request builders and asked for verification. DB-1 and QUAL-3 remain deferred
(the fixer re-assessed both and kept them deferred with sharpened rationale — see below).

Scaled to the change: one ⚪ cleanup commit touching 3 test files + 1 new helper. One isolated
lens (fresh context, **forbidden from reading `reviews/`**) judged whether the refactor is
exactly behavior-preserving; I ran the tests.

## TL;DR

**QUAL-4 → verified. Verdict: approved.** The two standing deferrals (DB-1, QUAL-3) are
unchanged and remain accepted. The bolt-035 loop is complete again — all 15 v5 findings are
terminal (13 verified · 2 accepted-deferred).

## Build & test

- `dotnet test` (payment integration scope) → **19 passed / 0 failed**. Full suite was
  **474/474** at the QUAL-4 commit (the consolidation is a behavior-preserving refactor — count
  unchanged from v6).

## Per-finding verdict

| ID | Sev | prior status | v7 verdict | Evidence |
|----|-----|--------------|-----------|----------|
| QUAL-4 | ⚪ | fixed (post-v6) | **verified** | The duplicated `Idempotency-Key` POST builders are extracted to `PaymentRequestHelpers` (`HttpClient` extensions `PostStripeIntentAsync`/`PostEuPlatescInitiateAsync`). The lens confirmed, line-by-line against the removed inline code, that HTTP method, URL, JSON body, and the request-message `Idempotency-Key` header are identical; the relational test still sets `Authorization: Bearer` on the client and uses its `CourierStripeRequest` body (delegation drops nothing — `SendAsync` preserves `DefaultRequestHeaders`); call-site signatures/semantics unchanged; no content-type/header-duplication surprises. 19 payment integration tests green. |

**1 verified · 0 reopened.**

## Standing deferrals (unchanged, still accepted)

| ID | Sev | status | Note |
|----|-----|--------|------|
| DB-1 | 🟠 | deferred | App uses `EnsureCreated()` at startup, never `Migrate()`; migrations are Postgres-only artifacts for a migration-based deployment that doesn't exist yet. A clean snapshot fix (phantom migration or full snapshot rewrite) belongs to the migration/deploy setup. Breadcrumb stands. |
| QUAL-3 | ⚪ | deferred | Codebase-wide convention (`WebhooksController` also injects `PhotoPrintDbContext`, 6 uses). Fixing one controller is inconsistent cosmetic churn; the real fix is a repo-wide boundary decision. |

## Recommendation

**Approve.** QUAL-4 is a clean, semantically identical extraction with no behavioral change,
backed by green integration tests and an independent line-by-line comparison. No regression, no
new finding. DB-1 and QUAL-3 stay deferred on sound, fact-based rationale.

**Bolt-035 resolution loop complete** — 13 verified, 2 accepted-deferred, 0 open. The two
deferrals are tracked for a future migration/deployment-infra pass (DB-1) and a repo-wide
controller-altitude tidy (QUAL-3); neither blocks anything.
