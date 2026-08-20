---
type: review-backlog
updated: 2026-08-11
---

# Backlog — unfixed minors from closed targets

A row enters when its target closes, or when the owner routes a defect noticed
outside any pass here at a round's gate — that row takes the next number from
[id-counter](id-counter).
A row leaves only two ways, and only after the terminal state is written back to
its home ledger row: fixed (with the normal verification a backlogged minor
requires) or owner-ruled wont-fix. An owner-routed row has no ledger row until a
loop opens for its area; until then it leaves on the owner's ruling alone,
recorded in that round's resolution and in this file's git history. Empty file
means nothing is owed. The pre-deployment regression phase requires it empty.
Seeded 2026-08-10 from every closed target: the ledgers of 015/043/044-045 plus
035's four close-time accepted deferrals. 042's rows were added on 2026-08-11,
and 035's four rows were re-keyed to its ledger the same day, when both targets'
records were retrofitted and their loops closed retroactively. The rows whose
Target reads `inbox` came from the holding file of that name, retired 2026-08-11:
the owner routed them here, and their full evidence text is in that file's git
history. Every row was re-keyed to `PPW-<n>` on 2026-08-11; the old names
translate through [archive/id-map.md](../archive/id-map.md).

| ID | Target | Sev | What | Area |
|---|---|---|---|---|
| PPW-12 | 035-payment-idempotency | 🟡 | The ddd-02 design sketch puts conflict resolution in the controller, the code puts it in the order service | `records` |
| PPW-20 | 035-payment-idempotency | 🟡 | Model snapshot is Npgsql-typed — next Npgsql migration scaffolds a phantom diff | `data` |
| PPW-32 | 035-payment-idempotency | ⚪ | The controller saves through the database context itself rather than through the order service | `payments` |
| PPW-36 | 035-payment-idempotency | 🟠 | Entire Postgres production DB path is unexercised by tests — deferred to the migration/3-env phase | `data` |
| PPW-39 | 035-payment-idempotency | 🟡 | Global single-column idempotency-key uniqueness = cross-tenant existence oracle + key-squatting; durable fix needs a per-tenant composite index (migration) | `payments` |
| PPW-40 | 035-payment-idempotency | 🟡 | EuPlatesc recovery-replay regenerates a different redirect URL (no gateway idempotency key); row-lock fix needs the Postgres arm | `payments` |
| PPW-74 | 042-thumbnail-cache | 🟡 | The migration's Postgres arm and the model snapshot are exercised by no test; the next scaffolded migration would show a phantom column change | `data` |
| PPW-79 | 042-thumbnail-cache | 🟡 | The storage contract assumes a rewindable stream with a readable length; deferred to bolt 043, which closed without taking it | `uploads` |
| PPW-82 | 042-thumbnail-cache | 🟡 | Nothing reclaims a thumbnail written between the cleanup job's read and its commit; deferred to bolt 043, which closed without taking it | `uploads` |
| PPW-85 | 042-thumbnail-cache | 🟠 | The cache-fill write races the cleanup job and strands a thumbnail on the dead row; the liveness re-read narrows the window but does not close it | `uploads` |
| PPW-93 | 042-thumbnail-cache | 🟠 | The one-frame decode cap, the defence against an animated-image bomb, is proven only through the internal helper, not the public call | `tests` |
| PPW-97 | 042-thumbnail-cache | 🟡 | The preview GET writes to the database on a cache miss, so it cannot be routed to a read replica; only documented | `uploads` |
| PPW-101 | 042-thumbnail-cache | 🟡 | Guest-session recovery after a failed init is untested — every specification supplies a successful init | `tests` |
| PPW-117 | 042-thumbnail-cache | 🟡 | `ExistsAsync` has no production caller, and inert test stubs would hide a reintroduced check-then-read | `uploads` |
| PPW-118 | 042-thumbnail-cache | 🟡 | Every cache-miss preview pays an extra database round-trip to spot the soft-delete race; it disappears with PPW-85's fix | `uploads` |
| PPW-119 | 042-thumbnail-cache | 🟡 | Nothing reports how saturated or how queued the decode limiter is, so a wrong slot count looks like ordinary slowness | `uploads` |
| PPW-120 | 042-thumbnail-cache | 🟡 | No test proves the decode slot is released when the decode throws; a leak would block every later preview | `tests` |
| PPW-121 | 042-thumbnail-cache | 🟡 | The allocator-exception-to-422 mapping is proven only by an injected instance, so a library upgrade could break it green | `tests` |
| PPW-122 | 042-thumbnail-cache | 🟡 | A failed thumbnail delete in the cleanup job is untested and silently leaks the file again | `tests` |
| PPW-123 | 042-thumbnail-cache | 🟡 | Parallel preview 401s defeat the init sharing, and a late 401 wipes a freshly minted token | `auth` |
| PPW-125 | 042-thumbnail-cache | 🟡 | The guest-init error path when files are dropped is untested, so files hang showing as uploading | `tests` |
| PPW-126 | 042-thumbnail-cache | 🟡 | Moving a file onto a shared key races other writers on Windows and returns 500; production on Linux is unaffected | `uploads` |
| PPW-127 | 042-thumbnail-cache | 🟡 | A cleanup delete fails against an open read handle on Windows and leaves an orphan; production on Linux is unaffected | `uploads` |
| PPW-130 | 042-thumbnail-cache | 🟡 | Storage faults and cancellation are reported as an unreadable image, so a storage outage looks like bad uploads | `uploads` |
| PPW-131 | 042-thumbnail-cache | 🟡 | The implementation plan's acceptance criteria still list the public cache directive and the per-axis cap, both replaced | `records` |
| PPW-132 | 042-thumbnail-cache | ⚪ | The reserved bomb event is written out at three sites and the batch copy omits which guard caught it | `uploads` |
| PPW-133 | 042-thumbnail-cache | ⚪ | `dropRestoredEntry` repeats the body of `onRemoveUpload` word for word | `auth` |
| PPW-134 | 042-thumbnail-cache | ⚪ | The client-abort log reads the raw correlation-id item instead of the accessor every sibling uses | `observability` |
| PPW-135 | 042-thumbnail-cache | ⚪ | Storage save and delete traces sit at Debug under an Information floor, so they never emit | `uploads` |
| PPW-141 | 042-thumbnail-cache | 🟡 | The 30-day private preview cache stays recoverable on a shared device; the owner decision on requiring revalidation was never taken | `uploads` |
| PPW-143 | 042-thumbnail-cache | 🟡 | A restore preview that resolves after the page is destroyed leaks an object URL | `gallery` |
| PPW-144 | 042-thumbnail-cache | 🟡 | No end-to-end test reaches the bomb-to-422 path, because the integration fake always reports 800×600 | `tests` |
| PPW-145 | 042-thumbnail-cache | 🟡 | A guest 401 away from the upload page is a silent dead end: no re-init, no message, no navigation | `auth` |
| PPW-146 | 042-thumbnail-cache | 🟡 | `localUrl()` mints an untracked object URL on every change-detection cycle for a photo held in the session | `gallery` |
| PPW-147 | 042-thumbnail-cache | 🟡 | The decode memory budget ignores the upload buffering that draws on the same memory | `uploads` |
| PPW-148 | 042-thumbnail-cache | ⚪ | The conditional GET matches only an exact strong tag, so weak, list and `*` forms fall back to a full response | `uploads` |
| PPW-188 | 043-cloud-storage-provider | 🟡 | Renamed `ExecuteAsync_ArchiveDisabled/_CloudTierOff` guard tests seed an empty DB → guard removal enqueues nothing anyway → `VerifyNoOtherCalls()` passes for the wrong reason. Seed a stuck Paid+Local order | `tests` |
| PPW-189 | 043-cloud-storage-provider | 🟡 | Anti-refresh-loop guard (`urlsRefreshed`) untested — no spec dispatches a *second* img `(error)` to assert no third `getOrderPhotos` fetch | `tests` |
| PPW-190 | 043-cloud-storage-provider | 🟡 | Lightbox focus-trap (`trapFocus` Tab/Shift+Tab `preventDefault` + refocus) has no spec — drop `preventDefault` and Tab escapes the modal, no test reddens | `tests` |
| PPW-191 | 043-cloud-storage-provider | 🟡 | Auto-heal shows "Reîncarcă pagina" while silently re-fetching a fresh URL → the user is told to reload for an error the app then auto-recovers. Show a neutral reloading state first | `gallery` |
| PPW-192 | 043-cloud-storage-provider | 🟡 | 401 on order fetch for a non-authenticated user (interceptor guest branch only clears the token, no navigate) → `loadOrder` sets neither error nor redirect → blank order body, no retry | `gallery` |
| PPW-193 | 043-cloud-storage-provider | ⚪ | Order retries + `ngOnInit` subs have no in-flight dedup / `takeUntilDestroyed` / `switchMap` — last-arriving-wins on rapid retries; late response sets signals post-destroy | `gallery` |
| PPW-194 | 043-cloud-storage-provider | 🟡 | The ZIP pre-flight throws `InvalidOperationException` — unmapped → generic 500 logged "Unhandled exception"; ops can't tell config-error from a crash. Map to 409/422 + Warning log | `orders` |
| PPW-197 | 043-cloud-storage-provider | 🟡 | Lightbox `failed()` reset keyed on `src !== lastSrc`; an identical refreshed presigned URL leaves `failed` stuck (+ `urlsRefreshed` blocks retry) → error until page reload | `gallery` |
| PPW-210 | 043-cloud-storage-provider | 🟡 | Retention `OrderBy/Take` window starved by persistent delete-failures (`ArchiveRetentionJob.cs:98`) | `uploads` |
| PPW-211 | 043-cloud-storage-provider | 🟡 | Admin ZIP mid-loop `GetStream` failure (incl. concurrent purge) truncates the archive after headers committed (`AdminOrderService.cs:197`) | `orders` |
| PPW-212 | 043-cloud-storage-provider | 🟡 | Preview cache-fill regeneration races retention delete → orphaned blob, ref nulled (`UploadService.cs:203` / `ArchiveRetentionJob.cs:124`) | `uploads` |
| PPW-213 | 043-cloud-storage-provider | 🟡 | Failed best-effort local delete in `OrderPhotoPromoter` leaks unreclaimable local bytes (`OrderPhotoPromoter.cs:212`) | `uploads` |
| PPW-214 | 043-cloud-storage-provider | 🟡 | `LocalStorageService` storage-root re-anchor uses prefix match without a separator boundary (`LocalStorageService.cs:99`) | `uploads` |
| PPW-215 | 043-cloud-storage-provider | 🟡 | ZIP entry extension taken from untrusted client filename instead of validated MIME (`AdminOrderService.cs:190`) | `orders` |
| PPW-216 | 043-cloud-storage-provider | 🟡 | Batch upload has no file-count cap (only 500MB total) (`UploadsController.cs:102`) | `uploads` |
| PPW-217 | 043-cloud-storage-provider | 🟡 | Broken grid thumbnails have no fallback/retry after the single presigned-URL refresh (`order-detail-page.ts:472`) | `gallery` |
| PPW-218 | 043-cloud-storage-provider | 🟡 | Originals of orders never reaching production-complete/Cancelled escape the retention window (`ArchiveRetentionJob.cs:92`) | `uploads` |
| PPW-219 | 043-cloud-storage-provider | 🟡 | 043 NFR "persistent S3 → 502 (BadGatewayException)" not implemented → surfaces as 500 (`S3StorageService.cs:145`) | `uploads` |
| PPW-220 | 043-cloud-storage-provider | 🟡 | Idempotent-skip reasons at Debug never emit under the Information floor (`OrderPhotoPromoter.cs:120`) | `uploads` |
| PPW-221 | 043-cloud-storage-provider | 🟡 | Transient vs permanent cloud-write failures collapsed into one Warning → poison retried like a blip (`OrderPhotoPromoter.cs:182`) | `uploads` |
| PPW-222 | 043-cloud-storage-provider | 🟡 | `GetPreviewAsync` dropped `AsNoTracking` on the hot cache-hit path (`UploadService.cs:139`) | `uploads` |
| PPW-223 | 043-cloud-storage-provider | 🟡 | Promotable-status set triplicated with a false "single source of truth" comment (`BackfillCommand.cs:43`) | `uploads` |
| PPW-224 | 043-cloud-storage-provider | 🟡 | S3 Polly retry classification/re-upload + presign HTTP/HTTPS protocol untested (`S3StorageService.cs:60/41`) | `tests` |
| PPW-225 | 043-cloud-storage-provider | 🟡 | Storage DI/config/CLI outside the lens manifest; AWS-native region config foot-gun (`StorageExtensions.cs:56`) | `uploads` |
| PPW-226 | 043-cloud-storage-provider | 🟡 | Recovery/retention sweeps run unindexed full scans every 6h; masked by InMemory tests (`UploadConfiguration.cs:30`) | `data` |
| PPW-227 | 043-cloud-storage-provider | ⚪ | Promoter materializes the whole original via `ToArray` + multiple undisposed `MemoryStream`s (`OrderPhotoPromoter.cs:138`) | `uploads` |
| PPW-228 | 043-cloud-storage-provider | ⚪ | Best-effort orphan-thumbnail delete swallows its exception with no log (`UploadService.cs:222`) | `uploads` |
| PPW-229 | 043-cloud-storage-provider | ⚪ | Local-preview `Cache-Control` mismatch: ADR-008 says public/immutable, code sends `private` (`UploadsController.cs:26`) | `uploads` |
| PPW-230 | 043-cloud-storage-provider | ⚪ | Freshly generated local thumbnail re-read from disk on cache miss (`UploadService.cs:240`) | `uploads` |
| PPW-231 | 043-cloud-storage-provider | ⚪ | Redundant dual feedback: interceptor toast plus inline error/redirect (`order-detail-page.ts:403`) | `gallery` |
| PPW-235 | 043-cloud-storage-provider | 🟡 | Retention deletes blobs before persisting key-null → broken-URL window on a concurrent read (`ArchiveRetentionJob.cs:146`) | `uploads` |
| PPW-236 | 043-cloud-storage-provider | 🟡 | Retention sweep query omits the `DeletedAt` filter → reprocesses soft-deleted rows, re-emits false audit (`ArchiveRetentionJob.cs:96`) | `uploads` |
| PPW-237 | 043-cloud-storage-provider | 🟡 | Promoter tests assert cloud-write keys but never the bytes written (`OrderPhotoPromoterTests.cs`) | `tests` |
| PPW-239 | 043-cloud-storage-provider | 🟡 | PPW-185 close-*during*-refresh resolve-time re-read has no spec (only close-before-error is tested) (`order-detail-page.spec.ts`) | `tests` |
| PPW-262 | 015-sameday-shipping | 🟡 | Dual-DB parity: migrations + `timestamptz` CAS never run on Postgres (offset-write may throw) *(hinted)* | `data` |
| PPW-279 | 015-sameday-shipping | ⚪ | New migration designer snapshots embed stale `StripeClientSecret` 255 vs 512 *(hinted)* | `data` |
| PPW-329 | 015-sameday-shipping | 🟡 | `ISamedayAuthenticator` singleton captures the transient typed `ISamedayClient` → handler never rotated (pre-existing, carried into the new extension) | `shipping` |
| PPW-330 | 015-sameday-shipping | ⚪ | `ISamedayClient` doc still claims NotImplementedException "until bolt 037" — stale twin of the claim stripped from `SamedayClient.cs` | `shipping` |
| PPW-331 | 015-sameday-shipping | ⚪ | `AwbNumber` (varchar(100)) is the unclamped sibling of PPW-299's clamp on the same post-bill persist | `shipping` |
| PPW-332 | 015-sameday-shipping | ⚪ | `Created` outcome reports the unclamped LabelUrl while the row stores null | `shipping` |
| PPW-333 | 015-sameday-shipping | ⚪ | `MaxRequestsPerSecond` missing from appsettings.json, the settings validator, and bolt-037 ddd-02 | `shipping` |
| PPW-334 | 015-sameday-shipping | ⚪ | PPW-306's 30 s poll buffer is a flat constant, not scaled to the interval | `shipping` |
| PPW-335 | 015-sameday-shipping | ⚪ | Record accuracy: resolution-v5/index.md say "backend 914" (tip = 916) and "fixed: 30" (frontmatter holds 41); index cites `66c6d50` not the tip `1816f5f` | `records` |
| PPW-359 | 044-045-observability | 🟡 | Scope enricher registered after auth — pre-auth failures reach Sentry untagged | `observability` |
| PPW-360 | 044-045-observability | 🟡 | EF spans ship full SQL and exception messages to OTLP unscrubbed | `observability` |
| PPW-361 | 044-045-observability | 🟡 | `NaN` sample rates pass both validators and silently drop everything | `observability` |
| PPW-362 | 044-045-observability | 🟡 | `PrometheusEndpoint="/"` passes validation and would gate the whole site | `edge` |
| PPW-363 | 044-045-observability | 🟡 | `ValidateOnStart` wiring untested — narrowed at v2: now exercised by `An_unparseable_allow_list_entry_aborts_boot`; only the blank-`PrometheusEndpoint` leg remains untested | `tests` |
| PPW-364 | 044-045-observability | 🟡 | Enricher sets `scope.User.Id` instead of the required `user_id` tag | `observability` |
| PPW-365 | 044-045-observability | 🟡 | Sampler startup log (story 003 AC) not implemented — changed shape at v2: `RouteAwareSampler.cs` is gone so there is no "resolved table", but nothing logs the sampler choice at boot and `Description_includes_the_rate_for_the_startup_log` pins a description for a log that does not exist | `observability` |
| PPW-366 | 044-045-observability | 🟡 | Neither subsystem logs its enabled state at boot — narrowed at v2: `observability.tracing.disabled` now covers the blank-endpoint case; Sentry's state and the observability master flag are still unlogged | `observability` |
| PPW-367 | 044-045-observability | 🟡 | Unsynchronized capture collections in the shared test fixture | `tests` |
| PPW-368 | 044-045-observability | ⚪ | Magic `"unknown"` label escapes `MetricNames`, docs and the cardinality budget | `observability` |
| PPW-369 | 044-045-observability | ⚪ | `///` blocks on concrete classes citing bolt/ADR/story IDs | `observability` |
| PPW-370 | 044-045-observability | ⚪ | Comment-sweep residue: dangling `/`, surviving bolt citations, run-on lines | `observability` |
| PPW-371 | 044-045-observability | ⚪ | `ddd-02` describes the `Random` approach ADR-017 forbids | `records` |
| PPW-372 | 044-045-observability | ⚪ | Metric vocabulary shipped ahead of emission (ANAF; constant `status` label) | `observability` |
| PPW-373 | 044-045-observability | ⚪ | Observability config re-read by string key after binding; duplicated default | `observability` |
| PPW-374 | 044-045-observability | ⚪ | Sentry wiring inlined in `Program.cs` while bolt 044 got an extension method | `observability` |
| PPW-386 | 044-045-observability | 🟡 | `TracingExporterSelectionTests` boots live TracerProviders outside `ObservabilityHostCollection` | `tests` |
| PPW-387 | 044-045-observability | 🟡 | `payment_failed` records `failed` unconditionally where its sibling uses `duplicate` | `payments` |
| PPW-388 | 044-045-observability | 🟡 | `MaskedForm` suggests an `::ffff:…/112` form the parser then rejects | `edge` |
| PPW-389 | 044-045-observability | 🟡 | `A_mapped_404_is_not_captured_to_sentry` is satisfied by an unrouted request | `tests` |
| PPW-390 | 044-045-observability | 🟡 | The documented `Sentry__Debug=true` verbosity knob is inert under Serilog's Information floor | `records` |
| PPW-391 | 044-045-observability | 🟡 | No volume ceiling on the new Sentry capture site | `observability` |
| PPW-393 | 044-045-observability | 🟡 | §13.10 still says a No-Data panel means a name mismatch, contradicting the accepted panel-8 decision | `records` |
| PPW-394 | 044-045-observability | 🟡 | AWB shutdown carve-out matches only `OperationCanceledException`; tests run on PostgreSQL, prod is Postgres | `shipping` |
| PPW-395 | 044-045-observability | 🟡 | `CapturingSentryTransport.Payloads` is an unsynchronized `List` across threads | `tests` |
| PPW-396 | 044-045-observability | 🟡 | `wrong_listener` and `not_allowed` denials share one 512-entry log budget | `edge` |
| PPW-397 | 044-045-observability | 🟡 | A throw escaping a webhook endpoint records no metric at all — sibling class resolved the opposite way | `payments` |
| PPW-398 | 044-045-observability | 🟡 | `Idempotency-Key` scrubbed, so duplicate-payment triage loses the colliding key | `observability` |
| PPW-399 | 044-045-observability | 🟡 | The fail-closed drop is never exercised through the hook, and has no metric behind it | `observability` |
| PPW-400 | 044-045-observability | ⚪ | Empty allow-list entry error names neither value nor index | `edge` |
| PPW-401 | 044-045-observability | ⚪ | `Scrub(Breadcrumb)` restamps `Timestamp` | `observability` |
| PPW-402 | 044-045-observability | ⚪ | bolt-045 walkthrough lines 39/46 still describe the deleted deny-list | `records` |
| PPW-403 | 044-045-observability | ⚪ | Series-count failure never names `DeclaredInstruments()` | `tests` |
| PPW-404 | 044-045-observability | ⚪ | `LogCapture` discards category and exception | `tests` |
| PPW-405 | 044-045-observability | ⚪ | Nothing proves `ContractViolations()` ever returns non-empty | `tests` |
| PPW-406 | 044-045-observability | ⚪ | "Background roots stay dropped" holds only below rate 1.0 | `observability` |
| PPW-407 | 044-045-observability | ⚪ | Stale-`Routes` boot abort sits below the `Enabled` early return, so §14.8 step 1 cannot catch it | `observability` |
| PPW-408 | 044-045-observability | ⚪ | Promotion emits no in-app signal, so "stopped" and "no errors" look identical | `observability` |
| PPW-420 | 044-045-observability | 🟡 | The breadcrumb test is absence-only — green with every breadcrumb dropped | `tests` |
| PPW-421 | 044-045-observability | 🟡 | `metrics.md`'s add-a-metric procedure never states `MetricCapture`'s execution-context requirement | `records` |
| PPW-422 | 044-045-observability | 🟡 | ADR-017 still says "a promoted error trace is a single root span" 19 lines below its own amendment | `records` |
| PPW-423 | 044-045-observability | 🟡 | Walker reach: `templating`/`annotations` queries and library panels unwalked; query-side parser mis-handles an escaped quote the exposition side handles | `tests` |
| PPW-425 | 044-045-observability | 🟡 | `OrderPhotoPromoter` hand-rolls `HasBeenPaid` as an ordinal comparison plus two name exclusions | `uploads` |
| PPW-426 | 044-045-observability | 🟡 | `AdminOrderService` logs client-disconnect cancellations at `Error` on its highest-signal strings | `orders` |
| PPW-427 | 044-045-observability | 🟡 | The `HasBeenPaid` invariant test excludes `Cancelled` by name, pushing a future author to add a refund status to `PaidStatuses` | `tests` |
| PPW-428 | 044-045-observability | 🟡 | `Verdict` counts ports, not reachability: a loopback-only API port plus a wildcard scrape port passes both rules | `edge` |
| PPW-429 | 044-045-observability | 🟡 | `PrometheusEndpoint` is coupled to the `Caddyfile`'s hard-coded path by comment only | `edge` |
| PPW-430 | 044-045-observability | 🟡 | `TracingWired == false` in Production warns and boots — same warn-only class as the admitted `ScrapePort == 0` | `observability` |
| PPW-431 | 044-045-observability | 🟡 | Inbound `baggage` rides out to Stripe, Sameday and Google | `observability` |
| PPW-433 | 044-045-observability | 🟡 | ADR-017's anti-salting rationale is now weaker than the ADR states — the amendment abandoned its only consumer | `records` |
| PPW-434 | 044-045-observability | ⚪ | `MetricCapture._outer` is provably always null; `Dispose`'s restore is dead code advertising forbidden nesting | `tests` |
| PPW-436 | 044-045-observability | ⚪ | bolt-044 ddd docs declare a four-value `result` set and `ParentBasedSampler` as shipped | `records` |
| PPW-437 | 044-045-observability | ⚪ | `resolution-v2.md` misdescribes TestServer as reporting the addresses feature "present but empty" — it returns null | `records` |
| PPW-459 | 044-045-observability | ⚪ | The guarded-selector list is hand-maintained, so a fifth hand-named success numerator ships unpinned and nothing notices — and the stated reason for not writing the class rule does not hold for a rule keyed on literal `=` matchers | `tests` |
| PPW-460 | inbox | 🔴 | Global rate limiter partitions on `Connection.RemoteIpAddress`, behind Caddy one value for all traffic — "100/min per IP" is 100/min for the whole internet; one client at ~2 rps can 429 the site. Must be fixed before deployment | `edge` |
| PPW-461 | inbox | 🔴 | Auth limiters are unpartitioned `AddFixedWindowLimiter` calls — registration 5/hour, resend 3/hour, forgot-password 3/hour are site-wide budgets; one actor locks every user out of signup and reset. Must be fixed before deployment | `edge` |
| PPW-462 | inbox | 🟠 | Security-audit log records Caddy's address as the client IP, so the audit trail cannot attribute an action to a caller (recorded by the fixer, unverified by any pass) | `edge` |
| PPW-463 | inbox | 🟠 | All 13 `BackgroundJobs/` files catch and log their own exceptions, never touch `IHub` — a total AWB-retry or email-retry outage produces no Sentry issue | `jobs` |
| PPW-464 | inbox | 🟠 | An order advanced to `Printing` with its AWB still pending exits the retry sweep permanently and silently (all four sites match `Status == Paid`); parcel ships with no label, no signal | `shipping` |
| PPW-465 | inbox | 🟠 | Sameday `HttpClient.Timeout` (10 s) bounds the whole handler chain including the 1+4+16 s backoff ladder — the third retry never runs; outages surface as cancellations, not vendor status (read, not executed) | `shipping` |
| PPW-466 | inbox | 🟡 | Two email-area tests flake under parallel load, pass in isolation (`EmailRetryJobTests.Processing_SuccessfulSend_MarksEmailAsSent`, `ReliableEmailServiceTests.SendAsync_FailedSend_QueuesEmailToDatabase`) — same suspected shared state | `tests` |
| PPW-467 | inbox | 🟡 | `/health` is proxied ungated and echoes each check's `Data` bag verbatim to anonymous callers; today only `freeGb`, but any future check publishing hostnames or connection strings leaks them | `edge` |
