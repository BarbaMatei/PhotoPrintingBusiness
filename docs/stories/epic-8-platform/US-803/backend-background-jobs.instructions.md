# US-803 — Background Jobs (Backend)

## Story
**As a** system  
**I want to** run scheduled cleanup and maintenance tasks without blocking the request pipeline

## Type
BACKEND — ASP.NET Core

## Epic
EPIC-8 | Platformă & Non-Funcționale

## Dependencies
- US-801 (Logging for job execution tracking)
- US-202 (Uploads entity for cleanup)
- US-109 (GuestSessions entity for cleanup)
- US-702 (Users entity for account deletion)

## Acceptance Criteria

1. **Hosted `IHostedService`** implementations (no Hangfire at MVP — keep it simple)
2. **UploadCleanupJob**: runs every hour; soft-deletes Uploads with no associated order after 24h; deletes physical files
3. **GuestSessionCleanupJob**: runs daily; purges GuestSessions with no orders after 7 days
4. **AccountDeletionJob**: runs daily; hard-deletes accounts where `DeletionRequestedAt < 30 days ago`
5. **EmailRetryJob**: processes failed email queue with exponential backoff

## Technical Notes

### Implementation Details

#### Base Pattern
```csharp
public abstract class RecurringJob : BackgroundService
{
    protected abstract TimeSpan Interval { get; }
    protected abstract Task ExecuteJobAsync(CancellationToken ct);
    
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try { await ExecuteJobAsync(ct); }
            catch (Exception ex) { _logger.LogError(ex, "Job {Job} failed", GetType().Name); }
            await Task.Delay(Interval, ct);
        }
    }
}
```

#### UploadCleanupJob (every 1 hour)
- Query: `Uploads WHERE DeletedAt IS NULL AND OrderItems.Count == 0 AND UploadedAt < 24h ago`
- Soft-delete: set `DeletedAt = now`
- Delete physical file from storage
- Log: number of files cleaned up

#### GuestSessionCleanupJob (every 24 hours)
- Query: `GuestSessions WHERE ExpiresAt < now AND Orders.Count == 0`
- Hard-delete session record
- Log: number of sessions cleaned

#### AccountDeletionJob (every 24 hours)
- Query: `Users WHERE DeletionRequestedAt IS NOT NULL AND DeletionRequestedAt < 30 days ago`
- Hard-delete: remove User, related ExternalLogins, RefreshTokens, Addresses
- Anonymize: set order records to `UserId=null` (keep for business records)
- Log: number of accounts deleted

#### EmailRetryJob (every 30 seconds)
- Process in-memory failed email queue
- Exponential backoff: 1s, 4s, 16s (3 attempts max)
- After max retries: log as permanent failure, discard from queue

## Files to Create/Modify
- `src/PhotoPrint.API/BackgroundJobs/RecurringJob.cs` (base class)
- `src/PhotoPrint.API/BackgroundJobs/UploadCleanupJob.cs`
- `src/PhotoPrint.API/BackgroundJobs/GuestSessionCleanupJob.cs`
- `src/PhotoPrint.API/BackgroundJobs/AccountDeletionJob.cs`
- `src/PhotoPrint.API/BackgroundJobs/EmailRetryJob.cs`
- `Program.cs` (register hosted services)

## Testing
- Unit test: upload cleanup identifies correct records
- Unit test: guest session cleanup identifies expired sessions
- Unit test: account deletion anonymizes orders
- Unit test: email retry exponential backoff logic
- Integration test: cleanup job with seeded data
