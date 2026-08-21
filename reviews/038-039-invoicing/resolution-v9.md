---
type: resolution
target: 038-039-invoicing
version: 9
answers: review-v9.md
status: in-progress
fixed_commit:
closed:
---

# Resolution v9 — 038-039-invoicing

## Findings

| ID | Status | Commit | Note |
|---|---|---|---|
| PPW-557 | deferred | — | parked: which fiscal address a parcel-locker order may carry is an owner decision, not a fix — the pre-check refuted every code-only option. Ledger row left `open`; the owner has been asked |
| PPW-558 | fixed | `9cc0273`, `7ad27df` | rejects >1 MB in the action with the 413-mapped exception before verifying; siblings capped. New surface: the bounded read, three byte caps, the `body_too_large` label, the Kestrel-status middleware branch |
| PPW-559 | fixed | `12dac3a`, `25f2097`, `d3ae1e7` | uploads ANAF never confirmed are counted on the row, which parks as `Failed` at `Anaf:MaxUnknownUploadOutcomes` (3) where the admin retry reaches it and resets both count and claim. New surface: the column, the setting, one lifecycle method |
| PPW-565 | fixed | `8d7a5e6` | the migration test now asserts `HasPendingModelChanges()` is false, so drift adding no column fails a run; proven red with a throwaway `Invoice` property. The relocated Sameday base class is covered through both subclasses |
| PPW-566 | fixed | `35bde0e` | classifier tested through a real client and stub handler, split by cause on upload and poll, plus one test that no wire outcome escapes unclassified. The per-attempt timeout is deliberately not built |
| PPW-564 | fixed | `c9ffae4`, `6527bcb` | gates on the invoice entity's tracked state as well as on the index violation, and reloads instead of saving again, so the winner's PaidAt still matches the invoice; email and notification skipped. New surface: the outcome enum, the reload |
| PPW-567 | fixed | `ba8e628`, `6527bcb` | exhausted retries log the order number, total and payment handles at Error, capture to Sentry, roll the transition back and answer 409. New surface: the optional IHub, the terminal catch, the rollback whose own reload failure is swallowed |
| PPW-568 | fixed | `573dfb8` | every branch of the loop is now covered in one Postgres-backed class; the two branch tests shipped in the commits whose fix they guard, this one relocates the happy-retry test and adds the container-resolution test |

## Scope

| Cluster | Findings | Files | Approach-check |
|---|---|---|---|
| A — anonymous webhook body cap | PPW-558 | `Controllers/WebhooksController.cs`, `Controllers/PaymentsController.cs`, `Filters/DetectLegacyShippingCostFilter.cs`, `Middleware/ExceptionHandlerMiddleware.cs`, `Observability/MetricNames.cs`, `memory-bank/operations/metrics.md` | not needed (review pre-check `revised`, adopted) |
| B — ANAF unknown-outcome upload | PPW-559, PPW-566 | `Services/Invoicing/Anaf/InvoiceUploadJob.cs`, `Services/Invoicing/InvoiceLifecycle.cs`, `Models/Invoice.cs`, `Configuration/AnafSettings.cs`, `Validators/AnafSettingsValidator.cs`, `Migrations/*`, `docs/DEPLOYMENT.md`, `memory-bank/operations/metrics.md` | not needed (both review pre-checks `revised`, adopted) |
| C — model-versus-plan drift guard | PPW-565 | `Tests/Integration/MigrationChainTests.cs` | not needed (adds one assertion; not trigger-list-shaped) |
| D — admin manual-Paid invoice race | PPW-564, PPW-567, PPW-568 | `Services/AdminOrderService.cs`, `Controllers/AdminOrdersController.cs`, `Tests/Unit/Services/AdminOrderServicePaidRaceTests.cs`, `Tests/Unit/Services/AdminOrderServiceTests.cs`, `docs/DEPLOYMENT.md`, `memory-bank/operations/metrics.md`, `UI/features/admin/pages/state-machine/admin-state-machine-page.ts` | not needed (PPW-564's review pre-check `revised`, adopted; it settles the whole cluster, exhausted branch included) |

Out of scope this round by the driver's decision, untouched: PPW-557 (owner decision pending,
stays `open`), PPW-561/562/563 (owner must choose the depth; peer work in flight on
`chore/faster-relational-tests`). Cluster D landed in a later part of the same round, after
clusters A to C.

## Decisions

### PPW-557 is a question, not a defect this round can close (PPW-557)

Its approach pre-check refuted every code-only option: `EasyboxLocker` carries no postal code and
is not loaded on the invoicing path, so filling the address from the locker cannot work, and the
one existing locker-address derivation invents the sentinel `000000`, which on a fiscal document
is worse than refusing. The drafted test would have inverted three tests that lock the throw in
deliberately. So what a parcel-locker order may carry as its fiscal buyer address is the owner's
ruling, and the fix — at order or invoice creation, plus a way to unstick the rows already
looping — cannot be designed before that answer. The round is unattended, so the question is
parked, not guessed: the row is `deferred` here and left `open` on the ledger, and the run's
report carries it to the owner. Nothing in the code moved for it.

### The webhook cap is enforced in the action, not by the attribute alone (PPW-558)

The review's own pre-check showed why an attribute cannot be the whole fix: Kestrel's oversize
exception is not in `ExceptionHandlerMiddleware`'s type map, so it lands in the unmapped branch
as a 500 plus a Sentry capture — an anonymous caller could turn a rejected body into an
error-budget burn. So the action now reads at most `StripeMaxBodyBytes + 1` bytes and throws the
already-mapped `RequestEntityTooLargeException`, which needs no `Content-Length` to work: the
red run proved the old code throws `OutOfMemoryException` on a chunked body that never ends.
1 MB, not the 256 KB first drafted, because rejecting a genuine Stripe event costs a three-day
retry cycle at Stripe; `[RequestSizeLimit]` at 2 MB stays as the byte backstop.

Three sibling sites carried the same class and are fixed in the same commit: the EuPlatesc IPN
(model binding materialises its form before any code runs, so only a byte ceiling bounds it),
`DetectLegacyShippingCostFilter` (buffered the whole body with no limit for anyone holding a
free guest token) and the middleware gap above, which now answers Kestrel's own status for
every endpoint rather than only these two.

### The claim was never what delayed the re-post, so the fix counts instead of holding (PPW-559)

The pre-check killed the drafted approach outright: there is no free field to hold a state in
(`UpdatedAt` is overwritten by the error write on the line before, no ANAF status member for a
hold exists), and a future-dated `ClaimedAt` would lie to the claim-lost log. It also showed the
30-minute per-row cooldown, not the 10-minute claim, is what actually delays the next attempt —
so releasing the claim changes no timing and a longer hold would only re-time a blind re-post.

So the row now counts uploads whose outcome ANAF never confirmed (`Invoice.UnknownUploadOutcomes`,
one migration, `AddColumn` with default 0) and the worker parks it as `Failed` once the budget is
spent. `Failed` is deliberate: `Rejected|Failed` is the only pair the admin retry endpoint accepts,
so parking creates the operator exit the pre-check said was missing, and `RetryAsync` clears the
counter so that retry starts fresh. Default 3, from `Anaf:MaxUnknownUploadOutcomes`, validated
1–10: a client timeout is more often a slow success than a lost request, so each extra attempt is
likelier to file a duplicate than to rescue the invoice, and 3 attempts spend about two hours of a
five-business-day deadline. The claim is left to expire — its micro-review showed releasing it
lets a co-replica re-post seconds later — and the job's header comment no longer asserts an ANAF
invoice-number dedupe nobody here has verified.

### The per-attempt timeout PPW-566 asked for is not built, by its own pre-check (PPW-566)

Its pre-check showed a per-attempt Polly timeout makes things strictly worse: the rejection Polly
raises is neither retried nor caught anywhere, so it escapes to the batch loop's generic catch,
which writes no `LastError` and releases no claim — the cooldown is then bypassed and the invoice
is re-posted on the very next tick. Adding it to the retry predicate instead duplicates the post
inside one call. What shipped is the half the pre-check endorsed: the classifier is now tested
through a real `AnafSpvClient` with a stub handler, split by cause — client timeout versus caller
cancellation, on both upload and poll — plus one test that no wire outcome escapes as an
unclassified exception. The misclassification itself survives (three slow 500s still end as a
timeout labelled outcome-unknown); its consequence no longer does, because PPW-559's budget now
bounds what an outcome-unknown label can cost.

### Parked, not fixed

- `UploadsController` buffers a whole `IFormFile` into a managed `MemoryStream` — 50 MB per
  request, 500 MB per batch, reachable with a free guest token. Same class as PPW-558 but
  bounded by an explicit `[RequestSizeLimit]` and load-bearing (`MimeValidator` needs a seekable
  stream), so changing it is a design question for the owner, not this round's fix. Noticed by
  cluster A's micro-review; no backlog row minted, because routing it is the owner's call.
- The bounded webhook read has no cancellation test (a client abort mid-read). The middleware
  already answers that path; the gap is coverage, not behaviour.

### Also parked (PPW-559)

- `Services/Sameday/AwbCreator.cs:129` and `:243` hold PPW-559's exact disproven belief:
  `PreserveClaim: true` keeps `AwbClaimedAt` so a possibly-billed AWB "waits out the TTL", but
  the TTL is 5 minutes and the re-enqueue cadence is 60, and only `AwbGiveUpHours = 24` bounds
  the re-creates — up to about 24 blind vendor calls, with no attempt counter. ADR-015 accepts
  duplicate creates, but not that reasoning. Found by cluster B's micro-review; fixing it is a
  behaviour change in another feature, so it is the owner's call, not this round's.
- The park emits an Error log and the `failed` status metric but no Sentry capture, and attempts
  below the budget emit no metric of their own. Deliberate: the status meter counts status
  transitions, and this repo reserves Sentry for systemic outages, not per-row fiscal items.
- One interleaving stays untested: the count landing and the park losing its CAS inside the same
  call. It self-heals — the row stays `Pending` at the budget and the next attempt parks it, which
  is tested — and forcing the window needs a mutation between two `ExecuteUpdateAsync` calls.

### Two renderer defects fixed in passing — review tooling, not a defect of this target

`render-records.mjs` read this round's empty `fixed_commit` as the text `closed:`, because its
regex crossed a newline into the next frontmatter key, and it counted `pre_cleared_consumed` as 0
because the worklog listed the three consumed ids where the renderer expected a number. Both are
fixed in the script, which now accepts either shape; its suite passes 74 assertions, 7 of them new
and 3 of those proven red against the old readings. The two wrong values stay in the round-9 metrics line
with a correction line each, because the metrics schema says a wrong line is corrected and never
edited. Recorded here because this round's commit carries the change; the review system is its own
target, so any further tracking of these belongs there, not on a `PPW-<n>`.

### The reload replaces the second save, and only two side effects are suppressed (PPW-564)

The pre-check corrected two things about the drafted approach and both mattered. The window the
finding named — the unique-index violation — is the narrower one: the creation service's existence
query returns a winner's committed invoice as an *unchanged* entity, so nothing throws and the
admin's own `PaidAt` is committed anyway. The gate is therefore on the entity's tracked state as
well as in the catch. And the reload has to *replace* the second `SaveChangesAsync`, not follow it,
or it re-reads the value it was meant to drop. Suppressed: the confirmation email and the paid
notification. Kept: the SignalR broadcast, the purge hook and the 200 response, because the order
really is Paid — just not by this request. The path gets its own `PaidSaveOutcome` because the
webhook's is private to that controller and pinned by a test that reflects on it. Proven against
real PostgreSQL: EF InMemory has no unique index and the violation classifier only matches the
Npgsql error, so the drafted test would have passed without the fix — the pre-check named that too.

### The exhausted branch answers 409, and its rollback may not throw (PPW-567)

Four number collisions used to let the raw `DbUpdateException` escape as a 500 with the order still
tracked Paid and nothing captured. It now mirrors the webhook's terminal catch: an Error line
carrying the order number, total and both payment identifiers, a Sentry capture, a reload that
discards the uncommitted transition, and a `ConflictException` so the endpoint answers 409 rather
than a Paid-looking 200 — `ProducesResponseType` updated, and §15.10 of the deployment guide now
tells an operator what the log line means and to check the sequence against `MAX("Number")` before
asking for a retry. Two deliberate choices: the rollback swallows its own reload failure, because a
throw there would turn the conflict into an unexplained 500; and the Sentry hub is an *optional*
constructor dependency, since Sentry registers no hub unless `Sentry:Enabled` — one test resolves
the service from a container with no hub registered, so a missing registration cannot break boot.
No metric: this endpoint has none today, and adding an instrument is wider than the finding asked.

### PPW-568's branch tests ship in the commits whose fix they guard (PPW-568)

Its own commit could only carry what was left: relocating the happy-retry test into the new
Postgres-backed class and adding the container-resolution test. The already-invoiced and exhausted
branch tests are in `c9ffae4` and `ba8e628`, because a regression test landing a commit later than
its fix leaves one commit where the tree is green for the wrong reason. All five branch tests now
share one migrated database instead of two, and the EF-InMemory class keeps only the tests that do
not need a real unique index.

### Class swept, one sibling examined and left (PPW-564)

`OrderService.cs:185` holds the third `SaveChangesAsync` retry loop in the codebase, and its
exhaustion also escapes untyped. Left alone on purpose: nothing is charged on that path (the order
is being created, not paid), the loop's own comment states that the escape is the intended signal
for a persistent clash, and on PostgreSQL the per-year sequence cannot reach it. The two
invoice-creating Paid paths are now both gated on an outcome — the webhook's Stripe and EuPlatesc
branches already were.

### Cluster D's micro-review: eight gaps folded in (PPW-564, PPW-567)

The side-effect gate was a negation, so a fourth outcome member added later would have mailed a
second confirmation; it now reads `paidOutcome is null or Created`, like the webhook's. The
entity-state gate tests for `Unchanged` rather than "not Added", so an untracked invoice cannot be
mistaken for a committed winner. `AbandonToWinnerAsync`'s reload was the one unprotected await
left: a cancelled read would have turned a benign lost race into a 500 plus a Sentry capture for an
order that *is* Paid, so it now swallows and logs `admin.order.abandon-reload-failed`, with a test.
The pre-insert window emitted no log at all; both windows now log `admin.order.invoice-already-created`
with a `window=` field, asserted on either side. The 409 message was English in a Romanian product.
Docs: the deployment row no longer implies a Sentry page when `Sentry:Enabled=false` is the shipped
default and it names the API-only call, `metrics.md` records that the admin path enters no meter,
and the in-product admin state-machine page no longer claims the transition is webhook-only, that
every invalid transition answers 400, or that a confirmation email always goes out.

### Parked from cluster D's micro-review

- The admin path emits no metric, so its exhausted outcome is invisible to the SLO the webhook's
  `failed` label feeds. A new instrument means a `MetricNames` contract entry and a dashboard row —
  wider than the finding asked; the gap is now stated in `metrics.md` instead.
- `maxNumberRetries = 3` is a second copy of the webhook's ceiling. Both predate this round and
  neither derives from a named constraint, so hoisting it is a refactor of the sibling, not a fix.
- The admin panel has no `AwaitingPayment` entry in `NEXT_STATUSES`, so this transition is reachable
  only through the API, and the order-detail page collapses every failure into one Romanian sentence,
  discarding the 409's detail. Both are questions about what the panel should offer — an owner call.
- `docs/stories/epic-5-admin/US-504/backend-admin-api.instructions.md` still documents 200/400 only;
  story instruction files are build-time specs rather than maintained references, so left alone.
- The container-resolution test builds its own `ServiceCollection`: it proves the optional hub
  parameter resolves, not that `Program.cs`'s registration still injects the real hub.

