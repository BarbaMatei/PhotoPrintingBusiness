using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Moq;
using PhotoPrint.API.Exceptions;
using PhotoPrint.API.Extensions;
using PhotoPrint.API.Filters;

namespace PhotoPrint.Tests.Unit.Filters;

/// <summary>
/// Unit tests for <see cref="IdempotencyKeyFilter"/>. These set the header value directly on
/// a <see cref="DefaultHttpContext"/> so the exact raw value reaches the filter — the HTTP
/// transport strips leading/trailing OWS from header values, which would otherwise hide the
/// Padding case at the integration layer.
/// </summary>
public class IdempotencyKeyFilterTests
{
    private static ActionExecutingContext ContextWithHeader(string? headerValue)
    {
        var httpContext = new DefaultHttpContext();
        if (headerValue is not null)
            httpContext.Request.Headers[IdempotencyKeyFilter.HeaderName] = headerValue;

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new ActionExecutingContext(
            actionContext, new List<IFilterMetadata>(), new Dictionary<string, object?>(), controller: null!);
    }

    private static IdempotencyKeyFilter CreateFilter()
        => new(Mock.Of<ILogger<IdempotencyKeyFilter>>());

    [Theory]
    [InlineData("  padded-key  ", "padded-key")] // surrounding spaces
    [InlineData("\tabc\t", "abc")]               // surrounding tabs
    [InlineData("abc ", "abc")]                   // trailing only
    [InlineData(" abc", "abc")]                   // leading only
    [InlineData("abc", "abc")]                    // already clean → unchanged
    public void OnActionExecuting_TrimsKeyBeforeStashing(string rawHeader, string expected)
    {
        // The stashed key is the exact unique-index key, so padding
        // must be trimmed — otherwise a padded resend is a distinct key and bypasses dedupe.
        var ctx = ContextWithHeader(rawHeader);

        CreateFilter().OnActionExecuting(ctx);

        Assert.Equal(expected, ctx.HttpContext.GetIdempotencyKey());
    }

    [Fact]
    public void OnActionExecuting_PaddedAndUnpaddedKey_NormalizeToTheSameKey()
    {
        // The concrete double-charge vector: the same logical key resent padded must dedupe.
        var padded = ContextWithHeader("  same-key  ");
        var plain = ContextWithHeader("same-key");

        var filter = CreateFilter();
        filter.OnActionExecuting(padded);
        filter.OnActionExecuting(plain);

        Assert.Equal(plain.HttpContext.GetIdempotencyKey(), padded.HttpContext.GetIdempotencyKey());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void OnActionExecuting_WhitespaceOnlyKey_NormalizesToNull(string rawHeader)
    {
        var ctx = ContextWithHeader(rawHeader);

        CreateFilter().OnActionExecuting(ctx);

        Assert.Null(ctx.HttpContext.GetIdempotencyKey());
    }

    [Fact]
    public void OnActionExecuting_OverLengthKeyAfterTrim_ThrowsBadRequest()
    {
        // The length cap applies to the TRIMMED key: padding must not
        // let an otherwise-valid key slip past, nor an in-bounds key trip the cap on whitespace.
        var overLength = new string('k', IdempotencyKeyFilter.MaxKeyLength + 1);
        var ctx = ContextWithHeader($"  {overLength}  ");

        Assert.Throws<BadRequestException>(() => CreateFilter().OnActionExecuting(ctx));
    }
}
