---
stage: test
bolt: 041-secrets-management
created: 2026-05-25T15:05:00Z
---

## Test Report: 001-secrets-rotation-and-guardrails

### Summary

- **Build**: API 0 errors.
- **Existing suite**: 449/449 passed (the only code change — the fail-fast message — broke nothing).
- **Pre-commit hook**: verified blocking + allowlist behaviour.
- **gitignore idiom**: verified placeholder tracked, real secrets ignored.
- **Two real bugs found and fixed during this stage** (see below).

### Verifications Run

| Check | Expected | Result |
|-------|----------|--------|
| `dotnet test` full suite | all pass | ✅ 449/449 |
| Hook: fake key at normal path staged | commit blocked (exit 1) | ✅ blocked |
| Hook: marker line in allowlisted `TestKeys.cs` | allowed (exit 0) | ✅ allowed |
| `git check-ignore secrets/.gitkeep` | trackable | ✅ not ignored |
| `git check-ignore secrets/dev-jwt-private.pem` | ignored | ✅ ignored |

### Bugs Found & Fixed (the point of the test stage)

1. **Pre-commit hook never matched anything.** `grep -Eq "$PATTERNS"` failed because the pattern begins with `-----`, which grep parsed as command-line flags → grep errored → no match → secret slipped through (hook exited 0 on a staged fake key). **Fix**: `grep -Eq -e "$PATTERNS"` (the `-e` flag marks the next arg as the pattern). Re-test: fake key now correctly blocked.

2. **`secrets/.gitkeep` was itself ignored.** `.gitignore` had `secrets/`, which ignores the whole directory including the placeholder — so the dir would not survive a clone and the placeholder was untrackable. **Fix**: changed to `secrets/*` + `!secrets/.gitkeep` (the standard idiom). Re-test: `.gitkeep` is trackable; a real `secrets/*.pem` is still ignored.

### Acceptance Criteria Validation

| Story | Criteria | Status |
|-------|----------|--------|
| 001 | Key generator + improved fail-fast message + rotation runbook | ✅ scripts + AuthExtensions message + README runbook (live prod rotation = ops, documented) |
| 002 | Key absent from file; boot fails fast; setup docs | ✅ (`PrivateKeyPem: ""` from 50213b1) + improved message + README first-time setup |
| 003 | gitignore secrets + `secrets/.gitkeep` placeholder | ✅ gitignore idiom fixed; placeholder now tracked |
| 004 | Pre-commit blocks secrets; CI gitleaks; allowlist for legit strings | ✅ hook block + allowlist verified; `secret-scan.yml` + `.gitleaks.toml` in place (CI run happens on push) |
| 005 | History decision recorded | ✅ ADR-006 (accept + rotate) + decision-index + README note |

### Not Verified Here (documented, out of repo)

- **CI gitleaks job execution** — runs on GitHub push/PR; cannot be exercised locally without the Actions runner. The config + allowlist are in place; first real run happens on push.
- **Live prod/staging key rotation** — operator action; runbook provided.

### Notes

- `git config core.hooksPath hooks` was set in this working copy during testing (the documented install step), so the guard is active here.
- Recommend a follow-up (likely bolt 040) to fold `core.hooksPath` into a bootstrap/setup step so new clones get the guard without a manual command.
