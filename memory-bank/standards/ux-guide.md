# UX Guide

## Overview
FotoTipar uses a custom design system with SCSS, following mobile-first responsive design principles. All UI text is in Romanian. The visual identity uses green (brand) and orange (CTAs) with clean, card-based layouts.

## Design System / Component Library

**Approach**: Custom design system — no UI framework dependency at MVP
**Styling**: SCSS with BEM naming convention
**Layout**: CSS Grid + Flexbox
**Component encapsulation**: Angular `ViewEncapsulation.Emulated` (default)

## Design Tokens

### Colors

| Token | Value | Usage |
|-------|-------|-------|
| `$primary` | `#2E7D32` | Brand green — headers, links, primary buttons |
| `$primary-dark` | `#1B5E20` | Hover state for primary elements |
| `$accent` | `#FF6F00` | Orange — CTAs and highlights |
| `$text-primary` | `#212121` | Main body text |
| `$text-secondary` | `#616161` | Secondary/muted text |
| `$background` | `#FAFAFA` | Page background |
| `$surface` | `#FFFFFF` | Card/surface background |
| `$error` | `#D32F2F` | Error states and messages |
| `$success` | `#388E3C` | Success states |
| `$border` | `#E0E0E0` | Borders and dividers |

### Typography

| Token | Size | Usage |
|-------|------|-------|
| `$font-family` | `'Inter', 'Roboto', sans-serif` | All text |
| `$font-size-xs` | `0.75rem` (12px) | Captions, labels |
| `$font-size-sm` | `0.875rem` (14px) | Secondary text, table cells |
| `$font-size-base` | `1rem` (16px) | Body text |
| `$font-size-lg` | `1.25rem` (20px) | Subheadings |
| `$font-size-xl` | `1.5rem` (24px) | Section headings |
| `$font-size-2xl` | `2rem` (32px) | Page titles |

### Spacing Scale

| Token | Value |
|-------|-------|
| `$space-1` | `0.25rem` (4px) |
| `$space-2` | `0.5rem` (8px) |
| `$space-3` | `0.75rem` (12px) |
| `$space-4` | `1rem` (16px) |
| `$space-5` | `1.5rem` (24px) |
| `$space-6` | `2rem` (32px) |
| `$space-8` | `3rem` (48px) |

## Styling Approach

**Methodology**: BEM (Block__Element--Modifier)
**File organization**: One SCSS file per component (co-located)
**Global styles**: `styles.scss` (reset, typography, tokens)
**Shared files**: `_variables.scss`, `_mixins.scss`

### SCSS Rules
- Use variables for all colors, spacing, and breakpoints — no hardcoded values
- Max nesting depth: 3 levels
- No `!important` unless overriding third-party styles
- All interactive elements must have hover/focus/active states

## Responsive Design Strategy

**Approach**: Mobile-first with breakpoint media queries

### Breakpoints

| Name | Value | Target |
|------|-------|--------|
| `$breakpoint-sm` | `576px` | Small phones |
| `$breakpoint-md` | `768px` | Tablets |
| `$breakpoint-lg` | `992px` | Desktop |
| `$breakpoint-xl` | `1200px` | Large desktop |

### Layout Patterns
- Page container: `max-width: 1200px`, centered with horizontal padding
- Photo grid: 2 cols (mobile) → 3 cols (tablet) → 4-5 cols (desktop)
- Forms: full-width on mobile, max-width constrained on desktop
- Navigation: hamburger menu on mobile, horizontal nav on desktop

## Component Patterns

### Buttons
- Padding: `$space-2 $space-5`
- Border radius: `8px`
- Font weight: 600
- Transition: `all 0.2s ease`
- Variants: `--primary` (filled green), `--secondary` (outlined), `--disabled` (0.5 opacity)

### Cards
- White background with subtle border or shadow
- Border radius: `12px`
- Padding: `$space-4` to `$space-5`
- Hover: elevate shadow for interactive cards

### Forms
- Labels above inputs
- Input height: 44px minimum (touch-friendly)
- Error messages: red text below field, shown on blur + submit
- Focus: 2px primary-color outline
- Full-width on mobile, max-width on desktop

### Toast Notifications
- Position: fixed bottom-right
- Auto-dismiss: 5 seconds
- Color-coded: green (success), red (error), blue (info)
- Animation: slide-in

## Accessibility Standards

**Target**: WCAG AA compliance

- Color contrast: minimum 4.5:1 ratio
- Focus indicators visible on all interactive elements
- Touch targets: minimum 44×44px on mobile
- Minimum text size: 14px body text
- Don't rely on color alone — use icons/text alongside color
- Semantic HTML elements (`<nav>`, `<main>`, `<section>`, `<button>`)
- ARIA labels on interactive elements
- Keyboard navigation support
- Focus management after route changes

## Romanian UI Conventions

- All user-visible text in Romanian (no i18n library at MVP)
- Currency: `XX,XX RON` (comma as decimal separator) — use custom `CurrencyRon` pipe
- Dates: `dd.MM.yyyy` (Romanian locale)
- Button labels: "Adaugă în coș", "Trimite comanda", "Încarcă fotografii"
- Error messages: "Câmpul este obligatoriu", "Adresa de email nu este validă"
- Status labels: "Nouă", "În procesare", "Expediată", "Livrată"

## Decision Relationships
- Custom design system (vs Angular Material) keeps bundle size small and gives full control over brand identity
- BEM naming prevents CSS specificity wars and works well with Angular component encapsulation
- Mobile-first approach matches target audience (most Romanian e-commerce traffic is mobile)
