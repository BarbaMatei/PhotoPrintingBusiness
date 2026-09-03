---
stage: plan
bolt: 041-secrets-management
created: 2026-05-25T14:30:00Z
---

## Implementation Plan: 001-secrets-rotation-and-guardrails

### Pre-flight: what commit `50213b1` already did

A prior commit on `main` ("chore: ignore build artifacts and move dev JWT key out of version control") front-ran a large part of this bolt. Verified on the current branch:

- `appsettings.Development.json` → `JwtSettings:PrivateKeyPem` is now `""` (key removed from the file). **Story 002 code change: already done.**
- `Program.cs` loads `appsettings.{Environment}.Local.json` (gitignored) last, so the real dev key lives outside version control. **Story 002 mechanism: already done.**
- `.gitignore` already has the full secrets block (`.env`, `.env.*`, `*.pem`, `*.pfx`, `*.key`, `secrets/`, `appsettings.*.Local.json`). **Story 003 gitignore: already done.**
- `AuthExtensions.cs` already throws `InvalidOperationException` on an empty `PrivateKeyPem` (boot fail-fast). **Story 002 fail-fast: already done** (message will be improved — see below).

So this bolt is now **finish + harden**, not redo. The remaining gaps are: the `secrets/` placeholder, setup docs, the pre-commit + CI guardrails, the rotation runbook, and the history-rewrite decision.

### Confirmed: the real key IS in git history

`git log -p -- src/PhotoPrint.API/appsettings.Development.json` shows a real `-----BEGIN RSA PRIVATE KEY-----` block in commit `6bdd2db` (initial commit). `50213b1` removed it from the working tree but **not from history**. This makes story 005 a live decision and story 001 (rotation) genuinely necessary — anyone with history access can extract the old dev key.

### Dependency note (bolt 040)

The bolt nominally `requires_bolts: [040-containers-and-pipelines]` for a `ci.yml` to host the secret-scan job. Bolt 040 is **not built** (no `.github/workflows/` exists). Rather than block, story 004's CI scan is delivered as a **standalone** `.github/workflows/secret-scan.yml` — it doesn't need the build/test pipeline and can coexist with (or be folded into) bolt 040's CI later.

### Deliverables

1. **`secrets/.gitkeep`** — placeholder so the gitignored `secrets/` dir survives clones (story 003).
2. **`AuthExtensions.cs`** — improve the fail-fast message to name the setup mechanism (user-secrets / `appsettings.{env}.Local.json`) (stories 001/002).
3. **`README.md`** — new. "First-time setup" (generate dev keypair → user-secrets / Local.json), env-var/secret table, and a **Key Rotation Runbook** (story 001 ops procedure) + **Historical secret leak** note (story 005 outcome).
4. **`hooks/pre-commit`** + install instructions — a tracked Git hook (portable; no Husky/npm or Python `pre-commit` dependency) that blocks staged content matching secret patterns. Installed via `git config core.hooksPath hooks` (documented in README) (story 004).
5. **`.gitleaks.toml`** — Gitleaks config with an **allowlist** for the known-legitimate PEM strings (`TestKeys.cs` test fixture, `docs/**`, `memory-bank/**`) so the scan doesn't false-positive (story 004).
6. **`.github/workflows/secret-scan.yml`** — standalone Gitleaks workflow on push/PR (story 004).
7. **`decision-index.md` + ADR** — record the history-rewrite decision (story 005).
8. **`scripts/gen-dev-keys.*`** (optional helper) — one-liner wrappers around `openssl` for generating a dev keypair (story 001 convenience).

### Technical Approach

**Story 001 — rotate JWT keypair**
- In-repo: improve the boot fail-fast message; add `scripts/gen-dev-keys.sh` (+ `.ps1`) generating an RSA 2048 keypair; document the rotation runbook in README.
- Out-of-repo (ops, cannot be executed here): generate fresh prod + staging keypairs, set them via env vars / secret store, rolling restart. **The plan documents this as a runbook; I cannot rotate live keys from the repo.** Acceptance for this story in-repo = runbook + generator + fail-fast; the actual prod rotation is an operator checklist item.

**Story 002 — remove key from repo**
- Code change already done by `50213b1`. Remaining: README "First-time setup" section; improve `AuthExtensions` message:
  ```
  "JwtSettings:PrivateKeyPem is required. Provide it via
   appsettings.{Environment}.Local.json (gitignored) or
   `dotnet user-secrets set JwtSettings:PrivateKeyPem`. See README → First-time setup."
  ```
- Verify no `BEGIN ... PRIVATE KEY` remains in any **tracked working-tree** `src/**` file except the intentional `TestKeys.cs` fixture.

**Story 003 — gitignore + secrets dir**
- gitignore already complete. Add `secrets/.gitkeep`. Verify `touch secrets/dev.pem && git status` shows nothing (already covered by `secrets/` ignore).

**Story 004 — pre-commit + CI scan**
- `hooks/pre-commit` (POSIX sh): runs `git diff --cached` and greps for `-----BEGIN [A-Z ]*PRIVATE KEY-----`, `sk_live_[0-9A-Za-z]{16,}`, `pk_live_…`, `whsec_…`, `ghp_…`; exits non-zero with remediation text on match. Skips the allowlisted `TestKeys.cs` path.
- Install: `git config core.hooksPath hooks` (documented; one-time per clone). No Husky/Python dependency — works on the Windows dev box and CI alike.
- `.gitleaks.toml`: extends the default ruleset; `[allowlist]` paths for `src/PhotoPrint.Tests/Helpers/TestKeys.cs`, `docs/`, `memory-bank/`.
- `.github/workflows/secret-scan.yml`: `gitleaks/gitleaks-action@v2` on `push` + `pull_request`, `fetch-depth: 0`.

**Story 005 — history-rewrite decision**
- Present the decision to the user (it is genuinely theirs). Inputs: real key is in history at `6bdd2db`; a remote `origin/main` exists (history likely already pushed); history is small (5 commits).
- Record the chosen option as **ADR-006** + a README "Historical secret leak" note. If "rewrite": include the `git filter-repo` runbook + force-push + team-coordination steps. If "accept": rely on rotation (story 001) as the active mitigation and document it.
- **The actual `git filter-repo` execution (if chosen) is a destructive, coordinated ops action — I will NOT execute it in this bolt without explicit, separate confirmation.** This bolt records the decision and runbook.

### Acceptance Criteria (consolidated; ✅ = already satisfied by `50213b1`)

- Story 001: fresh keypair generation documented + generator script; fail-fast message improved; prod/staging rotation runbook in README. (Live rotation = ops, out of repo.)
- Story 002: ✅ `PrivateKeyPem: ""` in file; ✅ boot fails fast; **+** README setup docs; **+** improved message.
- Story 003: ✅ gitignore block; **+** `secrets/.gitkeep`.
- Story 004: pre-commit hook blocks fake key locally; `.gitleaks.toml` allowlists legitimate PEM strings; standalone `secret-scan.yml` runs Gitleaks in CI.
- Story 005: decision recorded in `decision-index.md` (ADR-006) + README note; rewrite runbook included if chosen.

### Risks / Notes

- **Pre-commit portability**: `core.hooksPath` + POSIX `sh` hook works on Git for Windows (ships with bash). Avoids Husky (npm) and `pre-commit` (Python) toolchain additions. Documented as a one-time `git config` per clone.
- **Gitleaks false-positives**: `TestKeys.cs`, `docs/analysis/architect-review-2026-05-25.md`, and `memory-bank/intents/018-*/requirements.md` all contain the literal `BEGIN RSA PRIVATE KEY` marker for legitimate reasons. Without the allowlist, the very first scan would fail. The allowlist is essential, not optional.
- **History rewrite is out of automated scope** — decision + runbook only; execution needs separate explicit go-ahead.
- No application-code behaviour changes beyond the fail-fast message; no tests should regress. Stage 3 verifies build + a hook smoke test + (if network allows) a gitleaks dry run.
