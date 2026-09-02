# US-802 — Security Baselines (Backend)

## Story
**As a** system  
**I want to** meet minimum security requirements for a production e-commerce platform

## Type
BACKEND — ASP.NET Core

## Epic
EPIC-8 | Platformă & Non-Funcționale

## Dependencies
- US-801 (Error handling — uses middleware pipeline)

## Acceptance Criteria

1. **HTTPS enforced**; HSTS header (`max-age=31536000`, `includeSubDomains`)
2. **CORS**: exact FE origin whitelist only; no wildcard in production
3. **Rate limiting** (ASP.NET Core Rate Limiting middleware): 100 req/min per IP public; 10/min auth endpoints
4. **File upload**: UUID-named files, no path traversal possible; served with `Content-Disposition: attachment`
5. **All secrets** in environment variables / .NET Secret Manager — never in `appsettings` committed to git
6. **EF Core** parameterised queries by default (no raw SQL with user input)
7. **Security headers**: `X-Content-Type-Options: nosniff`; `X-Frame-Options: DENY` on all responses

## Technical Notes

### Implementation Details

#### HTTPS & HSTS
```csharp
app.UseHttpsRedirection();
app.UseHsts(); // in production
```

#### CORS
```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(builder.Configuration["AllowedOrigins"])
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // needed for cookies
    });
});
```

#### Rate Limiting
```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("public", opt => { opt.Window = TimeSpan.FromMinutes(1); opt.PermitLimit = 100; });
    options.AddFixedWindowLimiter("auth", opt => { opt.Window = TimeSpan.FromMinutes(1); opt.PermitLimit = 10; });
});
```

#### Security Headers Middleware
- Add `X-Content-Type-Options: nosniff` to all responses
- Add `X-Frame-Options: DENY` to all responses
- Add `Referrer-Policy: strict-origin-when-cross-origin`
- Add `Content-Security-Policy` for admin pages

#### Secret Management
- Development: .NET Secret Manager (`dotnet user-secrets`)
- Production: environment variables
- Never commit: Stripe keys, Google OAuth credentials, DB connection string, JWT signing key, email credentials

## Files to Create/Modify
- `src/PhotoPrint.API/Middleware/SecurityHeadersMiddleware.cs`
- `Program.cs` (CORS, rate limiting, HSTS, HTTPS redirect)
- `appsettings.json` (non-secret config only)
- `.gitignore` (ensure secrets excluded)

## Testing
- Integration test: CORS rejects unauthorized origin
- Integration test: rate limiting enforced
- Integration test: security headers present on responses
- Integration test: HTTPS redirect works
- Security audit: verify no secrets in committed config files
