using FluentAssertions;
using FluentValidation.TestHelper;
using PhotoPrint.API.DTOs.Payments;
using PhotoPrint.API.Models;
using PhotoPrint.API.Validators.Payments;
using Xunit;

namespace PhotoPrint.Tests.Unit.Validators;

public class CreateOrderRequestValidatorTests
{
    private readonly CreateOrderRequestValidator _sut = new();

    private static ShippingAddressSnapshot ValidAddress() => new()
    {
        Street = "Str. Test",
        Number = "1",
        City = "București",
        County = "Ilfov",
        PostalCode = "010101",
        RecipientName = "Test",
        Phone = "0700000000",
    };

    // ── PaymentProcessor / DeliveryType enum validation ───────────────────────

    [Fact]
    public void UnknownPaymentProcessor_FailsValidation()
    {
        var request = new CreateOrderRequest(
            PaymentProcessor: (PaymentProcessor)99,
            DeliveryType: DeliveryType.Easybox,
            EasyboxLockerId: Guid.NewGuid(),
            ShippingAddress: null);

        _sut.TestValidate(request)
            .ShouldHaveValidationErrorFor(x => x.PaymentProcessor);
    }

    [Fact]
    public void UnknownDeliveryType_FailsValidation()
    {
        var request = new CreateOrderRequest(
            PaymentProcessor: PaymentProcessor.Stripe,
            DeliveryType: (DeliveryType)99,
            EasyboxLockerId: Guid.NewGuid(),
            ShippingAddress: null);

        _sut.TestValidate(request)
            .ShouldHaveValidationErrorFor(x => x.DeliveryType);
    }

    // ── Easybox requires locker id ────────────────────────────────────────────

    [Fact]
    public void Easybox_WithoutLockerId_FailsWithFieldErrorAndMessage()
    {
        var request = new CreateOrderRequest(
            PaymentProcessor: PaymentProcessor.Stripe,
            DeliveryType: DeliveryType.Easybox,
            EasyboxLockerId: null,
            ShippingAddress: null);

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.EasyboxLockerId)
              .WithErrorMessage("Locker ID is required for Easybox delivery");
    }

    private static ShippingAddressSnapshot EasyboxContact() => new()
    {
        RecipientName = "Alice Pop",
        Phone = "0700000000",
        Street = "", Number = "", City = "", County = "", PostalCode = "",
    };

    [Fact]
    public void Easybox_WithLockerAndContact_Passes()
    {
        var request = new CreateOrderRequest(
            PaymentProcessor: PaymentProcessor.Stripe,
            DeliveryType: DeliveryType.Easybox,
            EasyboxLockerId: Guid.NewGuid(),
            ShippingAddress: EasyboxContact());

        var result = _sut.TestValidate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Easybox_WithoutRecipientContact_Fails()
    {
        var request = new CreateOrderRequest(
            PaymentProcessor: PaymentProcessor.Stripe,
            DeliveryType: DeliveryType.Easybox,
            EasyboxLockerId: Guid.NewGuid(),
            ShippingAddress: null);

        _sut.TestValidate(request).ShouldHaveValidationErrorFor(x => x.ShippingAddress);
    }

    [Fact]
    public void Easybox_WithBlankPhone_FailsOnPhone()
    {
        var contact = EasyboxContact();
        contact.Phone = "";

        var request = new CreateOrderRequest(
            PaymentProcessor.Stripe, DeliveryType.Easybox,
            EasyboxLockerId: Guid.NewGuid(), ShippingAddress: contact);

        _sut.TestValidate(request).ShouldHaveValidationErrorFor("ShippingAddress.Phone");
    }

    // ── Courier requires shipping address ─────────────────────────────────────

    [Fact]
    public void Courier_WithoutShippingAddress_FailsWithFieldErrorAndMessage()
    {
        var request = new CreateOrderRequest(
            PaymentProcessor: PaymentProcessor.Stripe,
            DeliveryType: DeliveryType.Courier,
            EasyboxLockerId: null,
            ShippingAddress: null);

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.ShippingAddress)
              .WithErrorMessage("Shipping address is required for courier delivery");
    }

    [Fact]
    public void Courier_WithEmptyPostalCode_FailsOnNestedField()
    {
        var address = ValidAddress();
        address.PostalCode = "";

        var request = new CreateOrderRequest(
            PaymentProcessor: PaymentProcessor.Stripe,
            DeliveryType: DeliveryType.Courier,
            EasyboxLockerId: null,
            ShippingAddress: address);

        _sut.TestValidate(request)
            .ShouldHaveValidationErrorFor("ShippingAddress.PostalCode");
    }

    [Fact]
    public void Courier_WithEmptyCity_FailsOnNestedField()
    {
        var address = ValidAddress();
        address.City = "";

        var request = new CreateOrderRequest(
            PaymentProcessor: PaymentProcessor.Stripe,
            DeliveryType: DeliveryType.Courier,
            EasyboxLockerId: null,
            ShippingAddress: address);

        _sut.TestValidate(request)
            .ShouldHaveValidationErrorFor("ShippingAddress.City");
    }

    [Fact]
    public void Courier_WithEmptyCounty_FailsOnNestedField()
    {
        var address = ValidAddress();
        address.County = "";

        var request = new CreateOrderRequest(
            PaymentProcessor: PaymentProcessor.Stripe,
            DeliveryType: DeliveryType.Courier,
            EasyboxLockerId: null,
            ShippingAddress: address);

        _sut.TestValidate(request)
            .ShouldHaveValidationErrorFor("ShippingAddress.County");
    }

    [Fact]
    public void Courier_WithValidAddress_Passes()
    {
        var request = new CreateOrderRequest(
            PaymentProcessor: PaymentProcessor.Stripe,
            DeliveryType: DeliveryType.Courier,
            EasyboxLockerId: null,
            ShippingAddress: ValidAddress());

        var result = _sut.TestValidate(request);
        result.IsValid.Should().BeTrue();
    }

    // ── Courier recipient fields must be present + sane (else the AWB request
    //    goes out blank and Sameday rejects it → permanent give-up) ────────────

    [Theory]
    [InlineData("ShippingAddress.RecipientName")]
    [InlineData("ShippingAddress.Phone")]
    [InlineData("ShippingAddress.Street")]
    [InlineData("ShippingAddress.Number")]
    public void Courier_WithBlankRecipientField_FailsOnThatField(string field)
    {
        var address = ValidAddress();
        switch (field)
        {
            case "ShippingAddress.RecipientName": address.RecipientName = "  "; break;
            case "ShippingAddress.Phone":         address.Phone = ""; break;
            case "ShippingAddress.Street":        address.Street = ""; break;
            case "ShippingAddress.Number":        address.Number = ""; break;
        }

        var request = new CreateOrderRequest(
            PaymentProcessor.Stripe, DeliveryType.Courier,
            EasyboxLockerId: null, ShippingAddress: address);

        _sut.TestValidate(request).ShouldHaveValidationErrorFor(field);
    }

    [Fact]
    public void Courier_WithNonNumericPhone_FailsOnPhone()
    {
        var address = ValidAddress();
        address.Phone = "not-a-phone";

        var request = new CreateOrderRequest(
            PaymentProcessor.Stripe, DeliveryType.Courier,
            EasyboxLockerId: null, ShippingAddress: address);

        _sut.TestValidate(request).ShouldHaveValidationErrorFor("ShippingAddress.Phone");
    }

    [Fact]
    public void Courier_WithOverlongRecipientName_FailsOnRecipientName()
    {
        var address = ValidAddress();
        address.RecipientName = new string('x', 256);

        var request = new CreateOrderRequest(
            PaymentProcessor.Stripe, DeliveryType.Courier,
            EasyboxLockerId: null, ShippingAddress: address);

        _sut.TestValidate(request).ShouldHaveValidationErrorFor("ShippingAddress.RecipientName");
    }

    [Theory]
    [InlineData("1-2-3-4")]
    [InlineData("()-. ()")]
    public void WithDigitPoorPhone_FailsOnPhone(string phone)
    {
        var contact = EasyboxContact();
        contact.Phone = phone;

        var request = new CreateOrderRequest(
            PaymentProcessor.Stripe, DeliveryType.Easybox,
            EasyboxLockerId: Guid.NewGuid(), ShippingAddress: contact);

        _sut.TestValidate(request).ShouldHaveValidationErrorFor("ShippingAddress.Phone");
    }

    [Fact]
    public void Easybox_WithOversizedAddressField_FailsOnThatField()
    {
        var contact = EasyboxContact();
        contact.Street = new string('x', 300);

        var request = new CreateOrderRequest(
            PaymentProcessor.Stripe, DeliveryType.Easybox,
            EasyboxLockerId: Guid.NewGuid(), ShippingAddress: contact);

        _sut.TestValidate(request).ShouldHaveValidationErrorFor("ShippingAddress.Street");
    }
}
