namespace PhotoPrint.API.Services;

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default);
}
