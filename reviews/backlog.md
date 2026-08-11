---
type: review-backlog
updated: 2026-08-11
---

# Backlog — unfixed minors from closed targets

A row enters when its target closes. A row leaves only two ways, and only after
the terminal state is written back to its home ledger row: fixed (with the normal
verification a backlogged minor requires) or owner-ruled wont-fix. Empty file
means nothing is owed. The pre-deployment regression phase requires it empty.
Seeded 2026-08-10 from every closed target: the ledgers of 015/043/044-045 plus
035's four close-time accepted deferrals (pre-ledger era, grandfathered IDs).
042's ledger had no surviving backlog rows.

| D# | Target | Sev | What | Area |
|---|---|---|---|---|
| DB-1 | 035-payment-idempotency | 🟠 | Entire Postgres production DB path is unexercised by tests — deferred to the migration/3-env phase | `data-stack/migrations` |
| SEC-1 | 035-payment-idempotency | 🟡 | Global single-column idempotency-key uniqueness = cross-tenant existence oracle + key-squatting; durable fix needs a per-tenant composite index (migration) | `Orders/idempotency` |
| BUG-2 | 035-payment-idempotency | 🟡 | EuPlatesc recovery-replay regenerates a different redirect URL (no gateway idempotency key); row-lock fix needs the Postgres arm | `Controllers/payments` |
| DB-2 | 035-payment-idempotency | 🟡 | Model snapshot is SQLite-flavored — next Npgsql migration scaffolds a phantom diff | `data-stack/migrations` |
| D90 | 015-sameday-shipping | 🟡 | `ISamedayAuthenticator` singleton captures the transient typed `ISamedayClient` → handler never rotated (pre-existing, carried into the new extension) | `sameday/shipping` |
| D91 | 015-sameday-shipping | ⚪ | `ISamedayClient` doc still claims NotImplementedException "until bolt 037" — stale twin of the claim stripped from `SamedayClient.cs` | `sameday/shipping` |
| D92 | 015-sameday-shipping | ⚪ | `AwbNumber` (varchar(100)) is the unclamped sibling of D60's clamp on the same post-bill persist | `sameday/shipping` |
| D93 | 015-sameday-shipping | ⚪ | `Created` outcome reports the unclamped LabelUrl while the row stores null | `sameday/shipping` |
| D94 | 015-sameday-shipping | ⚪ | `MaxRequestsPerSecond` missing from appsettings.json, the settings validator, and bolt-037 ddd-02 | `sameday/shipping` |
| D95 | 015-sameday-shipping | ⚪ | D67's 30 s poll buffer is a flat constant, not scaled to the interval | `sameday/shipping` |
| D96 | 015-sameday-shipping | ⚪ | Record accuracy: resolution-v5/index.md say "backend 914" (tip = 916) and "fixed: 30" (frontmatter holds 41); index cites `66c6d50` not the tip `1816f5f` | `sameday/shipping` |
| D23 | 015-sameday-shipping | 🟡 | Dual-DB parity: migrations + `timestamptz` CAS never run on Postgres (offset-write may throw) *(hinted)* | `sameday/shipping` |
| D40 | 015-sameday-shipping | ⚪ | New migration designer snapshots embed stale `StripeClientSecret` 255 vs 512 *(hinted)* | `sameday/shipping` |
| D39 | 043-cloud-storage-provider | 🟡 | Renamed `ExecuteAsync_ArchiveDisabled/_CloudTierOff` guard tests seed an empty DB → guard removal enqueues nothing anyway → `VerifyNoOtherCalls()` passes for the wrong reason. Seed a stuck Paid+Local order | `storage/gallery` |
| D40 | 043-cloud-storage-provider | 🟡 | Anti-refresh-loop guard (`urlsRefreshed`) untested — no spec dispatches a *second* img `(error)` to assert no third `getOrderPhotos` fetch | `storage/gallery` |
| D41 | 043-cloud-storage-provider | 🟡 | Lightbox focus-trap (`trapFocus` Tab/Shift+Tab `preventDefault` + refocus) has no spec — drop `preventDefault` and Tab escapes the modal, no test reddens | `storage/gallery` |
| D42 | 043-cloud-storage-provider | 🟡 | Auto-heal shows "Reîncarcă pagina" while silently re-fetching a fresh URL → the user is told to reload for an error the app then auto-recovers. Show a neutral reloading state first | `storage/gallery` |
| D43 | 043-cloud-storage-provider | 🟡 | 401 on order fetch for a non-authenticated user (interceptor guest branch only clears the token, no navigate) → `loadOrder` sets neither error nor redirect → blank order body, no retry | `storage/gallery` |
| D44 | 043-cloud-storage-provider | ⚪ | Order retries + `ngOnInit` subs have no in-flight dedup / `takeUntilDestroyed` / `switchMap` — last-arriving-wins on rapid retries; late response sets signals post-destroy | `storage/gallery` |
| D45 | 043-cloud-storage-provider | 🟡 | F9's ZIP pre-flight throws `InvalidOperationException` — unmapped → generic 500 logged "Unhandled exception"; ops can't tell config-error from a crash. Map to 409/422 + Warning log | `storage/gallery` |
| D48 | 043-cloud-storage-provider | 🟡 | Lightbox `failed()` reset keyed on `src !== lastSrc`; an identical refreshed presigned URL leaves `failed` stuck (+ `urlsRefreshed` blocks retry) → error until page reload | `storage/gallery` |
| D61 | 043-cloud-storage-provider | 🟡 | Retention `OrderBy/Take` window starved by persistent delete-failures (`ArchiveRetentionJob.cs:98`) | `storage/gallery` |
| D62 | 043-cloud-storage-provider | 🟡 | Admin ZIP mid-loop `GetStream` failure (incl. concurrent purge) truncates the archive after headers committed (`AdminOrderService.cs:197`) | `storage/gallery` |
| D63 | 043-cloud-storage-provider | 🟡 | Preview cache-fill regeneration races retention delete → orphaned blob, ref nulled (`UploadService.cs:203` / `ArchiveRetentionJob.cs:124`) | `storage/gallery` |
| D64 | 043-cloud-storage-provider | 🟡 | Failed best-effort local delete in `OrderPhotoPromoter` leaks unreclaimable local bytes (`OrderPhotoPromoter.cs:212`) | `storage/gallery` |
| D65 | 043-cloud-storage-provider | 🟡 | `LocalStorageService` storage-root re-anchor uses prefix match without a separator boundary (`LocalStorageService.cs:99`) | `storage/gallery` |
| D66 | 043-cloud-storage-provider | 🟡 | ZIP entry extension taken from untrusted client filename instead of validated MIME (`AdminOrderService.cs:190`) | `storage/gallery` |
| D67 | 043-cloud-storage-provider | 🟡 | Batch upload has no file-count cap (only 500MB total) (`UploadsController.cs:102`) | `storage/gallery` |
| D68 | 043-cloud-storage-provider | 🟡 | Broken grid thumbnails have no fallback/retry after the single presigned-URL refresh (`order-detail-page.ts:472`) | `storage/gallery` |
| D69 | 043-cloud-storage-provider | 🟡 | Originals of orders never reaching production-complete/Cancelled escape the retention window (`ArchiveRetentionJob.cs:92`) | `storage/gallery` |
| D70 | 043-cloud-storage-provider | 🟡 | 043 NFR "persistent S3 → 502 (BadGatewayException)" not implemented → surfaces as 500 (`S3StorageService.cs:145`) | `storage/gallery` |
| D71 | 043-cloud-storage-provider | 🟡 | Idempotent-skip reasons at Debug never emit under the Information floor (`OrderPhotoPromoter.cs:120`) | `storage/gallery` |
| D72 | 043-cloud-storage-provider | 🟡 | Transient vs permanent cloud-write failures collapsed into one Warning → poison retried like a blip (`OrderPhotoPromoter.cs:182`) | `storage/gallery` |
| D73 | 043-cloud-storage-provider | 🟡 | `GetPreviewAsync` dropped `AsNoTracking` on the hot cache-hit path (`UploadService.cs:139`) | `storage/gallery` |
| D74 | 043-cloud-storage-provider | 🟡 | Promotable-status set triplicated with a false "single source of truth" comment (`BackfillCommand.cs:43`) | `storage/gallery` |
| D75 | 043-cloud-storage-provider | 🟡 | S3 Polly retry classification/re-upload + presign HTTP/HTTPS protocol untested (`S3StorageService.cs:60/41`) | `storage/gallery` |
| D76 | 043-cloud-storage-provider | 🟡 | Storage DI/config/CLI outside the lens manifest; AWS-native region config foot-gun (`StorageExtensions.cs:56`) | `storage/gallery` |
| D77 | 043-cloud-storage-provider | 🟡 | Recovery/retention sweeps run unindexed full scans every 6h; masked by InMemory tests (`UploadConfiguration.cs:30`) | `storage/gallery` |
| D78 | 043-cloud-storage-provider | ⚪ | Promoter materializes the whole original via `ToArray` + multiple undisposed `MemoryStream`s (`OrderPhotoPromoter.cs:138`) | `storage/gallery` |
| D79 | 043-cloud-storage-provider | ⚪ | Best-effort orphan-thumbnail delete swallows its exception with no log (`UploadService.cs:222`) | `storage/gallery` |
| D80 | 043-cloud-storage-provider | ⚪ | Local-preview `Cache-Control` mismatch: ADR-008 says public/immutable, code sends `private` (`UploadsController.cs:26`) | `storage/gallery` |
| D81 | 043-cloud-storage-provider | ⚪ | Freshly generated local thumbnail re-read from disk on cache miss (`UploadService.cs:240`) | `storage/gallery` |
| D82 | 043-cloud-storage-provider | ⚪ | Redundant dual feedback: interceptor toast plus inline error/redirect (`order-detail-page.ts:403`) | `storage/gallery` |
| D86 | 043-cloud-storage-provider | 🟡 | Retention deletes blobs before persisting key-null → broken-URL window on a concurrent read (`ArchiveRetentionJob.cs:146`) | `storage/gallery` |
| D87 | 043-cloud-storage-provider | 🟡 | Retention sweep query omits the `DeletedAt` filter → reprocesses soft-deleted rows, re-emits false audit (`ArchiveRetentionJob.cs:96`) | `storage/gallery` |
| D88 | 043-cloud-storage-provider | 🟡 | Promoter tests assert cloud-write keys but never the bytes written (`OrderPhotoPromoterTests.cs`) | `storage/gallery` |
| D90 | 043-cloud-storage-provider | 🟡 | D36 close-*during*-refresh resolve-time re-read has no spec (only close-before-error is tested) (`order-detail-page.spec.ts`) | `storage/gallery` |
| D62 | 043-cloud-storage-provider | 🟡 | ZIP mid-loop `GetStream` truncation — v9 adds the concurrent-promotion trigger (Local→Cloud+delete) alongside the original concurrent-purge one (`AdminOrderService.cs`) | `storage/gallery` |
| D24 | 044-045-observability | 🟡 | Scope enricher registered after auth — pre-auth failures reach Sentry untagged | `Program.cs` |
| D25 | 044-045-observability | 🟡 | EF spans ship full SQL and exception messages to OTLP unscrubbed | `Extensions` |
| D26 | 044-045-observability | 🟡 | `NaN` sample rates pass both validators and silently drop everything | `Validators` |
| D27 | 044-045-observability | 🟡 | `PrometheusEndpoint="/"` passes validation and would gate the whole site | `Validators` |
| D28 | 044-045-observability | 🟡 | `ValidateOnStart` wiring untested — narrowed at v2: now exercised by `An_unparseable_allow_list_entry_aborts_boot`; only the blank-`PrometheusEndpoint` leg remains untested | `Program.cs` |
| D29 | 044-045-observability | 🟡 | Enricher sets `scope.User.Id` instead of the required `user_id` tag | `Middleware` |
| D30 | 044-045-observability | 🟡 | Sampler startup log (story 003 AC) not implemented — changed shape at v2: `RouteAwareSampler.cs` is gone so there is no "resolved table", but nothing logs the sampler choice at boot and `Description_includes_the_rate_for_the_startup_log` pins a description for a log that does not exist | `Observability/Sampling` |
| D31 | 044-045-observability | 🟡 | Neither subsystem logs its enabled state at boot — narrowed at v2: `observability.tracing.disabled` now covers the blank-endpoint case; Sentry's state and the observability master flag are still unlogged | `Program.cs` |
| D32 | 044-045-observability | 🟡 | Unsynchronized capture collections in the shared test fixture | `Tests/Integration` |
| D33 | 044-045-observability | ⚪ | Magic `"unknown"` label escapes `MetricNames`, docs and the cardinality budget | `Services` |
| D34 | 044-045-observability | ⚪ | `///` blocks on concrete classes citing bolt/ADR/story IDs | `Observability` |
| D35 | 044-045-observability | ⚪ | Comment-sweep residue: dangling `/`, surviving bolt citations, run-on lines | `Program.cs` |
| D36 | 044-045-observability | ⚪ | `ddd-02` describes the `Random` approach ADR-017 forbids | `memory-bank/bolts/044-tracing-and-metrics` |
| D37 | 044-045-observability | ⚪ | Metric vocabulary shipped ahead of emission (ANAF; constant `status` label) | `Observability` |
| D38 | 044-045-observability | ⚪ | Observability config re-read by string key after binding; duplicated default | `Program.cs` |
| D39 | 044-045-observability | ⚪ | Sentry wiring inlined in `Program.cs` while bolt 044 got an extension method | `Program.cs` |
| D51 | 044-045-observability | 🟡 | `TracingExporterSelectionTests` boots live TracerProviders outside `ObservabilityHostCollection` | `Tests/Unit/Observability` |
| D52 | 044-045-observability | 🟡 | `payment_failed` records `failed` unconditionally where its sibling uses `duplicate` | `Controllers` |
| D53 | 044-045-observability | 🟡 | `MaskedForm` suggests an `::ffff:…/112` form the parser then rejects | `Observability` |
| D54 | 044-045-observability | 🟡 | `A_mapped_404_is_not_captured_to_sentry` is satisfied by an unrouted request | `Tests/Integration` |
| D55 | 044-045-observability | 🟡 | The documented `Sentry__Debug=true` verbosity knob is inert under Serilog's Information floor | `docs` |
| D56 | 044-045-observability | 🟡 | No volume ceiling on the new Sentry capture site | `Middleware` |
| D58 | 044-045-observability | 🟡 | §13.10 still says a No-Data panel means a name mismatch, contradicting the accepted panel-8 decision | `docs` |
| D59 | 044-045-observability | 🟡 | AWB shutdown carve-out matches only `OperationCanceledException`; tests run on SQLite, prod is Postgres | `Services/Sameday` |
| D60 | 044-045-observability | 🟡 | `CapturingSentryTransport.Payloads` is an unsynchronized `List` across threads | `Tests/Helpers` |
| D61 | 044-045-observability | 🟡 | `wrong_listener` and `not_allowed` denials share one 512-entry log budget | `Middleware` |
| D62 | 044-045-observability | 🟡 | A throw escaping a webhook endpoint records no metric at all — sibling class resolved the opposite way | `Controllers` |
| D63 | 044-045-observability | 🟡 | `Idempotency-Key` scrubbed, so duplicate-payment triage loses the colliding key | `Configuration` |
| D64 | 044-045-observability | 🟡 | The fail-closed drop is never exercised through the hook, and has no metric behind it | `Configuration` |
| D65 | 044-045-observability | ⚪ | Empty allow-list entry error names neither value nor index | `Observability` |
| D66 | 044-045-observability | ⚪ | `Scrub(Breadcrumb)` restamps `Timestamp` | `Configuration` |
| D67 | 044-045-observability | ⚪ | bolt-045 walkthrough lines 39/46 still describe the deleted deny-list | `memory-bank/bolts/045-error-tracking-and-slos` |
| D68 | 044-045-observability | ⚪ | Series-count failure never names `DeclaredInstruments()` | `Tests/Unit/Observability` |
| D69 | 044-045-observability | ⚪ | `LogCapture` discards category and exception | `Tests/Helpers` |
| D70 | 044-045-observability | ⚪ | Nothing proves `ContractViolations()` ever returns non-empty | `Tests/Helpers` |
| D71 | 044-045-observability | ⚪ | "Background roots stay dropped" holds only below rate 1.0 | `Observability/Sampling` |
| D72 | 044-045-observability | ⚪ | Stale-`Routes` boot abort sits below the `Enabled` early return, so §14.8 step 1 cannot catch it | `Extensions/ObservabilityExtensions.cs:46 (was :42; the D41` |
| D73 | 044-045-observability | ⚪ | Promotion emits no in-app signal, so "stopped" and "no errors" look identical | `Observability` |
| D85 | 044-045-observability | 🟡 | The breadcrumb test is absence-only — green with every breadcrumb dropped | `Tests/Integration` |
| D86 | 044-045-observability | 🟡 | `metrics.md`'s add-a-metric procedure never states `MetricCapture`'s execution-context requirement | `memory-bank/operations` |
| D87 | 044-045-observability | 🟡 | ADR-017 still says "a promoted error trace is a single root span" 19 lines below its own amendment | `memory-bank/bolts/044-tracing-and-metrics` |
| D88 | 044-045-observability | 🟡 | Walker reach: `templating`/`annotations` queries and library panels unwalked; query-side parser mis-handles an escaped quote the exposition side handles | `Tests/Integration` |
| D90 | 044-045-observability | 🟡 | `OrderPhotoPromoter` hand-rolls `HasBeenPaid` as an ordinal comparison plus two name exclusions | `Services` |
| D91 | 044-045-observability | 🟡 | `AdminOrderService` logs client-disconnect cancellations at `Error` on its highest-signal strings | `Services` |
| D92 | 044-045-observability | 🟡 | The `HasBeenPaid` invariant test excludes `Cancelled` by name, pushing a future author to add a refund status to `PaidStatuses` | `Tests/Unit/Services` |
| D93 | 044-045-observability | 🟡 | `Verdict` counts ports, not reachability: a loopback-only API port plus a wildcard scrape port passes both rules | `Observability` |
| D94 | 044-045-observability | 🟡 | `PrometheusEndpoint` is coupled to the `Caddyfile`'s hard-coded path by comment only | `Configuration` |
| D95 | 044-045-observability | 🟡 | `TracingWired == false` in Production warns and boots — same warn-only class as the admitted `ScrapePort == 0` | `Program.cs` |
| D96 | 044-045-observability | 🟡 | Inbound `baggage` rides out to Stripe, Sameday and Google | `Extensions` |
| D98 | 044-045-observability | 🟡 | ADR-017's anti-salting rationale is now weaker than the ADR states — the amendment abandoned its only consumer | `memory-bank/bolts/044-tracing-and-metrics` |
| D99 | 044-045-observability | ⚪ | `MetricCapture._outer` is provably always null; `Dispose`'s restore is dead code advertising forbidden nesting | `Tests/Helpers` |
| D101 | 044-045-observability | ⚪ | bolt-044 ddd docs declare a four-value `result` set and `ParentBasedSampler` as shipped | `memory-bank/bolts/044-tracing-and-metrics` |
| D102 | 044-045-observability | ⚪ | `resolution-v2.md` misdescribes TestServer as reporting the addresses feature "present but empty" — it returns null | `Observability` |
| D124 | 044-045-observability | ⚪ | The guarded-selector list is hand-maintained, so a fifth hand-named success numerator ships unpinned and nothing notices — and the stated reason for not writing the class rule does not hold for a rule keyed on literal `=` matchers | `Tests/Integration` |
