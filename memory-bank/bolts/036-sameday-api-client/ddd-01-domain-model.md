---
stage: model
bolt: 036-sameday-api-client
created: 2026-06-02T09:05:00Z
---

# Stage 1 — Domain Model: Sameday API Client

## Bounded Context

The **Sameday Integration** bounded context owns every interaction with the
Sameday courier HTTP API: authentication, AWB creation, label retrieval,
parcel tracking. It is downstream of the **Ordering** context (it consumes
`Order` state) and upstream of nothing — its observable side effects on the
core domain are limited to two new persisted fields on `Order`
(`AwbLabelUrl`, `LastTrackingSyncAt`) plus existing `AwbNumber`.

Bolt 036 covers the *foundations only*:
- HTTP client + auth token lifecycle.
- Schema additions used by bolt 037 (AWB + tracking jobs).
- A feature-flag `SamedayShippingService : IShippingService` whose
  AWB-creation method is implemented in 036 only as a thin pass-through;
  the background workflow that *calls* it lives in bolt 037.

Anti-corruption: Sameday's wire formats (snake_case JSON, vendor-specific
error shapes, token wrapper objects) never leak past this context. The
rest of the system sees clean .NET records (`SamedayToken`,
`AwbCreationResult`, …) and our own exception taxonomy.

---

## Entities

There are no new aggregate roots in this bolt. The Sameday context is a
collection of services + value objects that decorate the existing `Order`
aggregate. The two persisted fields added here are properties of the
already-existing `Order` entity.

- **Order** (existing aggregate root, extended)
  - `AwbLabelUrl: string?` *(new — varchar(500), nullable)*
    - URL to the PDF shipping label hosted by Sameday.
  - `LastTrackingSyncAt: DateTimeOffset?` *(new — timestamptz, nullable)*
    - UTC timestamp of the most recent successful poll against the Sameday
      tracking endpoint for this order's AWB.
  - Invariants (unchanged in this bolt):
    - `AwbLabelUrl`, when non-null, is the URL Sameday returned alongside
      the `AwbNumber`. Both are set atomically by the AWB-creation
      operation (bolt 037).
    - `LastTrackingSyncAt` is monotonically non-decreasing (a successful
      poll never moves it backwards).
    - Both fields are always nullable; existing orders read back as `null`
      and the `Order` aggregate must tolerate that.

No new entity is introduced.

---

## Value Objects

All value objects are immutable, validated at construction, equal by value.

- **SamedayToken**
  - Shape: `record SamedayToken(string Value, DateTimeOffset ExpiresAt)`.
  - Constraints:
    - `Value` is a non-empty opaque string (Sameday-issued).
    - `ExpiresAt` is in UTC, in the future at the moment of issue.
  - Behaviour:
    - `IsValid(DateTimeOffset now, TimeSpan safetyWindow)` — true iff
      `now + safetyWindow < ExpiresAt`. Safety window is 60 s
      (compensates clock skew + in-flight request latency).
  - Lifetime: held in-process by the singleton token provider; never
    persisted, never logged.

- **SamedayCredentials**
  - Shape: `record SamedayCredentials(string Username, string Password)`.
  - Constraints:
    - Both fields non-empty.
    - Read from `SamedaySettings` once at boot; never reconstructed from
      logs / DTOs.
  - Equality: by value, but the type intentionally exposes no `ToString`
    override — log statements that interpolate this value get the
    default `record` formatter, which is itself disallowed (see security
    rule below).
  - Security rule: `SamedayCredentials` must never be logged. The
    type's *purpose* is to make "leak the password" a deliberate code
    change rather than an accidental one.

- **SamedayEndpoint** (path-only descriptor — pure documentation in this
  bolt; used in technical design as a closed enum of "where can we
  talk to Sameday")
  - Members: `Authenticate`, `CreateAwb`, `GetLabelPdf`, `TrackAwb`,
    `CancelAwb` (last two not implemented in 036, named for design
    completeness).

- **AwbCreationResult**
  - Shape: `record AwbCreationResult(string AwbNumber, string LabelUrl,
    decimal CalculatedPrice)`.
  - Constraints: all three fields present (Sameday returns 422 if any
    is missing; we surface a `SamedayProtocolException` in that case).
  - Returned by `ISamedayClient.CreateAwbAsync` in bolt 037; defined
    here so the static model is complete.

- **TrackingSnapshot**
  - Shape: `record TrackingSnapshot(string AwbNumber, TrackingState State,
    DateTimeOffset ObservedAt, IReadOnlyList<TrackingEvent> History)`.
  - Constraints: `ObservedAt` is the Sameday-side timestamp, not ours.
  - Used by bolt 037's tracking job; defined here so 037 has a
    settled vocabulary.

- **TrackingState** *(enum/value)*
  - Members: `Pending`, `InTransit`, `OutForDelivery`, `Delivered`,
    `Failed`, `Cancelled`, `Unknown`. The mapping from Sameday's
    domain-specific status codes to this set is part of the
    anti-corruption layer.

---

## Aggregates

No new aggregate in this bolt. The Sameday context is *service-heavy* and
extends the existing `Order` aggregate. The two new fields above belong
inside `Order`'s invariant boundary: they may only be written by domain
services that hold a reference to the aggregate, and changes are saved
through the existing `IOrderRepository`/`PhotoPrintDbContext`.

---

## Domain Services

Bolt 036 introduces the *contracts* and the *transport-level* services.
The lifecycle services that *use* them (`AwbCreationJob`,
`ShipmentTrackingJob`) belong to bolt 037.

- **`ISamedayTokenProvider`** — domain service
  - Operation: `Task<SamedayToken> GetTokenAsync(CancellationToken ct)`.
  - Responsibilities:
    - Returns a non-expired `SamedayToken`.
    - Caches the token in-process for the lifetime of the host.
    - Serialises concurrent first-time fetches behind a
      `SemaphoreSlim(1,1)` so a thundering-herd of inbound requests
      results in exactly one authentication call.
    - On expiry (or on caller-requested `Invalidate`), discards the
      cached token and fetches a fresh one on the next call.
  - Errors: `SamedayUnreachableException` (Polly exhausted retries),
    `SamedayAuthException` (Sameday returned 401 to authenticate
    itself — credential problem, *not* a transient network blip).
  - Lifetime: singleton. Cross-instance sharing (Redis) is explicitly
    *out of scope* per the story; intent 021 may revisit.

- **`ISamedayClient`** — domain service (a typed `HttpClient`)
  - Operations (declared in 036, only `AuthenticateAsync` *fully
    implemented* in 036; the rest are declared and throw
    `NotImplementedException` until bolt 037 unless their absence
    breaks compilation):
    - `Task<SamedayToken> AuthenticateAsync(SamedayCredentials,
      CancellationToken)`.
    - `Task<AwbCreationResult> CreateAwbAsync(AwbCreationRequest,
      CancellationToken)`.
    - `Task<Stream> GetLabelPdfAsync(string awbNumber,
      CancellationToken)`.
    - `Task<TrackingSnapshot> GetTrackingAsync(string awbNumber,
      CancellationToken)`.
  - Responsibilities:
    - Owns the `HttpClient` (via `IHttpClientFactory`).
    - Attaches the bearer token to every non-`Authenticate` call.
    - Translates Sameday wire shapes into the value objects above
      (anti-corruption).
    - On 401 from any non-`Authenticate` call: clears the token,
      re-authenticates once, retries the call exactly once. A
      second 401 → `SamedayAuthException`.
  - Cross-cutting (delegating handlers / Polly policies — detailed in
    Stage 2):
    - Polly rate-limit policy (5 req/s ceiling).
    - Polly retry policy (3 attempts; exponential 1 / 4 / 16 s; retry
      only `5xx | 408 | 429`).
    - Request/response logging redacts `Authorization` and any
      `password` field; never logs the bearer value.

- **`IShippingService`** *(existing interface — implementation
  selected by config in this bolt)*
  - New implementation: `SamedayShippingService`.
  - Registration rule: `services.AddSingleton<IShippingService,
    SamedayShippingService>()` is conditional on
    `Sameday:Enabled == true`. With `Enabled == false`,
    `StaticShippingService` (existing) remains registered and the
    rest of the system is bit-for-bit identical to today.
  - In bolt 036, `SamedayShippingService.GenerateAwbAsync` delegates
    to `ISamedayClient.CreateAwbAsync` — the *outer* "Paid →
    Processing → create AWB" workflow lives in bolt 037.

---

## Domain Events

No new domain events in this bolt. The events that *will* be raised
once the AWB workflow is wired up — `AwbCreated`, `AwbCreationFailed`,
`ShipmentDelivered` — belong to bolt 037 and are listed in that
bolt's unit-brief for visibility.

What this bolt *does* persist on the boundary, in lieu of an event
store, is:

- `Order.AwbLabelUrl` set ⇒ AWB exists and Sameday accepted it.
- `Order.LastTrackingSyncAt` updated ⇒ we successfully read the
  current state from Sameday at that timestamp.

These two fields are the durable record of the integration's
observable effects; the in-memory event types come in 037.

---

## Repository Interfaces

No new repositories. The existing `PhotoPrintDbContext.Orders`
`DbSet<Order>` and the existing service that wraps it (`IOrderService`
in `OrderService.cs`) are the only persistence path.

The migration in this bolt only adds two columns; no `IRepository<>`
contract changes.

---

## Error Taxonomy

A small, closed set of exception types lives inside this context and
escapes only as either `5xx` (mapped by the existing global exception
handler) or as a deliberate `SamedayUnreachableException` that the
*caller* (bolt 037's background jobs) catches to drive retry policy.

- **`SamedayException`** *(abstract base)*
  - Carries: `correlationId` (already supplied by the existing
    `CorrelationIdMiddleware`), `endpoint`, optional `httpStatus`.
  - Never carries: credentials, tokens, request bodies that contain
    PII.

- **`SamedayUnreachableException : SamedayException`**
  - Cause: Polly retry exhausted (3 attempts), `5xx`/`408`/`429`
    from Sameday, or `HttpRequestException` (DNS/TCP).
  - Caller contract: retry later (bolt 037 schedules a job).

- **`SamedayAuthException : SamedayException`**
  - Cause: 401 from `/authenticate`, or a *second* 401 from an
    operational call after a fresh token was attached.
  - Caller contract: stop retrying — credentials are wrong. The
    background job in bolt 037 will surface this as a
    `SamedayAuthFailureLogged` warning (placeholder until
    intent 020 wires an admin notification).

- **`SamedayProtocolException : SamedayException`**
  - Cause: Sameday returned 2xx but with a payload that doesn't
    match the contract (missing AWB number, malformed JSON, etc.).
  - Caller contract: log + manual fallback — retrying will not help.

- **`SamedayValidationException : SamedayException`**
  - Cause: 4xx (other than 401/408/429) from Sameday — i.e. *our*
    request was malformed (e.g. weight > 30 kg, missing pickup
    point).
  - Caller contract: do *not* retry; the bug is on our side.

---

## Ubiquitous Language

| Term | Definition |
|---|---|
| **AWB** | "Air Waybill" — Sameday's name for a shipment label / parcel identifier. A successful `CreateAwb` call returns a string AWB number (e.g. `RO12345678`) plus a label-URL pointing to a Sameday-hosted PDF. |
| **PickupPoint** | A Sameday-side configuration entity referenced by ID. Every AWB must declare which pickup point Sameday should collect the parcel from. We persist exactly one (`Sameday:PickupPointId`) per environment. |
| **Token** | Opaque bearer string obtained from `/api/authenticate`. Has a Sameday-supplied `expiresAt`. Used as `Authorization: Bearer <token>` on every other call. We cache one per host. |
| **Safety window** | 60 s before `Token.ExpiresAt` during which we treat the token as already expired. Absorbs clock skew + in-flight request time. |
| **Sandbox** | Sameday's test environment. Different `BaseUrl`, same wire contract. Dev/staging both point at sandbox; only production points at the live endpoint. |
| **Sameday-Enabled** | Boolean config (`Sameday:Enabled`) that selects whether `IShippingService` is `SamedayShippingService` (real API) or `StaticShippingService` (existing fallback). Default `false`; static service stays the boot-time default until ops flips the flag. |
| **Manual fallback** | The pre-existing behaviour where AWBs are created by hand in the Sameday web portal. The product invariant in this intent: a Sameday failure *never* breaks an order — it only triggers the fallback. |
| **Rate-limit ceiling** | The 5 req/s cap we apply on our side via Polly; well below Sameday's documented ~10 req/s so concurrent jobs do not collectively exceed it. |
| **TrackingState (internal)** | Our normalised view of where a parcel is (`Pending` … `Delivered` …). Sameday's raw vendor codes are mapped at the anti-corruption boundary. |

---

## Story Coverage

- **001-sameday-settings-and-typed-client** — covered by
  `SamedaySettings` (value-object-flavoured config), the typed
  `ISamedayClient`/`SamedayClient`, and the conditional
  `IShippingService` registration.
- **002-token-auth-and-refresh** — covered by `SamedayToken` (value
  object), `ISamedayTokenProvider` (domain service), and the
  `401 → re-auth → retry once → SamedayAuthException` flow on
  `ISamedayClient`.
- **003-sameday-schema-additions** — covered by the two new
  `Order` columns above and the EF migration noted in *Stage 2*.

---

## Completion Checklist

- [x] All domain entities identified (`Order` extended; no new aggregates).
- [x] Business rules captured (token lifetime, retry-once, credential
      handling, label/tracking field invariants).
- [x] Aggregate boundaries defined (`Order` continues to own the two
      new fields; Sameday services have no aggregate of their own).
- [x] Domain "events" — none in this bolt; persistence on
      `Order.AwbLabelUrl` / `LastTrackingSyncAt` plays the same role.
- [x] Repository interfaces — no new ones; existing path used.
- [x] All three stories (`001` / `002` / `003`) covered by the model.

---

## ⛔ Human Checkpoint

Stage 1 (Domain Model) is drafted. Please review and approve before I
move to Stage 2 (Technical Design).

**Ready to proceed?**

- **1** — Approve and continue to Stage 2.
- **2** — Need changes (specify which section).
