namespace PhotoPrint.API.Configuration;

public class EmailSettings
{
    public string Provider { get; set; } = "Smtp";
    public string FromAddress { get; set; } = "noreply@fototipar.ro";
    public string FromName { get; set; } = "FotoTipar";
    public string OperatorBcc { get; set; } = "";
    public SmtpSettings Smtp { get; set; } = new();
    public SendGridSettings SendGrid { get; set; } = new();
}

public class SmtpSettings
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1025;
    public bool UseSsl { get; set; } = false;
    public string? Username { get; set; }
    public string? Password { get; set; }
}

public class SendGridSettings
{
    public string ApiKey { get; set; } = "";
}
