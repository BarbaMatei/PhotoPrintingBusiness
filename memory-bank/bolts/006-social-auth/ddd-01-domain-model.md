---
stage: model
bolt: 006-social-auth
created: 2026-05-20T14:00:00Z
---

## Static Model: social-auth

### Entities

- **ExternalLogin**: `Id` (Guid), `UserId` (Guid FK→User), `Provider` (string "Google"), `ProviderKey` (string — Google `sub`), `CreatedAt` (DateTimeOffset) — Business Rules: one row per (UserId, Provider) pair; ProviderKey is stable even if user's Google email changes

- **User** *(extended from bolt-005)*: `PasswordHash` is nullable (null for Google-only accounts); `IsEmailConfirmed = true` always for Google-created users; `FirstName`/`LastName` sourced from Google `given_name`/`family_name`

### Value Objects

- **GooglePayload**: `Sub` (string), `Email` (string), `GivenName` (string), `FamilyName` (string), `Picture` (string?) — immutable carrier extracted from Google tokeninfo; equality by Sub

### Aggregates

- **User** (root): Members: ExternalLogin (zero-to-many, max one per Provider in v1) — Invariants: at most one ExternalLogin row per (UserId, Provider); PasswordHash may be null for OAuth-only accounts

### Domain Events

- **UserCreatedViaGoogle**: Trigger: new User row inserted via Google sign-in — Payload: UserId, Email, Provider ("Google"), ProviderKey
- **AccountLinked**: Trigger: ExternalLogin row added to existing email+password User — Payload: UserId, Provider, ProviderKey, AccountLinkedFirstTime=true

### Domain Services

- **IGoogleTokenValidator**: Operations: `ValidateAsync(idToken) → GooglePayload` — Dependencies: IHttpClientFactory (named "Google"), GoogleAuth:ClientId config — Throws UnauthorizedException (401) on invalid token, BadGatewayException (502) on unreachable endpoint
- **ISocialAuthService**: Operations: `GoogleSignInAsync(idToken, ipAddress, HttpResponse) → GoogleLoginResponse` — Dependencies: IGoogleTokenValidator, ITokenService, IPasswordHasher<User>, PhotoPrintDbContext

### Repository Interfaces

- **ExternalLogin** (EF DbSet): Methods: FindByProviderKeyAsync(provider, key) → ExternalLogin?, FindByUserIdAndProviderAsync(userId, provider) → ExternalLogin?

### Ubiquitous Language

- **id_token**: JWT issued by Google Identity Services; contains sub, email, given_name, family_name, picture, aud
- **ProviderKey**: Google's stable user identifier (sub claim); does not change if email changes
- **Account Linking**: Attaching a new ExternalLogin record to an existing email+password User; indicated by `accountLinked: true` in response
- **Upsert**: Find-or-create user logic: (1) find by ProviderKey → existing user, (2) find by email → link account, (3) neither → create new user
- **accountLinked**: Response boolean; true only when an existing email+password account was linked for the first time in this request
- **GoogleSignIn**: The full flow — validate token → upsert user → issue JWT + refresh cookie
