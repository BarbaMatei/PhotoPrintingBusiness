using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Services;

namespace PhotoPrint.Tests.Unit.Services;

public class LocalStorageServiceTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly LocalStorageService _sut;

    public LocalStorageServiceTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), $"pp-localstorage-{Guid.NewGuid():N}");
        var settings = Options.Create(new StorageSettings { BasePath = _tempRoot });
        _sut = new LocalStorageService(settings, Mock.Of<ILogger<LocalStorageService>>());
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
            Directory.Delete(_tempRoot, recursive: true);
    }

    [Fact]
    public void SupportsPresignedUrls_IsFalse()
    {
        _sut.SupportsPresignedUrls.Should().BeFalse();
    }

    [Fact]
    public async Task SaveAsync_AtNestedKey_CreatesDirectoriesAndWritesBytes()
    {
        var bytes = new byte[] { 1, 2, 3, 4, 5 };
        var key = "uploads/2026/05/abc.jpg";

        await _sut.SaveAsync(new MemoryStream(bytes), key);

        var fullPath = Path.Combine(_tempRoot, "uploads", "2026", "05", "abc.jpg");
        File.Exists(fullPath).Should().BeTrue();
        (await File.ReadAllBytesAsync(fullPath)).Should().Equal(bytes);
    }

    [Fact]
    public async Task ExistsAsync_AfterSave_ReturnsTrue()
    {
        var key = "thumbs/abc.jpg";
        await _sut.SaveAsync(new MemoryStream(new byte[] { 1 }), key);

        (await _sut.ExistsAsync(key)).Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_MissingKey_ReturnsFalse()
    {
        (await _sut.ExistsAsync("missing/x.jpg")).Should().BeFalse();
    }

    [Fact]
    public async Task GetStreamAsync_RoundTripsBytes()
    {
        var bytes = new byte[] { 0xFF, 0xD8, 0xFF };
        var key = "uploads/2026/05/round-trip.jpg";
        await _sut.SaveAsync(new MemoryStream(bytes), key);

        await using var stream = await _sut.GetStreamAsync(key);
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);

        ms.ToArray().Should().Equal(bytes);
    }

    [Fact]
    public async Task DeleteAsync_RemovesObject()
    {
        var key = "uploads/2026/05/delete-me.jpg";
        await _sut.SaveAsync(new MemoryStream(new byte[] { 1 }), key);

        await _sut.DeleteAsync(key);

        (await _sut.ExistsAsync(key)).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_NonexistentKey_NoOp()
    {
        var act = () => _sut.DeleteAsync("missing/y.jpg");
        await act.Should().NotThrowAsync();
    }

    // ── Presigned URLs are not supported on the local adapter ─────────────────

    [Fact]
    public async Task GetPresignedUrlAsync_ThrowsNotSupportedException()
    {
        var act = () => _sut.GetPresignedUrlAsync("thumbs/x.jpg", TimeSpan.FromHours(1));
        await act.Should().ThrowAsync<NotSupportedException>()
            .WithMessage("*LocalStorageService*");
    }

    // ── Path-traversal guards (StorageKeys.Validate) ────────────────

    [Theory]
    [InlineData("../etc/passwd")]
    [InlineData("/absolute/path.jpg")]
    [InlineData("uploads\\evil.jpg")]
    public async Task SaveAsync_RejectsUnsafeKey(string key)
    {
        var act = () => _sut.SaveAsync(new MemoryStream(new byte[] { 1 }), key);
        await act.Should().ThrowAsync<ArgumentException>();
    }
}
