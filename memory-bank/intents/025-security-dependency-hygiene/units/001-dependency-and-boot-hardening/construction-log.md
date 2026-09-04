---
unit: 001-dependency-and-boot-hardening
intent: 025-security-dependency-hygiene
created: 2026-09-03T20:42:35Z
last_updated: 2026-09-04T12:13:50Z
---

# Construction Log: dependency-and-boot-hardening

## Original Plan

**From Inception**: 1 bolt planned
**Planned Date**: 2026-06-05

| Bolt ID | Stories | Type |
|---------|---------|------|
| 054-dependency-and-boot-hardening | 001, 002, 003, 004 | simple-construction-bolt |

## Replanning History

| Date | Action | Change | Reason | Approved |
|------|--------|--------|--------|----------|
| — | — | — | — | — |

## Current Bolt Structure

| Bolt ID | Status | Stage |
|---------|--------|-------|
| 054-dependency-and-boot-hardening | in-progress | test |

## Execution Log

- **2026-09-03T20:42:35Z**: 054-dependency-and-boot-hardening started - Stage 1: plan
- **2026-09-04T02:20:00Z**: 054-dependency-and-boot-hardening paused by the wave coordinator mid stage-2 hand-off. All four stories are implemented, committed and pushed; both stage-4 fresh-eyes micro-review agents reported and their findings are folded in (including a correction: PPW-462 is NOT fixed by this bolt — the auth services accept an `ipAddress` argument and never record it). Remaining before hand-off: a test for `UntrustedForwardedPeerMiddleware`, the stale doc sweep the docs agent found (`memory-bank/standards/system-architecture.md` pipeline order and rate-limit lines, the intent's `requirements.md`/`system-context.md` rows that still promise the refused story-004 criterion, `bolt.md` success-criterion line 74 and its stage checkboxes, DEPLOYMENT.md §2 inventory clauses), then set `status: review-pending` by hand (never `bolt-complete.cjs`) and re-push.

## Stage exit — 054-dependency-and-boot-hardening — implement — 2026-09-04T12:13:50Z
- Done: stage 2 complete. All four stories were already coded and committed (OTel 1.15.x, Central Package Management with Stripe.net 47.0.0, `renovate.json`, trusted-proxy forwarded headers). This session closed the two gaps that remained: a unit test for the new mechanism, `src/PhotoPrint.Tests/Unit/Middleware/UntrustedForwardedPeerMiddlewareTests.cs` (4 tests, all green via `run-scoped-tests.mjs --filter UntrustedForwardedPeerMiddlewareTests`), and the stale-doc sweep — `memory-bank/standards/system-architecture.md` (pipeline order now shows forwarded headers ahead of `CorrelationId`, conditional on `ForwardedHeaders:TrustedProxies` and never on the scrape listener; rate-limit line says which IP is partitioned on), `memory-bank/intents/025-security-dependency-hygiene/requirements.md` (goal row, FR-4 description and acceptance criteria, NFR security row), `.../system-context.md` (Caddy bullet, Key NFR Goals bullet), `docs/DEPLOYMENT.md` §2 (`docker-compose.prod.yml` fixed subnet/address, `Caddyfile` replaces `X-Forwarded-For`), and `bolt.md` (implement checkbox ticked, `/metrics` success criterion reworded to the shipped inverse).
- Decisions: the test drives the real `Microsoft.AspNetCore.HttpOverrides.ForwardedHeadersMiddleware` as its `next`, with options built by the production `AddTrustedProxyForwardedHeaders` extension over an in-memory config — trust is never faked, so the test would break if the trust wiring changed (repo rule: mock only at system boundaries). Every doc row that still promised the refused story-004 criterion ("allow-list keys on the real client IP") now states the shipped inverse from story 004's SUPERSEDED block: the scrape gate keys on the connecting peer and `X-Forwarded-For` cannot open it. `bolt.md` keeps `status: in-progress` and its Success Criteria unchecked — the test stage verifies them, not this one.
- Dead ends: a private test helper first named `ForwardedHeadersOptions()` shadowed the framework type of the same name; renamed to `TrustedProxyOptions()`. Bulk doc edits with multi-line search strings silently match nothing in this repo — the files are CRLF, so normalise line endings before replacing.
- Next: stage 3 (test) — run the bolt's scoped suites and record the result in a test-report: `dotnet list package --vulnerable` clean, the Stripe/webhook suite, and `MetricsEndpointIntegrationTests` (including `Forwarded_for_cannot_open_the_scrape_gate`). Then set `status: review-pending` by hand — never `bolt-complete.cjs` — and push; the coordinator opens the PR.
