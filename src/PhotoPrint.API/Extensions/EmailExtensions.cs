using Microsoft.Extensions.Options;
using PhotoPrint.API.BackgroundJobs;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Data;
using PhotoPrint.API.Services;

namespace PhotoPrint.API.Extensions;

public static class EmailExtensions
{
    public const string RawSenderKey = "email-provider-raw";

    public static IServiceCollection AddEmailInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<EmailSettings>(configuration.GetSection("Email"));

        var provider = configuration["Email:Provider"]
            ?? throw new InvalidOperationException(
                "Email:Provider configuration is required. Set it to 'Smtp' or 'SendGrid'.");

        switch (provider)
        {
            case "Smtp":
                services.AddKeyedScoped<IEmailSender, SmtpEmailService>(RawSenderKey);
                break;

            case "SendGrid":
                var apiKey = configuration["Email:SendGrid:ApiKey"];
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    throw new InvalidOperationException(
                        "Email:SendGrid:ApiKey is required when Email:Provider is 'SendGrid'.");
                }

                services.AddKeyedScoped<IEmailSender, SendGridEmailService>(RawSenderKey);
                break;

            default:
                throw new InvalidOperationException(
                    $"Unknown Email:Provider '{provider}'. Expected 'Smtp' or 'SendGrid'.");
        }

        services.AddSingleton<IRazorTemplateService, RazorTemplateService>();

        services.AddScoped<IEmailService>(sp =>
        {
            var sender = sp.GetRequiredKeyedService<IEmailSender>(RawSenderKey);
            var templates = sp.GetRequiredService<IRazorTemplateService>();
            var db = sp.GetRequiredService<PhotoPrintDbContext>();
            var logger = sp.GetRequiredService<ILogger<ReliableEmailService>>();

            return new ReliableEmailService(sender, templates, db, logger);
        });

        services.AddScoped<IOrderEmailService, OrderEmailService>();

        services.AddHostedService<EmailRetryJob>();

        return services;
    }
}
