using FluentAssertions;
using PhotoPrint.API.Services;

namespace PhotoPrint.Tests.Unit.Services;

public class StorageKeysTests
{
    // ── Key generation ────────────────────────────────────────────────────────

    [Fact]
    public void Original_BuildsExpectedYearMonthScheme()
    {
        var id = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var when = new DateTimeOffset(2026, 5, 28, 12, 0, 0, TimeSpan.Zero);

        var key = StorageKeys.Original(id, when, "jpg");

        key.Should().Be("uploads/2026/05/11111111222233334444555555555555.jpg");
    }

    [Fact]
    public void Original_AcceptsExtensionWithLeadingDot()
    {
        var id = Guid.NewGuid();
        var key = StorageKeys.Original(id, DateTimeOffset.UtcNow, ".png");

        key.Should().EndWith(".png");
        key.Should().NotContain("..");
    }

    [Fact]
    public void Thumbnail_KeyedByUploadIdUnderThumbsPrefix()
    {
        var id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        var key = StorageKeys.Thumbnail(id);

        key.Should().Be("thumbs/aaaaaaaabbbbccccddddeeeeeeeeeeee.jpg");
    }

    [Fact]
    public void Preview_KeyedByUploadIdUnderPreviewsPrefix()
    {
        var id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        var key = StorageKeys.Preview(id);

        key.Should().Be("previews/aaaaaaaabbbbccccddddeeeeeeeeeeee.jpg");
    }

    // ── Validate — guards against path-traversal / abuse ──────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyOrWhitespace_Throws(string key)
    {
        var act = () => StorageKeys.Validate(key);
        act.Should().Throw<ArgumentException>().WithMessage("*empty*");
    }

    [Theory]
    [InlineData("/uploads/foo.jpg")]
    [InlineData("\\uploads\\foo.jpg")]
    public void Validate_LeadingSeparator_Throws(string key)
    {
        var act = () => StorageKeys.Validate(key);
        act.Should().Throw<ArgumentException>().WithMessage("*relative*");
    }

    [Fact]
    public void Validate_Backslash_Throws()
    {
        var act = () => StorageKeys.Validate("uploads\\evil.jpg");
        act.Should().Throw<ArgumentException>().WithMessage("*forward slashes*");
    }

    [Theory]
    [InlineData("uploads/../etc/passwd")]
    [InlineData("..")]
    [InlineData("uploads/2026/../leak.jpg")]
    public void Validate_TraversalSequence_Throws(string key)
    {
        var act = () => StorageKeys.Validate(key);
        act.Should().Throw<ArgumentException>().WithMessage("*traversal*");
    }

    [Fact]
    public void Validate_OverlyLongKey_Throws()
    {
        var key = "uploads/" + new string('a', 600);
        var act = () => StorageKeys.Validate(key);
        act.Should().Throw<ArgumentException>().WithMessage("*512 characters*");
    }

    [Fact]
    public void Validate_WellFormedRelativeKey_DoesNotThrow()
    {
        var act = () => StorageKeys.Validate("uploads/2026/05/abc.jpg");
        act.Should().NotThrow();
    }
}
