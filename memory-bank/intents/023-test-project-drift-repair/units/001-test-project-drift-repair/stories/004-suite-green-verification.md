---
id: 004-suite-green-verification
unit: 001-test-project-drift-repair
intent: 023-test-project-drift-repair
status: complete
priority: must
created: 2026-05-25T11:45:00Z
assigned_bolt: 049-test-project-drift-repair
implemented: true
implemented_at: 2026-05-25T12:20:00Z
---

# Story: 004-suite-green-verification

## User Story

**As** the team
**I want** a clean `dotnet test` run with no file exclusions
**So that** CI can run the whole suite and we never trip over silent skips

## Acceptance Criteria

- [ ] No `Directory.Build.targets` (or any other `<Compile Remove>` mechanism) exists under `src/PhotoPrint.Tests/`.
- [ ] `dotnet build src/PhotoPrint.Tests` exits 0.
- [ ] `dotnet test src/PhotoPrint.Tests` runs every test discovered.
- [ ] Bolt 033's three new tests (the `Cleanup_skips_*` and `Cleanup_deletes_orphan_upload_past_referenced_window` methods) pass without filter.
- [ ] Any test that fails for a *separate* reason (real flaky behaviour, environmental dependency) is documented in the test walkthrough with its failure mode — not silently re-quarantined.

## Technical Notes

- Final command for verification: `dotnet test src/PhotoPrint.Tests/PhotoPrint.Tests.csproj`.
- If pre-existing tests fail for reasons unrelated to the three drift-repair files, flag them as a separate ops follow-up but do not block bolt 049 completion on them — the bolt's scope is "build green + all repaired tests green," not "every test in the project passes."

## Dependencies

### Requires
- 001, 002, 003

### Enables
- Bolt 033 retro-verification without the isolation trick.
- Every future bolt that adds tests.

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| A previously-passing test now fails because the production code drifted but no test caught it | Document; do NOT widen bolt 049 scope to fix it |
| A test was secretly broken on `main` for months | Document; do NOT widen scope |

## Out of Scope

- Code-coverage thresholds.
- Performance baselines.
