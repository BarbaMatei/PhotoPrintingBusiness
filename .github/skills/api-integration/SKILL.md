---
name: api-integration
description: API integration patterns for FotoTipar — connecting Angular frontend to ASP.NET Core backend, Stripe payments, Sameday shipping, Google OAuth, and SendGrid email. Use this skill when implementing API calls, webhooks, or third-party service integrations.
---

## Internal API (Frontend ↔ Backend)

### Angular HTTP Service Pattern

```typescript
@Injectable({ providedIn: 'root' })
export class OrderService {
  private readonly apiUrl = `${environment.apiUrl}/api/orders`;

  constructor(private http: HttpClient) {}

  getOrders(page: number, size: number): Observable<PagedResult<OrderSummary>> {
    return this.http.get<PagedResult<OrderSummary>>(this.apiUrl, {
      params: { page: page.toString(), size: size.toString() }
    });
  }
}
```

### Request/Response Conventions

- All requests: `Content-Type: application/json`
- File uploads: `multipart/form-data`
- Auth: `Authorization: Bearer <jwt>` or `X-Guest-Token: <token>` header
- Errors: `ProblemDetails` (RFC 7807) format
- Pagination: `{ items: T[], total: number, page: number, size: number }`
- Dates: ISO 8601 UTC strings

### Error Response Format

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Conflict",
  "status": 409,
  "detail": "Adresa de email este deja înregistrată",
  "correlationId": "abc-123"
}
```

### Validation Error Format (422)

```json
{
  "errors": [
    { "field": "email", "message": "Adresa de email nu este validă" },
    { "field": "password", "message": "Parola trebuie să aibă minim 8 caractere" }
  ]
}
```

## Stripe Integration

### Frontend (Angular)

- Load Stripe.js via `@stripe/stripe-js` package
- Use Stripe Elements for card input (PCI compliant — card data never touches our server)
- Flow: Create PaymentIntent on backend → Confirm on frontend → Webhook confirms payment

### Backend (ASP.NET Core)

- Package: `Stripe.net`
- Create `PaymentIntent` with amount in RON bani (1 RON = 100 bani)
- Return `clientSecret` to frontend
- Webhook: `POST /api/webhooks/stripe` — verify signature with `Stripe.WebhookSecret`
- Handle events: `payment_intent.succeeded`, `payment_intent.payment_failed`

### Security

- Stripe secret key: server-side only (environment variable)
- Webhook signature verification mandatory
- Idempotent webhook handling (check if order already marked paid)

## Sameday Shipping API

### Endpoints Used

- `GET /api/shipping/easybox-lockers?city={city}` — list nearby Easybox lockers
- Backend proxies Sameday API calls (API key server-side only)
- Cache locker list for 24 hours (lockers don't change frequently)

### Integration Pattern

```
Frontend → Our API → Sameday API
                   ← cached response
```

- Use `HttpClient` (C#) with `IHttpClientFactory` for Sameday API calls
- Retry with exponential backoff on transient errors
- Sameday sandbox for development, production URL for live

## Google OAuth

### Frontend

- Use Google Identity Services (GSI) button
- Receive `id_token` (JWT) from Google
- Send `id_token` to `POST /api/auth/google` on our backend

### Backend

- Verify Google `id_token` using Google's public keys
- Extract `email`, `name`, `sub` (Google ID) from token claims
- Create user if not exists, or link to existing user by email
- Return our own JWT + refresh token

## SendGrid Email (Production)

### Backend Integration

- Package: `SendGrid` NuGet
- `IEmailService` interface → `SendGridEmailService` implementation
- Use SendGrid API v3 with API key (environment variable)
- Render HTML email from Razor templates before sending
- Track `MessageId` for delivery status

### Email Types

| Email | Trigger | Template |
|-------|---------|----------|
| Welcome | User registration | `welcome.cshtml` |
| Order Confirmed | Payment received | `order-confirmed.cshtml` |
| Order Shipped | Status → Expediată | `order-shipped.cshtml` |
| Order Delivered | Status → Livrată | `order-delivered.cshtml` |
| Password Reset | Reset requested | `password-reset.cshtml` |

### Development

- Use MailHog (SMTP on localhost:1025, Web UI on localhost:8025)
- `IEmailService` → `SmtpEmailService` (MailKit) in development

## SignalR (Real-time Admin Notifications)

### Hub

```csharp
[Authorize(Roles = "Admin")]
public class AdminHub : Hub { }
```

### Backend → Frontend

- Broadcast `NewOrder` event when new order is placed
- Broadcast `OrderStatusChanged` event on status transitions
- Only admin users receive events (hub authorization)

### Frontend (Angular)

```typescript
this.hubConnection = new HubConnectionBuilder()
  .withUrl(`${environment.apiUrl}/hubs/admin`, {
    accessTokenFactory: () => this.authService.getToken()
  })
  .withAutomaticReconnect()
  .build();

this.hubConnection.on('NewOrder', (order) => { ... });
```

## API Timeout & Retry

- Default HTTP timeout: 30 seconds
- File upload timeout: 120 seconds
- Retry: 3 attempts with exponential backoff for 5xx and network errors
- Circuit breaker for external APIs (Sameday, Stripe) — fail fast after repeated errors
