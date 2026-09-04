namespace PhotoPrint.Tests.Helpers;

public static class RepoFiles
{
    public static string Root { get; } = FindRoot();

    public static string Path(params string[] segments) =>
        System.IO.Path.Combine([Root, .. segments]);

    public static string ReadAllText(params string[] segments) =>
        File.ReadAllText(Path(segments));

    private static string FindRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(System.IO.Path.Combine(dir.FullName, "PhotoPrint.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root not found from " + AppContext.BaseDirectory);
    }
}
