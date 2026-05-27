---
intent: 023-test-project-drift-repair
phase: inception
status: units-decomposed
created: 2026-05-25T11:45:00Z
updated: 2026-05-25T11:45:00Z
---

# Units: Test Project Drift Repair

## Decomposition

| Unit | Type | Stories | Default Bolt Type |
|------|------|---------|-------------------|
| 001-test-project-drift-repair | test | US-023-1, US-023-2, US-023-3, US-023-4 | simple-construction-bolt |

## Rationale

Four small repairs against the same project, all gated by a single "the test suite must build and run" success criterion. No reason to split — coordination overhead would dwarf the change.

## Unit Dependency Graph

```text
[001-test-project-drift-repair]
```

## Execution Order

1. Half day — sequential through the four stories then one full `dotnet test` to gate.
