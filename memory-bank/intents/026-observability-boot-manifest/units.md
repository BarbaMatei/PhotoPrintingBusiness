---
intent: 026-observability-boot-manifest
phase: inception
status: units-decomposed
updated: 2026-06-05T09:30:00Z
---

# Observability, Boot Composition & System Manifest - Unit Decomposition

## Units Overview

Decomposes into **4 units** — three backend (boot/flags, manifest+liveness, docs) and one frontend (the admin System tab). Boot/flags is foundational; manifest and UI build on it.

### Unit 1: 001-boot-composition-and-flags
**Description**: Extract Program.cs subsystem composition into extension methods (P07) and introduce a typed `IFeatureGate` registry (P10).
**Stories**: 001-program-subsystem-extensions, 002-typed-feature-gate
**Deliverables**: `Extensions/{Sameday,Anaf,Invoicing,Payments,Sentry}Extensions.cs`, slim `Program.cs`, `Services/FeatureFlags/IFeatureGate.cs` + `ConfigFeatureGate`.
**Dependencies**: Depends on None · Depended by Unit 2, Unit 4
**Estimated Complexity**: M

### Unit 2: 002-system-manifest-and-liveness
**Description**: `GET /api/admin/system-info` manifest (P04 backend) + background-job liveness check and ANAF invoice metrics/SLO (P17).
**Stories**: 001-system-info-endpoint, 002-background-job-liveness-check, 003-anaf-invoice-metrics-and-slo
**Deliverables**: `Controllers/AdminSystemInfoController.cs`, `Services/SystemInfo/`, `Observability/IHeartbeat.cs`, `HealthChecks/BackgroundJobLivenessCheck.cs`, `FotoMetrics` additions, `slos.md` entry.
**Dependencies**: Depends on Unit 1 (manifest reads `IFeatureGate.GetAll()`) · Depended by Unit 4
**Estimated Complexity**: M

### Unit 3: 003-architecture-and-standards-docs
**Description**: Multi-replica-readiness doc (P12) + standards refresh, KNOWN_FAILURES, audit checklist (P19).
**Stories**: 001-multi-replica-readiness-doc, 002-refresh-tech-stack-and-known-failures, 003-architecture-audit-checklist
**Deliverables**: `docs/architecture/multi-replica-readiness.md`, refreshed `tech-stack.md`, `docs/KNOWN_FAILURES.md`, `docs/ARCHITECTURE_AUDIT_CHECKLIST.md`.
**Dependencies**: Depends on None · Depended by None
**Estimated Complexity**: S

### Unit 4: 004-observability-boot-manifest-ui
**Description**: Admin "System" tab that renders and searches the manifest (P04 UI).
**Stories**: 001-admin-system-info-tab
**Unit Type**: frontend
**Deliverables**: `features/admin/pages/system/` Angular page consuming `/api/admin/system-info`.
**Dependencies**: Depends on Unit 2 (and transitively Unit 1) · Depended by None
**Estimated Complexity**: S

## Requirement-to-Unit Mapping

- **FR-1 (P07)** → `001-boot-composition-and-flags`
- **FR-2 (P10)** → `001-boot-composition-and-flags`
- **FR-3 (P04, backend)** → `002-system-manifest-and-liveness`; **(P04, UI)** → `004-observability-boot-manifest-ui`
- **FR-4 (P17)** → `002-system-manifest-and-liveness`
- **FR-5 (P12)** → `003-architecture-and-standards-docs`
- **FR-6 (P19)** → `003-architecture-and-standards-docs`

## Unit Dependency Graph

```text
[001-boot-composition-and-flags] ──> [002-system-manifest-and-liveness] ──> [004-...-ui]
[003-architecture-and-standards-docs]  (independent)
```

## Execution Order

1. Unit 1 (boot + flags)
2. Unit 2 (manifest + liveness) — after Unit 1; Unit 3 (docs) in parallel
3. Unit 4 (UI) — after Unit 2
