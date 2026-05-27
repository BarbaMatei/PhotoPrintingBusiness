---
id: 049-test-project-drift-repair
unit: 001-test-project-drift-repair
intent: 023-test-project-drift-repair
type: simple-construction-bolt
status: complete
stories:
  - 001-uploadservicetests-mock-fileid
  - 002-cartservicetests-grouped-dto
  - 003-cart-controller-tests-grouped-dto
  - 004-suite-green-verification
created: 2026-05-25T11:45:00Z
started: 2026-05-25T11:55:00Z
completed: 2026-05-25T12:20:00Z
current_stage: null
stages_completed:
  - name: plan
    completed: 2026-05-25T12:00:00Z
    artifact: implementation-plan.md
  - name: implement
    completed: 2026-05-25T12:10:00Z
    artifact: implementation-walkthrough.md
  - name: test
    completed: 2026-05-25T12:20:00Z
    artifact: test-walkthrough.md

requires_bolts: [033-upload-cleanup-fix]
enables_bolts: []
requires_units: []
blocks: false

complexity:
  avg_complexity: 2
  avg_uncertainty: 2
  max_dependencies: 1
  testing_scope: 3
---

# Bolt: 049-test-project-drift-repair

## Overview

Three test-file repairs to bring `src/PhotoPrint.Tests` back to a green build and a green run.

## Objective

After this bolt, `dotnet test src/PhotoPrint.Tests` runs every test in the project — including bolt-033's three new `UploadCleanupJob` tests — without any `Directory.Build.targets` exclusion trick.

## Stories Included

- **001-uploadservicetests-mock-fileid** — Moq `Setup`/`Verify` lambdas updated for the new `IStorageService.SaveAsync` 5-arg overload (Must).
- **002-cartservicetests-grouped-dto** — every `result.Items` rewritten against `Groups[].Items`; every `CartRequest` ctor passes `FinishName` (Must).
- **003-cart-controller-tests-grouped-dto** — same rewrites applied to the controller integration tests (Must).
- **004-suite-green-verification** — final unfiltered `dotnet test` run, no exclusions (Must).

## Bolt Type

`simple-construction-bolt` — narrow scope, well-understood mechanical changes, ends on a single suite-green gate.

## Stage Plan

| Stage | Name | Output |
|-------|------|--------|
| 1 | Plan | `implementation-plan.md` — exact per-file diff strategy, decision on which tests (if any) become `Skip` |
| 2 | Implement | Patches to the three test files; `implementation-walkthrough.md` |
| 3 | Test | Full `dotnet test` run; `test-walkthrough.md` records results |

## Dependencies

- **Requires**: bolt `033-upload-cleanup-fix` is complete (its `UploadCleanupJobTests` additions are part of the suite this bolt must run green).
- **Enables**: every subsequent bolt that touches tests.

## Key Technical Notes

- No production code changes. If any test reveals a genuine production bug after the rewrite, escalate as a separate intent — do not widen this bolt.
- Prefer minimal mechanical rewrites that preserve each test's original assertion intent. If a test was exercising removed behaviour, mark `[Fact(Skip = "...")]` rather than delete silently.
- The `CartFactory` integration helper is the natural place to centralise the `FinishName` parameter — fix once, not at every call site.
