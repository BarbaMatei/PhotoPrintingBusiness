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

    [Fact]
    public void Easybox_WithLockerId_Passes()
    {
        var request = new CreateOrderRequest(
            PaymentProcessor: PaymentProcessor.Stripe,
            DeliveryType: DeliveryType.Easybox,
            EasyboxLockerId: Guid.NewGuid(),
            ShippingAddress: null);

        var result = _sut.TestValidate(request);
        result.IsValid.Should().BeTrue();
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
}
