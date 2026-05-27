using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using PhotoPrint.API.Filters;
using System.Text.Json;
using Xunit;

namespace PhotoPrint.Tests.Unit.Filters;

public class ValidationFilterTests
{
    private static ActionExecutingContext CreateContext(ModelStateDictionary? modelState = null)
    {
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor(),
            modelState ?? new ModelStateDictionary());

        return new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            new object());
    }

    [Fact]
    public void OnActionExecuting_ValidModelState_DoesNotShortCircuit()
    {
        // Arrange
        var filter = new ValidationFilter();
        var context = CreateContext();

        // Act
        filter.OnActionExecuting(context);

        // Assert
        context.Result.Should().BeNull();
    }

    [Fact]
    public void OnActionExecuting_InvalidModelState_Returns422()
    {
        // Arrange
        var filter = new ValidationFilter();
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("Email", "Adresa de email nu este validă.");
        var context = CreateContext(modelState);

        // Act
        filter.OnActionExecuting(context);

        // Assert
        context.Result.Should().BeOfType<ObjectResult>();
        var result = (ObjectResult)context.Result!;
        result.StatusCode.Should().Be(422);
    }

    [Fact]
    public void OnActionExecuting_InvalidModelState_ReturnsCamelCaseFieldNames()
    {
        // Arrange
        var filter = new ValidationFilter();
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("EmailAddress", "Câmpul este obligatoriu.");
        var context = CreateContext(modelState);

        // Act
        filter.OnActionExecuting(context);

        // Assert
        var result = (ObjectResult)context.Result!;
        var json = JsonSerializer.Serialize(result.Value);
        var doc = JsonDocument.Parse(json);
        var errors = doc.RootElement.GetProperty("errors").EnumerateArray().ToList();
        errors.Should().HaveCount(1);
        errors[0].GetProperty("field").GetString().Should().Be("emailAddress");
        errors[0].GetProperty("message").GetString().Should().Be("Câmpul este obligatoriu.");
    }

    [Fact]
    public void OnActionExecuting_MultipleErrors_ReturnsAllErrors()
    {
        // Arrange
        var filter = new ValidationFilter();
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("Email", "Email invalid.");
        modelState.AddModelError("Password", "Parola este prea scurtă.");
        var context = CreateContext(modelState);

        // Act
        filter.OnActionExecuting(context);

        // Assert
        var result = (ObjectResult)context.Result!;
        var json = JsonSerializer.Serialize(result.Value);
        var doc = JsonDocument.Parse(json);
        var errors = doc.RootElement.GetProperty("errors").EnumerateArray().ToList();
        errors.Should().HaveCount(2);
    }

    [Fact]
    public void OnActionExecuted_DoesNothing()
    {
        // Arrange
        var filter = new ValidationFilter();
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor(),
            new ModelStateDictionary());

        var executedContext = new ActionExecutedContext(
            actionContext,
            new List<IFilterMetadata>(),
            new object());

        // Act & Assert — should not throw
        var act = () => filter.OnActionExecuted(executedContext);
        act.Should().NotThrow();
    }
}
