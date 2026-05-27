---
intent: 012-ui-polish
phase: inception
status: complete
created: 2026-05-25T00:00:00Z
updated: 2026-05-25T00:00:00Z
---

# Requirements: UI Polish

## Intent Overview

Address structural and UX inconsistencies discovered during the May 2026 live web design review. Issues range from SCSS anti-patterns (P2) to minor responsive and UX gaps (P3). No backend changes are required — all work is Angular frontend.

## Business Goals

| Goal | Success Metric | Priority |
|------|----------------|----------|
| Eliminate SCSS coupling anti-patterns | `login-page.scss` no longer imports `register-page.scss`; shared partial exists | Must |
| Shared loading/empty components used everywhere | Zero pages with inline spinner or empty-state markup | Should |
| Consistent button styles | No feature SCSS file defines its own `.btn` classes | Should |
| Reusable breadcrumb navigation | `BreadcrumbComponent` used in admin detail pages | Could |
| No navigation dead zones | Users on 768–1023px viewport can access the menu | Should |
| Consistent password UX | Both register and profile change-password use the same checklist | Could |

---

## Functional Requirements

### FR-1: Shared Auth SCSS Partial
- **Description**: Shared auth layout styles (`.auth-layout`, `.auth-card`, `.auth-form`, `.form-field`, `.input-with-toggle`, `.toggle-btn`, `.field-error`) live in a dedicated `_auth-forms.scss` partial. Both `login-page.scss` and `register-page.scss` import from there. The local spinner CSS animation in `register-page.scss` is removed.
- **Acceptance Criteria**: `login-page.scss` contains no `@use` pointing to `register-page.scss`; `_auth-forms.scss` exists; both auth pages render correctly.
- **Priority**: Must
- **Related Stories**: US-A01, US-A02
- **Bolt**: 027

### FR-2: SpinnerComponent and EmptyStateComponent Adoption
- **Description**: Every feature page that displays a loading state or empty-results state uses `<app-spinner>` and `<app-empty-state>` from the shared component library.
- **Acceptance Criteria**: Grep for inline spinner/empty-state patterns returns zero matches in feature components; shared components import count > 1.
- **Priority**: Should
- **Related Stories**: US-B01–B04
- **Bolt**: 028

### FR-3: Global Button Styles
- **Description**: All button variants are defined once in `src/styles/_buttons.scss`, imported globally. No feature component SCSS file defines `.btn`, `.btn--primary`, or `.btn--secondary`.
- **Acceptance Criteria**: `_buttons.scss` exists; `styles.scss` imports it; grep for `.btn {` in feature SCSS returns zero matches.
- **Priority**: Should
- **Related Stories**: US-C01, US-C02
- **Bolt**: 029

### FR-4: Reusable Breadcrumb Component
- **Description**: A shared `BreadcrumbComponent` accepts `title` and `backLink` inputs and renders a consistent back-navigation header. Used in `admin-order-detail-page` at minimum.
- **Acceptance Criteria**: Component exists; admin order detail page uses `<app-breadcrumb>`; local breadcrumb SCSS removed from admin page.
- **Priority**: Could
- **Related Stories**: US-D01, US-D02
- **Bolt**: 030

### FR-5: Header Navigation Tablet Breakpoint
- **Description**: The hamburger button is visible at 768px+ (md breakpoint) so tablet users can access navigation. Desktop nav still appears at 1024px+.
- **Acceptance Criteria**: At 800px viewport width the hamburger button is visible and functional; desktop nav visible at 1024px.
- **Priority**: Should
- **Related Stories**: US-E01
- **Bolt**: 031

### FR-6: Password Requirements Checklist on Profile Page
- **Description**: The change-password form on the profile page shows the same always-visible password requirements checklist as the register page, powered by a shared `PasswordChecklistComponent`.
- **Acceptance Criteria**: Checklist visible immediately when change-password form is displayed; rules turn green/red as user types; both register and profile pages use `<app-password-checklist>`.
- **Priority**: Could
- **Related Stories**: US-F01, US-F02
- **Bolt**: 032

---

## Non-Functional Requirements

| Category | Requirement |
|----------|-------------|
| Architecture | No new services; no backend changes; purely frontend refactoring |
| Regression | All existing functionality must work after each bolt; ng build must pass |
| Angular version | Angular 21 conventions: standalone components, `signal()`, `@if`/`@for` |
| Design tokens | All colour values must use SCSS variables/tokens, not hardcoded hex |
| Accessibility | Existing a11y attributes must be preserved or improved |
