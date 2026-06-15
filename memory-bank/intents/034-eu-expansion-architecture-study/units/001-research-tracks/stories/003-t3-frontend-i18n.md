---
id: 003-t3-frontend-i18n
unit: 001-research-tracks
intent: 034-eu-expansion-architecture-study
status: ready
priority: must
created: 2026-06-05T12:57:50Z
assigned_bolt: 078-research-tracks
implemented: false
---

# Story: 003-t3-frontend-i18n

## User Story

**As the** team that will build EU readiness
**I want** a decision-ready comparison of Angular 21 i18n approaches
**So that** the frontend localization strategy fits the chosen site architecture and is realistic to maintain

## Acceptance Criteria

- [ ] **Given** Angular 21 specifically, **When** T3 reports, **Then** it compares built-in i18n (compile-time, one build per locale) vs runtime libraries (Transloco and peers) on maturity, bundle impact, developer workflow
- [ ] **Given** the T2 options, **When** T3 reports, **Then** it states how each i18n approach interacts with each site-architecture option (esp. one-build-per-locale vs path-prefix/subdomain)
- [ ] **Given** multi-currency is decided, **When** T3 reports, **Then** it covers currency/number/date formatting (Angular locale data); notes RTL is **not** required (EU scope)
- [ ] **Given** a claim about build/bundle behavior, **When** it appears, **Then** it is backed by Angular 21 docs or a cited throwaway experiment (archived/deleted, never merged)

## Technical Notes

- **Method (FR-1)**: researcher(s) on official Angular docs + library maturity; a ~20-line throwaway Angular 21 i18n build experiment is permitted to validate bundle/build claims, then archived/deleted.
- Output: `docs/analysis/eu-expansion/track-3-frontend-i18n.md`.
- Version sensitivity: Angular i18n changed across recent majors — reject claims about old Angular versions; verify against Angular 21.

## Dependencies

### Requires
- None (wave-parallel; references T2 conceptually but does not block on it)

### Enables
- 001-synthesis-options-paper (Unit 2)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Built-in i18n requires N builds | Quantify build/deploy cost per locale; tie to T2 env-multiplier |
| Runtime lib has SSR/hydration caveats | Document them explicitly |

## Out of Scope

- Backend localization (T4), actual translations, implementing i18n.
