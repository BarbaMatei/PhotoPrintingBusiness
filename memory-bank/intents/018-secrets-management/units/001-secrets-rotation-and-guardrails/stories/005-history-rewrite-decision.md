---
id: 005-history-rewrite-decision
unit: 001-secrets-rotation-and-guardrails
intent: 018-secrets-management
status: draft
priority: must
created: 2026-05-25T10:25:00Z
assigned_bolt: 041-secrets-management
implemented: false
---

# Story: 005-history-rewrite-decision

## User Story

**As** the team lead
**I want** a clear, recorded decision on whether to rewrite git history to purge the leaked key
**So that** we don't leave the question hanging and so future contributors understand the rationale

## Acceptance Criteria

- [ ] A new `decision-index.md` entry recording: chosen option (rewrite or accept), date, who decided, rationale, impact on open PRs / forks.
- [ ] If "rewrite": a separate runbook lists exact `git filter-repo` command, freeze window, communication plan, force-push step, and fork-rebase guidance.
- [ ] If "accept": README has a one-paragraph "Historical secret leak" note acknowledging the rotation as the active mitigation.
- [ ] All open PRs are surveyed before any rewrite executes.

## Technical Notes

- `git filter-repo` command:
  ```
  git filter-repo --invert-paths --path src/PhotoPrint.API/appsettings.Development.json
  ```
- After rewrite: force-push `main` and all release branches; notify every contributor; cached PRs may need recreation.
- Accept-the-leak path is faster but the key remains forever in mirrors / forks — the rotation neutralises operational risk.

## Dependencies

### Requires
- 001 through 004 (rotation + guardrails must be in place before history is touched)

### Enables
- Intent close-out

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Repo is public | Accept-the-leak; rewrite cannot pull the secret from forks |
| Repo is private and team < 10 | Rewrite is feasible; coordinate carefully |

## Out of Scope

- Auditing Stripe / EuPlatesc / SendGrid keys for past leaks (separate ops audit).
