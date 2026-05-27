---
stage: plan
bolt: 028-shared-components-audit
created: 2026-05-25T00:00:00Z
---

## Implementation Plan: 028-shared-components-audit

### Objective

Replace inline text-based loading states and inline empty-state divs across feature pages
with `<app-spinner>` and `<app-empty-state>`. Remove the now-redundant local CSS they leave behind.

### Confirmed Component APIs (from source)

**SpinnerComponent** (`app-spinner`):
- `size` signal input: `'sm' | 'md' | 'lg' | 'xl'` (default `'md'`)
- `label` signal input: `string` (default `'Se încarcă…'`)
- `showLabel` signal input: `boolean` (default `false`)
- Import: `src/app/shared/components/spinner/spinner.component.ts`

**EmptyStateComponent** (`app-empty-state`):
- `title` signal input: **required** `string`
- `icon` signal input: `string` (default `''`)
- `message` signal input: `string` (default `''`)
- `actionLabel` signal input: `string`
- `actionLink` signal input: `string` (renders `<a routerLink>`)
- `action` output: `EventEmitter<void>` (fires when button clicked, no actionLink set)
- `variant` signal input: `'default' | 'error' | 'compact'`
- Import: `src/app/shared/components/empty-state/empty-state.component.ts`

---

### Audit Results

#### ❌ OUT OF SCOPE — Skeleton loading UIs (do NOT replace with spinner)

These pages use CSS skeleton screens, which are superior UX to a spinner. Replacing
them would be a regression:

- `admin-page.html` — skeleton KPI cards (`.dash-skeleton`)
- `admin-products-page.html` — skeleton product cards (`.ap-skeleton`)
- `admin-orders-page.html` — skeleton table rows (`.ord-row-skeleton`)
- `pricing-page.ts` — skeleton pricing content (`.skeleton--title`, `.skeleton--body`)

#### ✅ IN SCOPE — Simple inline text spinners → `<app-spinner>`

| File | Line | Current markup | Replacement |
|------|------|----------------|-------------|
| `features/orders/pages/order-detail-page.ts` | 31 | `<p class="state-loading">Se încarcă comanda...</p>` + hardcoded `#6c757d` CSS | `<app-spinner label="Se încarcă comanda..." showLabel />` |
| `features/orders/pages/confirmation-page.ts` | 26 | `<div class="state-loading">Se verifică comanda...</div>` + hardcoded `#6c757d` CSS | `<app-spinner label="Se verifică comanda..." showLabel />` |
| `features/account/pages/profile/profile-page.ts` | 29 | `<p class="state-loading">Se încarcă...</p>` | `<app-spinner label="Se încarcă..." showLabel />` |
| `features/account/pages/saved-addresses/saved-addresses-page.ts` | 37 | `<p class="state-loading">Se încarcă adresele...</p>` | `<app-spinner label="Se încarcă adresele..." showLabel />` |
| `features/admin/pages/order-detail/admin-order-detail-page.html` | 9 | `<div class="loading">Se încarcă...</div>` | `<app-spinner showLabel />` (imports added to `.ts`) |

#### ✅ IN SCOPE — Inline empty-state divs → `<app-empty-state>`

| File | Current markup | Replacement |
|------|----------------|-------------|
| `features/account/pages/saved-addresses/saved-addresses-page.ts` | `<div class="empty-state"><p>Nu ai nicio adresă salvată.</p><button>Adaugă prima adresă</button></div>` | `<app-empty-state title="Nu ai nicio adresă salvată." actionLabel="Adaugă prima adresă" (action)="openAddForm()" />` |

Note: The catalog-page `<p class="catalog__empty">` is scoped inside a table-like
product grid context — leaving it out of scope as it's layout-bound and compact.

---

### Deliverables

1. `order-detail-page.ts` — add `SpinnerComponent` import; replace `<p class="state-loading">` with `<app-spinner>`; remove `.state-loading` CSS
2. `confirmation-page.ts` — add `SpinnerComponent` import; replace `<div class="state-loading">` with `<app-spinner>`; remove `.state-loading` CSS
3. `profile-page.ts` — add `SpinnerComponent` import; replace `<p class="state-loading">` with `<app-spinner>`; remove `.state-loading` CSS
4. `saved-addresses-page.ts` — add `SpinnerComponent` + `EmptyStateComponent` imports; replace both inline patterns; remove `.state-loading` and `.empty-state` CSS
5. `admin-order-detail-page.ts` + `.html` — add `SpinnerComponent` import to `.ts`; replace `<div class="loading">` in `.html`; remove `.loading` CSS from `.scss`

### Dependencies

- `SpinnerComponent` and `EmptyStateComponent` already exist — no new files needed
- Import path from `features/orders/pages/`: `../../../shared/components/spinner/spinner.component`
- Import path from `features/account/pages/saved-addresses/`: `../../../../shared/components/spinner/spinner.component`
- Import path from `features/admin/pages/order-detail/`: `../../../../shared/components/spinner/spinner.component`

### Acceptance Criteria

- [ ] All 5 inline `state-loading` / `loading` text blocks replaced with `<app-spinner>`
- [ ] Saved-addresses empty-state div replaced with `<app-empty-state>`
- [ ] All removed CSS classes verified absent from their respective style blocks
- [ ] `ng build --configuration development` passes with no errors
- [ ] Visual spot-check: spinner renders correctly on order-detail, profile, saved-addresses pages
