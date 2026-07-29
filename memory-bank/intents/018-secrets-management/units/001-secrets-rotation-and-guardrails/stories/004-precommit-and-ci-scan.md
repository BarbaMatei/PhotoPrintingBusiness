---
id: 004-precommit-and-ci-scan
unit: 001-secrets-rotation-and-guardrails
intent: 018-secrets-management
status: complete
priority: must
created: 2026-05-25T10:25:00Z
assigned_bolt: 041-secrets-management
implemented: true
implemented_at: 2026-05-25T15:10:00Z
---

# Story: 004-precommit-and-ci-scan

## User Story

**As** the team
**I want** secret-shaped strings blocked at commit time AND in CI
**So that** an absent-minded paste cannot reach `main`

## Acceptance Criteria

- [ ] Pre-commit hook installed via Husky (in `src/PhotoPrint.UI/package.json` `prepare` script) OR `pre-commit` framework (`.pre-commit-config.yaml`). Both work for the team.
- [ ] Hook rejects commits containing any of:
  - `-----BEGIN [A-Z ]+PRIVATE KEY-----`
  - `sk_live_[A-Za-z0-9]{16,}` (Stripe)
  - `pk_live_[A-Za-z0-9]{16,}` (Stripe)
  - `whsec_[A-Za-z0-9]{16,}` (Stripe webhook)
  - `ghp_[A-Za-z0-9]{20,}` (GitHub PAT)
- [ ] CI workflow `secrets` job runs Gitleaks on the diff against `main`; failure blocks merge.
- [ ] README documents the install (`npm install` or `pre-commit install`).

## Technical Notes

```yaml
# .github/workflows/ci.yml (new job)
secrets:
  runs-on: ubuntu-latest
  steps:
    - uses: actions/checkout@v4
      with: { fetch-depth: 0 }
    - uses: gitleaks/gitleaks-action@v2
      env: { GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }} }
```

## Dependencies

### Requires
- 003-gitignore-and-secrets-dir

### Enables
- 005-history-rewrite-decision

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| False positive (e.g. test fixture) | Allowlist via `.gitleaks.toml` with documented rationale |
| Local hook bypass `--no-verify` | CI catches it; document policy that CI is the source of truth |

## Out of Scope

- Custom rules for proprietary secret formats.
