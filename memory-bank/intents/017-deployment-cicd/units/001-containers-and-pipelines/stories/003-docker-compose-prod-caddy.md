---
id: 003-docker-compose-prod-caddy
unit: 001-containers-and-pipelines
intent: 017-deployment-cicd
status: draft
priority: must
created: 2026-05-25T10:20:00Z
assigned_bolt: 040-containers-and-pipelines
implemented: false
---

# Story: 003-docker-compose-prod-caddy

## User Story

**As** an operator
**I want** a `docker-compose.prod.yml` with Caddy in front of the API
**So that** TLS, HSTS, and static asset caching are handled at the edge with zero hand-rolled cert management

## Acceptance Criteria

- [ ] `docker-compose.prod.yml` runs `caddy` + `api` (Postgres pointed at a managed instance by default).
- [ ] API port not exposed on host.
- [ ] `Caddyfile` redirects HTTP → HTTPS, applies HSTS for 365 d, sets sensible compression.
- [ ] First boot against a real DNS hostname obtains a Let's Encrypt cert automatically.
- [ ] Caddy access logs persisted to a named volume.

## Technical Notes

```caddy
fototipar.ro {
    encode gzip
    reverse_proxy api:8080
    header {
        Strict-Transport-Security "max-age=31536000; includeSubDomains; preload"
        X-Content-Type-Options nosniff
        Referrer-Policy strict-origin-when-cross-origin
    }
    log {
        output file /var/log/caddy/access.log
    }
}
```

- `acme_ca` in dev environments points at LE staging to avoid rate limits.

## Dependencies

### Requires
- 001-api-dockerfile, 002-docker-compose-dev

### Enables
- 005-github-actions-deploy (deploys to a host running this compose)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Managed DB not yet provisioned | Compose includes commented-out `db` service for emergency standalone deploys |
| Cert issuance rate-limited | Use LE staging until cutover; documented in README |

## Out of Scope

- WAF in front of Caddy (Cloudflare etc.) — separate ops decision.
