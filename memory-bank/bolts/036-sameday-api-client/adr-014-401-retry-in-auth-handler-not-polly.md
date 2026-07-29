---
bolt: 036-sameday-api-client
created: 2026-06-02T10:15:00Z
status: accepted
---

# ADR-014: 401 Retry-Once Lives in `SamedayAuthHandler`, Not in Polly

## Context

The Sameday HTTP client has two distinct failure modes that both
benefit from automatic retry, but for very different reasons:

1. **Transient transport failures** — 5xx, 408, 429, DNS / TCP
   errors. The right response is *wait and try again with the
   same request*. Polly's `WaitAndRetryAsync` was designed for
   exactly this.

2. **Session-expiry failures** — a 401 returned by an operational
   call (not by `/api/authenticate` itself) means the cached
   bearer token is no longer valid: Sameday rotated it on their
   side, or our cache outlived its real TTL, or we got
   particularly unlucky on the safety-window boundary. The
   correct response is *invalidate the cached token, fetch a
   fresh one, and reissue the same request once*.

These two cases superficially look like the same thing ("got a
non-2xx → retry") but the *operational semantics* are
fundamentally different. We had to decide whether to fold the
401-handling into Polly's retry policy or keep it as a separate
concern.

## Decision

**The 401 retry-once is implemented in a dedicated
`SamedayAuthHandler : DelegatingHandler`, sitting outside (i.e.
*before*) the Polly retry policy in the HTTP message pipeline.
Polly retains exclusive ownership of transient-fault retry (5xx,
408, 429). 401 is *never* in Polly's retryable status set.**

Pipeline order, outer → inner:

```text
SamedayAuthHandler
  → Polly RateLimit (5 req/s)
    → Polly Retry (3 attempts, exponential 1 / 4 / 16 s)
      → PrimaryHandler (HttpClientHandler)
```

`SamedayAuthHandler` flow:

- On request: attach `Authorization: Bearer <token>` (except on
  `/api/authenticate` itself).
- On response 401: dispose the response; call
  `_tokenProvider.Invalidate()`; fetch a fresh token; clone the
  original request; reissue *exactly once*; if that returns 401
  too → `throw new SamedayAuthException(endpoint)`.

## Rationale

The decision pivots on three properties of Polly's retry policy
that don't fit the session-expiry case:

1. **Polly retries the *same* request.** A token-expired retry
   needs the same request URL/body but a *different* `Authorization`
   header. Polly's retry-with-mutation hooks exist (`onRetryAsync`
   can modify the outgoing request via captured state), but
   threading mutable state through Polly's policy DSL is awkward
   and error-prone, and the resulting code reads as "fight the
   framework."

2. **Polly's backoff between retries is wrong for session expiry.**
   Token-expiry retry should fire *immediately* (round-trip to
   `/api/authenticate` plus one operational call). Polly's
   exponential backoff starts at 1 s — that's an entire second of
   wasted latency for what is mechanically a no-op-then-retry. A
   per-policy `sleepDurationProvider` that returns `TimeSpan.Zero`
   on 401 is technically possible, but now we have policy-internal
   branching keyed off status codes, which is hard to test and
   easy to break with a future PR.

3. **Polly's retry budget is shared.** A `WaitAndRetryAsync(3)`
   gives us *three* attempts total. If a 5xx → token expiry → 5xx
   sequence collapses the budget into a single "session-refresh"
   step, we've silently halved the actual transient-failure retry
   capacity. Keeping the two concerns separate gives us a clean
   contract: 3 attempts for transport failures, *and* 1 free
   session-refresh on top.

### Alternatives Considered

| Alternative | Pros | Cons | Why Rejected |
|---|---|---|---|
| **Add 401 to Polly's retryable set with `onRetryAsync` doing the token refresh.** | One retry mechanism. | Conflates session and transport semantics. Threading the refreshed token into the retried request requires capturing mutable state. Eats into the transport-retry budget. Backoff is wrong for session expiry. | Wrong shape: this is two different concerns wearing the same status code. |
| **Use a custom `IAsyncPolicy<HttpResponseMessage>` that handles 401 alongside Polly's retry.** | Stays "policy-shaped." | Building a custom Polly policy is a meaningfully bigger surface than a `DelegatingHandler`. Token-cache state isn't naturally available inside a policy. Testing requires a real or substitute `IAsyncPolicy`. | More moving parts than a `DelegatingHandler`, for less expressiveness. |
| **Put the 401 retry inside `SamedayClient` itself** (each operation method catches 401 and retries). | Co-located with the call. | N methods × duplicate retry code. Easy to forget one. Indistinguishable from "we just don't have a 401-retry" if a new method is added without copying the boilerplate. | Violates DRY in a way that *will* drift. The whole point of a `DelegatingHandler` is to be the single place this lives. |
| **No automatic 401 retry; surface the failure to callers.** | Simplest. | Every caller (bolt 037's AWB and tracking jobs) would have to know about Sameday's token lifecycle. Defeats the point of having a `SamedayTokenProvider`. | Pushes vendor-specific knowledge out of the anti-corruption layer. |

## Consequences

### Positive

- **Clean separation of concerns.** 401-handling is
  session-management; 5xx-handling is transient-failure recovery.
  Each lives in the layer that understands it.
- **Independent retry budgets.** A session refresh doesn't burn
  transport-retry attempts.
- **Testable in isolation.** `SamedayAuthHandler` can be unit-tested
  against a `MockHttpMessageHandler` with a scripted
  `[200-on-auth, 401, 200]` sequence and no Polly in the picture.
  Polly's retry policy can be tested against an `[503, 503, 200]`
  sequence with no auth-handler in the picture.
- **Cheap evolution.** If Sameday adds a 419 ("token expired but
  here's a refresh nonce") in a future API version, that
  knowledge stays in `SamedayAuthHandler` and nothing else needs
  to change.
- **No mutable state inside Polly's DSL.** Polly stays declarative.

### Negative

- **Two retry layers instead of one.** A future reader has to look
  in two places to understand the retry behaviour. Mitigated by:
  the pipeline-order diagram in `ddd-02-technical-design.md` and
  this ADR.
- **Worst-case wall-clock latency adds up.** A request that hits a
  session-expired token *and* then a transient 5xx will: refresh
  token (one round-trip), retry (200ms), 5xx, Polly retry x3 with
  1/4/16 s backoff. Total bound ~22 s. Acceptable: callers are
  background jobs (bolt 037), not inbound HTTP requests with a
  user waiting.
- **`SamedayAuthHandler` runs *outside* the rate-limit policy.**
  A handler-driven 401-retry doesn't count against the 5 req/s
  budget. This is *intentional* — refreshing the token is not the
  same kind of activity as a steady-state call — but it does mean
  the *effective* maximum call rate on Sameday's side during a
  burst of token-expired calls is slightly above 5 req/s. In
  practice the cap is unreachable because there's only one shared
  cached token to refresh.

### Risks

- **Risk: someone adds 401 to Polly's retryable set in a future PR**
  (e.g. while triaging a bug, "oh, 401 looks transient, let's
  retry it"). This would silently duplicate retries and could
  thunder-herd Sameday with auth requests. Mitigation: the
  `SamedayPolicies.Retry` policy carries a `// 401 belongs to
  SamedayAuthHandler` comment, and the test suite includes a
  regression test that asserts 401 responses are *not* retried by
  Polly (`SamedayClient` issues exactly one 401 + one retry, never
  three).
- **Risk: stale request body on retry.** Cloning a request whose
  body has already been consumed will fail. Mitigation: every
  outbound payload uses `JsonContent` which buffers the serialised
  bytes; the clone reads from the buffer. The
  `SamedayAuthHandler.CloneAsync` helper is the single chokepoint
  enforcing this.

## Related

- **Stories**: 002-token-auth-and-refresh.
- **Previous ADRs**: none direct; ADR-013 (in-process token cache)
  is the companion decision — together they define how token
  lifecycle and 401-handling interact.
- **Read when**: working on the Sameday HTTP pipeline; debugging a
  "why was this request retried N times" trace; tempted to fold
  401 into Polly's retry list; adding a new outbound endpoint to
  `SamedayClient`; reviewing whether to use the same pattern for a
  *different* upstream that has token-expiry semantics.
