---
id: 002-token-auth-and-refresh
unit: 001-sameday-api-client
intent: 015-sameday-shipping-integration
status: draft
priority: must
created: 2026-05-25T10:10:00Z
assigned_bolt: 036-sameday-api-client
implemented: false
---

# Story: 002-token-auth-and-refresh

## User Story

**As** the Sameday client
**I want** to authenticate transparently and refresh the token on 401
**So that** callers never have to know about Sameday auth lifecycle

## Acceptance Criteria

- [ ] On first call, `SamedayClient.GetTokenAsync` POSTs to `/api/authenticate` with `Username` + `Password`; response token cached in-memory for its `expiresAt` minus a 60 s safety window.
- [ ] On 401 from any Sameday call, the client clears the cached token, re-authenticates, and retries the original call exactly once.
- [ ] If the re-authenticated retry also returns 401, throw `SamedayAuthException` with the request URL but **not** the password.
- [ ] Token cache is per-process (singleton); no cross-instance sharing yet (intent 021 may move this to Redis).

## Technical Notes

- Use `SemaphoreSlim(1,1)` around token refresh to prevent thundering-herd on first-boot 401s.
- Token model: `record SamedayToken(string Value, DateTimeOffset ExpiresAt)`.
- Avoid logging the token; log only `expiresAt`.

## Dependencies

### Requires
- 001-sameday-settings-and-typed-client

### Enables
- All AWB / tracking calls

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Concurrent first calls | Single token fetch via semaphore |
| Sameday auth endpoint returns 500 | Polly retries authenticate per its policy; eventually `SamedayUnreachableException` |
| Clock skew on `expiresAt` | 60 s safety window absorbs |

## Out of Scope

- OAuth replacement of legacy token endpoint.
