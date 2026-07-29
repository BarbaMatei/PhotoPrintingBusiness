namespace PhotoPrint.API.Services.Sameday;

/// <summary>
/// Username + password pair for the Sameday API. Built once at boot from
/// <c>SamedaySettings</c>. <see cref="ToString"/> redacts both fields so an
/// accidental <c>logger.LogX("{Creds}", creds)</c> emits literal stars rather
/// than the password. Defence in depth: the credential values should never
/// reach a logger call in the first place.
/// </summary>
public sealed record SamedayCredentials(string Username, string Password)
{
    public override string ToString()
        => "SamedayCredentials(Username=***, Password=***)";
}
