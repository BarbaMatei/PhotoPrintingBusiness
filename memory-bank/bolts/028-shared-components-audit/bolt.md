---
id: 028-shared-components-audit
unit: 002-shared-components-adoption
intent: 012-ui-polish
type: simple-construction-bolt
status: complete
priority: P2
stories:
  - 001-audit-pages-for-inline-loading
  - 002-replace-inline-patterns-admin
  - 003-replace-inline-patterns-catalog
  - 004-replace-inline-patterns-profile-cart
created: 2026-05-25T00:00:00Z
started: 2026-05-25T00:00:00Z
completed: 2026-05-25T00:00:00Z
current_stage: done
stages_completed:
  - name: plan
    completed: 2026-05-25T00:00:00Z
    artifact: implementation-plan.md
  - name: implement
    completed: 2026-05-25T00:00:00Z
    artifact: implementation-walkthrough.md
  - name: test
    completed: 2026-05-25T00:00:00Z
    artifact: test-walkthrough.md

requires_bolts: [004-angular-app-shell, 008-authentication-ui]
enables_bolts: []
requires_units: []
blocks: false

complexity:
  avg_complexity: 2
  avg_uncertainty: 2
  max_dependencies: 2
  testing_scope: 2
---

# Bolt: 028-shared-components-audit

## Overview

`SpinnerComponent` and `EmptyStateComponent` are shared components that exist in `src/app/shared/components/` but are used in zero feature pages (the recently fixed `order-history-page` is the only consumer). This bolt audits every feature page for inline loading/empty-state patterns and replaces them with the shared components.

## Objective

By the end of this bolt every feature page that shows a loading state or an empty-state message uses `<app-spinner>` and `<app-empty-state>` from the shared library instead of inline markup or local CSS animations.

## Stories Included

- **001-audit-pages-for-inline-loading**: Grep all feature page `.ts`/`.html` files for local `isLoading`, `loading`, spinner divs, and empty-state messages; produce an audit list with file paths and patterns (Must)
- **002-replace-inline-patterns-admin**: Replace inline loading/empty patterns in admin pages (`admin-dashboard`, `admin-orders`, `admin-order-detail`, `admin-products`) with `<app-spinner>` and `<app-empty-state>`; add imports to each component (Must)
- **003-replace-inline-patterns-catalog**: Replace inline patterns in product catalog pages (`product-catalog-page`, `product-detail-page`) (Must)
- **004-replace-inline-patterns-profile-cart**: Replace inline patterns in profile/account pages and the cart page (Must)

## Bolt Type

`simple-construction-bolt` — template and component class changes only; no new files, no backend changes.

## Stage Plan

| Stage | Name | Output |
|-------|------|--------|
| 1 | Plan | `implementation-plan.md` — full audit table of every affected file, the inline pattern found, and the replacement approach |
| 2 | Implement | Patch each component template to use `<app-spinner [isLoading]="...">` and `<app-empty-state>`; add component imports; remove redundant local CSS |
| 3 | Test | Build check; spec updates for any component whose template changed significantly |

## Dependencies

- **Requires**: bolt `004-angular-app-shell` (shared components must exist — ✅ complete)
- **Enables**: nothing (quality improvement)

## Files Affected (indicative — confirm via audit)

```
src/PhotoPrint.UI/src/app/features/admin/pages/**/*.ts
src/PhotoPrint.UI/src/app/features/admin/pages/**/*.html
src/PhotoPrint.UI/src/app/features/products/pages/**/*.ts
src/PhotoPrint.UI/src/app/features/products/pages/**/*.html
src/PhotoPrint.UI/src/app/features/account/pages/**/*.ts
src/PhotoPrint.UI/src/app/features/account/pages/**/*.html
src/PhotoPrint.UI/src/app/features/cart/pages/**/*.ts
src/PhotoPrint.UI/src/app/features/cart/pages/**/*.html
```

## Key Technical Notes

### SpinnerComponent API

```typescript
// Assumed API — confirm from source before implementing
@Input() isLoading: boolean = false;
// Usage: <app-spinner [isLoading]="isLoading" />
```

### EmptyStateComponent API

```typescript
// Assumed API — confirm from source before implementing
@Input() message: string = 'No items found';
@Input() icon?: string;
// Usage: <app-empty-state message="No orders yet" />
```

### Import pattern (standalone components)

```typescript
import { SpinnerComponent } from '@shared/components/spinner/spinner.component';
import { EmptyStateComponent } from '@shared/components/empty-state/empty-state.component';

@Component({
  imports: [SpinnerComponent, EmptyStateComponent, ...],
})
```

### What to look for in the audit

- `*ngIf="isLoading"` / `@if (isLoading)` blocks wrapping a spinner div
- `*ngIf="!isLoading && items.length === 0"` empty state divs
- Local CSS classes: `.spinner`, `.loading-overlay`, `.empty-state`, `.no-data`
- Inline `border-radius: 50%; animation: spin` CSS in component SCSS files
