using FluentAssertions;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Validators;

namespace PhotoPrint.Tests.Unit.Configuration;

public class AnafSettingsValidatorTests
{
    private static AnafSettings ValidEnabled(string certPath) => new()
    {
        Enabled = true,
        BaseUrl = "https://api.anaf.ro/test/FCTEL/rest/",
        ClientId = "client-abc",
        ClientSecret = "secret-123",
        CertPath = certPath,
        CertPassword = "cert-pwd",
        PollIntervalMinutes = 30,
        MaxBatchSize = 50,
        BackoffHours = new[] { 1, 4, 16, 64 },
    };

    private readonly AnafSettingsValidator _sut = new();

    [Fact]
    public void Disabled_settings_are_always_valid_even_when_empty()
    {
        var s = new AnafSettings { Enabled = false };
        _sut.Validate(null, s).Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Enabled_with_all_fields_set_to_existing_cert_passes()
    {
        using var temp = TempCertFile.Create();
        var result = _sut.Validate(null, ValidEnabled(temp.Path));
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Enabled_with_missing_cert_path_fails()
    {
        var s = ValidEnabled(certPath: "");
        var result = _sut.Validate(null, s);
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("Anaf:CertPath"));
    }

    [Fact]
    public void Enabled_with_nonexistent_cert_file_fails()
    {
        var s = ValidEnabled(certPath: @"C:\does-not-exist\anaf.p12");
        var result = _sut.Validate(null, s);
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("Anaf:CertPath"));
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("ftp://api.anaf.ro/")]
    [InlineData("collector:4317")]    // scheme-less, like the bolt-044 fix
    public void Non_http_base_url_fails(string url)
    {
        using var temp = TempCertFile.Create();
        var s = ValidEnabled(temp.Path);
        s.BaseUrl = url;
        var result = _sut.Validate(null, s);
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("Anaf:BaseUrl"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1441)]   // > 24h
    [InlineData(-1)]
    public void PollIntervalMinutes_out_of_range_fails(int minutes)
    {
        using var temp = TempCertFile.Create();
        var s = ValidEnabled(temp.Path);
        s.PollIntervalMinutes = minutes;
        var result = _sut.Validate(null, s);
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("Anaf:PollIntervalMinutes"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]      // one minute is shorter than one pipeline pass
    [InlineData(-5)]
    [InlineData(1441)]
    public void ClaimTtlMinutes_out_of_range_fails(int minutes)
    {
        using var temp = TempCertFile.Create();
        var s = ValidEnabled(temp.Path);
        s.ClaimTtlMinutes = minutes;
        var result = _sut.Validate(null, s);
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("Anaf:ClaimTtlMinutes"));
    }

    [Fact]
    public void ClaimTtlMinutes_default_is_accepted()
    {
        using var temp = TempCertFile.Create();
        _sut.Validate(null, ValidEnabled(temp.Path)).Failed.Should().BeFalse();
    }

    [Fact]
    public void Empty_backoff_array_fails()
    {
        using var temp = TempCertFile.Create();
        var s = ValidEnabled(temp.Path);
        s.BackoffHours = Array.Empty<int>();
        var result = _sut.Validate(null, s);
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("Anaf:BackoffHours"));
    }

    [Fact]
    public void Backoff_with_too_high_entry_fails()
    {
        using var temp = TempCertFile.Create();
        var s = ValidEnabled(temp.Path);
        s.BackoffHours = new[] { 1, 4, 200 };   // > 168 (1 week)
        _sut.Validate(null, s).Failed.Should().BeTrue();
    }

    /// <summary>
    /// Temporary cert file used as a stand-in for an actual PKCS#12 cert.
    /// The validator only checks <c>File.Exists</c>; the file's contents
    /// don't matter for these tests.
    /// </summary>
    private sealed class TempCertFile : IDisposable
    {
        public string Path { get; }

        private TempCertFile(string path) { Path = path; }

        public static TempCertFile Create()
        {
            var path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"anaf-test-cert-{Guid.NewGuid():N}.p12");
            File.WriteAllText(path, "stub");
            return new TempCertFile(path);
        }

        public void Dispose()
        {
            try { File.Delete(Path); } catch { /* best-effort cleanup */ }
        }
    }
}
