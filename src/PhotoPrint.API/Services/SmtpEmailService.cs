using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using PhotoPrint.API.Configuration;

namespace PhotoPrint.API.Services;

public class SmtpEmailService : IEmailSender
{
    private readonly EmailSettings _settings;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IOptions<EmailSettings> settings, ILogger<SmtpEmailService> logger)
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
        var message = BuildMessage(to, subject, htmlBody);

        using var client = new SmtpClient();

        await client.ConnectAsync(
            _settings.Smtp.Host,
            _settings.Smtp.Port,
            _settings.Smtp.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.None,
            cancellationToken);

        if (_settings.Smtp.Username is { Length: > 0 } username
            && _settings.Smtp.Password is { Length: > 0 } password)
        {
            await client.AuthenticateAsync(username, password, cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);

        _logger.LogInformation(
            "Email sent via SMTP to {To} | Subject: {Subject}",
            to, subject);
    }

    private MimeMessage BuildMessage(string to, string subject, string htmlBody)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
        message.To.Add(MailboxAddress.Parse(to));

        if (!string.IsNullOrWhiteSpace(_settings.OperatorBcc))
        {
            message.Bcc.Add(MailboxAddress.Parse(_settings.OperatorBcc));
        }

        message.Headers.Add(
            "List-Unsubscribe",
            $"<mailto:{_settings.FromAddress}?subject=unsubscribe>");

        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlBody };

        return message;
    }
}
