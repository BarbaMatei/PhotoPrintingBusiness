---
unit: 002-security-baselines
intent: 001-foundation-infrastructure
phase: inception
status: draft
created: 2026-05-05T15:24:00Z
updated: 2026-05-05T15:24:00Z
---

# Unit Brief: Security Baselines

## Purpose

Establish the minimum security posture for a production e-commerce platform: HTTPS enforcement, CORS policy, rate limiting, and security headers (including CSP) on all responses.

## Scope

### In Scope
- HTTPS redirect + HSTS header (max-age=31536000, includeSubDomains)
- CORS: exact frontend origin whitelist, allow credentials
- Rate limiting: 100 req/min per IP (public), 10 req/min per IP (auth endpoints)
- Security headers: X-Content-Type-Options, X-Frame-Options, Referrer-Policy, Content-Security-Policy
- Secret management patterns (.NET Secret Manager dev, env vars prod)

### Out of Scope
- JWT authentication implementation (Epic 1 — US-105)
- File upload validation (Epic 2 — US-202)
- Payment webhook signature verification (Epic 3)

---

## Assigned Requirements

| FR | Requirement | Priority |
|----|-------------|----------|
| FR-6 | HTTPS & HSTS enforcement | Must |
| FR-7 | CORS policy (exact origin whitelist) | Must |
| FR-8 | Rate limiting (100/10 req/min) | Must |
| FR-9 | Security headers including CSP on all pages | Must |

---

## Domain Concepts

### Key Entities
| Entity | Description | Attributes |
|--------|-------------|------------|
| RateLimitPolicy | Named rate limit window | name, window, permitLimit |
| SecurityHeaders | Response headers for security | header name, value |

### Key Operations
| Operation | Description | Inputs | Outputs |
|-----------|-------------|--------|---------|
| EnforceHTTPS | Redirect HTTP → HTTPS, add HSTS | HTTP request | 301 redirect or HSTS header |
| ValidateCORS | Check request origin against whitelist | Origin header | Allow/Deny |
| ApplyRateLimit | Track request count per IP per window | Client IP | Allow or 429 |
| AddSecurityHeaders | Append security headers to response | none | Response headers |

---

## Story Summary

| Metric | Count |
|--------|-------|
| Total Stories | 4 |
| Must Have | 4 |
| Should Have | 0 |
| Could Have | 0 |

### Stories

| Story ID | Title | Priority | Status |
|----------|-------|----------|--------|
| 001-https-hsts-enforcement | HTTPS redirect and HSTS header | Must | Planned |
| 002-cors-policy | CORS exact origin whitelist | Must | Planned |
| 003-rate-limiting | Rate limiting middleware | Must | Planned |
| 004-security-headers | Security headers including CSP | Must | Planned |

---

## Dependencies

### Depends On
| Unit | Reason |
|------|--------|
| 001-error-handling-logging | Uses middleware pipeline and error handling established there |

### Depended By
None within this intent. Future epics depend on the security posture being in place.
