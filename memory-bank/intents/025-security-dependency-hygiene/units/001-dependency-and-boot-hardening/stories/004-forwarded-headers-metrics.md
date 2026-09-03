---
id: 004-forwarded-headers-metrics
unit: 001-dependency-and-boot-hardening
intent: 025-security-dependency-hygiene
status: draft
priority: must
created: 2026-06-05T09:30:00Z
assigned_bolt: 054-dependency-and-boot-hardening
implemented: false
---

# Story: 004-forwarded-headers-metrics

## User Story

**As an** operator scraping `/metrics` in production
**I want** the IP allow-list to evaluate the real scraper IP behind Caddy
**So that** the allow-list is not silently wrong on day-1 of deployment

## Acceptance Criteria

- [ ] **Given** `app.UseForwardedHeaders()` registered before `UseCorrelationId`, **When** a request arrives via Caddy, **Then** `Connection.RemoteIpAddress` reflects `X-Forwarded-For`, not the proxy IP
- [ ] **Given** `ForwardedHeadersOptions`, **When** configured, **Then** `KnownNetworks`/`KnownProxies` are cleared and anchored to the reverse-proxy CIDR only
- [ ] **Given** `MetricsEndpointIntegrationTests`, **When** an `X-Forwarded-For` case runs, **Then** an allow-listed IP gets 200 and a non-listed IP gets 403
- [ ] **Given** the change, **When** DEPLOYMENT.md §14 is read, **Then** it documents the proxy-trust requirement

## Technical Notes

- `XForwardedFor | XForwardedProto`. Misconfigured `KnownNetworks` enables spoofing — anchor to the actual `docker-compose.prod.yml` bridge CIDR.
- Order: forwarded headers must run before middleware that reads the client IP.

## Dependencies

### Requires
- None (independent of 001-003, but ships last in sequence)

### Enables
- 029/001 P08 global rate limit (keys on the real client IP)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Spoofed X-Forwarded-For from untrusted source | Ignored (only trusted CIDR honoured) |
| Direct (non-proxied) request in dev | Falls back to connection IP |

## Out of Scope

- The global rate limiter itself (intent 029 P08).
