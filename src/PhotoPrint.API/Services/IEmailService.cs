namespace PhotoPrint.API.Services;

public interface IEmailService : IEmailSender
{
    Task SendTemplatedAsync<T>(
        string to,
        string subject,
        string templateName,
        T model,
        CancellationToken cancellationToken = default);
}
