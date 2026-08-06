using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using PhotoPrint.API.Configuration;
using PhotoPrint.Tests.Helpers;
using Sentry;
using Sentry.AspNetCore;
using Sentry.Protocol;

namespace PhotoPrint.Tests.Unit.Configuration;

public class SentryDataScrubbersTests
{
    private const string GuestToken = "guest-token-placeholder";
    private const string CustomerEmail = "ion.popescu@gmail.com";

    [Fact]
    public void Scrub_replaces_request_body_with_marker()
    {
        var e = new SentryEvent { Request = new SentryRequest { Data = "raw body with PII" } };

        var result = SentryDataScrubbers.Scrub(e);

        result.Should().NotBeNull();
        result!.Request!.Data!.ToString().Should().Be(SentryDataScrubbers.ScrubbedBodyMarker);
    }

    [Fact]
    public void Scrub_redacts_query_string_values_and_keeps_parameter_names()
    {
        var e = new SentryEvent
        {
            Request = new SentryRequest { QueryString = "?search=ion.popescu%40gmail.com&token=abc123&page=2" },
        };

        SentryDataScrubbers.Scrub(e);

        e.Request!.QueryString.Should().Be(
            $"?search={SentryDataScrubbers.ScrubbedMarker}" +
            $"&token={SentryDataScrubbers.ScrubbedMarker}" +
            $"&page={SentryDataScrubbers.ScrubbedMarker}");
    }

    [Theory]
    [InlineData("?ion.popescu%40gmail.com")]
    [InlineData("?ion.popescu@gmail.com=1")]
    public void Scrub_redacts_query_segments_that_are_not_plain_parameter_names(string queryString)
    {
        var e = new SentryEvent { Request = new SentryRequest { QueryString = queryString } };

        SentryDataScrubbers.Scrub(e);

        e.Request!.QueryString.Should().Be($"?{SentryDataScrubbers.ScrubbedMarker}");
    }

    [Fact]
    public void Scrub_strips_query_fragment_and_credentials_from_the_url()
    {
        var e = new SentryEvent
        {
            Request = new SentryRequest { Url = "https://user:pw@fototipar.ro/api/orders?email=x%40y.ro#frag" },
        };

        SentryDataScrubbers.Scrub(e);

        e.Request!.Url.Should().Be($"https://{SentryDataScrubbers.ScrubbedMarker}@fototipar.ro/api/orders");
    }

    [Theory]
    [InlineData("Authorization")]
    [InlineData("Cookie")]
    [InlineData("Set-Cookie")]
    [InlineData("X-Guest-Token")]
    [InlineData("x-guest-token")]
    [InlineData("cookie")]
    [InlineData("authorization")]
    [InlineData("Referer")]
    [InlineData("X-Api-Key")]
    public void Scrub_redacts_headers_outside_the_allow_list(string header)
    {
        var e = new SentryEvent { Request = new SentryRequest() };
        e.Request!.Headers[header] = "secret-value-xyz";

        SentryDataScrubbers.Scrub(e);

        e.Request.Headers[header].Should().Be(SentryDataScrubbers.ScrubbedMarker);
    }

    [Fact]
    public void Scrub_keeps_allow_listed_headers_regardless_of_case()
    {
        var e = new SentryEvent { Request = new SentryRequest() };
        e.Request!.Headers["User-Agent"] = "Mozilla/5.0";
        e.Request.Headers["x-correlation-id"] = "abc-123";
        e.Request.Headers["Content-Type"] = "application/json";

        SentryDataScrubbers.Scrub(e);

        e.Request.Headers["User-Agent"].Should().Be("Mozilla/5.0");
        e.Request.Headers["x-correlation-id"].Should().Be("abc-123");
        e.Request.Headers["Content-Type"].Should().Be("application/json");
    }

    [Fact]
    public void Scrub_redacts_request_env_outside_the_allow_list()
    {
        var e = new SentryEvent { Request = new SentryRequest() };
        e.Request!.Env["REMOTE_ADDR"] = "203.0.113.9";
        e.Request.Env["SERVER_NAME"] = "api-1";

        SentryDataScrubbers.Scrub(e);

        e.Request.Env["REMOTE_ADDR"].Should().Be(SentryDataScrubbers.ScrubbedMarker);
        e.Request.Env["SERVER_NAME"].Should().Be("api-1");
    }

    [Fact]
    public void Scrub_replaces_cookies_with_the_marker()
    {
        var e = new SentryEvent { Request = new SentryRequest { Cookies = "session=deadbeef" } };

        SentryDataScrubbers.Scrub(e);

        e.Request!.Cookies.Should().Be(SentryDataScrubbers.ScrubbedMarker);
    }

    [Theory]
    [InlineData("email")]
    [InlineData("phone")]
    [InlineData("newPassword")]
    [InlineData("orderId")]
    [InlineData("anything-at-all")]
    public void Scrub_redacts_every_extra_value(string key)
    {
        var e = new SentryEvent();
        e.SetExtra(key, "sensitive-value");

        SentryDataScrubbers.Scrub(e);

        e.Extra[key].Should().Be(SentryDataScrubbers.ScrubbedMarker);
    }

    [Fact]
    public void Scrub_keeps_the_user_id_and_drops_every_other_user_field()
    {
        var e = new SentryEvent
        {
            User = new SentryUser
            {
                Id = "42",
                Email = CustomerEmail,
                Username = "ion.popescu",
                IpAddress = "203.0.113.9",
            },
        };
        e.User.Other["nickname"] = "ionut";

        SentryDataScrubbers.Scrub(e);

        e.User.Id.Should().Be("42");
        e.User.Email.Should().BeNull();
        e.User.Username.Should().BeNull();
        e.User.IpAddress.Should().BeNull();
        e.User.Other.Should().BeEmpty();
    }

    [Fact]
    public void Scrub_keeps_tags_so_events_stay_joinable_to_the_logs()
    {
        var e = new SentryEvent();
        e.SetTag("correlation_id", "abc-123");
        e.SetTag("user_id", "42");

        SentryDataScrubbers.Scrub(e);

        e.Tags["correlation_id"].Should().Be("abc-123");
        e.Tags["user_id"].Should().Be("42");
    }

    [Fact]
    public void Scrub_redacts_the_response_context()
    {
        var e = new SentryEvent();
        var response = new Response { StatusCode = 500, Data = "body with PII", Cookies = "session=deadbeef" };
        response.Headers["Set-Cookie"] = "session=deadbeef";
        response.Headers["Content-Type"] = "application/json";
        e.Contexts[Response.Type] = response;

        SentryDataScrubbers.Scrub(e);

        response.Data.Should().Be(SentryDataScrubbers.ScrubbedBodyMarker);
        response.Cookies.Should().Be(SentryDataScrubbers.ScrubbedMarker);
        response.Headers["Set-Cookie"].Should().Be(SentryDataScrubbers.ScrubbedMarker);
        response.Headers["Content-Type"].Should().Be("application/json");
        response.StatusCode.Should().Be(500);
    }

    [Fact]
    public void Scrub_redacts_exception_mechanism_data()
    {
        var mechanism = new Mechanism();
        mechanism.Data["CustomerEmail"] = CustomerEmail;
        var e = new SentryEvent
        {
            SentryExceptions = new[]
            {
                new SentryException { Type = "InvalidOperationException", Value = "boom", Mechanism = mechanism },
            },
        };

        SentryDataScrubbers.Scrub(e);

        mechanism.Data["CustomerEmail"].Should().Be(SentryDataScrubbers.ScrubbedMarker);
        e.SentryExceptions!.Single().Value.Should().Be("boom");
    }

    [Fact]
    public void Scrub_drops_message_params_and_the_rendered_text()
    {
        var e = new SentryEvent
        {
            Message = new SentryMessage
            {
                Message = "Order for {Email} failed",
                Formatted = $"Order for {CustomerEmail} failed",
                Params = new object[] { CustomerEmail },
            },
        };

        SentryDataScrubbers.Scrub(e);

        e.Message!.Message.Should().Be("Order for {Email} failed");
        e.Message.Formatted.Should().BeNull();
        e.Message.Params.Should().BeNull();
    }

    [Fact]
    public void Scrub_handles_event_without_request_object()
    {
        var e = new SentryEvent();
        e.SetExtra("password", "secret");

        var result = SentryDataScrubbers.Scrub(e);

        result.Should().NotBeNull();
        result!.Extra["password"].Should().Be(SentryDataScrubbers.ScrubbedMarker);
    }

    [Fact]
    public void Scrub_drops_the_payload_when_scrubbing_itself_fails()
    {
        SentryDataScrubbers.Scrub((SentryEvent)null!).Should().BeNull();
        SentryDataScrubbers.Scrub((SentryTransaction)null!).Should().BeNull();
        SentryDataScrubbers.Scrub((Breadcrumb)null!).Should().BeNull();
    }

    [Theory]
    [InlineData("?", "?")]
    [InlineData("?&&", "?&&")]
    [InlineData("page=2", "page=<scrubbed>")]
    public void Scrub_handles_degenerate_query_strings(string queryString, string expected)
    {
        var e = new SentryEvent { Request = new SentryRequest { QueryString = queryString } };

        SentryDataScrubbers.Scrub(e);

        e.Request!.QueryString.Should().Be(expected);
    }

    [Fact]
    public void Scrub_redacts_a_schemeless_url_value_that_carries_an_address()
    {
        var breadcrumb = new Breadcrumb(
            message: null!,
            type: "http",
            data: new Dictionary<string, string> { ["url"] = $"mailto:{CustomerEmail}" });

        var result = SentryDataScrubbers.Scrub(breadcrumb);

        result!.Data!["url"].Should().Be(SentryDataScrubbers.ScrubbedMarker);
    }

    [Fact]
    public void Scrub_redacts_an_event_populated_by_the_aspnetcore_sdk()
    {
        var options = new SentryAspNetCoreOptions { SendDefaultPii = false };
        var scope = new Scope(options);
        scope.Populate(BuildHttpContext(), options);

        var e = new SentryEvent();
        scope.Apply(e);

        SentryDataScrubbers.Scrub(e);

        e.Request!.QueryString.Should().NotContain("ion.popescu").And.NotContain("abc123");
        e.Request.Headers["x-guest-token"].Should().Be(SentryDataScrubbers.ScrubbedMarker);
        e.Request.Headers["cookie"].Should().Be(SentryDataScrubbers.ScrubbedMarker);
        e.Request.Headers["Referer"].Should().Be(SentryDataScrubbers.ScrubbedMarker);
        e.Request.Headers["User-Agent"].Should().Be("Mozilla/5.0");
        e.Request.Url.Should().Be("https://fototipar.ro/api/admin/orders");
    }

    [Fact]
    public void Scrub_redacts_a_transaction_request_and_its_spans()
    {
        var transaction = BuildTransaction();

        var result = SentryDataScrubbers.Scrub(transaction);

        result.Should().NotBeNull();
        result!.Request!.Headers["x-guest-token"].Should().Be(SentryDataScrubbers.ScrubbedMarker);
        result.Request.QueryString.Should().NotContain("ion.popescu");

        var span = result.Spans.Single();
        span.Description.Should().Be("GET https://oauth2.googleapis.com/tokeninfo");
        span.Tags["url"].Should().Be("https://oauth2.googleapis.com/tokeninfo");
        span.Tags["http.request.method"].Should().Be("GET");
        span.Tags["db.statement"].Should().Be(SentryDataScrubbers.ScrubbedMarker);
    }

    [Fact]
    public void Scrub_strips_the_query_string_from_a_breadcrumb_url()
    {
        var breadcrumb = new Breadcrumb(
            message: null!,
            type: "http",
            data: new Dictionary<string, string>
            {
                ["url"] = "https://oauth2.googleapis.com/tokeninfo?id_token=LIVE-GOOGLE-ID-TOKEN",
                ["method"] = "GET",
                ["status_code"] = "400",
                ["payload"] = CustomerEmail,
            },
            category: "http");

        var result = SentryDataScrubbers.Scrub(breadcrumb);

        result.Should().NotBeNull();
        result!.Data!["url"].Should().Be("https://oauth2.googleapis.com/tokeninfo");
        result.Data["method"].Should().Be("GET");
        result.Data["status_code"].Should().Be("400");
        result.Data["payload"].Should().Be(SentryDataScrubbers.ScrubbedMarker);
    }

    [Fact]
    public async Task Register_scrubs_both_events_and_transactions_before_they_leave_the_sdk()
    {
        var transport = new CapturingSentryTransport();
        var options = new SentryOptions
        {
            Dsn = "https://key@o0.ingest.sentry.io/0",
            Transport = transport,
            AutoSessionTracking = false,
        };
        SentryDataScrubbers.Register(options);

        using (var client = new SentryClient(options))
        {
            var e = new SentryEvent(new InvalidOperationException("boom"))
            {
                Request = new SentryRequest { QueryString = $"?search={CustomerEmail}" },
            };
            e.Request.Headers["x-guest-token"] = GuestToken;
            client.CaptureEvent(e);

            client.CaptureTransaction(BuildTransaction());
            await client.FlushAsync(TimeSpan.FromSeconds(10));
        }

        transport.Payloads.Should().HaveCount(2);
        transport.Payloads.Should().OnlyContain(payload =>
            !payload.Contains(GuestToken)
            && !payload.Contains(CustomerEmail)
            && !payload.Contains("LIVE-GOOGLE-ID-TOKEN"));
    }

    private static DefaultHttpContext BuildHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("fototipar.ro");
        context.Request.Path = "/api/admin/orders";
        context.Request.QueryString = new QueryString("?search=ion.popescu%40gmail.com&token=abc123");
        context.Request.Headers["x-guest-token"] = GuestToken;
        context.Request.Headers["cookie"] = "session=deadbeef";
        context.Request.Headers["Referer"] = "https://fototipar.ro/confirm?token=secret";
        context.Request.Headers["User-Agent"] = "Mozilla/5.0";
        return context;
    }

    private static SentryTransaction BuildTransaction()
    {
        const string TraceId = "75302ac48a024bde9a3b3734a82e36c8";
        var json = $$"""
        {
          "type": "transaction",
          "event_id": "{{TraceId}}",
          "transaction": "GET /api/auth/google",
          "start_timestamp": "2026-07-31T10:00:00Z",
          "timestamp": "2026-07-31T10:00:01Z",
          "contexts": { "trace": { "op": "http.server", "span_id": "1000000000000000", "trace_id": "{{TraceId}}" } },
          "request": {
            "url": "https://fototipar.ro/api/auth/google",
            "query_string": "?search=ion.popescu%40gmail.com",
            "headers": { "x-guest-token": "{{GuestToken}}", "User-Agent": "Mozilla/5.0" }
          },
          "spans": [
            {
              "op": "http.client",
              "description": "GET https://oauth2.googleapis.com/tokeninfo?id_token=LIVE-GOOGLE-ID-TOKEN",
              "span_id": "2000000000000000",
              "parent_span_id": "1000000000000000",
              "trace_id": "{{TraceId}}",
              "start_timestamp": "2026-07-31T10:00:00Z",
              "timestamp": "2026-07-31T10:00:01Z",
              "tags": {
                "url": "https://oauth2.googleapis.com/tokeninfo?id_token=LIVE-GOOGLE-ID-TOKEN",
                "http.request.method": "GET",
                "db.statement": "SELECT * FROM Users WHERE Email = 'ion.popescu@gmail.com'"
              }
            }
          ]
        }
        """;

        using var document = JsonDocument.Parse(json);
        return SentryTransaction.FromJson(document.RootElement);
    }
}
