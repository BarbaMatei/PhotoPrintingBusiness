---
bolt: 044-tracing-and-metrics
created: 2026-06-03T02:00:00Z
status: accepted
---

# ADR-018: `/metrics` Uses IP Allow-List, Not JWT

## Context

The Prometheus scrape endpoint (`GET /metrics`) exposes the current
snapshot of every instrument the API has recorded — counters
(`orders_created_total`, `payment_webhook_total`), histograms
(`upload_size_bytes`, `order_processing_duration_seconds`), and
runtime metrics (GC, thread pool). The body is competitive
intelligence (volume, error rates, cardinality) and operationally
sensitive (which deploys are healthy vs degraded). It must not be
open to the public internet.

The rest of the API uses JWT-bearer auth on every protected endpoint
(intent 002). The default impulse is to extend the same pattern:
require an `Authorization: Bearer …` on `/metrics`, issue a long-lived
"scraper" token, and call it done.

But Prometheus scrapers are typically:
- Sidecar containers in the same pod (k8s `localhost`).
- Pull-based agents on a private VLAN (Docker Compose's internal
  network).
- Push-gateway processes on a trusted host.

They are not user-facing clients. Token issuance, rotation, refresh,
revocation — the entire machinery JWT exists to handle — is
operationally awful for a scraper. The standard industry posture
is network-level access control (IP allow-list, NetworkPolicy, mesh
mTLS) without app-level JWT.

We had to decide which posture to take for this codebase: harmonise
with the rest of the API (JWT) or deliberately deviate (IP allow-list).

## Decision

**`GET /metrics` is gated by `MetricsEndpointIpAllowListMiddleware`,
which checks `HttpContext.Connection.RemoteIpAddress` against a
configured list (`Observability:Metrics:AllowedScrapeIps`). Requests
from outside the list receive `403 Forbidden` with an empty body.
The endpoint does NOT participate in the JWT bearer middleware
chain.**

Restated as invariants:

- The `/metrics` endpoint MUST NOT be decorated with `[Authorize]`,
  `[Authorize(Roles=...)]`, or any JWT requirement.
- The `Observability:Metrics:AllowedScrapeIps` setting MUST be
  non-empty when `Observability:Enabled=true`; an empty list is a
  configuration error caught by the validator.
- Production allow-lists MUST be the minimum required IPs (typically
  one Prometheus scraper's IP / pod CIDR). The default
  `["127.0.0.1", "::1"]` is for local dev and Compose sidecar
  topologies — production deployments override.
- A 403 from this middleware MUST NOT include a response body that
  would leak the existence-or-shape of the endpoint to a
  reconnaissance probe. The default ASP.NET 403 (empty body) is
  what we want.

The middleware shape:

```csharp
public sealed class MetricsEndpointIpAllowListMiddleware : IMiddleware
{
    private readonly HashSet<IPAddress> _allowed;
    private readonly ILogger<MetricsEndpointIpAllowListMiddleware> _logger;
    private readonly ConcurrentDictionary<IPAddress, byte> _loggedDenies = new();

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var ip = context.Connection.RemoteIpAddress;
        if (ip is null || !_allowed.Contains(ip))
        {
            if (ip is not null && _loggedDenies.TryAdd(ip, 0))
                _logger.LogInformation("metrics.scrape.denied ip={Ip}", ip);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }
        await next(context);
    }
}
```

## Rationale

IP allow-list is the standard pattern for server-to-server scrape
endpoints across the industry (Prometheus's own documentation, the
operator pattern, Grafana Agent, etc.). It's the right primitive
for three reasons:

1. **Scrapers don't have users.** JWT is designed around user
   identity — issuance via login, rotation via refresh, audience
   binding, expiry. A scraper has none of these. Stuffing a long-lived
   token into the scraper config gets you the worst of both worlds:
   the security overhead of JWT (signing key, validator, rotation
   runbook) without any of its identity benefits.
2. **Network topology already enforces it.** In Docker Compose,
   k8s, and a single-VM with localhost-bound Prometheus, the
   scraper IS on a trusted network segment. The IP allow-list
   formalises a constraint the deployment topology already
   provides; JWT would be a second control on top that adds no
   security but adds a failure mode (token expires → all metrics
   go dark).
3. **Production blast radius is well-bounded.** The allow-list
   misconfiguration story is one-shot: either the scraper IP is in
   the list and works, or it isn't and 403s. There's no
   intermediate state (expired token, wrong audience, clock skew)
   that produces silent failures.

### Alternatives Considered

| Alternative | Pros | Cons | Why Rejected |
|---|---|---|---|
| **JWT bearer with a long-lived scraper token** | Consistent with the rest of the API. | Token issuance, rotation, revocation — all manual ops. A 12-month expiry token is "long-lived enough to forget about" but also "long-lived enough to be a real leak risk." Scraper config now has a secret to manage. Industry pattern is to not do this for server-to-server scrape. | Wrong primitive. Adds operational cost for zero security benefit over IP allow-list (which we'd want anyway in defence-in-depth). |
| **JWT + IP allow-list (both)** | Defence in depth. | Same operational costs as JWT-only, with marginal additional security. Most networks where this would matter (k8s with NetworkPolicy, hosted Prometheus with VPC peering) already provide stronger isolation than IP allow-list, making JWT the only practical control — and we just rejected JWT for the reasons above. | Diminishing returns; the JWT layer adds cost without proportionate benefit. |
| **Basic auth (username/password)** | Trivially simple. | Same key-management problem as JWT, without any of JWT's benefits. Credential in plaintext over HTTP unless TLS terminates correctly. Doesn't compose with the rest of the API's auth model. | Worse than JWT in every dimension. |
| **mTLS** | Strong identity binding to a specific client cert. | Requires a CA, a cert-rotation pipeline, and a cert-validation middleware. Massive overkill for a single-tenant scrape endpoint. | Right answer for multi-tenant / cross-org scrape; wrong scale for ours. |
| **Don't expose `/metrics` at all; push via OTLP-metrics** | No scrape endpoint to secure. | OTLP-metrics is push-based; requires a collector running somewhere that can receive metrics. Doubles outbound bandwidth (we already push traces via OTLP). The Grafana dashboard from bolt 045 was written for Prometheus scrape, so we'd have to rewrite it. | Out of scope for this bolt. May revisit if scale demands it. |
| **Bind the endpoint to a non-default port** (security by obscurity) | "Hidden" from the main port. | Adds a deployment artefact (second Kestrel listener). Doesn't actually solve the problem — port-scanners find it. Mixes Kestrel binding with auth concerns. | Doesn't make the endpoint safer, just harder to scrape. |
| **Path-based obscurity** (`/_internal_xyz/metrics`) | Trivial. | Provides no security at all — path is in CI logs, dashboard, scraper config. | Security theatre. |

## Consequences

### Positive

- **Operational simplicity.** A scraper drops onto `127.0.0.1:8080`
  or its sidecar URL and works. No token to provision, no
  rotation runbook, no expiry-induced outage.
- **Composes with deployment-layer security.** Cloud-native
  deployments add a NetworkPolicy / security group that bounds the
  IPs that can even REACH `/metrics`; the IP allow-list is the
  application-layer confirmation of that intent. Defence in depth
  comes from the network layer, not from JWT.
- **No silent failure mode.** If the scraper IP changes (pod reschedule,
  rolling restart), the failure is loud — every dashboard goes
  dark immediately — and the fix is one config-line edit.
- **Avoids JWT-everywhere temptation.** Without this ADR, a future
  "let's harmonize auth across all endpoints" PR would silently
  add `[Authorize]` to `/metrics` and the dashboards would break.

### Negative

- **Deviates from the JWT-everywhere posture.** Code reviewers
  unfamiliar with this ADR may initially flag the absence of
  `[Authorize]` on `/metrics` as an oversight. Mitigation: this
  ADR; the middleware itself is named explicitly
  (`MetricsEndpointIpAllowListMiddleware`) so the deviation is
  visible at the call site.
- **IP allow-list is brittle to topology changes.** A new
  Prometheus replica needs its IP added to the list. Mitigation:
  in k8s, use a CIDR (a `/24` for the scraper namespace) rather
  than individual IPs; the config supports either via
  `IPAddress.Parse` + range checks.
- **Spoofable in environments without ingress validation.** An
  attacker who can spoof the source IP on a packet (rare on
  modern networks, common on misconfigured ones) could bypass the
  allow-list. Mitigation: production deployments rely on the
  network layer (cloud provider security group, k8s NetworkPolicy)
  for the real enforcement; the in-app allow-list is the second
  line.

### Risks

- **Risk: harmonization-PR adds `[Authorize]` to `/metrics`.**
  Highest-likelihood silent regression. Mitigation: this ADR;
  `MetricsEndpointTests.Allowed_ip_no_auth_header_returns_200`
  pins the no-JWT path with an integration test.
- **Risk: production allow-list ships as the default
  `["127.0.0.1", "::1"]` and a remote scraper can't reach it.**
  Same operational failure mode as misconfigured DNS — loud,
  immediately visible, easy to fix. Documented in DEPLOYMENT.md
  §14 as the most-likely-misconfiguration in the rollout.
- **Risk: the allow-list is set to `["0.0.0.0/0"]`** (effectively
  public) by an operator who treats it as a checkbox.
  Mitigation: the documentation explicitly calls out that the
  allow-list is the only application-layer control on the
  endpoint and lists the metric categories that would be exposed
  if it's wide open. The validator does NOT enforce "not 0.0.0.0"
  because some valid topologies (single-VM with strict firewall)
  legitimately use a wide-open app-layer allow-list and rely on
  the firewall.

## Related

- **Stories**: 002-business-metrics-and-prometheus (the immediate
  consumer); intent 020 unit 002 (the SLO dashboard that consumes
  `/metrics` data).
- **Previous ADRs**: none directly — this is the first endpoint in
  the codebase that deliberately opts out of JWT.
- **External**: [Prometheus documentation —
  authentication](https://prometheus.io/docs/operating/security/);
  [Grafana Agent — scrape
  config](https://grafana.com/docs/agent/latest/operator/api/).
- **Future ADRs**: if we ever expose `/metrics` to a multi-tenant
  scrape consumer (hosted Prometheus across orgs), that bolt
  introduces mTLS — which would supersede this ADR for that
  specific deployment topology.
- **Read when**: adding any server-to-server endpoint that doesn't
  have a user identity attached (push-gateways, health checks
  beyond `/health`, internal admin APIs); reviewing PRs that touch
  `/metrics` or its middleware; tempted to add `[Authorize]` to
  `/metrics` "for consistency"; designing a NetworkPolicy / security
  group that bounds traffic to the API; debugging "why does the
  scraper get 403."
