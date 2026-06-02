---
id: 005-guest-checkout-prompt
unit: 004-authentication-ui
intent: 002-authentication
status: complete
priority: must
created: 2026-05-20T12:58:00Z
assigned_bolt: 008-authentication-ui
implemented: true
---

# Story: 005-guest-checkout-prompt

## User Story

**As a** visitor who starts the checkout process without being logged in
**I want** to choose between continuing as a guest, logging in, or registering
**So that** I can complete my order with minimal friction

## Acceptance Criteria

- [ ] **Given** an unauthenticated user navigates to `/checkout`, **When** the `guestOrAuthGuard` determines no JWT and no guest token, **Then** the `GuestCheckoutPromptComponent` modal is shown before the checkout page loads
- [ ] **Given** the prompt modal, **When** rendered, **Then** shows three options: "Continuă ca oaspete", "Conectează-te", "Creează cont"
- [ ] **Given** the user selects "Conectează-te", **When** clicked, **Then** navigates to `/auth/login` with return URL `/checkout`
- [ ] **Given** the user selects "Creează cont", **When** clicked, **Then** navigates to `/auth/register`
- [ ] **Given** the user selects "Continuă ca oaspete", **When** clicked, **Then** shows the `GuestCheckoutFormComponent` inline in the modal
- [ ] **Given** the guest form with valid data, **When** submitted, **Then** calls `POST /api/auth/guest`, stores `{guestToken, firstName, lastName, email, phone}` in `localStorage`, closes the modal, and allows checkout to proceed
- [ ] **Given** the guest form, **When** phone is invalid (not `07xxxxxxxx`), **Then** shows inline error `"Număr de telefon invalid (ex: 0712345678)"`
- [ ] **Given** a guest session already in `localStorage` (returning guest), **When** `guestOrAuthGuard` checks, **Then** checkout loads directly (no modal)

## Technical Notes

- `GuestAuthService.createGuestSession(dto)` → `POST /api/auth/guest` → stores to `localStorage` under key `guestSession`
- `guestOrAuthGuard` (bolt 004) already checks `getGuestToken() !== null` — this story only adds the UI flow
- The modal can be a standalone `dialog`-element component with `@ViewChild` focus management for accessibility
- After storing, update `AuthService` observable state? No — guest is NOT authenticated; `guestInterceptor` reads from `localStorage` directly

## Dependencies

### Requires
- Bolt 007 (Unit 003-guest-sessions: `POST /api/auth/guest`)
- Bolt 004 (guestOrAuthGuard, guestInterceptor, routing)

### Enables
- Nothing within auth intent — enables full checkout flow in future intents

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| User closes modal without choosing | Stays on previous page (modal dismissed, no navigation) |
| Guest token in localStorage is expired (7 days) | Backend rejects; `errorInterceptor` gets 401 → clears guest token; prompt shows again |
| User has both expired guest token and valid JWT | JWT takes precedence; checkout proceeds |

## Out of Scope

- Post-order "create account" nudge (shown after order placed, not at checkout entry)
