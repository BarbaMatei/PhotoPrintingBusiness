---
id: 001-sameday-settings-and-typed-client
unit: 001-sameday-api-client
intent: 015-sameday-shipping-integration
status: draft
priority: must
created: 2026-05-25T10:10:00Z
assigned_bolt: 036-sameday-api-client
implemented: false
---

# Story: 001-sameday-settings-and-typed-client

## User Story

**As** a backend developer
**I want** a typed HTTP client for Sameday with rate-limit and retry policies
**So that** all Sameday calls share consistent timeouts, headers, and failure handling

## Acceptance Criteria

- [ ] `SamedaySettings` binds from `Sameday:` section with fields `Enabled`, `BaseUrl`, `Username`, `Password`, `PickupPointId`, `RequestTimeoutSeconds` (default 10).
- [ ] `services.AddHttpClient<SamedayClient>` registers the typed client with `BaseUrl`, request timeout, and Polly handlers: rate-limit (5 req/s ceiling) + retry (3 attempts, exponential 1 / 4 / 16 s, retry only 5xx + 408 + 429).
- [ ] `services.AddSingleton<IShippingService, SamedayShippingService>` is conditional on `Sameday:Enabled`.
- [ ] No log statement ever emits `Username` or `Password` plaintext.
- [ ] Health check `/health` includes a passive Sameday liveness probe only when `Sameday:Enabled` (no extra call in normal operation; just a flag).

## Technical Notes

```csharp
// Configuration/SamedaySettings.cs
public sealed class SamedaySettings
{
    public const string SectionName = "Sameday";
    public bool   Enabled                { get; init; }
    public string BaseUrl                { get; init; } = "https://api.sameday.ro";
    public string Username               { get; init; } = string.Empty;
    public string Password               { get; init; } = string.Empty;
    public string PickupPointId          { get; init; } = string.Empty;
    public int    RequestTimeoutSeconds  { get; init; } = 10;
}
```

```csharp
// Program.cs (excerpt)
services.AddHttpClient<SamedayClient>((sp, c) =>
{
    var s = sp.GetRequiredService<IOptions<SamedaySettings>>().Value;
    c.BaseAddress = new Uri(s.BaseUrl);
    c.Timeout     = TimeSpan.FromSeconds(s.RequestTimeoutSeconds);
})
.AddPolicyHandler(SamedayPolicies.RateLimit(5))
.AddPolicyHandler(SamedayPolicies.Retry(3));
```

## Dependencies

### Requires
- 003-sameday-schema-additions (downstream tests need the new columns)

### Enables
- 002-token-auth-and-refresh

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| `Enabled=true` but missing credentials | `ValidateOnStart` fails boot |
| `BaseUrl` unreachable | Polly retries then throws `SamedayUnreachableException` |

## Out of Scope

- Caching response bodies (Sameday responses are not cacheable in our flow).
