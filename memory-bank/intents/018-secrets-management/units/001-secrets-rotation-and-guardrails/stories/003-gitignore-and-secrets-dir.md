---
id: 003-gitignore-and-secrets-dir
unit: 001-secrets-rotation-and-guardrails
intent: 018-secrets-management
status: draft
priority: must
created: 2026-05-25T10:25:00Z
assigned_bolt: 041-secrets-management
implemented: false
---

# Story: 003-gitignore-and-secrets-dir

## User Story

**As** a contributor
**I want** the repo to actively prevent accidental commits of common secret formats
**So that** I don't have to remember to gitignore every key file by hand

## Acceptance Criteria

- [ ] `.gitignore` (project root) appends:
  ```
  # Secrets — never commit
  appsettings.*.local.json
  secrets/
  *.pem
  *.pfx
  *.key
  .env
  .env.*
  !.env.example
  ```
- [ ] A `secrets/.gitkeep` placeholder exists so the directory survives clones but its contents stay untracked.
- [ ] Verified: `touch secrets/dev-private.pem && git status` shows nothing.

## Technical Notes

- The `!.env.example` rule lets the matrix file (intent 017) stay tracked while `.env` does not.

## Dependencies

### Requires
- 002-remove-key-from-repo

### Enables
- 004-precommit-and-ci-scan

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Existing committed `.env` file | Force-untrack: `git rm --cached .env` |
| Subproject `.gitignore` overrides | Unlikely; if found, the same block goes there too |

## Out of Scope

- Tracking what's in the secrets dir on shared dev machines (out of git's responsibility).
