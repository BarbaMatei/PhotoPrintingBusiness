---
intent: 012-ui-polish
created: 2026-05-25T00:00:00Z
completed: 2026-05-25T00:00:00Z
status: complete
---

# Inception Log: 012-ui-polish

## Overview

**Intent**: Address structural and UX inconsistencies discovered during the May 2026 live web design review
**Type**: brown-field
**Trigger**: Web design review (session 2026-05-25) — live navigation, screenshots, and source analysis
**Created**: 2026-05-25T00:00:00Z

## Artifacts Created

| Artifact | Status | File |
|----------|--------|------|
| Requirements | ✅ | requirements.md |
| Units | ✅ | units.md |
| Bolt 027 | ✅ | memory-bank/bolts/027-auth-scss-shared-extraction/bolt.md |
| Bolt 028 | ✅ | memory-bank/bolts/028-shared-components-audit/bolt.md |
| Bolt 029 | ✅ | memory-bank/bolts/029-global-button-styles/bolt.md |
| Bolt 030 | ✅ | memory-bank/bolts/030-breadcrumb-component/bolt.md |
| Bolt 031 | ✅ | memory-bank/bolts/031-header-nav-tablet-fix/bolt.md |
| Bolt 032 | ✅ | memory-bank/bolts/032-password-requirements-profile/bolt.md |

## Summary

| Metric | Count |
|--------|-------|
| Functional Requirements | 6 |
| Non-Functional Requirements | 5 |
| Units | 4 |
| Stories (planned) | 11 |
| Bolts Planned | 6 |

## Units Breakdown

| Unit | Bolt(s) | Priority | Type |
|------|---------|----------|------|
| 001-auth-scss-refactor | 027 | P2 / Must | Frontend |
| 002-shared-components-adoption | 028 | P2 / Should | Frontend |
| 003-global-ui-primitives | 029, 030 | P2+P3 / Should | Frontend |
| 004-responsive-ux-fixes | 031, 032 | P3 / Should + Could | Frontend |

## Design Review Findings Summary

| Issue | Priority | Bolt | Status |
|-------|----------|------|--------|
| A: login imports register SCSS (antipattern) | P2 | 027 | ready |
| B: SpinnerComponent/EmptyStateComponent unused | P2 | 028 | ready |
| C: .btn styles duplicated across feature SCSS | P2 | 029 | ready |
| D: Breadcrumb defined inline in admin-order-detail | P3 | 030 | ready |
| E: No navigation at 768–1023px tablet breakpoint | P3 | 031 | ready |
| F: Password checklist missing on profile page | P3 | 032 | ready |

## Already Applied (not in bolts)

The following fixes were applied directly during the design review session:
- `register-page.scss` — password rule list items changed from red default to neutral with conditional `rule-err` class
- `order-history-page.ts` — Bootstrap hex colours replaced with SCSS design tokens
- `order-history-page.ts` — wired up `SpinnerComponent` and `EmptyStateComponent`
