---
id: 002-admin-policy-constant
unit: 001-access-hardening
intent: 029-decomposition-and-hardening
status: draft
priority: should
created: 2026-06-05T09:30:00Z
assigned_bolt: 063-access-hardening
implemented: false
---

# Story: 002-admin-policy-constant

## User Story

**As a** developer
**I want** a centralised `Policies.Admin` constant instead of string-literal roles
**So that** a typo (`"admin"`, `"Admin "`) can't silently open an admin endpoint

## Acceptance Criteria

- [ ] **Given** `public static class Policies { public const string Admin = "AdminRole"; }`, **When** registered via `AddAuthorization(p => p.RequireRole("Admin"))`, **Then** the policy is available
- [ ] **Given** the 6 controllers using `[Authorize(Roles="Admin")]`, **When** migrated, **Then** they use `[Authorize(Policy = Policies.Admin)]` and no role literal remains
- [ ] **Given** an anonymous request to `/api/admin/*`, **When** made, **Then** it returns 401 (not 403)
- [ ] **Given** the existing `DualAuth` policy, **When** centralised, **Then** it also lives in `Policies`

## Technical Notes

- Find/replace across controllers; integration test asserts 401 for anonymous admin access.

## Dependencies

### Requires
- None

### Enables
- 026/002 system-info endpoint reuses `Policies.Admin`; 031 refund endpoint reuses it

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| New admin endpoint added later | Uses the constant; no new literal |

## Out of Scope

- The global rate limit (previous story).
