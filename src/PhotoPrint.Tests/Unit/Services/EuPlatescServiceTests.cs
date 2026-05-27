using PhotoPrint.API.Services;

namespace PhotoPrint.Tests.Unit.Services;

public class EuPlatescServiceTests
{
    // Known test key: 32 hex chars (16 bytes)
    private const string TestKey = "000102030405060708090a0b0c0d0e0f";

    // ── ComputeHmac ───────────────────────────────────────────────────────────

    [Fact]
    public void ComputeHmac_KnownInputs_ReturnsLowercaseHex()
    {
        var result = EuPlatescService.ComputeHmac(TestKey, "100.00");
        Assert.True(result.Length == 32, $"HMAC-MD5 should be 32 hex chars, got {result.Length}");
        Assert.Equal(result, result.ToLower());
    }

    [Fact]
    public void ComputeHmac_SameInputs_ReturnsSameValue()
    {
        var a = EuPlatescService.ComputeHmac(TestKey, "100.00", "RON", "order123");
        var b = EuPlatescService.ComputeHmac(TestKey, "100.00", "RON", "order123");
        Assert.Equal(a, b);
    }

    [Fact]
    public void ComputeHmac_DifferentFields_ReturnsDifferentValues()
    {
        var a = EuPlatescService.ComputeHmac(TestKey, "100.00", "RON");
        var b = EuPlatescService.ComputeHmac(TestKey, "200.00", "RON");
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void ComputeHmac_MessageIncludesFieldLength_DifferentFromNaiveConcat()
    {
        // "1" + "2" with length-prefix should differ from "12" alone
        var a = EuPlatescService.ComputeHmac(TestKey, "1", "2");
        var b = EuPlatescService.ComputeHmac(TestKey, "12");
        Assert.NotEqual(a, b);
    }

    // ── ValidateIpnSignature ─────────────────────────────────────────────────

    [Fact]
    public void ValidateIpnSignature_ValidSignature_ReturnsTrue()
    {
        // Compute the expected fp with the same fields and key
        var fields = new Dictionary<string, string>
        {
            ["amount"]      = "100.00",
            ["curr"]        = "RON",
            ["invoice_id"]  = "order-123",
            ["ep_id"]       = "EP999",
            ["merch_id"]    = "MerchXXX",
            ["action"]      = "0",
            ["message"]     = "Approved",
            ["approval"]    = "123456",
            ["timestamp"]   = "20260521120000",
            ["nonce"]       = "abcdef1234567890abcdef1234567890",
        };

        // Compute the correct fp
        var ipnOrder = new[] { "amount", "curr", "invoice_id", "ep_id", "merch_id", "action", "message", "approval", "timestamp", "nonce" };
        var values = ipnOrder.Select(k => fields[k]).ToArray();
        var fp = EuPlatescService.ComputeHmac(TestKey, values);
        fields["fp"] = fp;

        Assert.True(EuPlatescService.ValidateIpnSignature(fields, TestKey));
    }

    [Fact]
    public void ValidateIpnSignature_TamperedSignature_ReturnsFalse()
    {
        var fields = new Dictionary<string, string>
        {
            ["amount"] = "100.00", ["curr"] = "RON", ["invoice_id"] = "o1",
            ["ep_id"] = "e1", ["merch_id"] = "m1", ["action"] = "0",
            ["message"] = "OK", ["approval"] = "a1", ["timestamp"] = "ts1",
            ["nonce"] = "n1", ["fp"] = "deadbeefdeadbeefdeadbeefdeadbeef",
        };

        Assert.False(EuPlatescService.ValidateIpnSignature(fields, TestKey));
    }

    [Fact]
    public void ValidateIpnSignature_MissingFpField_ReturnsFalse()
    {
        var fields = new Dictionary<string, string>
        {
            ["amount"] = "100.00", ["curr"] = "RON",
        };

        Assert.False(EuPlatescService.ValidateIpnSignature(fields, TestKey));
    }

    // ── BuildIpnResponse ─────────────────────────────────────────────────────

    [Fact]
    public void BuildIpnResponse_ReturnsValidXmlFormat()
    {
        var response = EuPlatescService.BuildIpnResponse(TestKey);
        Assert.StartsWith("<epayment>", response);
        Assert.EndsWith("</epayment>", response);
        Assert.Contains("|", response);
    }

    [Fact]
    public void BuildIpnResponse_ContainsValidHmac()
    {
        var response = EuPlatescService.BuildIpnResponse(TestKey);
        // Extract date and hmac from <epayment>{date}|{hmac}</epayment>
        var inner = response.Replace("<epayment>", "").Replace("</epayment>", "");
        var parts = inner.Split('|');
        Assert.Equal(2, parts.Length);
        var date = parts[0];
        var hmac = parts[1];
        // Validate the hmac over the date
        var expectedHmac = EuPlatescService.ComputeHmac(TestKey, date);
        Assert.Equal(expectedHmac, hmac);
    }
}
