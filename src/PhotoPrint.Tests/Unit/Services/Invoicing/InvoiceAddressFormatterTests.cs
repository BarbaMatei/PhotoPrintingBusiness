using FluentAssertions;
using PhotoPrint.API.Services.Invoicing;
using Xunit;

namespace PhotoPrint.Tests.Unit.Services.Invoicing;

public class InvoiceAddressFormatterTests
{
    [Fact]
    public void Truncate_null_returns_empty_rather_than_throwing()
    {
        // Declared non-nullable, but arrive null when a client omits the field.
        InvoiceAddressFormatter.Truncate(null, InvoiceAddressFormatter.CityNameMaxLength)
            .Should().BeEmpty();
    }

    [Fact]
    public void Truncate_empty_returns_empty()
    {
        InvoiceAddressFormatter.Truncate("", 10).Should().BeEmpty();
    }

    [Fact]
    public void Truncate_shorter_than_limit_is_returned_unchanged()
    {
        InvoiceAddressFormatter.Truncate("Cluj-Napoca", 50).Should().Be("Cluj-Napoca");
    }

    [Fact]
    public void Truncate_at_the_limit_is_returned_unchanged()
    {
        InvoiceAddressFormatter.Truncate(new string('x', 50), 50).Should().HaveLength(50);
    }

    [Fact]
    public void Truncate_longer_than_limit_is_cut_to_the_limit()
    {
        InvoiceAddressFormatter.Truncate(new string('x', 60), 50).Should().HaveLength(50);
    }

    [Fact]
    public void Truncate_never_splits_a_surrogate_pair()
    {
        // A lone surrogate half is not valid XML, so the invoice would never build.
        var value = new string('x', 9) + "\U00010437";

        var cut = InvoiceAddressFormatter.Truncate(value, 10);

        cut.Should().HaveLength(9, "the pair cannot fit, so neither half is emitted");
        char.IsSurrogate(cut[^1]).Should().BeFalse();
    }

    [Fact]
    public void Truncate_keeps_a_surrogate_pair_that_fits_whole()
    {
        var value = new string('x', 8) + "\U00010437";

        var cut = InvoiceAddressFormatter.Truncate(value, 10);

        cut.Should().Be(value);
    }

    [Fact]
    public void FormatStreetName_skips_blank_parts()
    {
        InvoiceAddressFormatter.FormatStreetName("Str. Test", "", null).Should().Be("Str. Test");
    }
}
