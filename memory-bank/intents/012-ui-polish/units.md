---
intent: 012-ui-polish
phase: inception
status: complete
created: 2026-05-25T00:00:00Z
---

# Units: UI Polish

## Decomposition

| Unit | Type | Issues | Default Bolt Type |
|------|------|--------|-------------------|
| 001-auth-scss-refactor | frontend | Issue A | simple-construction-bolt |
| 002-shared-components-adoption | frontend | Issue B | simple-construction-bolt |
| 003-global-ui-primitives | frontend | Issues C + D | simple-construction-bolt |
| 004-responsive-ux-fixes | frontend | Issues E + F | simple-construction-bolt |

## Rationale

All six design-review issues are pure Angular/SCSS frontend work with no backend dependency. They are grouped into four units by concern:

- **001-auth-scss-refactor**: SCSS coupling anti-pattern specific to the auth feature. Isolated to two files and a new partial.
- **002-shared-components-adoption**: Cross-cutting audit — touches many feature pages but follows the same mechanical pattern (find inline pattern → replace with shared component).
- **003-global-ui-primitives**: Two new shared building blocks (button styles partial + breadcrumb component) that benefit the whole app. Breadcrumb depends on button styles being centralised first, so they share a unit.
- **004-responsive-ux-fixes**: Two UX consistency fixes (tablet nav gap + password checklist) that are independent of each other but both in the "responsive/UX" category.
