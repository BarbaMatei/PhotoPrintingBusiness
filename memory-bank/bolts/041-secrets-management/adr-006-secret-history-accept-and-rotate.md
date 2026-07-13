---
bolt: 041-secrets-management
created: 2026-05-25T14:45:00Z
status: accepted
superseded_by: null
---

# ADR-006: Accept the Historical Key Leak and Mitigate by Rotation (No History Rewrite)

## Context

A real dev RSA JWT signing key was committed to `src/PhotoPrint.API/appsettings.Development.json` in the
initial commit (`6bdd2db`). A later commit (`50213b1`) emptied the value in the working tree and moved
secrets to gitignored local config, but the key **remains in git history** at `6bdd2db`.

The repository has a remote (`origin/main`) and history was likely already pushed/cloned. Two mitigation
paths exist: rewrite history to purge the key, or accept its presence in history and neutralize it by
rotation.

## Decision

**Accept the leak in history and rely on key rotation as the active mitigation.** Do **not** rewrite git
history.

Concretely:
- Rotate the leaked dev key out of every environment (dev/staging/prod) per the README rotation runbook,
  rendering the historical copy useless.
- The file ships with `PrivateKeyPem: ""`; secrets load from gitignored `appsettings.{env}.Local.json` /
  user-secrets.
- A pre-commit hook + CI gitleaks scan prevent recurrence.
- A "Historical secret leak" note in the README documents this for transparency.

## Rationale

Rotation fully neutralizes the operational risk: once the key is no longer accepted by any environment,
its presence in history is inert. History rewrite (`git filter-repo` + force-push) is disruptive — it
invalidates every existing clone/fork and open PR and requires coordinated team re-cloning — for a
benefit that rotation already delivers. The key was a **dev** key, not a production credential.

### Alternatives Considered

| Alternative | Pros | Cons | Why Rejected |
|-------------|------|------|--------------|
| Accept + rotate (chosen) | No disruptive force-push; risk neutralized by rotation; fast | Key string remains in history forever (inert after rotation) | **Accepted** |
| Rewrite history (`git filter-repo`) | Key physically removed from history | Force-push invalidates all clones/forks/PRs; coordinated re-clone; the key was already pushed so mirrors may retain it anyway | Rejected — high disruption, marginal benefit over rotation for a dev key |
| Do nothing | Zero effort | Leaked key stays usable | Rejected — fails the intent's whole purpose |

## Consequences

### Positive
- No force-push; no team disruption; open work unaffected.
- Operational risk eliminated once rotation completes.
- Recurrence blocked by the pre-commit hook + CI scan.

### Negative
- The literal key bytes remain in history (inert post-rotation). A future auditor scanning full history
  will still find the old blob; the README note explains why that is acceptable.

### Risks
- **Risk**: rotation is documented but not yet executed in prod/staging (ops action outside this repo).
  **Mitigation**: README runbook + this ADR make it an explicit operator checklist item; the dev default
  is already empty + fail-fast.

## Related

- **Stories**: 005-history-rewrite-decision (this ADR is its output), 001-rotate-jwt-keypair
- **Standards**: README → "Historical secret leak" + "JWT key rotation runbook"
- **Previous ADRs**: none directly; complements the secrets-management intent (018)
