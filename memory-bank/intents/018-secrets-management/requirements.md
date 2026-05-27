---
intent: 018-secrets-management
phase: inception
status: complete
created: 2026-05-25T10:25:00Z
updated: 2026-05-25T10:25:00Z
source: docs/architecture-analysis-2026-05-25.md#6
priority_score: 22
---

# Requirements: Secrets Management

## Intent Overview

`appsettings.Development.json` line 13 contains a real `-----BEGIN RSA PRIVATE KEY-----` block. Anyone with repo read access can forge JWTs signed by it; any staging/prod environment that reused this key is compromised. This intent rotates the key, removes it from source, documents the user-secrets workflow, prevents recurrence with a pre-commit guard + secret scanning, and decides on git history rewrite vs. accepted leak.

## Business Goals

| Goal | Success Metric | Priority |
|------|----------------|----------|
| Eliminate JWT forgery risk from leaked dev key | Production / staging JWT signing keys are different from the leaked dev key | Must |
| Prevent recurrence | Future commits matching common secret patterns are blocked locally + in CI | Must |
| Make secret onboarding trivial | New dev runs one documented command and is ready to go | Should |

---

## Functional Requirements

### FR-1: Rotate JWT signing keypair
- **Description**: Generate a fresh RSA 2048-bit keypair. Rotate any staging/prod key derived from the current one immediately.
- **Acceptance Criteria**:
  - Existing JWTs signed with the old key are invalidated on rollout (revocation of issuer key).
  - Refresh tokens persist normally (DB rows untouched); next refresh issues a new access token signed with the new key.
- **Priority**: Must
- **Related Stories**: US-018-1

### FR-2: Remove key from `appsettings.Development.json`
- **Description**: Replace `Jwt:PrivateKeyPem` value with an empty string; document `dotnet user-secrets set Jwt:PrivateKeyPem "$(cat dev-key.pem)"` in README.
- **Acceptance Criteria**:
  - File contains no `-----BEGIN RSA PRIVATE KEY-----` block on `main`.
  - README has a "First-time setup" section with the user-secrets command.
  - Boot fails fast if `Jwt:PrivateKeyPem` resolves empty (`ValidateOnStart`).
- **Priority**: Must
- **Related Stories**: US-018-2

### FR-3: `.gitignore` additions and `.env.local` discipline
- **Description**: Ignore `appsettings.*.local.json`, `secrets/`, `*.pem`, `*.pfx`, `.env`. Provide a `secrets/.gitkeep` placeholder.
- **Acceptance Criteria**:
  - `git status` is clean after `dotnet user-secrets set` (writes to `~/.microsoft/usersecrets/...`, not the repo).
  - `git status` shows nothing when a developer drops `dev-key.pem` into `secrets/`.
- **Priority**: Must
- **Related Stories**: US-018-3

### FR-4: Pre-commit and CI secret scanning
- **Description**: Add a pre-commit hook (Husky or `pre-commit` framework) and a CI job that fail on detecting `-----BEGIN [A-Z ]*PRIVATE KEY-----`, `sk_live_`, `pk_live_`, `whsec_`, `ghp_`, plus extended Gitleaks ruleset.
- **Acceptance Criteria**:
  - Pre-commit blocks `git commit` if a staged file matches any rule.
  - CI step fails the workflow if the same patterns appear in any diff against `main`.
  - Clear remediation message points contributors to docs.
- **Priority**: Must
- **Related Stories**: US-018-4

### FR-5: Decide on history rewrite
- **Description**: Decide between (a) `git filter-repo --invert-paths --path src/PhotoPrint.API/appsettings.Development.json` history rewrite (preferred) and (b) accepting the leak + relying on key rotation. Document choice in `decision-index.md`.
- **Acceptance Criteria**:
  - Decision recorded with rationale + impact on open PRs/forks.
  - If (a): execution plan with team notification, freeze window, force-push, fork-rebase guidance.
  - If (b): a banner in README acknowledging the historical leak + the rotation that mitigates it.
- **Priority**: Must
- **Related Stories**: US-018-5

---

## Non-Functional Requirements

### Security
| Requirement | Standard | Notes |
|-------------|----------|-------|
| Key strength | RSA 2048-bit minimum | Match current algorithm (RS256) |
| Secret-at-rest | Per-environment env vars | Aligns with intent 017 env-matrix |
| Access | Production keys read by exactly the API container + ops | No CI artefact stores keys |

### Compliance
| Requirement | Standard | Notes |
|-------------|----------|-------|
| Audit log | Track who rotated and when | Document in `decision-index.md` |

---

## Constraints

### Technical Constraints
- Must coordinate with intent 014's `Idempotency-Key` rollout to avoid double-deploy surprise.
- Must not break refresh-token flow during rollover.

### Business Constraints
- Ship as a quick win — score 22, complexity 1.

---

## Assumptions

| Assumption | Risk if Invalid | Mitigation |
|------------|-----------------|------------|
| Staging/prod did NOT actually reuse the dev key | Bigger blast radius | Audit before rotation; rotate staging/prod regardless to be safe |
| Pre-commit hook acceptable to all contributors | Friction → bypassed | Provide install command; document `--no-verify` policy (only emergency) |

---

## Open Questions

| Question | Owner | Due Date | Resolution |
|----------|-------|----------|------------|
| Q1: Rewrite git history or accept the leak? | Team lead | 2026-06-01 | Pending — recommend rewrite if repo is private and team is small; otherwise accept + rotate |
