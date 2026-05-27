---
id: 005-environment-config
unit: 004-angular-app-shell
intent: 001-foundation-infrastructure
status: draft
priority: must
created: 2026-05-05T15:27:00Z
assigned_bolt: null
implemented: false
---

# Story: 005-environment-config

## User Story

**As a** developer
**I want** environment-specific configuration files for dev and production
**So that** API URLs, third-party keys, and feature flags are correct per environment without code changes

## Acceptance Criteria

- [ ] **Given** a development build (`ng serve`), **When** the app runs, **Then** `environment.ts` values are used (localhost API, test Stripe key, test Google client ID)
- [ ] **Given** a production build (`ng build --configuration=production`), **When** the app runs, **Then** `environment.prod.ts` values are used (production API, live keys)
- [ ] **Given** environment files, **When** examined, **Then** they export `apiUrl`, `stripePublishableKey`, `googleClientId`, and `production` boolean
- [ ] **Given** any service making API calls, **When** constructing the URL, **Then** it uses `environment.apiUrl` as the base

## Technical Notes

- `src/environments/environment.ts` (dev):
  ```typescript
  export const environment = {
    production: false,
    apiUrl: 'https://localhost:5001/api',
    stripePublishableKey: 'pk_test_xxx',
    googleClientId: 'xxx.apps.googleusercontent.com'
  };
  ```
- `src/environments/environment.prod.ts` (prod):
  ```typescript
  export const environment = {
    production: true,
    apiUrl: 'https://api.fototipar.ro/api',
    stripePublishableKey: 'pk_live_xxx',
    googleClientId: 'xxx.apps.googleusercontent.com'
  };
  ```
- Angular CLI `fileReplacements` in `angular.json` handles the swap

## Dependencies

### Requires
- None

### Enables
- All services that need API base URL or third-party keys

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Missing environment variable | TypeScript compilation error (strongly typed) |
| Staging environment needed | Add `environment.staging.ts` and matching angular.json config |

## Out of Scope

- Runtime environment injection (e.g., from server) — build-time replacement is sufficient for MVP
- Feature flags system
