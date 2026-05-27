---
name: ui-styling
description: UI styling, SCSS conventions, responsive design, and visual patterns for FotoTipar. Use this skill when implementing component styles, layouts, responsive breakpoints, or visual design elements for the photo printing website.
---

## Tech Stack

- **SCSS** (Sass) for component and global styles
- **CSS Grid + Flexbox** for layouts
- **Angular component encapsulation** (`ViewEncapsulation.Emulated` default)
- Custom design system — no UI framework dependency at MVP

## Design Tokens

### Colors

```scss
$primary: #2E7D32;        // Green — brand color
$primary-dark: #1B5E20;
$accent: #FF6F00;         // Orange — CTAs and highlights
$text-primary: #212121;
$text-secondary: #616161;
$background: #FAFAFA;
$surface: #FFFFFF;
$error: #D32F2F;
$success: #388E3C;
$border: #E0E0E0;
```

### Typography

```scss
$font-family: 'Inter', 'Roboto', sans-serif;
$font-size-xs: 0.75rem;   // 12px
$font-size-sm: 0.875rem;  // 14px
$font-size-base: 1rem;    // 16px
$font-size-lg: 1.25rem;   // 20px
$font-size-xl: 1.5rem;    // 24px
$font-size-2xl: 2rem;     // 32px
```

### Spacing Scale

```scss
$space-1: 0.25rem;   // 4px
$space-2: 0.5rem;    // 8px
$space-3: 0.75rem;   // 12px
$space-4: 1rem;      // 16px
$space-5: 1.5rem;    // 24px
$space-6: 2rem;      // 32px
$space-8: 3rem;      // 48px
```

### Breakpoints

```scss
$breakpoint-sm: 576px;    // Small phones
$breakpoint-md: 768px;    // Tablets
$breakpoint-lg: 992px;    // Desktop
$breakpoint-xl: 1200px;   // Large desktop
```

## Layout Patterns

### Page Layout

```scss
.page-container {
  max-width: 1200px;
  margin: 0 auto;
  padding: 0 $space-4;
}
```

### Mobile-First Responsive

```scss
// Base: mobile
.grid {
  display: grid;
  grid-template-columns: 1fr;
  gap: $space-4;

  @media (min-width: $breakpoint-md) {
    grid-template-columns: repeat(2, 1fr);
  }

  @media (min-width: $breakpoint-lg) {
    grid-template-columns: repeat(3, 1fr);
  }
}
```

### Photo Grid (upload/cart)

- Mobile: 2 columns
- Tablet: 3 columns
- Desktop: 4-5 columns
- Thumbnails: square aspect ratio with `object-fit: cover`
- Hover: subtle scale + shadow

## Component Patterns

### Buttons

```scss
.btn {
  padding: $space-2 $space-5;
  border-radius: 8px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s ease;

  &--primary {
    background: $primary;
    color: white;
    &:hover { background: $primary-dark; }
  }

  &--secondary {
    background: transparent;
    border: 2px solid $primary;
    color: $primary;
  }

  &--disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }
}
```

### Cards

- White background with subtle border or shadow
- `border-radius: 12px`
- Padding: `$space-4` to `$space-5`
- Hover: elevate shadow for interactive cards

### Forms

- Labels above inputs
- Input height: 44px minimum (touch-friendly)
- Error messages: red text below field, shown on blur + submit
- Focus: 2px primary-color outline
- Full-width inputs on mobile, max-width on desktop

### Toast Notifications

- Fixed bottom-right position
- Auto-dismiss after 5 seconds
- Color-coded: green (success), red (error), blue (info)
- Slide-in animation

## SCSS Conventions

- BEM naming: `.block__element--modifier`
- One SCSS file per component (co-located)
- Global styles only in `styles.scss` (reset, typography, tokens)
- Variables and mixins in `_variables.scss` and `_mixins.scss`
- No `!important` unless overriding third-party styles
- Use nesting max 3 levels deep
- Use variables for all colors, spacing, and breakpoints — no hardcoded values

## Accessibility

- Color contrast: minimum 4.5:1 ratio (WCAG AA)
- Focus indicators visible on all interactive elements
- Touch targets: minimum 44×44px on mobile
- Text sizes: minimum 14px body text
- Don't rely on color alone to convey information (use icons/text too)

## Romanian UI Text

- All user-visible text in Romanian
- Button labels: "Adaugă în coș", "Trimite comanda", "Încarcă fotografii"
- Error messages: "Câmpul este obligatoriu", "Adresa de email nu este validă"
- Status labels: "Nouă", "În procesare", "Expediată", "Livrată"
