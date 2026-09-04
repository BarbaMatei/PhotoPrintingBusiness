---
intent: 025-security-dependency-hygiene
phase: inception
status: inception-complete
created: 2026-06-05T09:00:00Z
updated: 2026-06-05T09:00:00Z
source: docs/analysis/architect-review-2026-06-03.md (Group 1 — P01, P02, P03, P05)
priority_score: 22
---

# Requirements: Security & Dependency Hygiene

## Intent Overview

The architect review surfaced a cluster of dependency-tree and ops-correctness defects that are **pre-launch blockers**: a known CVE in the deployed observability pipeline, two `Stripe.net` versions silently co-existing in one solution, no central place to track package versions, no automated upgrade flow, and a `/metrics` IP allow-list that is broken on day-1 of production behind the reverse proxy. This intent groups the four proposals that touch the dependency manifests (`*.csproj`) and the boot pipeline (`Program.cs`) so they ship FIRST and in strict sequence (they overlap the same files). None change customer-facing behaviour; all reduce audit and "works-in-tests / breaks-in-prod" risk.

## Business Goals

| Goal | Success Metric | Priority |
|------|----------------|----------|
| Remove known CVEs from the shipped dependency tree | `dotnet list package --vulnerable` returns clean | Must |
| Single source of truth for "what we depend on" | Every package version declared once in `Directory.Packages.props` | Must |
| Sustainable, low-noise dependency upkeep | Grouped upgrade PRs land automatically; CVE alerts within 24h | Should |
| `/metrics` allow-list enforces correctly in production | The scrape gate keys on the connecting peer, and `X-Forwarded-For` cannot open it | Must |

---

## Functional Requirements

### FR-1 (P01): Patch the OpenTelemetry CVE (GHSA-4625-4j76-fww9)
- **Description**: Bump the entire `OpenTelemetry.*` package suite in `PhotoPrint.API.csproj` from `1.11.x` to the matching `1.15.x` line, in lockstep (version skew across OTel sub-packages causes init failures). Accept a pinned pre-release for `Instrumentation.EntityFrameworkCore` / `Prometheus.AspNetCore` only if no stable peer exists.
- **Acceptance Criteria**:
  - All six `OpenTelemetry.*` references updated; `dotnet restore && dotnet build && dotnet test` green.
  - `dotnet list package --vulnerable` reports zero vulnerable packages.
  - `MetricsEndpointIntegrationTests` (bolt 044) still passes; `/metrics` and EF span emission smoke-tested.
- **Priority**: Must
- **Related Stories**: TBD

### FR-2 (P02): Unify Stripe.net version + adopt Central Package Management
- **Description**: Introduce `Directory.Packages.props` (+ `Directory.Build.props` with `<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>`) at the solution root, strip `Version=` from every `<PackageReference>`, and pin a single `Stripe.net` version so the silent 46.3.0/47.0.0 split across API vs Tests can no longer happen.
- **Acceptance Criteria**:
  - A `<PackageVersion>` exists for every package referenced by `PhotoPrint.API.csproj` and `PhotoPrint.Tests.csproj`; no per-project `Version=` attributes remain.
  - `CentralPackageTransitivePinningEnabled` is on; `dotnet restore` fails (not warns) if a transitive override re-introduces a second version.
  - Both projects resolve the **same** `Stripe.net` version; the full webhook/payment integration suite (`PaymentControllerIntegrationTests`) passes against it.
- **Priority**: Must
- **Related Stories**: TBD

### FR-3 (P03): Add Renovate config with grouped, scheduled upgrade PRs
- **Description**: Add `.github/renovate.json` configured conservatively — monthly minor/patch, quarterly major roll-up, grouped by ecosystem (OTel suite, EF Core + Npgsql, Angular), with a dependency dashboard and security-alert labelling.
- **Acceptance Criteria**:
  - Package groups defined for `^OpenTelemetry\.`, `^Microsoft\.EntityFrameworkCore`/`^Npgsql`, `^@angular/`.
  - Schedule pins routine updates to the first of the month; majors to Jan/Apr/Jul/Oct.
  - `dependencyDashboard: true`; `vulnerabilityAlerts` labelled `security` with `automerge: false`.
  - Renovate GitHub App installed (one-time repo-admin action — tracked as an Open Question, not code).
- **Priority**: Should
- **Related Stories**: TBD

### FR-4 (P05): Register `ForwardedHeadersMiddleware` so the `/metrics` allow-list works behind Caddy
- **Description**: Register forwarded headers before `UseCorrelationId`, trusting only the reverse proxy's own address, so every request's client identity is the real client — **except on the metrics scrape listener**, which is excluded so `MetricsEndpointIpAllowListMiddleware` keeps judging the address the request actually came from. ADR-018 closed the proxied-`/metrics` hole topologically and rejects trusting `X-Forwarded-For` on that gate.
- **Acceptance Criteria**:
  - `XForwardedFor | XForwardedProto` enabled; `KnownNetworks`/`KnownProxies` cleared then anchored to the proxy's fixed address (no open trust, and never the container subnet).
  - `MetricsEndpointIntegrationTests` gains a case proving `X-Forwarded-For` **cannot** open the scrape gate.
  - DEPLOYMENT.md §14 updated with the proxy-trust note.
- **Priority**: Must
- **Related Stories**: TBD

---

## Non-Functional Requirements

### Security
| Requirement | Standard | Notes |
|-------------|----------|-------|
| No known vulnerable packages | Restore-time NuGet audit over direct **and** transitive packages at advisory level `low` (`NuGetAuditMode=all`, `NuGetAuditLevel=low`) | Enforced in CI: the restore step runs `-p:FailOnAudit=true`, which promotes NU1901–NU1905 to errors. `dotnet list package --vulnerable` is not the gate — it exits 0 on findings |
| No IP spoofing via forwarded headers | The proxy's own address only, never the container subnet; a range wider than a single address pair is refused at boot | Any container on a trusted range could name the client (P05 risk) |
| Single resolved version per package | Central Package Management | Eliminates silent multi-version load |

### Reliability
| Requirement | Metric | Target |
|-------------|--------|--------|
| Boot pipeline regressions caught | Integration tests | `/metrics` + payment suites green post-change |

---

## Constraints

### Technical Constraints
- **Ship order is sequential**: P01 → P02 → P03 → P05 (same `*.csproj` / `Program.cs` files; parallel edits = merge hell).
- P02 is a prerequisite for P03 — Renovate needs `Directory.Packages.props` to group meaningfully.
- Stripe.net 46→47 may carry breaking API changes (event-type renames, deserialization). Full webhook integration pass required before merge; have a rollback PR ready.
- OTel 1.11→1.15 `Prometheus.AspNetCore` / `EntityFrameworkCore` instrumentation are on a `-beta` track; API surface may move.

### Business Constraints
- Pre-launch must-have group — must ship before the first real-money transaction (P01 audit blocker; P05 day-1 ops blocker).

---

## Assumptions

| Assumption | Risk if Invalid | Mitigation |
|------------|-----------------|------------|
| A stable `1.15.x` peer exists for most OTel packages | Forced to ship more beta packages | Pin the beta with a documented note; track stable upgrade in Renovate |
| Tests genuinely need Stripe.net 47 (the resolved version) | Pinning to 46 breaks a test | Verify against test code before pinning; bump API to match |
| The reverse-proxy network range is known and stable | Allow-list trusts wrong CIDR | Anchor to the actual Caddyfile upstream; document in DEPLOYMENT.md |

---

## Open Questions

| Question | Owner | Due Date | Resolution |
|----------|-------|----------|------------|
| Q1: Who installs the Renovate GitHub App (repo-admin action)? | Maintainer | 2026-06-12 | Pending |
| Q2: Pin Stripe.net to 46 or 47? | Dev | 2026-06-12 | Recommend 47 (the already-resolved Tests version) after webhook-suite verification |
| Q3: Exact reverse-proxy CIDR for `KnownNetworks`? | Ops | 2026-06-12 | No CIDR: trust Caddy's own address alone (`172.28.0.2`, pinned in docker-compose.prod.yml). The bridge CIDR is refused at boot — the API's ports are exposed on that network, so any container in the range could forge a client IP. See DEPLOYMENT.md §16.2. |
