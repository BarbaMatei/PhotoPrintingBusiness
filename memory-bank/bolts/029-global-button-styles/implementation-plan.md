---
stage: plan
bolt: 029-global-button-styles
created: 2026-05-25T00:00:00Z
---

## Implementation Plan: 029-global-button-styles

### Objective

Extract the global `.btn` block already in `styles.scss` into a dedicated `_buttons.scss`
partial, add one missing variant, then remove all duplicate local `.btn` definitions from
component files — letting every button across the app draw from one source of truth.

### Discovery Findings

**Global button system already exists in `styles.scss` (lines 121–218)**:
The `.btn` block is fully token-based with all major variants already defined:
`.btn--primary`, `.btn--accent`, `.btn--secondary`, `.btn--ghost`, `.btn--danger`,
`.btn--sm`, `.btn--lg`, `.btn--xl`, `.btn--full`, `.btn--icon-only`

This bolt's work is therefore:
1. Extract this existing block into `_buttons.scss`
2. Replace it in `styles.scss` with `@use 'styles/buttons' as *`
3. Add the missing `&--danger-ghost` variant (used by `saved-addresses-page`)
4. Remove all local `.btn` overrides from component files

**Component `.btn` duplicate inventory**:

| File | Local `.btn` Lines | Issues |
|------|--------------------|--------|
| `guest-checkout-form.scss` | 51–75 | Uses tokens, subset of global — remove |
| `catalog-page.scss` | 60–75 | Hardcoded hex (`#1a6ef5`), non-token sizes — remove |
| `cookie-consent.ts` | 73–97 | Intentional green (`#16a34a`) for accept/reject semantics — **KEEP** |
| `order-history-page.ts` | 157–172 | Mixed tokens/hardcoded, subset — remove |
| `confirmation-page.ts` | 188–198 | Hardcoded hex (`#1a73e8`), non-token sizing — remove |
| `saved-addresses-page.ts` | 326–370 | Wrong green primary (`#16a34a`), hardcoded hex — remove; `--danger-ghost` variant needs adding to global |
| `profile-page.ts` | 310–355 | Wrong green primary (`#16a34a`), hardcoded hex — remove |

**Missing global variant**:
`saved-addresses-page.ts` uses `.btn--danger-ghost` (transparent red outline button for
delete actions). Add to `_buttons.scss`:

```scss
&--danger-ghost {
  background: transparent;
  color: $color-error;
  border: 1.5px solid rgba($color-error, 0.4);
  &:hover:not(:disabled) { background: $color-error-light; }
}
```

### Deliverables

1. `src/PhotoPrint.UI/src/styles/_buttons.scss` — new file containing the full `.btn` system
   extracted from `styles.scss` + new `.btn--danger-ghost` variant
2. `src/PhotoPrint.UI/src/styles.scss` — replace inline `.btn` block with `@use 'styles/buttons' as *`
3. Remove local `.btn` definitions from 6 files:
   - `guest-checkout-form.scss`
   - `catalog-page.scss`
   - `order-history-page.ts`
   - `confirmation-page.ts`
   - `saved-addresses-page.ts`
   - `profile-page.ts`

### Dependencies

- `src/PhotoPrint.UI/src/styles/_variables.scss` — SCSS tokens (already available)
- `src/PhotoPrint.UI/src/styles/_mixins.scss` — `btn-base` mixin (already available)
- `styles.scss` already has `@use 'sass:color'` at the top — `_buttons.scss` needs same

### Technical Approach

- `_buttons.scss` starts with `@use 'sass:color'` + `@use 'styles/variables' as *` + `@use 'styles/mixins' as *`
- `styles.scss` keeps `@use 'sass:color'` and other uses but replaces the `.btn { }` block with `@use 'styles/buttons' as *`
- Removing local `.btn` blocks in `.ts` files: locate the block between `.btn {` and its closing `}` and delete it entirely
- `catalog-page.scss` has the `.btn` block at line 60 — the rest of the file (catalog grid styles) must be preserved

### Acceptance Criteria

- [ ] `_buttons.scss` contains all button variants including `.btn--danger-ghost`
- [ ] `styles.scss` no longer has an inline `.btn { }` block
- [ ] Zero local `.btn` definitions remain in `guest-checkout-form.scss`, `catalog-page.scss`,
      `order-history-page.ts`, `confirmation-page.ts`, `saved-addresses-page.ts`, `profile-page.ts`
- [ ] `cookie-consent.ts` local `.btn` left intact (intentional green)
- [ ] `ng build --configuration development` passes
- [ ] Visual spot-check: profile save button, saved-addresses add/delete buttons, confirmation page CTA
