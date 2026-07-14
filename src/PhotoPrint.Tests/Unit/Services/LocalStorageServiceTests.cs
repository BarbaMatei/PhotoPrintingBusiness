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

    // M2 (review 042-v4): two concurrent writers of the SAME deterministic key (e.g. two
    // first-previews of one upload) must not collide. The first writer is held with the
    // destination handle open; a second writer of the same key must still succeed rather than
    // throw a sharing-violation IOException that surfaces as a 500.
    [Fact]
    public async Task SaveAsync_ConcurrentWritersSameKey_BothSucceedWithoutCollision()
    {
        var owner = Guid.NewGuid();
        var id = Guid.NewGuid();
        var payloadHeld = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var payloadFast = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD };

        var opened = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        using var heldSource = new GatedStream(payloadHeld, opened, release.Task);

        // Writer 1 opens its output and blocks mid-copy, holding the handle.
        var writer1 = _sut.SaveAsync(heldSource, owner, "jpg", fileId: id, prefix: "thumbs");
        await opened.Task;

        // Writer 2 targets the identical key while writer 1 still holds its handle open.
        Func<Task> writer2 = () => _sut.SaveAsync(Bytes(payloadFast), owner, "jpg", fileId: id, prefix: "thumbs");
        await writer2.Should().NotThrowAsync();

        release.SetResult();
        var key = await writer1;

        (await _sut.ExistsAsync(key)).Should().BeTrue();
        await using var stream = await _sut.GetStreamAsync(key);
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        // Writer 1 committed last (released after writer 2 finished), so its bytes win.
        ms.ToArray().Should().Equal(payloadHeld);
    }

    /// <summary>
    /// A seekable source stream that blocks on its first read until released, signalling when
    /// that first read begins — lets a test hold one writer's output handle open while a second
    /// writer races the same storage key.
    /// </summary>
    private sealed class GatedStream : Stream
    {
        private readonly MemoryStream _inner;
        private readonly TaskCompletionSource _opened;
        private readonly Task _release;
        private bool _blockedOnce;

        public GatedStream(byte[] data, TaskCompletionSource opened, Task release)
        {
            _inner = new MemoryStream(data);
            _opened = opened;
            _release = release;
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default)
        {
            if (!_blockedOnce)
            {
                _blockedOnce = true;
                _opened.TrySetResult();
                await _release;
            }
            return await _inner.ReadAsync(buffer, ct);
        }

        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => _inner.Length;
        public override long Position { get => _inner.Position; set => _inner.Position = value; }
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing) _inner.Dispose();
            base.Dispose(disposing);
        }
    }
}
