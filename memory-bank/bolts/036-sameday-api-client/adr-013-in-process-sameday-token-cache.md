---
bolt: 036-sameday-api-client
created: 2026-06-02T10:10:00Z
status: accepted
---

# ADR-013: In-Process Singleton Token Cache for the Sameday API

## Context

The Sameday courier API authenticates via a `POST /api/authenticate` call
that returns an opaque bearer token plus a Sameday-supplied
`expire_at_utc`. Every other Sameday call carries this token as
`Authorization: Bearer …`. Tokens are reusable for the lifetime
Sameday gives us (currently ~24 h, but the contract leaves it free
to change).

The integration ships in bolt 036 as a **single-VM service**. The
broader project will eventually run on multiple replicas (intent 021
introduces Redis and unblocks horizontal scale-out — same pattern
that ADR-010's promotion queue defers across).

We therefore had to decide where the Sameday token lives:

- **In-process**, as a field on a singleton `SamedayTokenProvider`.
- **In Redis** (or any cross-instance cache), shared by every replica
  in the deployment.
- **In Postgres**, persisted across restarts as well as across
  replicas.

The decision affects scale-out behaviour, restart latency, blast
radius of a credential rotation, and how testable the token
lifecycle is.

## Decision

**The Sameday token cache lives in-process on a singleton
`SamedayTokenProvider`. There is one cached `SamedayToken` per
host. Cross-instance sharing is explicitly *out of scope* and is
deferred to intent 021, alongside the project's first introduction
of Redis.**

Restated as invariants the code must hold:

- Exactly one `SamedayTokenProvider` is registered, as a singleton.
- A `SemaphoreSlim(1, 1)` serialises concurrent first-time fetches
  (no thundering-herd on cold boot).
- The cached token is treated as *expired* during the 60 s safety
  window before its `ExpiresAt`.
- On 401 from any operational call, the cached token is
  invalidated and a fresh one fetched; the request retries
  *exactly once*. A second 401 → `SamedayAuthException`.

## Rationale

The in-process choice mirrors the same trade-off the project
already made deliberately in **ADR-010** (in-process
`Channel<PromotionJob>` for the photo-promotion queue): the system
is single-replica today, the consequences of *being* single-replica
are well understood, and any durable / distributed alternative
introduces a new piece of infrastructure that we will introduce on
purpose, once, in intent 021.

For the Sameday token specifically, the in-process choice is also
unusually cheap:

- A re-authentication takes one HTTP round-trip (~200 ms typical).
- After a process restart the worst-case cost is *one* extra call
  before steady state.
- Concurrent requests during cold-start are serialised by the
  `SemaphoreSlim`, so the worst-case is still one re-auth, not N.

### Alternatives Considered

| Alternative | Pros | Cons | Why Rejected |
|---|---|---|---|
| **Redis-backed shared cache** | Survives restarts; one auth call per *deployment* not per *instance*; aligns with eventual scale-out. | Requires Redis (not yet in the project). Introduces a new infra dependency for a single endpoint. Adds I/O on every Sameday call's hot path (cache check). Adds a failure mode (Redis down → can we still talk to Sameday?). | Premature given the single-VM topology and the very low cost of in-process re-auth. Will revisit when intent 021 introduces Redis for other reasons. |
| **Postgres-backed token row** | Durable across restarts and replicas; no new infra. | Adds a writes-on-the-auth-hot-path concern. Introduces a row that has to be GC'd. Token is *short-lived* — persisting it durably is the wrong shape for the data. | Wrong tool: tokens are session state, not domain data. The DB is reserved for things we'd lose sleep over losing. |
| **No cache at all (re-authenticate on every call)** | Trivial code. | Doubles every Sameday operation's latency. Burns through Sameday's rate-limit budget (5 req/s ceiling) for no business reason. Pointless given the token is reusable for hours. | Defeats the purpose of having a token; the spec explicitly says the token has `expiresAt`. |
| **JWT-style stateless verification on our side** | Zero state. | Not what Sameday issues — token is opaque. | Not available. |

## Consequences

### Positive

- **Zero new dependencies.** Bolt 036 ships without Redis, without
  a new table, without a new migration beyond the two columns
  already required for AWB persistence.
- **Deterministic, fast unit tests.** Token expiry is driven by
  `TimeProvider`, which a `FakeTimeProvider` can step forward
  arbitrarily — no `Thread.Sleep`, no flaky timing.
- **Single source of truth for token state.** No cache-coherence
  question; no "is the cache stale?" debugging path.
- **Failure mode is small and obvious.** A restarted host
  re-authenticates exactly once on its next outbound call.
  Workload re-converges in ~200 ms.
- **Auth correctness is unaffected by scale-out direction.** The
  `SemaphoreSlim` + `Invalidate()` semantics work the same way
  whether there's one host or one hundred — each host just refreshes
  its own copy.

### Negative

- **Each replica re-authenticates independently.** With *N* replicas
  on the same Sameday account, cold start of the deployment results
  in *N* simultaneous `/api/authenticate` calls. With the Polly
  rate-limit ceiling at 5 req/s and bursty-then-throttle semantics,
  this is well within budget for any plausible *N*, but the cost
  is non-zero.
- **Each replica's first call after restart pays the auth latency.**
  Healthy steady state for a replica is "already authenticated";
  cold start is "one extra HTTP round-trip on the first user
  request that triggers a Sameday call." Bolt 037's workload is
  background jobs, so this is invisible to customers.
- **A credential rotation requires restarting every replica** to
  evict the in-memory cached token. With one replica today, a
  rolling restart is one host. With multiple replicas, this becomes
  a fleet-restart operation. Mitigation: the `Invalidate()` method
  is public on `ISamedayTokenProvider`, so a future *admin*
  endpoint could clear the cache without restart — out of scope
  here; written down so future work knows the seam exists.

### Risks

- **Risk: We need horizontal scale-out before intent 021.** If the
  business decides to deploy two API replicas before the Redis
  introduction, the in-process token cache continues to function
  correctly (each replica just maintains its own); the cost is
  visible only as duplicated `/api/authenticate` calls. Mitigation:
  the rate-limit budget absorbs this comfortably.
- **Risk: Sameday tightens token TTL.** If Sameday shortens
  `expiresAt` from hours to minutes, the 60 s safety window
  combined with the singleton cache continues to behave correctly
  but the re-auth rate climbs. Mitigation: we surface
  `SamedayTokenRefreshed` log events at `Information` level (with
  `ExpiresAt`, never the token value) so this is observable.

## Related

- **Stories**: 002-token-auth-and-refresh.
- **Previous ADRs**: ADR-010 (same "in-process now, durable later"
  trade-off for the photo-promotion queue).
- **Future ADRs**: intent 021 is expected to supersede this
  decision when it introduces Redis. At that point, this ADR
  transitions to `status: superseded` with `superseded_by` set to
  the intent-021 ADR.
- **Read when**: working on `SamedayTokenProvider`; reviewing token
  caching behaviour during an outage post-mortem; planning
  horizontal scale-out before intent 021 lands Redis; tempted to
  persist the token in Postgres; sizing the rate-limit ceiling
  against `/api/authenticate` call rates; rotating Sameday
  credentials.
