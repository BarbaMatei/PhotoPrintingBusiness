---
id: 030-breadcrumb-component
unit: 003-global-ui-primitives
intent: 012-ui-polish
type: simple-construction-bolt
status: completed
priority: P3
stories:
  - 001-breadcrumb-standalone-component
  - 002-wire-breadcrumb-admin-order-detail
created: 2026-05-25T00:00:00Z
started: 2026-05-25T00:00:00Z
completed: 2026-05-25T00:00:00Z
current_stage: null
stages_completed: [plan, implement, test]

requires_bolts: [022-admin-ui, 029-global-button-styles]
enables_bolts: []
requires_units: []
blocks: false

complexity:
  avg_complexity: 1
  avg_uncertainty: 1
  max_dependencies: 1
  testing_scope: 1
---

# Bolt: 030-breadcrumb-component

## Overview

`admin-order-detail-page.scss` defines breadcrumb styles locally — they are not reusable. This bolt creates a proper shared `BreadcrumbComponent` with `title` and `backLink` inputs, moves the styles there, and wires it into `admin-order-detail-page`. Other pages that use ad-hoc back-links can be migrated in the same pass.

## Objective

By the end of this bolt a reusable `<app-breadcrumb>` component exists in the shared library, `admin-order-detail-page` renders its breadcrumb using this component, and the local breadcrumb SCSS is removed from the admin page.

## Stories Included

- **001-breadcrumb-standalone-component**: Create `BreadcrumbComponent` as a standalone `OnPush` component; accepts `@Input() title: string` and `@Input() backLink: string`; renders a back-arrow link followed by the page title; styles in its own SCSS file (Must)
- **002-wire-breadcrumb-admin-order-detail**: Replace the inline breadcrumb markup in `admin-order-detail-page.html` with `<app-breadcrumb [title]="'Order #' + orderId" [backLink]="'/admin/orders'">`, add component import, remove local breadcrumb SCSS from `admin-order-detail-page.scss` (Must)

## Bolt Type

`simple-construction-bolt` — one new shared Angular component and targeted wiring to the admin order detail page.

## Stage Plan

| Stage | Name | Output |
|-------|------|--------|
| 1 | Plan | `implementation-plan.md` — component API, template structure, SCSS approach, list of all pages that could benefit from migration |
| 2 | Implement | `breadcrumb.component.ts` + `.html` + `.scss`; patch `admin-order-detail-page` |
| 3 | Test | Spec for `BreadcrumbComponent`; updated spec for `AdminOrderDetailPage` (breadcrumb rendered) |

## Dependencies

- **Requires**: bolt `022-admin-ui` (admin order detail page must exist)
- **Requires**: bolt `029-global-button-styles` (breadcrumb back-link uses `.btn--ghost` style)
- **Enables**: nothing

## Files Affected

```
src/PhotoPrint.UI/src/app/shared/components/breadcrumb/breadcrumb.component.ts    ← NEW
src/PhotoPrint.UI/src/app/shared/components/breadcrumb/breadcrumb.component.html  ← NEW
src/PhotoPrint.UI/src/app/shared/components/breadcrumb/breadcrumb.component.scss  ← NEW
src/PhotoPrint.UI/src/app/features/admin/pages/admin-order-detail/admin-order-detail-page.ts
src/PhotoPrint.UI/src/app/features/admin/pages/admin-order-detail/admin-order-detail-page.html
src/PhotoPrint.UI/src/app/features/admin/pages/admin-order-detail/admin-order-detail-page.scss
```

## Key Technical Notes

### BreadcrumbComponent API

```typescript
@Component({
  selector: 'app-breadcrumb',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink],
  template: `...`
})
export class BreadcrumbComponent {
  @Input({ required: true }) title!: string;
  @Input({ required: true }) backLink!: string;
  @Input() backLabel: string = 'Back';
}
```

### Template structure

```html
<nav class="breadcrumb" aria-label="breadcrumb">
  <a [routerLink]="backLink" class="breadcrumb__back btn btn--ghost">
    ← {{ backLabel }}
  </a>
  <span class="breadcrumb__title">{{ title }}</span>
</nav>
```

### SCSS

Breadcrumb SCSS should live in the component's own `.scss` file (view-encapsulated). No global styles needed beyond what `_buttons.scss` provides for the `.btn--ghost` back-link.
