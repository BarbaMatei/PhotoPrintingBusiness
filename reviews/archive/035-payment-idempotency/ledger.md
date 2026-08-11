---
type: review-ledger
target: 035-payment-idempotency
updated: 2026-08-11
closed: 2026-08-11 — retroactive owner sign-off (resolution loop complete at v10 2026-07-04; no certification pass ran)
---

# Ledger — 035-payment-idempotency

## Findings

| D# | Sev | First seen | Title | File | Status | Affirmed |
|---|---|---|---|---|---|---|
| D1 | 🔴 | v1 (BUG-1) | Two concurrent requests with the same key both insert, and the loser returns 500 instead of a replay | `Services/OrderService.cs:392` | verified | `650f615` |
| D2 | 🟠 | v1 (SEC-1) | The idempotency lookup is not scoped to the caller, so another tenant's order and its Stripe secret can be handed over | `Services/OrderService.cs:437` | verified | `650f615` |
| D3 | 🟠 | v1 (BUG-3) | The divergence check ignores cart contents, so a reused key can replay the wrong photos at the same total | `Services/OrderService.cs:449` | verified | `650f615` |
| D4 | 🟠 | v1 (OPS-1) | Nothing tracks the planned change that makes the key header required | `Controllers/PaymentsController.cs:105` | verified | `650f615` |
| D5 | 🟠 | v1 (QUAL-1) | A second database round-trip looks up the stale row the first query already covered | `Services/OrderService.cs:408` | verified | `650f615` |
| D6 | 🟠 | v1 (QUAL-3) | Header extraction and the missing-key warning are copied into both payment endpoints | `Controllers/PaymentsController.cs:49` | verified | `650f615` |
| D7 | 🟠 | v1 (QUAL-4) | The replay, compute and persist sequence is duplicated between the two processor branches | `Controllers/PaymentsController.cs:51` | verified | `650f615` |
| D8 | 🟡 | v1 (BUG-4) | A freed and reused key is forwarded to Stripe with a possibly different amount | `Services/StripePaymentGateway.cs:31` | verified | `650f615` |
| D9 | 🟡 | v1 (BUG-5) | The migration writes an unfiltered `TEXT` index while the runtime model filters it on Postgres | `Migrations/20260527075359_AddOrderIdempotencyKey.cs:34` | verified | `650f615` |
| D10 | 🟡 | v1 (DOC-1) | A comment credits the per-statement constraint check to Postgres alone, though SQLite enforces it too | `Services/OrderService.cs:405` | verified | `650f615` |
| D11 | 🟡 | v1 (DOC-2) | The unique-index null comment reads as if duplicate nulls were forbidden | `Data/PhotoPrintDbContext.cs:146` | verified | `650f615` |
| D12 | 🟡 | v1 (DOC-3) | The ddd-02 design sketch puts conflict resolution in the controller, the code puts it in the order service | `memory-bank/…/ddd-02` | deferred | `650f615` |
| D13 | ⚪ | v1 (QUAL-2) | A second conflict exception type exists only to carry the divergent-field payload | `Exceptions/IdempotencyConflictException.cs` | wont-fix | `b52f4b6` |
| D14 | ⚪ | v1 (QUAL-5) | The correlation id is read out of the request items bag by a raw string key in two places | `Controllers/PaymentsController.cs:109` | verified | `650f615` |
| D15 | ⚪ | v1 (QUAL-6) | Each create saves twice, once in the service and once in the controller | `Controllers/PaymentsController.cs:65` | wont-fix | `b52f4b6` |
| D16 | ⚪ | v2 (INFO-1) | The cross-tenant integration test runs on a provider that does not enforce the unique index | `Tests/…/PaymentControllerIntegrationTests.cs:79` | verified | `650f615` |
| D17 | 🟡 | v2 (INFO-2) | An expired key stays reserved for its first owner, so a second caller is refused a key the contract calls free | `Services/OrderService.cs:184` | verified | `01b5264` |
| D18 | 🟠 | v3 (BUG-6) | The order-number service has no SQLite branch, so every order creation in the development environment fails | `Services/OrderNumberService.cs:15` | verified | `650f615` |
| D19 | 🟡 | v3 (DOC-4) | The filter refactor dropped the searchable follow-up marker D4 asked for | `Filters/IdempotencyKeyFilter.cs:11` | verified | `650f615` |
| D20 | 🟡 | v5 (DB-1) | The model snapshot is SQLite-flavoured, so the next Postgres scaffold emits a phantom migration | `Migrations/PhotoPrintDbContextModelSnapshot.cs` | backlog | `065a516` |
| D21 | 🟠 | v5 (DB-2) | The Stripe secret column is sized at exactly the vendor ceiling, so a longer secret fails on Postgres after the charge | `Data/PhotoPrintDbContext.cs:296` | verified | `3faaae6` |
| D22 | 🟠 | v5 (OBS-1) | The 409 body names the divergent fields only outside Development, and no test reads the body | `Middleware/ExceptionHandlerMiddleware.cs:103` | verified | `3faaae6` |
| D23 | 🟡 | v5 (BUG-1) | The recovery catch infers that any database write failure was the key collision | `Services/OrderService.cs:161` | verified | `3faaae6` |
| D24 | 🟡 | v5 (SEC-1) | With both owner ids null the scope test collapses to "any order without a guest id" | `Services/OrderService.cs:235` | verified | `3faaae6` |
| D25 | 🟡 | v5 (SEC-2) | Key length is never checked, so an over-long key fails on Postgres instead of being refused | `Filters/IdempotencyKeyFilter.cs:22` | verified | `3faaae6` |
| D26 | 🟡 | v5 (OBS-2) | The reserved conflict log event is never emitted | `Middleware/ExceptionHandlerMiddleware.cs` | verified | `3faaae6` |
| D27 | 🟡 | v5 (OBS-3) | The recovery replay calls the gateway again and writes no replay log, so it reads as a fresh request | `Controllers/PaymentsController.cs:117` | verified | `3faaae6` |
| D28 | 🟡 | v5 (DOC-1) | The design document says the stale key is freed inside the insert's transaction; the code uses a separate save | `Services/OrderService.cs:108` | verified | `3faaae6` |
| D29 | ⚪ | v5 (DOC-2) | No document states that the gateway is keyed by the order id rather than the caller's key | `memory-bank/…/ddd-02` | verified | `3faaae6` |
| D30 | ⚪ | v5 (QUAL-1) | The pre-insert and post-collision resolution blocks are near duplicates | `Services/OrderService.cs` | verified | `3faaae6` |
| D31 | ⚪ | v5 (QUAL-2) | Provider names are written out as literal strings in four places | `Data/PhotoPrintDbContext.cs` | verified | `3faaae6` |
| D32 | ⚪ | v5 (QUAL-3) | The controller saves through the database context itself rather than through the order service | `Controllers/PaymentsController.cs:125` | deferred | `fbb4c7c` |
| D33 | ⚪ | v5 (QUAL-4) | The payment request builders and the SQLite fixture setup are duplicated across test files | `Tests/…` | verified | `fbb4c7c` |
| D34 | ⚪ | v5 (QUAL-5) | The order-number query raises a compiler warning and its Postgres branch has no test | `Services/OrderNumberService.cs:33` | verified | `3faaae6` |
| D35 | ⚪ | v6 (DOC-3) | The ddd-02 controller sketch still forwards the caller's key to Stripe, contradicting the section above it | `memory-bank/…/ddd-02:265` | fixed | `3faaae6` |
| D36 | 🟠 | v8 (DB-1) | No test touches the Postgres path: not the constraint match, not the filtered index, not the migration | `Services/OrderService.cs:197` | backlog | `065a516` |
| D37 | 🟠 | v8 (OBS-1) | The cross-tenant key collision has no distinct log event, so key probing is invisible | `Services/OrderService.cs:178` | verified | `01b5264` |
| D38 | 🟡 | v8 (BUG-1) | On SQLite the key violation is recognised by a phrase in the error message, not a structured code | `Services/OrderService.cs:194` | verified | `01b5264` |
| D39 | 🟡 | v8 (SEC-1) | One global key index tells an attacker whether a guessed key is in use, and lets them reserve it first | `Data/PhotoPrintDbContext.cs:308` | backlog | `065a516` |
| D40 | 🟡 | v8 (BUG-2) | The EuPlatesc recovery replay builds a fresh redirect URL instead of returning the stored one | `Controllers/PaymentsController.cs:131` | backlog | `065a516` |
| D41 | 🟡 | v8 (SEC-2) | The key is never trimmed, so a padded copy of the same key creates a second order and a second charge | `Filters/IdempotencyKeyFilter.cs:23` | verified | `01b5264` |
| D42 | 🟡 | v8 (BUG-3) | Freeing the stale key and inserting the new order are two separate saves with nothing to roll them back together | `Services/OrderService.cs:99` | verified | `01b5264` |
| D43 | 🟡 | v8 (BUG-4) | On SQLite two racing requests can collide on the order-number index first, which the recovery does not handle | `Services/OrderService.cs:162` | verified | `01b5264` |
| D44 | 🟡 | v8 (QUAL-2) | The order service's price-tier lookup is a comment-claimed copy of the cart service's, but reads a different quantity | `Services/OrderService.cs:413` | verified | `01b5264` |
| D45 | ⚪ | v8 (QUAL-1) | The public key lookup has no production caller left | `Services/OrderService.cs:220` | verified | `01b5264` |
| D46 | ⚪ | v8 (QUAL-3) | The cart seed graph is rebuilt in three test fixtures and has already drifted apart | `Tests/…/OrderServiceTests.cs:43` | verified | `01b5264` |
| D47 | ⚪ | v8 (QUAL-4) | The concurrency test hand-builds the winning order with copied number totals | `Tests/…/OrderServiceIdempotencyConcurrencyTests.cs:166` | verified | `01b5264` |
| D48 | ⚪ | v8 (QUAL-5) | The two replay-logging branches repeat the event shape and test the replay flag twice | `Controllers/PaymentsController.cs:115` | verified | `01b5264` |
| D49 | ⚪ | v8 (QUAL-6) | The exception middleware reads the hosting environment two different ways in one request | `Middleware/ExceptionHandlerMiddleware.cs:88` | verified | `01b5264` |
| D50 | ⚪ | v8 (OBS-2) | The declared 409 response has no body type, so generated clients never see the divergent-field list | `Controllers/PaymentsController.cs:45` | verified | `01b5264` |
| D51 | ⚪ | v8 (OBS-3) | The transitional missing-key event logs at warning level on every payment request | `Filters/IdempotencyKeyFilter.cs:49` | verified | `065a516` |

## Details

### D1 — Two concurrent requests with the same key both insert, and the loser returns 500 instead of a replay

- **What:** Both requests looked the key up, found nothing, and inserted. The unique index stopped the
  duplicate order, but the loser's database error was not in the middleware's map, so the caller got a
  500 on the exact retry the feature exists to make safe.
- **History:**
  - v1: found (BUG-1) — one of the pass's two fixes required before merge; two lenses reached it
  - round 1: fixed @`2093302` — catch the unique violation, re-resolve owner-scoped, replay or return 409
  - v2: verified @`b52f4b6` — removing the catch reddens both new SQLite race tests with the index violation
  - v3: re-checked, unchanged @`b6198b6` · v4: re-checked, unchanged @`650f615`
  - v5: the catch's breadth was raised separately as D23; v8 raised the SQLite match as D38

### D2 — The idempotency lookup is not scoped to the caller, so another tenant's order and its Stripe secret can be handed over

- **What:** The lookup matched on key and age only, and the divergence check never compared the order's
  owner with the caller. A caller presenting someone else's key with a matching total received that
  order and its live Stripe client secret.
- **History:**
  - v1: found (SEC-1) — the second of the pass's two fixes required before merge; three lenses reached it
  - round 1: fixed @`2093302` — the lookup and the stale-key free both filter on user id or guest session id
  - v2: verified @`b52f4b6` — unit, controller and integration tests prove no order and no secret cross over
  - v3: re-checked, unchanged @`b6198b6` · v4: re-checked, unchanged @`650f615`
  - v5: the both-null edge of the same predicate was raised separately as D24

### D3 — The divergence check ignores cart contents, so a reused key can replay the wrong photos at the same total

- **What:** Divergence compared four scalar fields but not the items. With uniform per-print pricing,
  five prints of one photo and five of another cost the same, so a reused key replayed the wrong order's
  images.
- **History:**
  - v1: found (BUG-3) — strongly recommended alongside the pass's two required fixes
  - round 1: fixed @`2093302` — an order-independent item signature joins the divergence comparison
  - v2: verified @`b52f4b6` — a same-total different-items request now names `items` in the 409
  - v4: re-checked, unchanged @`650f615`

### D4 — Nothing tracks the planned change that makes the key header required

- **What:** Two documents call the missing-key warning transitional and promise a later escalation to a
  400, but no marker, ticket or follow-up recorded that breaking change anywhere in the code.
- **History:**
  - v1: found (OPS-1)
  - round 1: fixed @`b52f4b6` — a searchable follow-up marker on the warning helper
  - v2: verified @`b52f4b6`
  - v3: the D6 refactor dropped the marker, recorded separately as D19; the warning behaviour was unchanged
  - v4: re-checked after D19's fix restored the marker @`650f615`

### D5 — A second database round-trip looks up the stale row the first query already covered

- **What:** The keyed lookup already filtered on age. On a miss the code ran a second lookup by key
  without the age filter to find the stale row.
- **History:**
  - v1: found (QUAL-1)
  - round 1: fixed @`2093302` — one owner-scoped lookup, with the age branch decided in memory
  - v2: verified @`b52f4b6` · v4: re-checked, unchanged @`650f615`

### D6 — Header extraction and the missing-key warning are copied into both payment endpoints

- **What:** Both endpoints repeated the header read, the missing-key warning and the correlation-id read.
  A third processor would have repeated them again.
- **History:**
  - v1: found (QUAL-3) — deferred at v2 as a cross-cutting refactor, then implemented on request
  - round 1: fixed @`0b0fa04` — an action filter owns extraction, normalisation and the warning
  - v3: verified @`b6198b6` — endpoint labels, log lines and the short-circuit paths all match the old behaviour
  - v4: re-checked, unchanged @`650f615`

### D7 — The replay, compute and persist sequence is duplicated between the two processor branches

- **What:** The Stripe and EuPlatesc branches had the same shape and differed only in the gateway call
  and the cached field.
- **History:**
  - v1: found (QUAL-4) — deferred at v2, then implemented on request
  - round 1: fixed @`0b0fa04` — one generic method holds the sequence; both processors are thin adapters
  - v3: verified @`b6198b6` — the replay-versus-compute split and the null-cached fall-through are preserved
  - v4: re-checked, unchanged @`650f615`

### D8 — A freed and reused key is forwarded to Stripe with a possibly different amount

- **What:** The local key window and the vendor's key window are both about a day. At the boundary a
  freed key reused for a new amount could reach Stripe while Stripe still held it, giving either a
  rejection or a stale payment intent.
- **History:**
  - v1: found (BUG-4)
  - round 1: fixed @`b52f4b6` — Stripe is keyed by the order id, which is stable and unique per order
  - v2: verified @`b52f4b6` — retries of one logical order still share a key, distinct orders never do
  - v3: re-checked, unchanged @`b6198b6` · v4: re-checked, unchanged @`650f615`

### D9 — The migration writes an unfiltered `TEXT` index while the runtime model filters it on Postgres

- **What:** The migration created the unique index with no filter and typed the column `TEXT`, ignoring
  the maximum length on Postgres, while the runtime model added the null filter for Postgres only.
- **History:**
  - v1: found (BUG-5) — deferred at v2, then implemented on request
  - round 1: fixed @`2f1872c` — the migration branches on the active provider; the SQLite output is unchanged
  - v3: verified @`b6198b6` — the down migration and the snapshot were untouched, and editing in place is safe before deployment
  - v3: the fix left the snapshot half undone; that residual was raised at v5 as D20
  - v4: re-checked, unchanged @`650f615`

### D10 — A comment credits the per-statement constraint check to Postgres alone, though SQLite enforces it too

- **What:** A reader could take the stale-key null-out for Postgres-only behaviour.
- **History:**
  - v1: found (DOC-1)
  - round 1: fixed @`2093302` — the comment now names both providers
  - v2: verified @`b52f4b6` · v3: re-checked, unchanged @`b6198b6` · v4: re-checked, unchanged @`650f615`

### D11 — The unique-index null comment reads as if duplicate nulls were forbidden

- **What:** The comment used the standard phrase "nulls are distinct", which one lens read as confusing.
  The review itself recorded the item as contested and closer to a wording preference than a defect.
- **History:**
  - v1: found (DOC-2) — recorded as low severity and contested in the same paragraph
  - round 1: fixed @`b52f4b6` — reworded to "multiple nulls are permitted"
  - v2: verified @`b52f4b6` · v3: re-checked, unchanged @`b6198b6` · v4: re-checked, unchanged @`650f615`

### D12 — The ddd-02 design sketch puts conflict resolution in the controller, the code puts it in the order service

- **What:** The implementation deviated from its own design sketch, for the better, and the deviation was
  written up in the walkthrough but never carried back into the sketch a future reader would open first.
- **History:**
  - v1: found (DOC-3)
  - round 1: deferred — historical-document churn, batched into a later documents pass
  - v2: deferral accepted @`b52f4b6` · v4: still batched @`650f615`
  - 2026-08-11: still deferred at close and not carried to `reviews/backlog.md`

### D13 — A second conflict exception type exists only to carry the divergent-field payload

- **What:** Two exception types both map to 409. The newer one exists only to carry the divergent-field
  list to the client.
- **History:**
  - v1: found (QUAL-2)
  - round 1: wont-fix — the D1 fix also throws the plain conflict type, so both now carry meaning
  - v2: ruling accepted @`b52f4b6`

### D14 — The correlation id is read out of the request items bag by a raw string key in two places

- **What:** Two files reached into the request items bag with an untyped string key set by middleware.
- **History:**
  - v1: found (QUAL-5)
  - round 1: fixed @`b52f4b6` — a shared accessor and a shared key constant replace both raw reads
  - v2: verified @`b52f4b6` · v3: re-checked, unchanged @`b6198b6` · v4: re-checked, unchanged @`650f615`

### D15 — Each create saves twice, once in the service and once in the controller

- **What:** The order is saved before the gateway call and again after it.
- **History:**
  - v1: found (QUAL-6) — the review itself noted the split may be deliberate
  - round 1: wont-fix — the order must exist before the gateway call so a crash mid-call is recoverable
  - v2: ruling accepted @`b52f4b6`

### D16 — The cross-tenant integration test runs on a provider that does not enforce the unique index

- **What:** The web factory used the in-memory provider, which does not enforce the unique index, so the
  second tenant's insert succeeded instead of colliding. The test proved no data was disclosed but never
  reached the real 409 path.
- **History:**
  - v2: found (INFO-1) — informational, raised while verifying the D2 fix
  - round 1: fixed @`b6198b6` — a real SQLite factory builds the schema and drives the scenario over HTTP
  - v3: verified @`b6198b6` — disabling the 409 branch turns the test from 409 to 500, so it is not vacuous
  - v3: the factory had to fake the order-number service, which exposed D18
  - v4: re-checked and strengthened once the fake was removed @`650f615`

### D17 — An expired key stays reserved for its first owner, so a second caller is refused a key the contract calls free

- **What:** Stale keys are freed only when their own owner resubmits. A different caller presenting that
  expired key collides on the global unique index and is refused, although the contract says a key is
  free after a day.
- **History:**
  - v2: found (INFO-2) — recorded as an accepted consequence of keeping one global key index
  - round 1: wont-fix — vanishingly unlikely with random keys, and the refusal discloses nothing
  - v3: ruling accepted @`b6198b6` · v4: carried unchanged @`650f615`
  - v8: re-found (REQ-1), severity raised from ⚪ to 🟡 as a contract mismatch rather than an edge case
  - round 8: fixed @`6de2e58` — the documents now state that reclamation is owner-scoped and the key stays reserved
  - v9: verified @`01b5264` — code, entity documentation and the service contract now agree

### D18 — The order-number service has no SQLite branch, so every order creation in the development environment fails

- **What:** The service had a count branch for the in-memory provider and Postgres-only SQL for
  everything else. The development environment runs on SQLite, so creating an order there hit Postgres
  syntax and failed.
- **History:**
  - v3: found (BUG-6) — surfaced because the D16 test factory had to fake the service to run at all
  - round 1: fixed @`3415ec7` — SQLite joins the count branch; the Postgres path is byte-identical
  - v4: verified @`650f615` — reverting the branch reddens both new tests and flips the D16 test from 409 to 500
  - v4: the count branch's behaviour under concurrency was noted and dismissed as not a finding; v8 raised it as D43

### D19 — The filter refactor dropped the searchable follow-up marker D4 asked for

- **What:** Moving the warning into the new filter replaced the marker and its document pointer with
  prose, so the search that D4 existed to answer returned nothing.
- **History:**
  - v3: found (DOC-4) — introduced by the D6 fix; the warning behaviour itself was unchanged
  - round 1: fixed @`650f615` — the marker and the document pointer are back in the missing-key branch
  - v4: verified @`650f615` — the search hits the restored line and the log call is untouched

### D20 — The model snapshot is SQLite-flavoured, so the next Postgres scaffold emits a phantom migration

- **What:** The snapshot records the idempotency columns as `TEXT` and the unique index without a filter,
  while the Postgres model uses sized text and a filtered index. The next scaffolded migration therefore
  proposes column and index changes to columns that are already correct, and on a large orders table that
  index rebuild takes a lock.
- **Evidence:** `Migrations/PhotoPrintDbContextModelSnapshot.cs:314-374` holds the SQLite-flavoured
  entries; the migration's own comment acknowledges the phantom difference; v9 re-checked both at
  `01b5264`.
- **Suggested fix:** Per-provider migration assemblies, or regenerate the snapshot under Postgres. A
  cheaper stopgap is a check that fails when a scaffolded migration contains an unexpected change to
  these columns.
- **History:**
  - v5: found (DB-1) — the residual left by D9's fix, which corrected the migration but not the snapshot
  - round 5: deferred @`659056a` — the durable fix belongs with the migration and deployment work; the
    migration comment was sharpened to spell out the exact phantom difference
  - v6: deferral accepted @`3faaae6` — the breadcrumb is accurate and there is no startup drift
  - v7: deferral re-affirmed @`fbb4c7c` — the application creates its schema directly and never runs migrations
  - v8: re-found (DB-2), severity recorded as 🟡 rather than v5's 🟠
  - round 8: deferred again @`01b5264` · v9: deferral sound @`01b5264` · v10: unchanged @`065a516`
  - 2026-08-11: row carried to `reviews/backlog.md` under its old name DB-2

### D21 — The Stripe secret column is sized at exactly the vendor ceiling, so a longer secret fails on Postgres after the charge

- **What:** The column was sized at the vendor's documented identifier ceiling with no margin. SQLite and
  the in-memory provider ignore the limit, so tests stayed green, while Postgres would refuse the write
  after the charge already existed, leaving a live payment intent with no stored secret.
- **History:**
  - v5: found (DB-2) — one of the two items the pass recommended before deploying
  - round 5: fixed @`11e72c1` — widened in the model, the undeployed migration and the snapshot
  - v6: verified @`3faaae6` — the new guard fails at the old width and passes at the new one, on any provider

### D22 — The 409 body names the divergent fields only outside Development, and no test reads the body

- **What:** The middleware added the divergent-field list only in the non-development branch, so a
  developer working against the local API never saw the field the contract promises. No test asserted the
  body in any environment.
- **History:**
  - v5: found (OBS-1) — the pass's second recommendation before deploying
  - round 5: fixed @`e957ac1` — the list is computed once and emitted in both branches
  - v6: verified @`3faaae6` — unit tests cover both environments and one test reads the field out of the response

### D23 — The recovery catch infers that any database write failure was the key collision

- **What:** The catch treated every write failure as a possible key collision and then reasoned from a
  second lookup. An unrelated failure could surface as a misleading 409, and a genuine collision whose
  other row vanished in between surfaced as a 500.
- **History:**
  - v5: found (BUG-1) — a residual of D1's fix; distinct from D1, which was the 500 itself
  - round 5: fixed @`6aad926` — the catch confirms the violated constraint instead of inferring it
  - v6: verified @`3faaae6` — reverting the guard flips the new test's exception type, so it is not vacuous
  - v8: the SQLite half of that constraint match was raised separately as D38

### D24 — With both owner ids null the scope test collapses to "any order without a guest id"

- **What:** The owner filter picks the user id when present and the guest id otherwise. Had a request ever
  reached it with both null, it would have matched every signed-in user's order. The pass traced that
  exactly one identity is always set today, so it was recorded as fragility, not an exposure.
- **History:**
  - v5: found (SEC-1) — a residual of D2's fix, not reachable at the reviewed commit
  - round 5: fixed @`c7c2b97` — the lookup refuses the both-null case outright
  - v6: verified @`3faaae6` — the guard covers all callers and the retargeted stale-key test keeps its intent

### D25 — Key length is never checked, so an over-long key fails on Postgres instead of being refused

- **What:** The specification caps the key at 80 characters. The filter only normalised blank keys, so a
  longer key passed local tests and would have failed the Postgres insert as a server error.
- **History:**
  - v5: found (SEC-2)
  - round 5: fixed @`8278bbe` — the filter refuses an over-long key with a 400 before the action runs
  - v6: verified @`3faaae6` — the boundary is right at 80 and 81, and an integration test proves the 400

### D26 — The reserved conflict log event is never emitted

- **What:** The design reserves three log event names so metrics can be wired later. Two were emitted;
  the conflict one never was, so conflicts were indistinguishable from any other handled error.
- **History:**
  - v5: found (OBS-2)
  - round 5: fixed @`8d5b240` — the middleware emits the reserved event with the correlation id and field names
  - v6: verified @`3faaae6` — scoping it to the divergent-request conflict matches the design's own event
  - v8: the cross-tenant collision left outside that scope was raised as D37

### D27 — The recovery replay calls the gateway again and writes no replay log, so it reads as a fresh request

- **What:** When a replay found no stored gateway value, the controller fell through and called the
  gateway again. This is safe, because Stripe is keyed by the order id, but it was undocumented,
  untested and invisible in the logs.
- **History:**
  - v5: found (OBS-3)
  - round 5: fixed @`1c36ff5` — the path is documented and emits its own log event; behaviour unchanged
  - v6: verified @`3faaae6` — an integration test drives the path and proves one order and a usable secret
  - v8: the EuPlatesc half of the same path was raised as D40

### D28 — The design document says the stale key is freed inside the insert's transaction; the code uses a separate save

- **What:** The document claimed one transaction. The code freed the key in its own save and then
  inserted, deliberately, to avoid colliding on the per-statement constraint check.
- **History:**
  - v5: found (DOC-1)
  - round 5: fixed @`03fa13d` — the document now describes the two saves in both affected sections
  - v6: verified @`3faaae6`
  - v8: the non-atomicity itself was raised as a defect, D42, overturning this round's document-only ruling

### D29 — No document states that the gateway is keyed by the order id rather than the caller's key

- **What:** The design recorded the signature change but never the choice D8's fix made, so a future
  reader would ask why the header key is not forwarded.
- **History:**
  - v5: found (DOC-2)
  - round 5: fixed @`03fa13d` — the integration section now states the choice and why
  - v6: verified @`3faaae6`, with the incomplete half of the same edit recorded as D35

### D30 — The pre-insert and post-collision resolution blocks are near duplicates

- **What:** Both blocks found the holder, checked the age, compared the divergent fields and then
  replayed or refused.
- **History:**
  - v5: found (QUAL-1)
  - round 5: fixed @`0bc6ecd` — two helpers hold the shared logic and all three call sites use them
  - v6: verified @`3faaae6` — line-for-line equivalent at every call site

### D31 — Provider names are written out as literal strings in four places

- **What:** The provider names were repeated across the database context, the order-number service and
  the migration, free to drift apart.
- **History:**
  - v5: found (QUAL-2)
  - round 5: fixed @`24ed333` — one constants class; two further files beyond the three named were converted
  - v6: verified @`3faaae6` — the constants match the original literals and no runtime literal remains

### D32 — The controller saves through the database context itself rather than through the order service

- **What:** The controller persists the gateway field with its own save, which sits below the layer the
  order service owns.
- **History:**
  - v5: found (QUAL-3) — the pass recorded it as a pre-existing pattern, not introduced by this work
  - round 5: deferred — one more controller does the same in six places, so fixing one is inconsistent churn
  - v6: deferral accepted @`3faaae6`
  - v7: deferral re-affirmed @`fbb4c7c` — the real fix is a repository-wide boundary decision
  - 2026-08-11: still deferred at close and not carried to `reviews/backlog.md`

### D33 — The payment request builders and the SQLite fixture setup are duplicated across test files

- **What:** The request builders for the two payment endpoints, the unit-test request helper and the
  SQLite fixture setup were each written out again in the new test files. The consolidation that closed
  it touched three test files and added one helper.
- **History:**
  - v5: found (QUAL-4)
  - round 5: deferred first, then fixed on request @`fbb4c7c` — shared client extensions; call sites unchanged
  - v6: deferral accepted @`3faaae6`, before the owner asked for it anyway
  - v7: verified @`fbb4c7c` — an isolated lens compared method, address, body and header line by line

### D34 — The order-number query raises a compiler warning and its Postgres branch has no test

- **What:** The year sequence is built with an interpolated statement, which raises a warning. The value
  is a server-side number, so it is not an injection route, but the branch has no automated coverage.
- **History:**
  - v5: found (QUAL-5)
  - round 5: fixed @`738993e` — the warning is suppressed at that one call with its justification
  - v6: verified @`3faaae6` — the warning is gone from the build; the coverage gap is unchanged and became part of D36

### D35 — The ddd-02 controller sketch still forwards the caller's key to Stripe, contradicting the section above it

- **What:** D29's edit corrected the integration section but left the code sketch further down forwarding
  the caller's key, so the document contradicted itself. The shipped code was always right.
- **History:**
  - v6: found (DOC-3) — raised by a lens during the verification of round 5
  - round 5: fixed in the same pass @`3faaae6` — the sketch now passes the order id
  - no later pass re-checked this row, so it stands at fixed rather than verified

### D36 — No test touches the Postgres path: not the constraint match, not the filtered index, not the migration

- **What:** Production runs on Postgres and every test runs on SQLite or in memory. The Postgres branch
  that turns a double submit into a clean replay, the filtered unique index, and the migration itself are
  all exercised by nothing, so drift between the model and the migration would first appear at deployment.
- **Evidence:** `Services/OrderService.cs:197-199` holds the Postgres-only constraint match; a repository
  search for migration calls in tests returns nothing, and every fixture creates its schema directly.
  v9 re-ran that search at `01b5264` and confirmed zero matches.
- **Suggested fix:** One container-backed Postgres regression that applies the real migration, drives two
  genuinely concurrent same-key creates, and asserts that the live constraint name equals the literal the
  code matches on.
- **History:**
  - v8: found (DB-1) — one of the pass's two mediums; the pass confirmed the literal matches today
  - round 8: deferred @`01b5264` — belongs with the migration and deployment work, like D20
  - v9: deferral sound @`01b5264` · v10: unchanged @`065a516`
  - 2026-08-11: row carried to `reviews/backlog.md` under its old name DB-1

### D37 — The cross-tenant key collision has no distinct log event, so key probing is invisible

- **What:** Only the same-caller divergence conflict got the reserved log event. A key held by a
  different caller threw the plain conflict type, which logged as any other handled error, so the one
  signal worth alerting on during a duplicate-charge incident could not be found.
- **History:**
  - v8: found (OBS-1) — re-raises the round 5 decision that deliberately scoped D26's event narrowly
  - round 8: fixed @`21a295a` — a distinct exception type and its own reserved log event
  - v9: verified @`01b5264` — the exact-type mapping is present, the event fires, and the other path is unchanged

### D38 — On SQLite the key violation is recognised by a phrase in the error message, not a structured code

- **What:** Postgres was matched on a structured constraint name, SQLite on a phrase in the human-readable
  message. A library upgrade that rewords the message would silently turn the clean 409 back into a 500,
  with no compiler warning.
- **History:**
  - v8: found (BUG-1) — five lenses reached it independently, the pass's strongest agreement
  - round 8: fixed @`6a370e0` — a shared index-name constant plus the structured SQLite code and a name reference
  - v9: verified @`01b5264` — the coupling is a real reference, so a rename now breaks the build

### D39 — One global key index tells an attacker whether a guessed key is in use, and lets them reserve it first

- **What:** The lookup is correctly scoped to the caller, but the uniqueness constraint is one global
  column. A caller presenting somebody else's key is refused with a 409 while a free key gives a 200, so
  the pair of responses reveals whether a key is in use, and keys can be reserved ahead of their owner.
- **Evidence:** `Data/PhotoPrintDbContext.cs:308-310` declares the single-column unique index;
  `Services/OrderService.cs:162-180` shows the scoped lookup and the collision path that returns the 409.
- **Suggested fix:** Scope uniqueness per caller with a composite index on owner and key, and update the
  constraint match with it. Otherwise record it as an accepted residual in the threat notes.
- **History:**
  - v8: found (SEC-1) — re-raises the round 1 decision to keep one global key index; shares a root with D17
  - round 8: deferred @`01b5264` — the durable fix is a schema change; exploitability is low with random keys
  - v9: deferral sound @`01b5264` — the accepted-residual note at the index is present and accurate
  - v10: unchanged @`065a516`
  - 2026-08-11: row carried to `reviews/backlog.md` under its old name SEC-1

### D40 — The EuPlatesc recovery replay builds a fresh redirect URL instead of returning the stored one

- **What:** Stripe is deduplicated by the gateway because the call carries the order id as its key.
  EuPlatesc has no such key, and the recovery path rebuilds the URL with a fresh timestamp, so two
  concurrent retries produce two different signed URLs and the stored value is overwritten. No double
  charge follows, because the invoice reference is the order id, but the promise that a replay returns
  the stored value verbatim is broken.
- **Evidence:** `Controllers/PaymentsController.cs:131-138` rebuilds the URL whenever the stored value is
  null; the build path documents the asymmetry between the two gateways.
- **Suggested fix:** Re-read the stored URL inside the recovery scope and reuse it when present, or take a
  short row lock. The lock needs the Postgres path that D36 covers.
- **History:**
  - v8: found (BUG-2) — three lenses reached it independently
  - round 8: deferred @`01b5264` — the row lock needs the unbuilt Postgres path; the asymmetry was documented
  - v9: deferral sound @`01b5264` — no double charge today; only the verbatim-replay promise breaks
  - v10: unchanged @`065a516`
  - 2026-08-11: row carried to `reviews/backlog.md` under its old name BUG-2

### D41 — The key is never trimmed, so a padded copy of the same key creates a second order and a second charge

- **What:** The filter turned a blank key into no key and capped its length, but never trimmed one that
  had content. A retry layer that resends the same key with a space around it therefore matched nothing,
  collided with nothing, and produced a second order and a second payment intent.
- **History:**
  - v8: found (SEC-2)
  - round 8: fixed @`b76eede` — the key is trimmed before the blank and length checks
  - v9: verified @`01b5264` — the tests set the raw header directly and prove padded and plain now match

### D42 — Freeing the stale key and inserting the new order are two separate saves with nothing to roll them back together

- **What:** The stale holder's key was cleared and committed in its own save, then the new order was
  inserted in another, with no enclosing transaction. If the insert failed the old order had lost its key
  and no replacement held it, so the key stopped deduplicating anything.
- **History:**
  - v8: found (BUG-3) — re-raises the round 5 ruling that treated the same behaviour as a document error, D28
  - round 8: fixed @`0d5a721` — the free and the insert share one save; a failing insert rolls the free back
  - v9: verified @`01b5264` — the intermediate save is gone and the rollback test reddens against the old code

### D43 — On SQLite two racing requests can collide on the order-number index first, which the recovery does not handle

- **What:** The recovery only treats a key-index violation as idempotent. On SQLite the order number comes
  from a row count, so two racing requests can produce the same number and violate that index instead,
  which propagated as a 500 rather than replaying the winner. Postgres uses a sequence and is unaffected.
- **History:**
  - v4: raised as an observation while verifying D18 and dismissed as not a finding
  - v8: found (BUG-4) — the same behaviour, this time recorded as a defect, overturning that dismissal
  - round 8: fixed @`f71041f` — a bounded retry on the number collision, proven with the real generator
  - v9: verified @`01b5264` — the retry is bounded at three and the test reddens when the bound is zero

### D44 — The order service's price-tier lookup is a comment-claimed copy of the cart service's, but reads a different quantity

- **What:** A comment called the two identical. They are not: one resolves the tier from the total copies
  in a group, the other from a single line's quantity. The tier rules match, so the comment hid the one
  real difference, and the replay's total comparison now depends on this path.
- **History:**
  - v8: found (QUAL-2) — the difference itself predates this work
  - round 8: fixed @`f7a314a` — one shared resolver; each caller keeps its own quantity source; comment corrected
  - v9: verified @`01b5264` — the resolver matches the previous rule exactly and both callers behave as before

### D45 — The public key lookup has no production caller left

- **What:** The method was declared on the service contract and implemented, but only tests called it;
  production resolved idempotency entirely inside the create path.
- **History:**
  - v8: found (QUAL-1) — three lenses reached it independently
  - round 8: fixed @`b8238a9` — the method is removed and its tests retargeted at the create path
  - v9: verified @`01b5264` — no live references remain and the coverage the tests gave is preserved

### D46 — The cart seed graph is rebuilt in three test fixtures and has already drifted apart

- **What:** Three fixtures each built the same product, size, tier, finish, upload and cart-item graph,
  and one of them had already stopped setting the size link the others set.
- **History:**
  - v8: found (QUAL-3)
  - round 8: fixed @`71255b1` — one shared seed helper; the drifted link is set consistently
  - v9: verified @`01b5264` — all three fixtures delegate and the in-memory behaviour is unchanged

### D47 — The concurrency test hand-builds the winning order with copied number totals

- **What:** The test wrote the subtotal, shipping and total out as literals copied from the service's own
  arithmetic, so a price or shipping change would flip the test from replay to conflict for the wrong reason.
- **History:**
  - v8: found (QUAL-4)
  - round 8: fixed @`4bbd9c5` — the winner is built by calling the real create path
  - v9: verified @`01b5264` — no copied totals remain and each test pins the collision it means to test

### D48 — The two replay-logging branches repeat the event shape and test the replay flag twice

- **What:** The cached-replay branch and the recovery branch repeated the same log shape and re-checked
  the replay flag.
- **History:**
  - v8: found (QUAL-5)
  - round 8: fixed @`694788a` — one branch over the replay flag and the cached value
  - v9: verified @`01b5264` — all three cases are preserved and the cached replay still skips the gateway call

### D49 — The exception middleware reads the hosting environment two different ways in one request

- **What:** The middleware held the injected environment but its writer method resolved the environment
  again from the request's services, because the method was static.
- **History:**
  - v8: found (QUAL-6)
  - round 8: fixed @`cfa23af` — the writer is an instance method using the injected value
  - v9: verified @`01b5264` — the service-locator hop is gone and both response shapes are unchanged

### D50 — The declared 409 response has no body type, so generated clients never see the divergent-field list

- **What:** Both endpoints declared the 409 with no body type, while the runtime response always carries
  the divergent-field list. Generated clients therefore missed the one field that tells the caller what to
  correct.
- **History:**
  - v8: found (OBS-2)
  - round 8: fixed @`b37d322` — a typed problem-details body is declared on both endpoints
  - v9: verified @`01b5264` — the declared type matches what the runtime actually sends

### D51 — The transitional missing-key event logs at warning level on every payment request

- **What:** Until the front end sends a key, the missing-key event fired on every payment request at
  warning level, which can trip alerts that watch the warning rate.
- **History:**
  - v8: found (OBS-3)
  - round 8: fixed @`065a516` — the event logs at information level until the key becomes required
  - v9: code verified, row reopened @`01b5264` — four documents still described the old level
  - round 8: document alignment completed @`065a516`
  - v10: verified @`065a516` — no reference states the current level as warning; the remaining ones are historical
