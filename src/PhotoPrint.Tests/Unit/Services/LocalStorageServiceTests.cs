using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Services;
using Xunit;

namespace PhotoPrint.Tests.Unit.Services;

public class LocalStorageServiceTests : IDisposable
{
    private readonly string _baseDir =
        Path.Combine(Path.GetTempPath(), $"pp-storage-tests-{Guid.NewGuid():N}");
    private readonly LocalStorageService _sut;

    public LocalStorageServiceTests()
    {
        Directory.CreateDirectory(_baseDir);
        _sut = new LocalStorageService(
            Options.Create(new StorageSettings { BasePath = _baseDir }),
            Mock.Of<ILogger<LocalStorageService>>());
    }

    public void Dispose()
    {
        try { Directory.Delete(_baseDir, recursive: true); } catch { /* best-effort cleanup */ }
    }

    private static MemoryStream Bytes(params byte[] b) => new(b);

    // NEW-4 (review 042-v2): stored keys must be OS-independent ('/'-separated), not
    // backslash-separated on Windows — a key written on a Windows dev box must read on Linux
    // and map cleanly to a cloud object key (bolt-043).
    [Fact]
    public async Task SaveAsync_WithPrefix_ReturnsForwardSlashKey()
    {
        var owner = Guid.NewGuid();
        var id = Guid.NewGuid();

        var key = await _sut.SaveAsync(Bytes(1, 2, 3), owner, "jpg", fileId: id, prefix: "thumbs");

        key.Should().Be($"thumbs/{owner}/{id:N}.jpg");
        key.Should().NotContain("\\");
    }

    [Fact]
    public async Task SaveAsync_WithoutPrefix_ReturnsForwardSlashKey()
    {
        var owner = Guid.NewGuid();
        var id = Guid.NewGuid();

        var key = await _sut.SaveAsync(Bytes(1, 2, 3), owner, "png", fileId: id);

        key.Should().Be($"{owner}/{id:N}.png");
        key.Should().NotContain("\\");
    }

    [Fact]
    public async Task SaveAsync_ThenRoundTrips_ExistsGetDelete()
    {
        var owner = Guid.NewGuid();
        var payload = new byte[] { 0xFF, 0xD8, 0xFF, 0x42 };

        var key = await _sut.SaveAsync(Bytes(payload), owner, "jpg", fileId: Guid.NewGuid(), prefix: "thumbs");

        (await _sut.ExistsAsync(key)).Should().BeTrue();

        await using (var stream = await _sut.GetStreamAsync(key))
        {
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            ms.ToArray().Should().Equal(payload);
        }

        await _sut.DeleteAsync(key);
        (await _sut.ExistsAsync(key)).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_MissingFile_IsNoOp()
    {
        var act = () => _sut.DeleteAsync($"thumbs/{Guid.NewGuid()}/{Guid.NewGuid():N}.jpg");
        await act.Should().NotThrowAsync();
    }
}
