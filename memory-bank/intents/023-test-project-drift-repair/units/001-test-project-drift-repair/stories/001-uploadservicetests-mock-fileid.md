---
id: 001-uploadservicetests-mock-fileid
unit: 001-test-project-drift-repair
intent: 023-test-project-drift-repair
status: implemented
priority: must
created: 2026-05-25T11:45:00Z
assigned_bolt: 049-test-project-drift-repair
implemented: true
implemented_at: 2026-05-25T12:20:00Z
---

# Story: 001-uploadservicetests-mock-fileid

## User Story

**As** the test project
**I want** `UploadServiceTests` to bind its Moq setup to the current `IStorageService.SaveAsync` signature
**So that** the file compiles and all existing assertions continue to verify the service's behaviour

## Acceptance Criteria

- [ ] `dotnet build src/PhotoPrint.Tests` no longer reports CS0854 in `Unit/Services/UploadServiceTests.cs`.
- [ ] Every `Setup(s => s.SaveAsync(...))` lambda passes `It.IsAny<Guid?>()` for the new `fileId` parameter (or an explicit `null` where the test specifically wants the auto-id path).
- [ ] Every `Verify(s => s.SaveAsync(...))` lambda similarly accepts the new parameter.
- [ ] All existing tests in the file pass after the change.

## Technical Notes

- Production signature is now `Task<string> SaveAsync(Stream stream, Guid ownerId, string extension, CancellationToken ct = default, Guid? fileId = null)`.
- Moq expression trees cannot resolve optional args; the fix is to make the trailing args explicit using `It.IsAny<...>()`.
- No production code changes.

## Dependencies

### Requires
- None

### Enables
- 004-suite-green-verification

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| A test wanted to assert the *auto-id* code path was taken | Use `It.Is<Guid?>(id => id == null)` for clarity |
| A test wanted to assert a *specific* `fileId` was passed | Use `It.Is<Guid?>(id => id == expected)` |

## Out of Scope

- Refactoring the test class structure.
