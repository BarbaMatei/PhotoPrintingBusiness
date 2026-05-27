---
id: 002-retention-config
unit: 001-upload-cleanup-job-fix
intent: 013-upload-cleanup-fix
status: implemented
priority: must
created: 2026-05-25T10:00:00Z
assigned_bolt: 033-upload-cleanup-fix
implemented: true
implemented_at: 2026-05-25T11:35:00Z
---

# Story: 002-retention-config

## User Story

**As** an operations engineer
**I want** the cleanup retention windows exposed as configuration
**So that** I can tune retention for production without redeploying code

## Acceptance Criteria

- [ ] **Given** `appsettings.json` contains `UploadCleanup:OrphanRetentionHours: 24` and `UploadCleanup:ReferencedRetentionDays: 365`, **When** the API boots, **Then** `IOptions<UploadCleanupSettings>` resolves to those values.
- [ ] **Given** neither key is present, **When** the API boots, **Then** defaults of 24 / 365 are applied (no boot failure).
- [ ] **Given** the values are non-positive, **When** the API boots, **Then** options validation throws and the job does not start.
- [ ] **When** the job ticks the first time, **Then** it logs `UploadCleanupJob effective retention — orphan_hours={oh}, referenced_days={rd}` at Information.

## Technical Notes

```csharp
// src/PhotoPrint.API/Configuration/UploadCleanupSettings.cs
public sealed class UploadCleanupSettings
{
    public const string SectionName = "UploadCleanup";
    public int OrphanRetentionHours    { get; init; } = 24;
    public int ReferencedRetentionDays { get; init; } = 365;
}

// Program.cs
builder.Services
    .AddOptions<UploadCleanupSettings>()
    .Bind(builder.Configuration.GetSection(UploadCleanupSettings.SectionName))
    .Validate(s => s.OrphanRetentionHours    > 0, "OrphanRetentionHours must be > 0")
    .Validate(s => s.ReferencedRetentionDays > 0, "ReferencedRetentionDays must be > 0")
    .ValidateOnStart();
```

## Dependencies

### Requires
- None

### Enables
- 001-skip-referenced-uploads

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Hot reload changes a value | Pick up on next tick (`IOptionsMonitor<T>`) |
| Misspelled section name | Defaults apply; log a warning at startup |

## Out of Scope

- Multi-tenant retention overrides.
