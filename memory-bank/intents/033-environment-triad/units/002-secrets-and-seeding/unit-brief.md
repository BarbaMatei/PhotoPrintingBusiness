---
unit: 002-secrets-and-seeding
intent: 033-environment-triad
phase: inception
status: stories-defined
unit_type: frontend
default_bolt_type: simple-construction-bolt
created: 2026-06-05T12:20:00Z
updated: 2026-06-05T12:30:00Z
---

# Unit Brief: Secrets & Seeding

## Purpose

Layer the per-environment secrets strategy and the per-environment seeding policy onto the dev-env tier defined in unit 001. The secrets work is a documented matrix + a dev-env `.env` template (test-mode keys); the seeding work is a documented policy + a per-`ASPNETCORE_ENVIRONMENT` selection mechanism reusing the existing seed classes, with a guard preventing demo data in Production. **Readiness only — no real secrets provisioned, no host stood up.**

## Scope

### In Scope
- `docs/environments/secrets-matrix.md`: every secret × tier (required?, test-vs-live, where stored).
- `.env.dev-env.example`: dev-env template with placeholders + test-mode defaults, no real values.
- Seeding-policy doc: prod = reference-only; dev-env = demo; local = dev/lighter.
- A selection mechanism applying the right seed set per environment + a Production demo-data guard.

### Out of Scope
- Defining the tier / compose / config map (unit 001).
- The promotion runbook (unit 003).
- Provisioning real secrets or running a seed against a real host.

---

## Assigned Requirements

| FR | Requirement | Priority |
|----|-------------|----------|
| FR-3 | Per-environment secrets strategy | Must |
| FR-4 | Per-environment seeding policy + selection | Should |

---

## Domain Concepts

### Key Operations
| Operation | Description | Inputs | Outputs |
|-----------|-------------|--------|---------|
| Map secrets | Enumerate secret × tier needs | known secrets | matrix |
| Select seed set | Choose seed by environment | `ASPNETCORE_ENVIRONMENT` | applied seed |
| Guard demo data | Block `DevDataSeed` in Production | env + seed mode | refusal in prod |

---

## Story Summary

| Metric | Count |
|--------|-------|
| Total Stories | 4 |
| Must Have | 2 |
| Should Have | 2 |
| Could Have | 0 |

### Stories

| Story ID | Title | Priority | Status |
|----------|-------|----------|--------|
| 001-secrets-tier-matrix | Secrets × tier matrix (test vs live) | Must | Planned |
| 002-dev-env-secrets-template | `.env.dev-env.example` template | Must | Planned |
| 003-seeding-policy-and-selector | Per-environment seeding policy + selector | Should | Planned |
| 004-prod-demo-data-guard | Guard: no demo data in Production | Should | Planned |

---

## Dependencies

### Depends On
| Unit | Reason |
|------|--------|
| 001-config-tiers-and-compose | The dev-env tier must exist to scope secrets + seeding to it |

### Depended By
| Unit | Reason |
|------|--------|
| 003-promotion-readiness | Runbook references the secrets matrix + seeding policy |

### External Dependencies
| System | Purpose | Risk |
|--------|---------|------|
| Existing seed classes | Reused (ProductCatalogSeed/DevDataSeed) | Low |
| Secret-scanning (intent 018) | Strategy must stay compatible | Low |

---

## Technical Context

### Suggested Technology
Markdown matrix under `docs/environments/`, `.env.*.example` templates, `dotnet user-secrets` (local), env-var injection, the existing `--seed`/`--seed-dev` entrypoints.

### Integration Points
| Integration | Type | Protocol |
|-------------|------|----------|
| Seed selection | CLI / boot | `--seed` / `--seed-dev` |
| Secrets | env vars | ADR-006 |

---

## Constraints

- Reuse existing seed classes; no parallel seeder.
- dev-env = test/sandbox keys; live keys are prod-only.
- ADR-006: secrets never committed; templates carry placeholders only.
- Compatible with the pre-commit + Gitleaks scanning (intent 018).

---

## Success Criteria

### Functional
- [ ] Secrets matrix covers every secret across all three tiers.
- [ ] `.env.dev-env.example` exists with placeholders + test-mode defaults.
- [ ] Seed selection applies the correct set per environment; idempotent.
- [ ] `DevDataSeed` cannot run in Production (guard refuses).

### Non-Functional
- [ ] No real secrets committed; scanning stays green.

### Quality
- [ ] Single matrix + single policy doc (no duplication).

---

## Bolt Suggestions

| Bolt | Type | Stories | Objective |
|------|------|---------|-----------|
| 074-secrets-and-seeding | simple | 001–004 | Secrets matrix + dev-env template + seeding policy + prod guard |

---

## Notes

The Production demo-data guard is a small but high-value safety net: it makes "demo users/orders in prod" structurally impossible, not just discouraged.
