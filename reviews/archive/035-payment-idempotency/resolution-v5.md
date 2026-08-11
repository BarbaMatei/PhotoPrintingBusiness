---
type: resolution
target: 035-payment-idempotency
version: 5
answers: review-v5.md
status: resolved
fixed_commit: fbb4c7c
closed: 2026-06-19
---

# Resolution v5 — 035-payment-idempotency

## Findings

| D# | Status | Commit | Note |
|---|---|---|---|
| D20 | deferred | `659056a` | The durable fix is per-provider migration assemblies, the same follow-up D9 already left open. Did the review's stated minimum instead: the migration comment now spells out the exact phantom difference the next author will see. |
| D21 | fixed | `11e72c1` | Widened to 512 in the model, the undeployed migration and the snapshot. A guard asserts the configured maximum is at least 512, so it runs on any provider and needs no Postgres. |
| D22 | fixed | `e957ac1` | The divergent-field list is computed once and written into both the development shape and the production shape. Unit tests cover both, and one HTTP test reads the field names out of the body. |
| D23 | fixed | `6aad926` | The catch now confirms the violated constraint instead of inferring it from a second lookup, so unrelated write failures propagate honestly. The test forces a double violation and was proven by reverting the fix. |
| D24 | fixed | `c7c2b97` | The lookup refuses outright when both owner ids are null, covering every caller. One existing stale-key test leaned on that path for convenience and was retargeted to a real owner. |
| D25 | fixed | `8278bbe` | The filter refuses a key longer than 80 characters with a 400 before the action runs. An integration test sends an 81-character key with a seeded cart, so a pass would have been a 200. |
| D26 | fixed | `8d5b240` | The middleware emits the reserved conflict event with the correlation id and the field names when it maps the divergent-request conflict. A logger test asserts it fires. |
| D27 | fixed | `1c36ff5` | The recovery replay is documented and emits its own event. Behaviour is unchanged. A new integration test drives the path and proves one order and a usable secret come back. |
| D28 | fixed | `03fa13d` | The document now says the stale key is freed by its own save before the insert, not inside one transaction, and records why a single transaction would collide. |
| D29 | fixed | `03fa13d` | The integration section now states that the gateway is keyed by the order id rather than the caller's key, and why. |
| D30 | fixed | `0bc6ecd` | A freshness check and a replay-or-refuse helper were extracted; both resolution blocks and the public lookup call them. Named for what it does rather than the review's sketched name. |
| D31 | fixed | `24ed333` | A constants class replaces the literals in the database context and the order-number service, and in two further files the review did not name. The migration keeps its literal. |
| D32 | deferred | — | One more controller does the same thing in six places, so the pattern is repository-wide and predates this work. Fixing one controller would be inconsistent churn; the real fix is a boundary decision. |
| D33 | fixed | `fbb4c7c` | Deferred first, then done on request after the checking pass: the duplicated request builders became shared client extensions and the call sites are unchanged. The unit builder and the fixture setup were left alone. |
| D34 | fixed | `738993e` | The compiler warning is suppressed at that one call with a justification: the interpolated value is a server-side number and the identifier cannot be parameterised. The coverage gap is unchanged. |
| D35 | fixed | `3faaae6` | Raised by a lens during the checking pass, when D29's edit turned out to be half done: the code sketch further down the same document still forwarded the caller's key. The sketch now passes the order id. |

## Scope

| Cluster | Findings | Files | Approach-check |
|---|---|---|---|
| A — Column width and the divergent-field body (`11e72c1`, `e957ac1`) | D21, D22 | `Data/PhotoPrintDbContext.cs`, `Middleware/ExceptionHandlerMiddleware.cs` | not recorded (predates approach-checks) |
| B — The recovery catch and the owner guard (`6aad926`, `c7c2b97`) | D23, D24 | `Services/OrderService.cs` | not recorded (predates approach-checks) |
| C — Key length at the filter (`8278bbe`) | D25 | `Filters/IdempotencyKeyFilter.cs` | not recorded (predates approach-checks) |
| D — The reserved conflict and recovery-replay events (`8d5b240`, `1c36ff5`) | D26, D27 | `Middleware/ExceptionHandlerMiddleware.cs`, `Controllers/PaymentsController.cs` | not recorded (predates approach-checks) |
| E — Design-document corrections (`03fa13d`, `3faaae6`) | D28, D29, D35 | `memory-bank/…/ddd-02` | not recorded (predates approach-checks) |
| F — Shared helpers and provider constants (`0bc6ecd`, `24ed333`) | D30, D31 | `Services/OrderService.cs`, `Data/PhotoPrintDbContext.cs`, `Services/OrderNumberService.cs` | not recorded (predates approach-checks) |
| G — The suppressed compiler warning (`738993e`) | D34 | `Services/OrderNumberService.cs` | not recorded (predates approach-checks) |
| H — Test-helper consolidation, after the checking pass (`fbb4c7c`) | D33 | `Tests/…/PaymentRequestHelpers.cs` | not recorded (predates approach-checks) |
| I — Left undone this round | D20, D32 | — | not needed (no code changed) |

## Decisions

### The snapshot was left alone and only its trap was documented (D20)

The durable fix is per-provider migration assemblies, which is the follow-up D9's fix already left open.
Regenerating the snapshot now means either committing a phantom migration or hand-rewriting a generated
file into Postgres form, and the application creates its schema directly and never runs migrations, so
none of it is exercised yet. The review's own stated minimum was a breadcrumb, and that is what was
written: the exact column and index changes the next author will see and should discard.

### The conflict event was scoped to the divergent-request case only (D26)

The design reserves that event name for a request that diverges from the one the key already answered.
A key held by a different caller also produces a refusal, but at the middleware it is indistinguishable
from any other refusal, so it was left on the generic warning. This was flagged for the checker to push
back on. A later pass did exactly that and raised it as D37.

### The recovery-replay work is additive, not a behaviour change (D27)

The path was already safe, because the gateway is keyed by the order id. The round added a document, a
distinct log event and a test that characterises the path. The new event line itself is not asserted,
which matches the existing replay event.

### The constraint test leans on the order SQLite reports two violations in (D23)

The test forces an order-number collision and a key collision at once and relies on SQLite reporting the
order-number index first, which is deterministic for a directly created schema. Reverting the fix flips
the exception type, so the test is not vacuous. The fix itself does not depend on that ordering: the
match returns true only for the key index, whichever violation is reported.

### The provider constants went beyond the three files the review named (D31)

Two further files held the same literals. Converting only the named three would have left them free to
drift. The migration deliberately keeps its literal, because a migration is a self-contained historical
artefact and should not couple to a runtime constant a later refactor may move.

### One row was deferred, then fixed on request (D33)

The consolidation was accepted as deferred by the checking pass, and then the owner asked whether the
standing deferrals could be closed. This one could be done without risk and was. The other two were
re-assessed and stayed deferred: D20 belongs with the migration work, and D32 is repository-wide.

### Two record errors are left standing rather than corrected

The pass recorded its verdict as `approved`. Under the protocol as it stands only a certification pass
may use that word, and this was a discovery pass; the verdict is kept as recorded. The pass's metrics
line counts thirteen refuted suspicions while the review body lists twelve. Both numbers are kept as
they were written.
