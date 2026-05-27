---
intent: 018-secrets-management
phase: inception
status: units-decomposed
created: 2026-05-25T10:25:00Z
updated: 2026-05-25T10:25:00Z
---

# Units: Secrets Management

## Decomposition

| Unit | Type | Stories | Default Bolt Type |
|------|------|---------|-------------------|
| 001-secrets-rotation-and-guardrails | ops | US-018-1, US-018-2, US-018-3, US-018-4, US-018-5 | simple-construction-bolt |

## Rationale

One small, sequential ops chore. Five tightly coupled steps that share a single rollout window.

## Execution Order

1. Rotate keypair → 2. Remove from repo → 3. Add .gitignore → 4. Add hook + CI scan → 5. Decide history rewrite.
