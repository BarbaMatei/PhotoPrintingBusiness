---
bolt: 001-error-handling-logging
created: 2026-05-05T15:50:00Z
status: accepted
superseded_by: null
---

# ADR-001: Health Endpoint Always Returns HTTP 200

## Context

ASP.NET Core's built-in `HealthCheckOptions` defaults to returning `HTTP 503 Service Unavailable` when any registered health check reports `Unhealthy` or `Degraded` status. For FotoTipar, the `/health` endpoint must report operational status for monitoring tools (uptime monitors, Docker healthchecks) while the actual health state is expressed in the JSON body.

The question is: should the HTTP status code reflect health state (503 when unhealthy) or should the endpoint always succeed at the transport layer (200 OK) with health encoded in the body?

Constraints:
- Monitoring tools (UptimeRobot, Pingdom, simple HTTP probes) often interpret any non-200 as "service down" and trigger alerts
- Load balancers that use 503 for health checks would remove the node from rotation when DB is temporarily unreachable — this could cause cascading issues during brief DB blips
- The Angular frontend may call `/health` for its own display; a 503 would be treated as a failed HTTP request by `HttpClient`

## Decision

The `/health` endpoint always returns `HTTP 200 OK`. The `status` field in the JSON response body (`"Healthy"` or `"Unhealthy"`) conveys the actual operational state. All monitoring tools are configured to parse the body rather than rely solely on HTTP status code.

## Rationale

Returning 200 always decouples transport-level success (the endpoint is reachable and responding) from application-level health (the DB and disk are operational). This is more appropriate when:

1. Monitoring tools need to distinguish "endpoint unreachable" (network/process failure → connection refused or timeout) from "endpoint reachable but system degraded" (200 with `status: Unhealthy`)
2. Load balancers should not automatically remove a node just because the DB is briefly unreachable — a 5-second DB timeout is too sensitive to use as a routing signal
3. The health check is informational, not a routing gate at MVP

### Alternatives Considered

| Alternative | Pros | Cons | Why Rejected |
|-------------|------|------|--------------|
| Default ASP.NET Core behavior (503 on unhealthy) | Standard behavior, automatic load balancer integration | Triggers false "down" alerts on transient DB blips; complicates Angular HttpClient error handling | Rejected — too aggressive for MVP; load balancer routing not yet configured |
| Multi-status (200 Healthy, 207 Degraded, 503 Unhealthy) | Precise HTTP semantics | Complex to configure in ASP.NET Core; monitoring tool configuration more complex | Rejected — over-engineering for MVP |

## Consequences

### Positive

- Monitoring tool configuration is simpler: check for `"status": "Healthy"` in body
- Transient DB connectivity issues do not trigger false "service down" alerts
- Angular `HttpClient` can handle the response uniformly without error handling for 503

### Negative

- Deviates from ASP.NET Core default and RFC conventions; developers unfamiliar with this decision may change it inadvertently
- Load balancers cannot use HTTP status code alone for routing decisions

### Risks

- **Risk**: Future load balancer integration (bolt 002 or later) may need custom health probe configuration to read JSON body. **Mitigation**: Document in infrastructure runbook; revisit when load balancer is introduced.

## Related

- **Stories**: US-1-004 (health-check-endpoint)
- **Standards**: api-conventions.md (status code usage)
- **Previous ADRs**: none
