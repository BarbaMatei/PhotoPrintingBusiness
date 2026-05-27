# Bolt 029 — Implementation Walkthrough

## Stage 2: Implement

### Files Created

**`src/PhotoPrint.UI/src/styles/_buttons.scss`** (new)
- Contains the full `.btn` system moved from `styles.scss`
- Adds new `--danger-ghost` variant (transparent bg, error-color border)
- Self-contained: imports `sass:color`, `styles/variables`, `styles/mixins`

### Files Modified

**`src/PhotoPrint.UI/src/styles.scss`**
- Added `@use 'styles/buttons' as *` at the top (line 4, alongside other `@use` rules)
- Removed the entire inline `.btn { … }` block (~96 lines) from the middle of the file
- Key fix: `@use` rules must be at the top of the file — placing it mid-file caused an esbuild SCSS compile error

**Local `.btn` blocks removed from 6 component files:**

| File | Lines removed |
|------|--------------|
| `auth/components/guest-checkout-form/guest-checkout-form.scss` | 51–75 (25 lines) |
| `upload/pages/catalog/catalog-page.scss` | 60–75 (16 lines) |
| `orders/pages/order-history-page.ts` | 157–172 (16 lines) |
| `orders/pages/confirmation-page.ts` | 188–198 (11 lines) |
| `account/pages/saved-addresses/saved-addresses-page.ts` | 326–367 (42 lines) |
| `account/pages/profile/profile-page.ts` | 310–354 (45 lines) |

**Intentionally untouched:**
- `shared/components/cookie-consent/cookie-consent.ts` — local `.btn` is intentional (green accept/reject colors)
- `styles/_auth-forms.scss` — `.spinner` is a button animation, unrelated to button styles

### Build Verification
- `ng build --configuration development` passed in 15.337 seconds, zero errors
