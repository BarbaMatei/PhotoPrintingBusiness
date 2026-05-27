namespace PhotoPrint.API.Models;

public class EmailQueue
{
    public Guid Id { get; set; }
    public string To { get; set; } = "";
    public string Subject { get; set; } = "";
    public string HtmlBody { get; set; } = "";
    public EmailStatus Status { get; set; } = EmailStatus.Pending;
    public int Attempts { get; set; }
    public DateTimeOffset NextRetryAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public string? LastError { get; set; }
}

public enum EmailStatus
{
    Pending,
    Sent,
    Failed,
}
