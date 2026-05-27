---
id: 001-app-shell-layout
unit: 004-angular-app-shell
intent: 001-foundation-infrastructure
status: draft
priority: must
created: 2026-05-05T15:27:00Z
assigned_bolt: null
implemented: false
---

# Story: 001-app-shell-layout

## User Story

**As a** customer
**I want** a consistent page layout with header navigation and footer across all pages
**So that** I can always find navigation, cart, and account links regardless of which page I'm on

## Acceptance Criteria

- [ ] **Given** any route, **When** the page loads, **Then** a header is displayed with FotoTipar logo (links to home), nav links ("Acasă", "Tipărește"), cart icon with item count badge, and login/register links
- [ ] **Given** a logged-in user, **When** the header renders, **Then** login/register links are replaced with user avatar/name dropdown (with "Contul meu", "Comenzile mele", "Deconectare")
- [ ] **Given** an admin user, **When** the header renders, **Then** an "Admin" link appears in the navigation
- [ ] **Given** any route, **When** the page loads, **Then** a footer is displayed with links to privacy policy, terms & conditions, cookie policy, and copyright text
- [ ] **Given** a mobile viewport (< 768px), **When** the page loads, **Then** navigation collapses to a hamburger menu
- [ ] **Given** all shell components, **When** examined, **Then** they are standalone Angular 17+ components (no NgModules)

## Technical Notes

- `AppComponent` (standalone): contains `<app-header>`, `<router-outlet>`, `<app-footer>`
- `HeaderComponent` (standalone): uses `AuthService.isAuthenticated$` and `CartService.itemCount$` observables
- `FooterComponent` (standalone): static links to legal pages
- Use `@if` / `@for` control flow (Angular 17+ template syntax)
- Mobile hamburger: simple CSS/JS toggle, no external library
- Design tokens from UX guide: `$primary`, `$font-family`, `$breakpoint-md`

## Dependencies

### Requires
- None

### Enables
- 002-lazy-loaded-routes (routes render inside router-outlet)
- All feature components render within this shell

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Very long user name | Truncate with ellipsis in header dropdown |
| Cart count = 0 | Hide badge or show "0" (design decision) |
| No JavaScript | App won't work (SPA requirement — acceptable) |

## Out of Scope

- Cart count logic (stub BehaviorSubject with 0 for now)
- Auth state logic (stub with isAuthenticated$ = false for now)
- Actual page content (only shell layout)
