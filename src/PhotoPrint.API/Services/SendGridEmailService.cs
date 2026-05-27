using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace PhotoPrint.API.Services;

public class SendGridEmailService : IEmailSender
{
    private readonly EmailSettings _settings;
    private readonly ILogger<SendGridEmailService> _logger;

    public SendGridEmailService(IOptions<EmailSettings> settings, ILogger<SendGridEmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendAsync(
        string to,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        var client = new SendGridClient(_settings.SendGrid.ApiKey);

        var message = new SendGridMessage
        {
            From = new EmailAddress(_settings.FromAddress, _settings.FromName),
            Subject = subject,
            HtmlContent = htmlBody,
        };

        message.AddTo(new EmailAddress(to));

        if (!string.IsNullOrWhiteSpace(_settings.OperatorBcc))
        {
            message.AddBcc(new EmailAddress(_settings.OperatorBcc));
        }

        message.AddHeader(
            "List-Unsubscribe",
            $"<mailto:{_settings.FromAddress}?subject=unsubscribe>");

        var response = await client.SendEmailAsync(message, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Body.ReadAsStringAsync(cancellationToken);

            _logger.LogError(
                "SendGrid returned {StatusCode} for {To} | Body: {Body}",
                (int)response.StatusCode, to, body);

            throw new InvalidOperationException(
                $"SendGrid delivery failed with status {(int)response.StatusCode}.");
        }

        _logger.LogInformation(
            "Email sent via SendGrid to {To} | Subject: {Subject}",
            to, subject);
    }
}
