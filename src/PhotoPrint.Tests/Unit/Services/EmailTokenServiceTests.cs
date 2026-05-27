using FluentAssertions;
using PhotoPrint.API.Services;

namespace PhotoPrint.Tests.Unit.Services;

public class EmailTokenServiceTests
{
    private readonly EmailTokenService _sut = new();

    [Fact]
    public void GenerateEmailToken_EachCallReturnsDifferentToken()
    {
        var (raw1, hash1) = _sut.GenerateEmailToken();
        var (raw2, hash2) = _sut.GenerateEmailToken();

        raw1.Should().NotBe(raw2);
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void GenerateEmailToken_HashIsUppercaseHex()
    {
        var (_, hash) = _sut.GenerateEmailToken();

        hash.Should().MatchRegex("^[0-9A-F]+$");
    }

    [Fact]
    public void GenerateEmailToken_RawTokenIsUrlSafeBase64()
    {
        var (raw, _) = _sut.GenerateEmailToken();

        // URL-safe base64 chars only (no +, /, or padding =)
        raw.Should().MatchRegex("^[A-Za-z0-9_-]+$");
    }

    [Fact]
    public void VerifyEmailToken_CorrectToken_ReturnsTrue()
    {
        var (raw, hash) = _sut.GenerateEmailToken();

        _sut.VerifyEmailToken(raw, hash).Should().BeTrue();
    }

    [Fact]
    public void VerifyEmailToken_WrongRawToken_ReturnsFalse()
    {
        var (_, hash) = _sut.GenerateEmailToken();

        _sut.VerifyEmailToken("this-is-not-the-right-token", hash).Should().BeFalse();
    }

    [Fact]
    public void VerifyEmailToken_TamperedHash_ReturnsFalse()
    {
        var (raw, _) = _sut.GenerateEmailToken();
        var (_, wrongHash) = _sut.GenerateEmailToken();

        _sut.VerifyEmailToken(raw, wrongHash).Should().BeFalse();
    }

    [Fact]
    public void VerifyEmailToken_EmptyRawToken_ReturnsFalse()
    {
        var (_, hash) = _sut.GenerateEmailToken();

        _sut.VerifyEmailToken("", hash).Should().BeFalse();
    }
}
