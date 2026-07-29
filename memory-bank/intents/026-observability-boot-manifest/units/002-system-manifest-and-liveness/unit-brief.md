---
unit: 002-system-manifest-and-liveness
intent: 026-observability-boot-manifest
phase: inception
status: draft
unit_type: backend
default_bolt_type: simple-construction-bolt
created: 2026-06-05T09:30:00Z
updated: 2026-06-05T09:30:00Z
---

# Unit Brief: System Manifest & Job Liveness

## Purpose

Expose what is wired right now (P04 manifest) and detect silently-dead background jobs + measure ANAF lag against the legal SLA (P17). Together these close the "hidden functionality / silent failure" gap.

## Scope

### In Scope
- `GET /api/admin/system-info` returning hosted services, flags, routes, CLI verbs (cached ~30s, admin-only, no secrets).
- `IHeartbeat` + `BackgroundJobLivenessCheck`; `invoice_upload_total{result}` + `invoice_upload_lag_seconds`; ANAF SLO entry.

### Out of Scope
- The admin UI tab (unit 004).
- Implementing the flag registry (unit 001 provides `IFeatureGate`).

---

## Assigned Requirements

| FR | Requirement | Priority |
|----|-------------|----------|
| FR-3 (P04, backend) | `/api/admin/system-info` feature manifest | Should |
| FR-4 (P17) | Background-job liveness check + ANAF invoice metrics + SLO | Must |

---

## Domain Concepts

### Key Entities
| Entity | Description | Attributes |
|--------|-------------|------------|
| SystemManifest | Introspection result | Version, HostedServices, FeatureFlags, AdminRoutes, WebhookRoutes, CliVerbs |
| Heartbeat registry | Last-beat per job | jobName → DateTimeOffset |

### Key Operations
| Operation | Description | Inputs | Outputs |
|-----------|-------------|--------|---------|
| GetManifest | Introspect DI + config + routes | IServiceProvider, IFeatureGate | SystemManifest (cached 30s) |
| Beat / liveness eval | Track + assess job heartbeats | jobName, interval | Healthy/Degraded |
| Stamp invoice metric | Record ANAF upload outcome + lag | result, lag | counter/histogram |

---

## Story Summary

| Metric | Count |
|--------|-------|
| Total Stories | 3 |
| Must Have | 2 |
| Should Have | 1 |
| Could Have | 0 |

### Stories

| Story ID | Title | Priority | Status |
|----------|-------|----------|--------|
| 001-system-info-endpoint | /api/admin/system-info manifest | Should | Planned |
| 002-background-job-liveness-check | Heartbeat + liveness health check | Must | Planned |
| 003-anaf-invoice-metrics-and-slo | invoice_upload metrics + SLO | Must | Planned |

---

## Dependencies

### Depends On
| Unit | Reason |
|------|--------|
| 001-boot-composition-and-flags | Manifest reads `IFeatureGate.GetAll()` |

### Depended By
| Unit | Reason |
|------|--------|
| 004-...-ui | Consumes the manifest endpoint |

### External Dependencies
| System | Purpose | Risk |
|--------|---------|------|
| Prometheus | Scrapes new metrics | Low |
| ANAF SPV | SLA source for the SLO | Medium (legal SLA) |

---

## Technical Context

### Suggested Technology
ASP.NET Core health checks, `System.Diagnostics.Metrics`, reflection over `IEndpointRouteBuilder`.

### Integration Points
| Integration | Type | Protocol |
|-------------|------|----------|
| Admin UI | API | GET /api/admin/system-info |
| Prometheus | metrics | /metrics |
| Health probe | health | /health (job liveness) |

---

## Constraints

- Manifest must be derived from `IFeatureGate.GetAll()` (no duplicated flag list).
- Admin-only; expose no secrets.

---

## Success Criteria

### Functional
- [ ] With `Anaf:Enabled=true`, manifest reports `InvoiceUploadJob: Running`; removing the registration fails a test.
- [ ] Liveness reports Degraded for a stale heartbeat (>3× interval).
- [ ] `invoice_upload_total{result}` + lag histogram emitted; SLO in `slos.md`.

### Non-Functional
- [ ] Manifest cache hit < 50ms; no secrets in payload.

### Quality
- [ ] Integration test for the flag/job regression case.

---

## Bolt Suggestions

| Bolt | Type | Stories | Objective |
|------|------|---------|-----------|
| 056-system-manifest-and-liveness | simple | 001, 002, 003 | Manifest + liveness + ANAF metrics |

---

## Notes

P17 (liveness + metrics) is a pre-launch must-have; P04 is a strong nice-to-have.
