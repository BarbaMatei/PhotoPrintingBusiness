---
bolt: 044-tracing-and-metrics
created: 2026-06-03T02:00:00Z
status: accepted
---

# ADR-017: Deterministic Trace-ID Sampling, Not Random

## Context

`RouteAwareSampler` is the inner sampler in the OTel trace pipeline (the
outer is `ParentBasedSampler`, which honours the upstream decision when a
parent span exists). For the root span — an incoming HTTP request with no
parent — the inner sampler decides "record this trace, or drop it" based
on the route's configured rate (e.g. `GET /api/products` → 0.05).

The decision function has to map `(trace_id, rate) → bool`. The naive
choice is `Random.Shared.NextDouble() < rate`. The non-obvious choice is
to derive the decision from a deterministic hash of the `trace_id` so the
result is the same every time the same trace is evaluated. The
distinction matters because:

- An incoming HTTP request creates a server span with a fresh `trace_id`.
- That trace_id propagates via W3C trace-context (`traceparent` header)
  on every outbound HTTP call (Stripe, Sameday, ANAF) and into every EF
  Core span.
- Each span the SDK records is evaluated by the sampler. If the function
  is random, two evaluations within the same trace can disagree —
  the server span might be sampled and a child EF span dropped, producing
  a trace with holes ("partial trace").
- Worse: when our trace_id reaches a downstream service that also runs
  OTel with its own sampler, both samplers must agree. Otherwise the
  downstream might record spans that have no parent on our side, or
  vice versa.

We had to decide what function the inner sampler uses to convert
`(trace_id, rate)` into a deterministic, system-wide-consistent decision.

## Decision

**`RouteAwareSampler` derives its sampling decision from a deterministic
hash of the `trace_id`. The same trace_id always yields the same
decision for the same configured rate. Random number generation
(`Random.Shared.NextDouble`, `System.Security.Cryptography.RandomNumberGenerator`,
etc.) is forbidden in the sampling path.**

The canonical shape:

```csharp
public SamplingResult ShouldSample(in SamplingParameters parameters)
{
    var route = ResolveRoute(parameters);                   // "GET /api/products"
    var rate  = _routeRates.GetValueOrDefault(route, _defaultRate);

    if (rate >= 1.0) return _alwaysSample;
    if (rate <= 0.0) return _neverSample;

    // Lower 63 bits of the trace_id, normalised to [0, 1).
    var traceId = parameters.TraceId;
    var lower   = BinaryPrimitives.ReadUInt64BigEndian(traceId.ToByteArray().AsSpan(8));
    var ratio   = (lower & 0x7FFFFFFFFFFFFFFFul) / (double)long.MaxValue;

    return ratio < rate ? _recordAndSample : _drop;
}
```

Restated as invariants:

- **Same `trace_id` + same `rate` → same decision.** Always. No exceptions.
- The hash function is publicly documented and stable. Lower 63 bits of
  the trace_id treated as a uniform integer, normalised against
  `long.MaxValue`. Industry-standard (see [OpenTelemetry sampling
  SIG](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/trace/tracestate-probability-sampling.md)).
- **The sampling path MUST NOT call `Random.*`, `Guid.NewGuid`, or any
  other entropy source per evaluation.** PR reviewers should flag any
  such call inside `RouteAwareSampler` or any future sampler in this
  codebase.
- The `ErrorOverride` post-decision processor (5xx → force-sampled) is a
  separate concern and does NOT make the decision function non-deterministic;
  it's a span-completion fix-up, not a sampling decision.

## Rationale

Deterministic-by-trace-id sampling solves three concrete problems that
random sampling would create:

1. **Trace completeness within a single request.** A request that
   produces 4 spans (HTTP server → 2× EF queries → 1× outbound Stripe
   call) is either entirely sampled (4 spans land in Tempo/Jaeger) or
   entirely dropped (0 spans). Never 3-of-4. Random sampling produces
   "frankenstein" traces where the root is visible but the slow EF
   query that explains the latency is missing.

2. **System-wide trace consistency under W3C propagation.** When a
   downstream service receives our `traceparent` header and runs its
   own deterministic-by-trace-id sampler with the same rate, both
   services make the same decision. The trace is either complete
   end-to-end or absent end-to-end. With random sampling, the two
   services disagree probabilistically; debugging cross-service
   latency becomes a guessing game.

3. **Reproducibility for debugging.** "This specific request id was
   slow — let me find its trace in Tempo." With deterministic sampling,
   if the trace exists for the given trace_id, every subsequent
   re-evaluation (e.g. a test that constructs a span with that
   trace_id) yields the same decision. Random sampling makes this a
   different roll each time.

### Alternatives Considered

| Alternative | Pros | Cons | Why Rejected |
|---|---|---|---|
| **`Random.Shared.NextDouble() < rate`** | Trivial code; obvious correctness for the single-span case. | Produces partial traces; disagrees with downstream services. | Wrong semantics for distributed tracing. The single-span "correctness" is irrelevant — sampling is about whether the WHOLE TRACE is recorded. |
| **`new Random().NextDouble() < rate`** | Same as above, with worse properties (constant-seeded Random in older .NET). | Same problems as above plus higher risk of correlation between requests served by the same thread. | Same rejection, slightly more concerning. |
| **`RandomNumberGenerator.GetInt32(0, 100) < rate * 100`** | Cryptographically uniform; immune to thread-correlation issues. | Still random — still produces partial traces. Slower (cryptographic entropy is wasted here). | Wrong primitive for the same reason as `Random.NextDouble`. Sampling decisions don't need crypto entropy; they need determinism. |
| **Hash the trace_id with SHA-256, take low bits** | Cryptographically uniform; deterministic. | Significant CPU cost per request (SHA-256 in the hot path). The trace_id is already a uniformly random 128-bit value generated by the OTel SDK; hashing it doesn't add entropy. | Over-engineered. The trace_id IS the hash. |
| **Always-on (rate=1.0) + collector-side tail sampling** | Decision moved to the collector, which can be smarter. | Doubles the egress traffic; tail sampling requires a collector that supports it (Tempo does, some don't). Couples app to specific collector. | Out of scope. May revisit at scale; until then, in-app head sampling is the right primitive. |

## Consequences

### Positive

- **No partial traces.** When a trace is recorded, every span in it
  is recorded (modulo the SDK's bounded buffer overflow, which is a
  separate concern). When a trace is dropped, every span is dropped.
  Clean signal for the operator.
- **Cross-service trace consistency for free.** Any downstream
  service that follows the OTel spec's recommended trace-state
  probability sampling (the same algorithm) makes the same
  decision. No coordination needed.
- **Reproducible.** "Replay this request with trace_id X" yields the
  same sampling decision deterministically; useful for debugging
  edge cases.
- **Hot-path cheap.** Two arithmetic ops + a dictionary lookup. No
  syscall, no allocation, no crypto.

### Negative

- **Sampler choice is harder to explain than `Random.NextDouble`.**
  A new contributor reading the code may wonder "why are we hashing
  the trace_id?" This ADR is the answer — link from the sampler's
  doc comment.
- **The sampling decision depends on trace_id quality.** If the SDK
  ever generated trace_ids with low entropy, the sampler's
  uniformity would degrade. We rely on the OTel SDK using
  cryptographic randomness for trace_id generation (it does, per
  spec).
- **Tuning a route's rate doesn't immediately rebalance which traces
  are sampled.** With random sampling, dropping rate from 0.10 →
  0.05 immediately changes half the traces. With deterministic
  sampling, the traces that were sampled at 0.10 are a strict
  superset of those at 0.05 — no shuffling. This is actually a
  feature (rate changes don't randomly lose individual debug
  traces) but worth being aware of.

### Risks

- **Risk: someone "simplifies" the sampler to `Random.NextDouble`.**
  Highest-likelihood silent regression. Mitigation: this ADR; a unit
  test (`RouteAwareSamplerTests.Same_trace_id_same_rate_same_decision`)
  that re-evaluates the same trace_id 100 times and asserts the
  decision is constant; PR review on any change to the sampling
  path.
- **Risk: someone adds a second custom sampler for a different
  reason and uses `Random` in it.** Pattern leak — the rule "no
  randomness in the sampling path" applies to ALL samplers in this
  codebase, not just `RouteAwareSampler`. Mitigation: documented in
  this ADR's "Decision" section and in the sampler's namespace-level
  doc comment.
- **Risk: a future contributor reads the lower 63 bits and decides
  it's "obviously wrong, should use all 128 bits."** Switching to
  128 bits is fine as long as the function stays deterministic, but
  changes which traces are sampled — i.e. invalidates existing
  Tempo queries that filter on trace_id presence. Mitigation: this
  ADR pins the specific function shape.

## Related

- **Stories**: 003-per-route-sampling (the immediate consumer);
  001-otel-tracing-instrumentation (which produces the trace_ids the
  sampler reads).
- **Previous ADRs**: none directly; ADR-003 (correlation id) is
  tangentially related (both deal with cross-system request
  identity).
- **External**: [OpenTelemetry sampling SIG — TraceState probability
  sampling](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/trace/tracestate-probability-sampling.md);
  [W3C trace-context](https://www.w3.org/TR/trace-context/).
- **Future ADRs**: if we ever introduce tail sampling at the
  collector, that ADR will explicitly NOT supersede this one —
  head sampling stays deterministic; tail adds a second filter on
  top.
- **Read when**: implementing or modifying any sampler in
  `Observability/Sampling/`; reviewing PRs that touch the sampling
  path; debugging "why does this trace_id exist in Tempo but its
  EF spans don't"; reasoning about cross-service trace
  completeness; tempted to use `Random.NextDouble` "for
  simplicity"; designing similar deterministic-by-id decisions in
  other domains (e.g. feature flag rollouts, A/B test bucketing).
