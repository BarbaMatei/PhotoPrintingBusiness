---
stage: implement
bolt: 041-secrets-management
created: 2026-05-25T14:50:00Z
---

## Implementation Walkthrough: 001-secrets-rotation-and-guardrails

### Summary

Finished and hardened the secrets posture that commit `50213b1` started. Added the missing `secrets/`
placeholder, a portable pre-commit secret guard, a gitleaks config + standalone CI scan, dev-key
generation scripts, a README (first-time setup + rotation runbook + leak note), and recorded the
history decision as ADR-006 (accept + rotate). One code change: a more helpful boot fail-fast message.

### Completed Work

- [x] `secrets/.gitkeep` — keeps the gitignored secrets dir present on clone (story 003).
- [x] `src/PhotoPrint.API/Extensions/AuthExtensions.cs` — the empty-key `InvalidOperationException` now names the fix (Local.json / user-secrets / generator script / README) (stories 001, 002).
- [x] `README.md` — **new**. First-time setup, env/secret matrix, secret-scanning install, JWT rotation runbook, "Historical secret leak" note (stories 001, 002, 005).
- [x] `hooks/pre-commit` — **new**. POSIX-sh staged-diff secret guard; installed via `git config core.hooksPath hooks`; allowlists `TestKeys.cs` (story 004).
- [x] `.gitleaks.toml` — **new**. Extends default rules; allowlists the test fixture + `docs/` + `memory-bank/` so legitimate PEM markers don't false-positive (story 004).
- [x] `.github/workflows/secret-scan.yml` — **new**. Standalone gitleaks workflow on push/PR; independent of the (not-yet-built) bolt-040 CI pipeline (story 004).
- [x] `scripts/gen-dev-keys.sh` + `scripts/gen-dev-keys.ps1` — **new**. RSA-2048 dev keypair generators writing into `secrets/` (story 001).
- [x] `memory-bank/bolts/041-secrets-management/adr-006-secret-history-accept-and-rotate.md` — **new** (story 005 decision).
- [x] `memory-bank/standards/decision-index.md` — ADR-006 entry; `total_decisions` 5 → 6.

### Already done by commit `50213b1` (not re-done)

- `appsettings.Development.json` → `PrivateKeyPem: ""`.
- `Program.cs` loads `appsettings.{Environment}.Local.json` last.
- `.gitignore` secrets block (`.env*`, `*.pem`, `*.pfx`, `*.key`, `secrets/`, `appsettings.*.Local.json`).
- `AuthExtensions` boot fail-fast on empty key (this bolt only improved its message).

### Key Decisions

- **Story 005 = accept + rotate** (ADR-006), per user direction. No history rewrite — the dev key is neutralized by rotation; force-push disruption avoided.
- **Pre-commit via `core.hooksPath`, not Husky/pre-commit-framework.** A tracked `hooks/` dir + a one-line `git config` install avoids adding an npm or Python toolchain. Works on the Windows dev box (Git for Windows ships sh) and in CI.
- **Standalone `secret-scan.yml`** rather than waiting on bolt 040's `ci.yml` — secret scanning shouldn't be gated on the build pipeline existing.
- **Allowlist is load-bearing**: `TestKeys.cs`, the analysis doc, and intent-018 requirements all contain the `BEGIN RSA PRIVATE KEY` marker legitimately; both the hook and gitleaks must permit them or the very first scan fails.

### Deviations from Plan

None. Story 005 resolved to "accept + rotate" as the user chose option 2.

### Scope honestly NOT covered in-repo

- **Live prod/staging key rotation** — an ops action requiring environment access I don't have. Delivered as the README runbook + generator script; the actual rotation is an operator checklist item.
- **Git history rewrite** — explicitly decided against (ADR-006). Not executed.

### Dependencies Added

None as code dependencies. CI uses the `gitleaks/gitleaks-action@v2` GitHub Action (no repo dependency). The pre-commit hook is plain `sh` + `git` + `grep`.

### Developer Notes

- API builds clean (0 errors); the only compiled change is the exception message string.
- Stage 3 will: confirm build, smoke-test the pre-commit hook (stage a fake key → commit blocked; stage the allowlisted fixture → allowed), and confirm the existing test suite is unaffected.
- `core.hooksPath` install is per-clone and not automatic — documented in README. A future bolt 040 could wire it into a `make setup` / bootstrap script.
