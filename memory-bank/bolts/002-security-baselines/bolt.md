---
id: 002-security-baselines
unit: 002-security-baselines
intent: 001-foundation-infrastructure
type: ddd-construction-bolt
status: complete
stories:
  - 001-https-hsts-enforcement
  - 002-cors-policy
  - 003-rate-limiting
  - 004-security-headers
created: 2026-05-05T15:30:00Z
started: 2026-05-19T00:00:00Z
completed: 2026-05-19T00:00:00Z
current_stage: complete
stages_completed:
  - domain-model
  - technical-design
  - adr-analysis
  - implement
  - test

requires_bolts: [001-error-handling-logging]
enables_bolts: []
requires_units: []
blocks: false

complexity:
  avg_complexity: 2
  avg_uncertainty: 1
  max_dependencies: 1
  testing_scope: 2
---

# Bolt: 002-security-baselines

## Overview

Establish the security posture for the production API: HTTPS enforcement, CORS policy, rate limiting, and security headers (including CSP on all responses).

## Objective

Configure HTTPS redirect + HSTS, exact-origin CORS with credentials, per-IP rate limiting (100 public / 10 auth per minute), and security headers middleware — producing an API that meets OWASP baseline security requirements.

## Stories Included

- **001-https-hsts-enforcement**: HTTPS redirect and HSTS header (Must)
- **002-cors-policy**: CORS exact origin whitelist (Must)
- **003-rate-limiting**: Rate limiting middleware (Must)
- **004-security-headers**: Security headers including CSP (Must)

## Bolt Type

**DDD Construction Bolt** — 5 stages: Domain Model → Technical Design → Implementation → Testing → Review

## Dependencies

### Bolt Dependencies (within intent)
- **001-error-handling-logging** (Required): Middleware pipeline and error handling must be established first

### Unit Dependencies (cross-unit)
- None

### Enables (other bolts waiting on this)
- None within this intent. Future epics depend on security being in place.

## Expected Outputs

- `src/PhotoPrint.API/Middleware/SecurityHeadersMiddleware.cs`
- CORS, rate limiting, HSTS, HTTPS configuration in `Program.cs`
- Integration tests: CORS rejection, rate limit enforcement, security headers presence
