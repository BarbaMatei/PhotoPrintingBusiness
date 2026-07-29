---
id: 002-t2-site-url-architecture
unit: 001-research-tracks
intent: 034-eu-expansion-architecture-study
status: ready
priority: must
created: 2026-06-05T12:57:50Z
assigned_bolt: 077-research-tracks
implemented: false
---

# Story: 002-t2-site-url-architecture

## User Story

**As the** owner deciding EU expansion
**I want** a comparison of site/URL architectures under a single EU-wide brand, with SEO and deployment-topology consequences
**So that** I can choose how the site is structured across countries without surprises later

## Acceptance Criteria

- [ ] **Given** one EU-wide brand, **When** T2 reports, **Then** it compares multi-locale single site vs subdomains vs path prefixes (`/de/`), and documents per-country ccTLD sites as the **rejected-by-default** option with reasons
- [ ] **Given** SEO matters, **When** T2 reports, **Then** it covers hreflang, domain-authority splitting, and legal-page/content management per jurisdiction for each option
- [ ] **Given** intent 033 defines a Staging/Production triad, **When** T2 reports, **Then** each option states how it multiplies (or doesn't) the environment count, referencing intent 033 explicitly
- [ ] **Given** each option, **When** T2 reports, **Then** it states the option's interaction with the T3 i18n choice (build strategy implications)

## Technical Notes

- **Method (FR-1)**: parallel researchers (SEO, hosting topology, content/legal management) + verification of SEO claims against official Google/search guidance.
- Output: `docs/analysis/eu-expansion/track-2-site-architecture.md`.
- Brand strategy is fixed (one brand EU-wide) — evaluate options under that constraint; ccTLD-per-brand is documented as rejected, not re-litigated.
- Must reference `memory-bank/intents/033-environment-triad/` for the env-triad baseline.

## Dependencies

### Requires
- None (wave-parallel with T1, T3–T7)

### Enables
- 003-t3-frontend-i18n (T3 builds on T2's options) — informational, not a hard build dependency
- 001-synthesis-options-paper (Unit 2)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Legal pages differ per country but brand is shared | Document content-management approach per option |
| Path-prefix vs subdomain SEO is contested | Present both views with sources; give a reasoned lean |

## Out of Scope

- Implementing any routing/build changes.
- Fulfillment, tax, payments (other tracks).
