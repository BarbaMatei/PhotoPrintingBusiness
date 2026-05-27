---
id: 001-https-hsts-enforcement
unit: 002-security-baselines
intent: 001-foundation-infrastructure
status: draft
priority: must
created: 2026-05-05T15:26:00Z
assigned_bolt: null
implemented: false
---

# Story: 001-https-hsts-enforcement

## User Story

**As a** system
**I want** all HTTP traffic redirected to HTTPS with HSTS headers in production
**So that** all data in transit is encrypted and browsers remember to use HTTPS

## Acceptance Criteria

- [ ] **Given** an HTTP request in production, **When** processed, **Then** a 301 redirect to HTTPS is returned
- [ ] **Given** an HTTPS request in production, **When** processed, **Then** the response includes `Strict-Transport-Security: max-age=31536000; includeSubDomains`
- [ ] **Given** the app runs in Development, **When** processed, **Then** HSTS is NOT applied (to avoid dev certificate issues)

## Technical Notes

- `app.UseHttpsRedirection()` in pipeline
- `app.UseHsts()` conditionally in production only
- Configure HSTS options: maxAge=365 days, includeSubDomains=true

## Dependencies

### Requires
- None

### Enables
- All subsequent HTTPS-only features (secure cookies, CORS with credentials)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Health check via HTTP | Still redirect to HTTPS (health check is public but not exempt from HTTPS) |
| Behind reverse proxy | Respect X-Forwarded-Proto header for HTTPS detection |

## Out of Scope

- SSL certificate provisioning (handled by reverse proxy / Let's Encrypt)
