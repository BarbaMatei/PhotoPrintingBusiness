namespace PhotoPrint.API.Configuration;

public class StorageSettings
{
    public const string SectionName = "Storage";

    /// <summary>Absolute path to the root upload directory.</summary>
    public string BasePath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "PhotoPrint", "uploads");
}
