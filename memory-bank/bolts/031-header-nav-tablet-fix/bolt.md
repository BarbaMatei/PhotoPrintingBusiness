---
id: 031-header-nav-tablet-fix
unit: 004-responsive-ux-fixes
intent: 012-ui-polish
type: simple-construction-bolt
status: complete
priority: P3
stories:
  - 001-show-hamburger-at-md-breakpoint
created: 2026-05-25T00:00:00Z
started: 2026-05-25T00:00:00Z
completed: 2026-05-25T00:00:00Z
current_stage: null
stages_completed: [plan, implement, test]

requires_bolts: [004-angular-app-shell]
enables_bolts: []
requires_units: []
blocks: false

complexity:
  avg_complexity: 1
  avg_uncertainty: 1
  max_dependencies: 1
  testing_scope: 1
---

# Bolt: 031-header-nav-tablet-fix

## Overview

At the tablet breakpoint (768–1023px) users have NO navigation: the desktop nav is hidden (`@include lg` shows it), but the hamburger menu button is also hidden until `@include lg`. This bolt fixes the gap by revealing the hamburger at the `md` breakpoint (768px) so tablet users can access the menu.

## Objective

By the end of this bolt users on tablet devices (768–1023px viewport width) see the hamburger button in the header and can open the mobile navigation menu.

## Stories Included

- **001-show-hamburger-at-md-breakpoint**: Change the hamburger button's display breakpoint from `@include lg` to `@include md`; verify the mobile menu panel remains functional at this breakpoint; ensure desktop nav still appears at 1024px+ (Must)

## Bolt Type

`simple-construction-bolt` — single SCSS breakpoint change in the header component.

## Stage Plan

| Stage | Name | Output |
|-------|------|--------|
| 1 | Plan | `implementation-plan.md` — identify the exact SCSS rules for the hamburger and desktop nav; confirm breakpoint mixin definitions |
| 2 | Implement | Patch `header.component.scss` (or wherever the nav/hamburger breakpoints live); update both the hamburger visibility and any padding/layout adjustments for the md range |
| 3 | Test | Visual check at 768px, 900px, 1023px, 1024px, 1440px viewports using browser DevTools |

## Dependencies

- **Requires**: bolt `004-angular-app-shell` (header component must exist — ✅ complete)
- **Enables**: nothing

## Files Affected

```
src/PhotoPrint.UI/src/app/core/components/header/header.component.scss
src/PhotoPrint.UI/src/app/core/components/header/header.component.html  (possibly — confirm hamburger button selector)
```

## Key Technical Notes

### Breakpoint definitions to verify

Check `src/PhotoPrint.UI/src/styles/_breakpoints.scss` (or `_mixins.scss`) for:
- `@mixin md` → min-width: 768px
- `@mixin lg` → min-width: 1024px

### Expected fix pattern

```scss
// BEFORE (broken — hamburger only shown at lg+)
.hamburger {
  display: none;
  @include lg {
    display: none; // desktop hides it
  }
}
.desktop-nav {
  display: none;
  @include lg {
    display: flex;
  }
}

// AFTER (fixed — hamburger shown at md+, hidden when desktop nav appears)
.hamburger {
  display: flex; // visible at mobile
  @include lg {
    display: none; // hidden when desktop nav takes over
  }
}
.desktop-nav {
  display: none;
  @include lg {
    display: flex;
  }
}
```

### Caution

Confirm the mobile menu panel (drawer/overlay) renders correctly at 768–1023px. It may need `max-width` or positioning adjustments if it was only designed for narrow mobile screens.
