---
type: resolution
target: 038-039-invoicing
version: 6
answers: review-v6.md
status: resolved
fixed_commit: 2979ea0
closed: 2026-08-21
---

# Resolution v6 — 038-039-invoicing

## Findings

| ID | Status | Commit | Note |
|---|---|---|---|
| PPW-512 | fixed | `8b79a5a` | The builder now refuses an empty StreetName, CityName or PostalZone instead of filing them blank; 4 tests, 3 proven red. The address a locker order should carry stays an owner question — see Decisions |
| PPW-513 | fixed | `8917f9f` | Index expression pins the timestamp to UTC, which is immutable. The fix carried into the regenerated `InitialPostgres` baseline, and every Postgres-backed test now migrates through it |
| PPW-514 | fixed | `6aabff9` | The Error log now carries order number, total and both processor references, captured before the rollback reload discards them |
| PPW-515 | fixed | `42e5988` | The tick guards cancellation that is not a shutdown, so an ANAF, storage or DB timeout no longer stops the host; an upload timeout holds its claim rather than re-uploading. See Decisions |
| PPW-517 | fixed | `67df511` | `Invoice.StorageLocation` stamped with the path in one save; the read treats it as a preference with a fallback and a mismatch log. `data-stack.md` updated in the same change |
| PPW-518 | fixed | `084c579` | Classifiers extracted to `InvoiceUniqueViolation` and reused, so manual mark-Paid retries a taken number instead of 500ing. Proven against real Postgres, not a mock |
| PPW-521 | fixed | `8917f9f` | Truncate is null-tolerant; 8 tests with both guards proven red |
| PPW-522 | fixed | `03f71c5` | A row that just recorded an error waits one poll interval before re-selection, so it stops heading every batch. Terminal state split out — see Decisions |
| PPW-523 | fixed | `7e4a215` | A missing blob logs `invoice.pdf.blob-missing` and answers a 404 with no Retry-After, distinct from the not-yet-rendered case |
| PPW-527 | fixed | `b108c25` | An unclassified invoice-creation failure now records the webhook before rethrowing; cancellation stays deliberately unrecorded. Both legs proven separately |
| PPW-529 | fixed | `8aea0b7` | `PostgresTestDatabase` migrates, so every Postgres test applies the real chain; three tests pin that plus the composite index and its violation |
| PPW-531 | fixed | `8aea0b7` | The composite index is now classified as a number collision, proven by a real violation naming the constraint |
| PPW-532 | fixed | `5300b78` | One credential failure logs and captures once per tick and summarises the rest, instead of up to 50 incidents |
| PPW-533 | fixed | `5300b78` | A status-aware `RecordErrorAsync` records the reason on Submitted rows too, which is the dominant case |
| PPW-534 | fixed | `8aea0b7` | Renamed to `invoice.creation.number-attempted`, because the number is logged before commit and a retry may discard it |
| PPW-535 | fixed | `8917f9f` | Truncate never splits a surrogate pair; same method as PPW-521, so it was closed in the same change |
| PPW-516 | deferred | — | Approach-check advised deferral on four grounds and the Postgres-only move removed its reachability. See Decisions |
| PPW-519 | disputed | — | The behaviour is a vetted prior decision, not a defect. See Decisions |
| PPW-520 | deferred | — | Depends on a rule the repo does not document, and the check that would settle it was never built. See Decisions |
| PPW-524 | deferred | — | Owner ruled the missing SPA consumer out of scope for this round |
| PPW-525 | deferred | — | Needs the order-transfer capability that was never built; not a defect this round can close. See Decisions |
| PPW-526 | wont-fix | — | Owner ruled EuPlatesc is being removed, so its coverage was waived rather than written; PPW-511 tracks that the removal is untracked |
| PPW-528 | deferred | — | Wants a metric value whose only use is a dashboard panel this round should not add. See Decisions |
| PPW-530 | false-positive | — | The migration it names was deleted by the Postgres-only squash; one baseline builds the index on an empty database, so there is nothing to de-duplicate |

## Scope

| Cluster | Findings | Files | Approach-check |
|---|---|---|---|
| A — upload job resilience | PPW-515, PPW-522, PPW-532, PPW-533 | `Services/Invoicing/Anaf/InvoiceUploadJob.cs`, `AnafSpvClient.cs`, `AnafExceptions.cs`, `InvoiceLifecycle.cs` | needed: worklog `check-returned` (revised — guard moved to the tick, PPW-522 split) |
| B — webhook metric safety | PPW-516, PPW-527, PPW-528 | `Controllers/WebhooksController.cs` | needed: worklog `check-returned` (revised — wrapper narrowed, PPW-516 and PPW-528 deferred) |
| C — UBL address and price | PPW-512, PPW-520, PPW-521, PPW-535 | `Services/Invoicing/InvoiceXmlBuilder.cs`, `InvoiceAddressFormatter.cs` | needed: worklog `check-returned` (revised — locker fallback refuted, two owner questions raised) |
| D — storage tier | PPW-517, PPW-523 | `Controllers/InvoicesController.cs`, `Models/Invoice.cs`, `Data/PhotoPrintDbContext.cs`, migration | needed: worklog `check-returned` (revised — no config backfill, read as preference) |
| E — invoice numbering integrity | PPW-513, PPW-518, PPW-529, PPW-530, PPW-531, PPW-534 | `Migrations/`, `Services/Invoicing/InvoiceUniqueViolation.cs`, `Services/AdminOrderService.cs` | not needed (reuses an existing mechanism) |
| F — parked by ruling | PPW-524, PPW-525, PPW-526 | — | not needed (no code change) |

## Decisions

### Every approach-check came back revised (PPW-515)

Four checks ran and none cleared. Each corrected a material error in the drafted approach, and the
corrections are why this round's fixes differ from what the review suggested. For PPW-515 the guard
belongs in the tick, not the ANAF client, because a storage or database timeout reaches the same
exit; the client-side conversion is an accuracy improvement on top. An upload timeout now holds its
claim, because ANAF may already hold the invoice — only polling is safely idempotent.

### The drafted terminal state was a silent no-op (PPW-522)

`MarkFailedAsync` guards on the row being Submitted, so calling it for a build failure on a Pending
row affects nothing and returns false. It would also have looked green, because the job tests mock
the lifecycle. Only the starvation half shipped: a row that just recorded an error waits one poll
interval. The terminal half needs a new transition, a real attempt counter and an ADR superseding
ADR-024, so it is not in this round.

### The locker address is still an owner question (PPW-512)

The builder now refuses rather than filing empty mandatory elements, which is strictly better than
what shipped. What it cannot do is invent an address: `EasyboxLocker` carries no postal code, the
only precedent is a shipping sentinel that would be fabricated data on a fiscal document, and
substituting the locker would assert the buyer lives at a parcel locker. A buyer-owned
`SavedAddress` exists but only for registered users. Locker orders therefore cannot be invoiced
until that ruling lands.

### No emitted-scale change without a validator (PPW-520)

The repo documents two decimal places for emitted amounts and says nothing about a separate scale
for a unit price or about `BaseQuantity`. Story 001 requires the output to validate against a
bundled schema in the tests; that check was never built, so nothing local can adjudicate the
options. Changing what is emitted on a guess is the wrong move on a fiscal document.

### The prior decision stands (PPW-519)

Clearing `XmlPayload` on retry is what PPW-480 deliberately chose, on a vetted approach-check, so a
retry rebuilds the XML instead of resubmitting a stale payload. The finding reads that as losing the
submitted-XML snapshot. Both are true; the trade was made knowingly and the alternative reintroduces
the defect PPW-480 fixed. Recorded as disputed rather than reopened.

### Two rows wait on capability, not effort (PPW-525)

Guest invoice access is defeated by the guest-session lifetime and by an order-transfer capability
that was never implemented. Nothing in this round's scope closes that, and inventing a transfer
mechanism here would be a feature, not a fix. PPW-528 is similar in shape: its new metric value is
only useful alongside a dashboard panel, and the approach-check was explicit that adding the value
without the panel buys a documentation tax and nothing an operator can see.
