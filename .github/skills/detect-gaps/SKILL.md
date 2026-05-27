---
name: detect-gaps
description: >
  Compares what the codebase has against a capability matrix for its detected domain.
  Identifies missing capabilities, scalability bottlenecks, security gaps,
  UX limitations, and operational blind spots.
  Run this skill after infer-workflows.
allowed-tools:
  - read_file
  - search_files
---

# Gap Detector

## Capability matrix by domain

Compare what you found against the standard expected capabilities for the detected domain. Mark each as ✅ Present, ⚠️ Partial, or ❌ Missing.

### Universal capabilities (every system needs these)

| Capability | What to look for |
|---|---|
| Authentication | JWT / OAuth2 / session with refresh tokens |
| Authorisation / RBAC | Role checks on routes, not just auth |
| Input validation | Schema validation library (zod, joi, pydantic, etc.) |
| Structured error responses | Consistent error format across all endpoints |
| Pagination | Cursor or offset pagination on all list endpoints |
| Rate limiting | Rate limit middleware or API gateway config |
| Soft deletes | `deleted_at` or `is_deleted` fields on main entities |
| Audit log | Created/updated timestamps + user references |
| Health check endpoint | `/health` or `/ping` returning 200 |
| Structured logging | JSON log output with correlation IDs |
| Environment config | No secrets in source, proper env file structure |
| Tests | At least unit tests for business logic |

### E-commerce specific

| Capability | What to look for |
|---|---|
| Cart / basket persistence | Cart table or session-stored cart |
| Inventory management | Stock level tracking with reservation |
| Order state machine | Clear status transitions (pending→paid→shipped→delivered) |
| Payment integration | Stripe / PayPal / payment gateway integration |
| Return / refund flow | Refund endpoint + reversal logic |
| Tax calculation | Tax rules or third-party tax service |
| Email receipts | Order confirmation email trigger |
| Search / filtering | Product search with filters |
| Product variants | SKU / variant management |
| Discount / coupon system | Promo code model and validation |

### SaaS / B2B platform specific

| Capability | What to look for |
|---|---|
| Multi-tenancy | Tenant isolation in DB queries |
| Subscription billing | Stripe Billing / Chargebee integration |
| Usage metering | Usage tracking per tenant |
| Team / member management | Invite flow, role assignment |
| SSO / SAML | Enterprise SSO option |
| Audit log per tenant | Tenant-scoped activity log |
| Feature flags | Feature flag system for plan gating |
| Onboarding flow | Guided setup for new tenants |

### Fintech specific

| Capability | What to look for |
|---|---|
| Double-entry bookkeeping | Ledger with debit/credit entries |
| Transaction idempotency | Idempotency keys on payment endpoints |
| KYC / identity verification | KYC status on user model |
| Compliance logging | Immutable audit trail |
| Fraud detection hooks | Risk score or rules engine |
| Currency handling | Decimal-safe amounts (no floats) |
| Reconciliation | Nightly reconciliation job |

### Healthcare specific

| Capability | What to look for |
|---|---|
| HIPAA / GDPR data handling | Encryption at rest, data access logs |
| Patient consent | Consent records model |
| Appointment scheduling | Availability + booking conflict check |
| Clinical notes | Structured note format |
| Prescription management | Prescription model with expiry |
| Provider credentialing | Provider licence/credential model |

## Scalability bottleneck checklist

Check for each of these — flag any you find:

- **Synchronous chains**: long chains of await calls with no queuing for background work
- **No job queue**: emails, notifications, or heavy processing done synchronously in request handler
- **No caching layer**: no Redis / Memcached / CDN for frequently read data
- **N+1 queries**: loops that call the DB inside a loop (look for `findOne` or `findById` inside `forEach` / `map`)
- **No database connection pooling**: raw `new Client()` on every request
- **No read replicas**: single DB instance handling both reads and writes
- **Large monolithic deployments**: single container with no horizontal scaling config
- **No CDN**: static assets served directly from the app server
- **Unbounded queries**: list endpoints without pagination that could return millions of rows
- **Missing indexes**: foreign key columns without corresponding indexes

## Output format

Produce a section titled **Gap Analysis** with:

1. **Universal capability matrix** — the table with ✅/⚠️/❌ for every row
2. **Domain-specific matrix** — same for the detected domain
3. **Scalability bottlenecks found** — bullet list of actual findings with file:line references where possible
4. **Security gaps** — list of specific issues found, not generic advice
5. **Observability score** — rate 1-5 with justification
6. **Top 5 critical gaps** — the 5 most important missing things, ranked by business risk
