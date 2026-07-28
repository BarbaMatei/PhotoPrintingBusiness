namespace PhotoPrint.API.Services.Sameday;

/// <summary>
/// Result of one attempt by <c>IAwbCreator</c> to create an AWB for an order.
/// Discriminated union — every case is a sealed subtype of this base record.
///
/// <list type="bullet">
///   <item><see cref="Created"/> — happy path; <c>AwbNumber</c> + <c>LabelUrl</c> are persisted.</item>
///   <item><see cref="Skipped"/> — order no longer eligible (cancelled, AWB already exists).</item>
///   <item><see cref="RetryLater"/> — transient or operator-fix-needed; re-enqueue or wait for the retry job. The <c>IsTransient</c> flag tells the dispatcher whether to re-enqueue in-process; <c>PreserveClaim</c> marks outcomes where the AWB may already exist (vendor timeout, post-create persist failure) so the claim is held through its TTL and the re-attempt is deferred past the vendor round-trip.</item>
///   <item><see cref="GiveUp"/> — terminal; our request is malformed and retrying with the same input cannot help.</item>
/// </list>
/// </summary>
public abstract record AwbCreationOutcome
{
    public sealed record Created(string AwbNumber, string LabelUrl) : AwbCreationOutcome;
    public sealed record Skipped(string Reason)                       : AwbCreationOutcome;
    public sealed record RetryLater(string Reason, bool IsTransient, bool PreserveClaim = false) : AwbCreationOutcome;
    public sealed record GiveUp(string Reason)                        : AwbCreationOutcome;
}
