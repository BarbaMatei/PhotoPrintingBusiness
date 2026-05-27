---
id: 029-global-button-styles
unit: 003-global-ui-primitives
intent: 012-ui-polish
type: simple-construction-bolt
status: completed
priority: P2
stories:
  - 001-create-buttons-partial
  - 002-remove-local-btn-definitions
created: 2026-05-25T00:00:00Z
started: 2026-05-25T00:00:00Z
completed: 2026-05-25T00:00:00Z
current_stage: null
stages_completed: [plan, implement, test]

requires_bolts: [004-angular-app-shell]
enables_bolts: [030-breadcrumb-component]
requires_units: []
blocks: false

complexity:
  avg_complexity: 1
  avg_uncertainty: 1
  max_dependencies: 1
  testing_scope: 1
---

# Bolt: 029-global-button-styles

## Overview

Many feature component SCSS files define their own `.btn`, `.btn--primary`, `.btn--secondary` (and variants) locally, creating visual inconsistencies and duplication. This bolt centralises all button styles into a single `_buttons.scss` partial imported globally, then removes the local definitions from each component.

## Objective

By the end of this bolt every button variant across the application draws from a single source of truth in `src/PhotoPrint.UI/src/styles/_buttons.scss`, and no feature component SCSS file contains local `.btn` style definitions.

## Stories Included

- **001-create-buttons-partial**: Audit all feature SCSS files for `.btn` patterns; extract the canonical set of button variants into `_buttons.scss`; import it in `styles.scss` (Must)
- **002-remove-local-btn-definitions**: Remove duplicate `.btn`/`.btn--*` blocks from every component SCSS file that has them; verify no visual regressions (Must)

## Bolt Type

`simple-construction-bolt` — SCSS refactoring only; no TypeScript or HTML changes.

## Stage Plan

| Stage | Name | Output |
|-------|------|--------|
| 1 | Plan | `implementation-plan.md` — full inventory of `.btn` definitions across all SCSS files; canonical button variant table |
| 2 | Implement | Create `_buttons.scss` with all variants; add `@use`/`@forward` in `styles.scss`; remove local definitions |
| 3 | Test | Build check; visual spot-check all pages that use buttons |

## Dependencies

- **Requires**: bolt `004-angular-app-shell` (global styles infrastructure must exist — ✅ complete)
- **Enables**: `030-breadcrumb-component` (breadcrumb uses button-style back-link)

## Files Affected

```
src/PhotoPrint.UI/src/styles/_buttons.scss               ← NEW
src/PhotoPrint.UI/src/styles/styles.scss                 ← add import
src/PhotoPrint.UI/src/app/features/**/*.scss             ← remove local .btn definitions
```

## Key Technical Notes

### Expected button variants to consolidate

| Variant | Purpose |
|---------|---------|
| `.btn` | base button reset (display, cursor, border-radius, font) |
| `.btn--primary` | filled primary brand colour, white text |
| `.btn--secondary` | outlined or subtle variant |
| `.btn--danger` | red destructive action |
| `.btn--ghost` | transparent, for icon-only buttons |
| `.btn--sm` / `.btn--lg` | size modifiers |
| `.btn[disabled]` | disabled state with opacity |

### SCSS design token alignment

All colour values must reference design tokens (e.g. `$color-primary`, `$color-danger`) rather than hardcoded hex values. Cross-reference `src/PhotoPrint.UI/src/styles/_variables.scss` (or equivalent) when writing the canonical styles.

### Global vs encapsulated

Since Angular component SCSS is encapsulated, the `_buttons.scss` partial must be included in the **global** `styles.scss` (not a component-level `@use`) so that the classes are available without encapsulation piercing.
