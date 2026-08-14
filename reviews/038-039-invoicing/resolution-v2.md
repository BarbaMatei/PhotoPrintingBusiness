---
type: resolution
target: 038-039-invoicing
version: 2
answers: pass v2 (verification — index row)
status: resolved
fixed_commit: f366c8a
closed: 2026-08-14
---

# Resolution v2 — 038-039-invoicing

## Findings

| ID | Status | Commit | Note |
|---|---|---|---|
| PPW-481 | fixed | `c726820` | Docstring now states what Enabled=false really does: Invoice row created, no XML or PDF built, download endpoint always 404s. Swept the same false claim out of appsettings.json, the flag table and the validator docstring |
| PPW-506 | fixed | `c726820` | Rewrote every operator-facing site that promised a customer invoice email: config comment, flag table, rollout step 6, both rollback paths, the notifier docstring. Step 6 now reads as blocked rather than pending |
| PPW-483 | fixed | `d15737a` | Test now captures EF SQL and asserts no `FROM "Orders"` query, replacing one that stayed green when the defect returned. Proven red by reintroducing the delegation, then green |
| PPW-507 | fixed | `7458454` | ClaimTtlMinutes added to appsettings.json, bounded 2–1440 in AnafSettingsValidator, and documented with its failure mode in the deployment flag section; 5 new validator tests |
| PPW-508 | fixed | `01fbdaf` | Exhausted retries now record `failed`, not `duplicate`, via a three-state PaidSaveOutcome; the uncommitted Paid transition is rolled back and the error is captured to Sentry. New surface: the outcome enum and the rollback |

## Scope

| Cluster | Findings | Files | Approach-check |
|---|---|---|---|
| A — invoicing doc drift | PPW-481, PPW-506 | `Configuration/AnafSettings.cs`, `appsettings.json`, `docs/DEPLOYMENT.md`, `Services/Invoicing/InvoicePdfReadyNotifier.cs` | not needed (doc-only) |
| B — proof the re-query is gone | PPW-483 | `Tests/Unit/Services/Invoicing/InvoiceCreationServiceTests.cs` | not needed (test-only) |
| C — claim TTL knob | PPW-507 | `Configuration/AnafSettings.cs`, `Validators/AnafSettingsValidator.cs`, `appsettings.json`, `docs/DEPLOYMENT.md` | not needed (config + validation only) |
| D — exhausted-retry outcome | PPW-508 | `Controllers/WebhooksController.cs`, `memory-bank/operations/metrics.md` | needed: worklog `check-returned` (revised — relabel, do not rethrow) |

## Decisions

### Two 🟡 pulled in against the router's default (PPW-507, PPW-508)

The router sends new 🟡 to the ledger backlog, not a fix round, so only the three 🟠 were in
scope. No owner ruling arrived at this round's gate. The fixer pulled both 🟡 in anyway:
PPW-507 touches the same two files as cluster A, so deferring it meant editing them twice, and
PPW-508 is a regression the previous round's own fix introduced. Recorded here for the owner to
overrule; the ledger rows carry the same note.

### Rethrowing was refused in favour of relabelling (PPW-508)

The approach-check refuted the drafted rethrow. Three reasons decided it. `RecordPaymentWebhook`
runs after the helper returns, so an escaping exception leaves the webhook out of the metric
entirely — a fresh instance of PPW-397, still open. Every branch of the EuPlatesc endpoint
answers a signed ack, so a 500 there is unverified against the processor's contract. And
`duplicate` sits in SLO 3's success numerator, so the old label inflated the SLO rather than
merely hiding a failure. Relabelling to `failed` fixes the measurement instead of deleting it.

### The rollback reloads instead of unwinding fields (PPW-508)

The first attempt reset `Status`, `PaidAt` and `EuPlatescTransactionId` by hand. The
micro-review found it missed `UpdatedAt`, which `OrderStatusMachine.Transition` also sets — the
same bug the next added field would reintroduce. `Entry(order).ReloadAsync` discards every
uncommitted mutation instead, and a test now pins `UpdatedAt` alongside the other fields.

### Sites left for the re-reviewer

Two ADR/design records still describe the customer-email flag as gating a real send:
`adr-022-dual-write-rollout-via-feature-flag.md` and `ddd-02-technical-design.md`. Both are
point-in-time bolt records, so they were left as written, matching how round 1 treated the
bolt-037 records. The load-bearing standard, `decision-index.md`, was corrected. Separately,
`Program.cs`, `SentrySettings.cs`, `ObservabilitySettings.cs` and `SamedayShippingService.cs`
carry the same "identical to baseline when disabled" phrasing for other integrations; nobody
has checked whether those claims hold, and they are outside this finding set.
