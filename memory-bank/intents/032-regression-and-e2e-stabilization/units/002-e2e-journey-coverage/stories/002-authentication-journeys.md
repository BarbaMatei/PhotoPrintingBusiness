---
id: 002-authentication-journeys
unit: 002-e2e-journey-coverage
intent: 032-regression-and-e2e-stabilization
status: draft
priority: must
created: 2026-06-05T11:35:00Z
assigned_bolt: 071-e2e-journey-coverage
implemented: false
---

# Story: 002-authentication-journeys

## User Story

**As a** maintainer
**I want** end-to-end specs covering all three authentication paths
**So that** registration, social login, and guest-claim are proven to work together

## Acceptance Criteria

- [ ] **Given** a new visitor, **When** they register → hit the email-verification gate → verify → log in → log out, **Then** each step behaves per spec (unverified login blocked; verified login succeeds; logout clears the session)
- [ ] **Given** Google sign-in, **When** a user signs in with a (test-mode mocked) `id_token`, **Then** an account is created/linked and the user is authenticated — without any real external Google call in CI
- [ ] **Given** a guest who placed an order, **When** they register or log in, **Then** the **guest session is claimed** and the prior guest order becomes visible in their order history
- [ ] **Given** all auth specs, **When** they run in CI, **Then** they pass deterministically with no external network dependency

## Technical Notes

- Mock the server-side Google `id_token` verification in test mode (Q4) so CI has no external dependency.
- Guest-claim reuses the guest fixture (unit 001) then the registered-user fixture; assert the claimed order via the orders API/UI.

## Dependencies

### Requires
- 002-builder-backed-fixtures (unit 001)

### Enables
- 003-uploads-cart-and-merge (shares guest→user transition)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Login before email verification | Blocked with the correct message |
| Account lockout after repeated failures | Lockout enforced (matches auth-core behaviour) |
| Google account email matches existing local account | Auto-linked, not duplicated |

## Out of Scope

- Password-reset email rendering details (covered by integration tests; e2e asserts the page flow only).
