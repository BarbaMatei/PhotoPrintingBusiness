---
type: review
target: discount-module
version: 1
supersedes: null
commit: fixture-base
branch: fixture/discount-module
pass-type: discovery
date: 2026-08-31
lenses: [correctness, security, requirements, quality, tests-coverage, completeness-critic]
lenses-not-run: [db-parity, input-validation, observability, race, frontend-ux]
verdict: request-changes
blockers: [PPW-9701, PPW-9702]
findings: { high: 2, medium: 0, low: 1, cleanup: 1, refuted: 1 }
tests: { node: "4/4" }
---

# Review v1 — discount-module

Six lenses over `src/`, `test/` and `docs/`. The defect detail, the fix briefs and the
histories live on the ledger; this file is the point-in-time record and is immutable.

## Findings

| ID | Sev | Title | File | Fix now? |
|---|---|---|---|---|
| PPW-9701 | 🔴 | A discount above the total pays the customer | `src/discount.mjs:3` | yes |
| PPW-9702 | 🔴 | A cart lookup returns every user's lines | `src/cart.mjs:8` | yes |
| PPW-9703 | 🟡 | The pricing doc states the unclamped behaviour as the rule | `docs/pricing.md:9` | yes |
| PPW-9704 | ⚪ | The discount ceiling is declared in two modules | `src/cart.mjs:1` | yes |

## Refuted

| Suspicion | Why it is not real |
|---|---|
| `cartTotal` double-counts a line | It sums `qty` once per line |

## Notes for the fixer

- Two blockers in two different files: no shared surface, so no protocol block is owed.
- PPW-9702 changes what the cart lookup keys on, which is a key-scheme change — the ledger
  marks it trigger-list-shaped and it carries no pre-check verdict.
