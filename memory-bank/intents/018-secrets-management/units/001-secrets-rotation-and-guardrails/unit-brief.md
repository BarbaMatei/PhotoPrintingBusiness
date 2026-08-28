---
unit: 001-secrets-rotation-and-guardrails
intent: 018-secrets-management
phase: inception
status: draft
created: 2026-05-25T10:25:00Z
updated: 2026-05-25T10:25:00Z
---

# Unit Brief: Secrets Rotation & Guardrails

## Purpose

Rotate the JWT keypair, remove the leaked one from source, and stand up controls (gitignore, pre-commit, CI scan) so this can't happen again.

## Scope

### In Scope
- Key generation + ops rotation runbook
- `appsettings.Development.json` change
- `.gitignore` additions
- Pre-commit hook config + install instructions
- CI workflow job
- `decision-index.md` entry for history-rewrite choice

### Out of Scope
- Vault / KMS adoption
- Full audit of every potentially leaked secret (cover Stripe, the legacy processor keys too if discovered, but as ops tasks)

---

## Story Summary

| Story ID | Title | Priority |
|----------|-------|----------|
| 001-rotate-jwt-keypair | Generate + rotate keys across environments | Must |
| 002-remove-key-from-repo | Empty value + user-secrets workflow | Must |
| 003-gitignore-and-secrets-dir | `.gitignore` discipline + placeholder dir | Must |
| 004-precommit-and-ci-scan | Pre-commit hook + Gitleaks CI job | Must |
| 005-history-rewrite-decision | Decide and record in `decision-index.md` | Must |
