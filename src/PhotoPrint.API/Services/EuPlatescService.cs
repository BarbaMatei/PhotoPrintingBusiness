using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;
using PhotoPrint.API.Models;

namespace PhotoPrint.API.Services;

public class EuPlatescService : IEuPlatescService
{
    private readonly EuPlatescSettings _settings;
    private readonly IHttpClientFactory _httpClientFactory;

    public EuPlatescService(IOptions<EuPlatescSettings> settings, IHttpClientFactory httpClientFactory)
    {
        _settings = settings.Value;
        _httpClientFactory = httpClientFactory;
    }

    // ── Initiation ────────────────────────────────────────────────────────────

    public string BuildInitiateUrl(Order order)
    {
        var amount = order.TotalRon.ToString("F2", CultureInfo.InvariantCulture);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var nonce = GenerateNonce();

        var fp = ComputeHmac(
            _settings.SecretKey,
            amount,
            "RON",
            order.Id.ToString(),
            $"FotoTipar comanda {order.OrderNumber}",
            _settings.MerchantId,
            timestamp,
            nonce);

        var query = HttpUtility.ParseQueryString(string.Empty);
        query["amount"]     = amount;
        query["curr"]       = "RON";
        query["invoice_id"] = order.Id.ToString();
        query["order_desc"] = $"FotoTipar comanda {order.OrderNumber}";
        query["merch_id"]   = _settings.MerchantId;
        query["timestamp"]  = timestamp;
        query["nonce"]      = nonce;
        query["fp"]         = fp;

        return $"{_settings.GatewayUrl}?{query}";
    }

    // ── HMAC algorithm (EuPlatesc v3 spec) ────────────────────────────────────

    /// <summary>
    /// Computes HMAC-MD5 using the EuPlatesc v3 algorithm:
    /// message = concat of (strlen(field) + field) for each field.
    /// The key is passed as a lowercase hex string (as stored in settings).
    /// </summary>
    public static string ComputeHmac(string hexKey, params string[] fields)
    {
        var keyBytes = Convert.FromHexString(hexKey);
        var message = string.Concat(fields.Select(f => $"{f.Length}{f}"));
        var dataBytes = Encoding.UTF8.GetBytes(message);
        using var hmac = new HMACMD5(keyBytes);
        return Convert.ToHexString(hmac.ComputeHash(dataBytes)).ToLower();
    }

    // ── IPN validation ────────────────────────────────────────────────────────

    /// <summary>
    /// Validates the fingerprint field sent by EuPlatesc in an IPN callback.
    /// The fingerprint is computed over: amount, curr, invoice_id, ep_id,
    /// merch_id, action, message, approval, timestamp, nonce (in that order).
    /// </summary>
    public static bool ValidateIpnSignature(
        IReadOnlyDictionary<string, string> fields,
        string hexKey)
    {
        if (!fields.TryGetValue("fp", out var receivedFp))
            return false;

        var ordered = new[]
        {
            "amount", "curr", "invoice_id", "ep_id",
            "merch_id", "action", "message", "approval",
            "timestamp", "nonce",
        };

        var values = ordered.Select(k => fields.GetValueOrDefault(k, "")).ToArray();
        var computed = ComputeHmac(hexKey, values);
        return string.Equals(computed, receivedFp, StringComparison.OrdinalIgnoreCase);
    }

    // ── IPN response ──────────────────────────────────────────────────────────

    /// <summary>
    /// Builds the XML acknowledgment response required by EuPlatesc:
    /// <c>&lt;epayment&gt;{date}|{hmac}&lt;/epayment&gt;</c>
    /// </summary>
    public static string BuildIpnResponse(string hexKey)
    {
        var date = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var hmac = ComputeHmac(hexKey, date);
        return $"<epayment>{date}|{hmac}</epayment>";
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string GenerateNonce()
    {
        var bytes = RandomNumberGenerator.GetBytes(16);
        return Convert.ToHexString(bytes).ToLower();
    }

    // ── Refund (storno) ───────────────────────────────────────────────────────

    public async Task RefundAsync(string transactionId, decimal amount, CancellationToken ct = default)
    {
        var amountStr = amount.ToString("F2", CultureInfo.InvariantCulture);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var nonce = GenerateNonce();

        var fp = ComputeHmac(
            _settings.SecretKey,
            transactionId, amountStr, "RON",
            _settings.MerchantId, timestamp, nonce);

        var fields = new Dictionary<string, string>
        {
            ["ExId"]     = transactionId,
            ["amount"]   = amountStr,
            ["curr"]     = "RON",
            ["MerchId"]  = _settings.MerchantId,
            ["timestamp"]= timestamp,
            ["nonce"]    = nonce,
            ["fp"]       = fp,
        };

        using var client = _httpClientFactory.CreateClient();
        using var content = new FormUrlEncodedContent(fields);
        var response = await client.PostAsync(_settings.GatewayUrl, content, ct);
        response.EnsureSuccessStatusCode();
    }
}
