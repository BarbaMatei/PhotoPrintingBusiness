---
type: review
target: 038-039-invoicing
version: 9
supersedes: null
commit: c8d6bb4
branch: feat/bolt-038-vat-calculation
pass-type: delta-discovery
date: 2026-08-21
lenses: [correctness, db-parity, security, race, completeness-critic]
lenses-not-run: [requirements, quality, tests-coverage, input-validation, observability, frontend-ux]
verdict: request-changes
blockers: [PPW-557, PPW-558, PPW-559]
findings: { high: 3, medium: 9, low: 7, cleanup: 5, refuted: 1 }
tests: { dotnet: "382/392 — 10 skipped, scoped filter", frontend: "n/a — backend-only delta" }
---

# Review v9 — 038-039-invoicing

## Findings

| ID | Sev | Title | File | Fix now? |
|---|---|---|---|---|
| PPW-557 | 🔴 | New mandatory-address guard makes every Easybox order permanently un-invoiceable | `Services/Invoicing/InvoiceXmlBuilder.cs:131` | yes |
| PPW-558 | 🔴 | Anonymous Stripe webhook buffers an unbounded request body into a string before any signature check | `Controllers/WebhooksController.cs:69` | yes |
| PPW-559 | 🔴 | Upload-timeout branch holds a claim that always expires before the next tick, so the same invoice is re-uploaded to ANAF | `Services/Invoicing/Anaf/InvoiceUploadJob.cs:345` | yes |
| PPW-560 | 🟠 | Squashed InitialPostgres baseline has no upgrade path: a database that ran the deleted chain cannot boot | `Migrations/20260820133204_InitialPostgres.cs:10` | yes |
| PPW-561 | 🟠 | PostgresTestDatabase catch-all turns any CREATE DATABASE failure into "no PostgreSQL server", with no retry | `Tests/Helpers/PostgresTestDatabase.cs:33` | yes |
| PPW-562 | 🟠 | PostgresTestDatabase is per-test, not per-class: about 100 real databases plus full migration chains per run | `Tests/Helpers/PostgresTestDatabase.cs:25` | yes |
| PPW-563 | 🟠 | Removing the skip guard hard-fails every Postgres-backed test, and the default credentials do not match docker-compose | `Tests/Helpers/PostgresTestDatabase.cs:28` | yes |
| PPW-564 | 🟠 | Admin Paid path swallows the invoice-already-created race but still fires Paid side effects and overwrites the webhook's PaidAt | `Services/AdminOrderService.cs:425` | yes |
| PPW-565 | 🟠 | Changed files no lens owns: EF model snapshot and Designers, Sameday registry, both .csproj, ci.yml | `Migrations/PhotoPrintDbContextModelSnapshot.cs:1` | yes |
| PPW-526 | 🟠 | the legacy processor paid leg's new three-state outcome and its rollback have no endpoint-driven test | `Controllers/WebhooksController.cs:204` | no — re-affirmed |
| PPW-566 | 🟠 | AnafSpvClient timeout-versus-shutdown classifier is untested, and Polly retries inside the 30 s budget misclassify definite failures | `Services/Invoicing/Anaf/AnafSpvClient.cs:56` | yes |
| PPW-567 | 🟡 | Exhausted invoice-number collision retry escapes AdminOrderService with the order still tracked Paid | `Services/AdminOrderService.cs:417` | no |
| PPW-568 | 🟡 | Admin manual-Paid retry loop: only the happy retry is tested, the exhausted and already-invoiced branches are not | `Services/AdminOrderService.cs:414` | no |
| PPW-569 | 🟡 | CREATE SEQUENCE IF NOT EXISTS is not race-safe and only the ft_2026 sequence is seeded | `Services/Invoicing/PostgresInvoiceNumberingService.cs:46` | no |
| PPW-570 | 🟡 | PostgresTestDatabase contexts omit the split-query behaviour production configures | `Tests/Helpers/PostgresTestDatabase.cs:53` | no |
| PPW-571 | 🟡 | PostgresTestDatabase.Dispose clears every Npgsql pool in the process while parallel test classes hold their own databases | `Tests/Helpers/PostgresTestDatabase.cs:99` | no |
| PPW-572 | 🟡 | MemoryCacheOnceRegistry.MarkOnce is a non-atomic read-then-write despite promising first-caller-only | `Services/MemoryCacheOnceRegistry.cs:23` | no |
| PPW-573 | 🟡 | data-stack standard and the deployment guide left stale by the migration squash and the provider removal | `memory-bank/standards/data-stack.md:29` | no |
| PPW-574 | ⚪ | InvoiceAddressFormatter.Truncate with maxLength 0 indexes before the string start and throws IndexOutOfRangeException | `Services/Invoicing/InvoiceAddressFormatter.cs:20` | no |
| PPW-575 | ⚪ | PostalZone is truncated with the borrowed CityNameMaxLength constant | `Services/Invoicing/InvoiceXmlBuilder.cs:122` | no |
| PPW-576 | ⚪ | Blob-missing log omits the stamped storage tier, so a cloud-off misconfiguration reads as a lost file | `Controllers/InvoicesController.cs:122` | no |
| PPW-552 | ⚪ | PPW-515's fix orphaned `AnafUnreachableException`'s XML doc comment | `Services/Invoicing/Anaf/AnafExceptions.cs:32` | no — re-affirmed |
| PPW-577 | ⚪ | Dead DatabaseProvider environment entry left in the Dockerfile, .env.example and both compose files | `Dockerfile:42` | no |

## Refuted

| Suspicion | Why it is not real |
|---|---|
| The migration test cannot detect a model change with no migration scaffolded, so a missing column reaches production | The sibling test in the same file inserts an Invoice into the migrated PostgreSQL database and CI supplies a real server, so an unscaffolded column fails there with SQLSTATE 42703. Residual, not this claim: drift that adds no column — an index, a length, a precision — is still invisible, and PPW-565 carries the missing snapshot-versus-model guard |

## Notes for the fixer

Request changes. Three 🔴 rows: PPW-557, PPW-558, PPW-559. Two are this loop's own fix
regressions — PPW-512's fix produced PPW-557, PPW-515's produced PPW-559 — so the first
question on each is what the earlier round decided, not what the code does.

Start with PPW-557. Its approach pre-check came back **refuted**: the fix this pass suggested
cannot be built at all, and the owner question v6 parked is now load-bearing. Get that decision
before any code moves.

PPW-558, PPW-559, PPW-561, PPW-562, PPW-563, PPW-564 and PPW-566 all carry **revised**
pre-checks on their ledger rows. Adopt the revision; a fix round re-checks only where it
deviates. Every one of the seven found the drafted approach unbuildable or actively worse, so
read the row before writing the test.

Four clusters group by owner file. Test helper: PPW-561, PPW-562, PPW-563, PPW-570, PPW-571 —
one file, and the pre-check fixes their order (pool change, then message and default together,
then the shared database last, which needs test-file surgery). Admin order service: PPW-564,
PPW-567, PPW-568, all three descended from PPW-518's fix. ANAF timeout: PPW-559 with PPW-566 —
one design, and landing PPW-566's drafted half alone makes PPW-559 worse. Records: PPW-573 and
PPW-577 overlap on the dead provider entry, so one change closes both.

Before touching PPW-562, check the branch `chore/faster-relational-tests` — a peer session
already has work in flight on the shared relational-test database.

PPW-526 and PPW-552 re-raise decided rows. Read the prior ruling on each ledger row first;
re-opening either is a decision, not a fix.

PPW-560 and PPW-565 carry `plausible`, not `confirmed`. Both traces found today's failing state
unreachable — no deployed PostgreSQL, and no model drift right now — so confirm against the
code and treat each as a guard to add rather than a break to repair.

The recorded suite state is a scoped run over the delta's surface: 382 passed, 0 failed,
10 skipped. No full run was made, and the skips are the storage suite that needs its own
credentials.
