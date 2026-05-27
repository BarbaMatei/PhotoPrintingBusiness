---
stage: technical-design
bolt: 018-orders-api
created: 2026-05-22T07:30:00Z
---

## Technical Design: 001-orders-api

### Architecture Pattern

**Pattern**: Thin Controller → Service → EF Core (existing project pattern)

The project uses a type-based layout with thin controllers delegating to `IService` + `Service` pairs, querying EF Core directly via `IApplicationDbContext`. No CQRS or repository abstraction is introduced — consistent with all prior bolts.

### Layer Structure

```text
┌─────────────────────────────────────────────┐
│  Presentation   OrdersController             │  ← Route, auth, HTTP shape
├─────────────────────────────────────────────┤
│  Application    IOrderService / OrderService │  ← Query logic, ownership check
├─────────────────────────────────────────────┤
│  Domain         Order, OrderItem, Upload     │  ← Existing entities (read-only here)
├─────────────────────────────────────────────┤
│  Infrastructure ApplicationDbContext (EF)   │  ← Existing — no new migrations
└─────────────────────────────────────────────┘
```

---

### New Files

| File | Location | Purpose |
|------|----------|---------|
| `OrdersController.cs` | `Controllers/` | `GET /api/orders`, `GET /api/orders/{id}` |
| `IOrderService.cs` | `Services/` | Service contract |
| `OrderService.cs` | `Services/` | EF Core query implementation |
| `OrderSummaryDto.cs` | `DTOs/` | List-item response shape |
| `OrderDetailDto.cs` | `DTOs/` | Full detail response shape |
| `OrderItemDto.cs` | `DTOs/` | Line item within detail |
| `ShippingAddressDto.cs` | `DTOs/` | Embedded delivery address |
| `GetOrdersQueryValidator.cs` | `Validators/` | FluentValidation for page/pageSize |
| `OrdersControllerTests.cs` | `Tests/Integration/` | Integration tests |

---

### API Design

#### `GET /api/orders`

```
Authorization: Bearer {jwt}
Query params: page (int, default 1, min 1), pageSize (int, default 10, min 1, max 50)

Response 200:
{
  "items": [ OrderSummaryDto ],
  "total": 42,
  "page": 1,
  "size": 10
}

Headers: X-Total-Count: 42

Response 401: (no/invalid JWT)
Response 400: (page < 1 or pageSize > 50) — ProblemDetails
```

> Note: Following the project's API convention for paginated collections: `{ items, total, page, size }` response body + `X-Total-Count` header.

#### `GET /api/orders/{id}`

```
Authorization: Bearer {jwt}
Route param: id (Guid)

Response 200: OrderDetailDto

Response 401: no/invalid JWT
Response 403: order exists but belongs to a different user
Response 404: order not found
Response 400: id is not a valid GUID
```

---

### DTO Shapes (C#)

```csharp
// DTOs/OrderSummaryDto.cs
public record OrderSummaryDto(
    Guid Id,
    string OrderNumber,
    string Status,
    decimal TotalRon,
    DateTime CreatedAt,
    string DeliveryType,
    int ItemCount);

// DTOs/OrderItemDto.cs
public record OrderItemDto(
    Guid UploadId,
    string? PreviewUrl,
    string ProductName,
    string FinishName,
    int Quantity,
    decimal UnitPriceRon,
    decimal LineTotal);

// DTOs/ShippingAddressDto.cs
public record ShippingAddressDto(
    string RecipientName,
    string Street,
    string StreetNumber,
    string? Block,
    string City,
    string County,
    string PostalCode,
    string Phone);

// DTOs/OrderDetailDto.cs
public record OrderDetailDto(
    Guid Id,
    string OrderNumber,
    string Status,
    decimal SubtotalRon,
    decimal ShippingCostRon,
    decimal TotalRon,
    DateTime CreatedAt,
    DateTime? PaidAt,
    string DeliveryType,
    string PaymentProcessor,
    string? LockerId,
    string? LockerName,
    ShippingAddressDto? ShippingAddress,
    IReadOnlyList<OrderItemDto> Items);
```

---

### Service Interface

```csharp
// Services/IOrderService.cs
public interface IOrderService
{
    Task<(IReadOnlyList<OrderSummaryDto> Items, int Total)> GetOrdersAsync(
        Guid userId, int page, int pageSize, CancellationToken ct);

    Task<OrderDetailDto> GetOrderDetailAsync(
        Guid orderId, Guid userId, CancellationToken ct);
}
```

---

### Service Implementation Design

**`GetOrdersAsync`:**
```
1. Query: Orders WHERE UserId = userId ORDER BY CreatedAt DESC
2. Total: CountAsync (same filter, no skip/take)
3. Page slice: Skip((page-1)*pageSize).Take(pageSize)
4. Project to OrderSummaryDto:
   - ItemCount = o.Items.Sum(i => i.Quantity)   [use .Include(o => o.Items)]
5. Return (items, total)
```

**`GetOrderDetailAsync`:**
```
1. Query: Orders.Include(Items).FirstOrDefaultAsync(o => o.Id == orderId)
2. If null → throw NotFoundException("Order not found")
3. If order.UserId != userId → throw ForbiddenException("Access denied")
4. Project items:
   - JOIN Upload on UploadId → get PreviewUrl
   - JOIN Product on ProductId → get ProductName
   - JOIN ProductFinish (via CartItem/OrderItem) → get FinishName
     [Note: if FinishName is not on OrderItem, store it at order creation time or
      join via Product → ProductFormats → ProductFinish]
5. Return OrderDetailDto
```

> **Design decision for FinishName**: Store `ProductName` and `FinishName` as denormalized string columns on `OrderItem` at the time of order creation (set by payment flow in bolt 015). This avoids a complex join and makes historical orders immune to catalog changes. Check if bolt 015 already stores these; if not, add columns.

---

### Data Persistence

**No new migrations required** if `ProductName`/`FinishName` are already denormalized on `OrderItem`.

**Check needed in Stage 4**: Inspect `OrderItem` entity — if `ProductName`/`FinishName` are missing, add migration to add nullable columns (set during order creation in payment flow).

**EF Core query for detail:**
```csharp
_db.Orders
   .Include(o => o.Items)
     .ThenInclude(i => i.Upload)
   .FirstOrDefaultAsync(o => o.Id == orderId, ct)
```

---

### Controller Design

```csharp
[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    // GET /api/orders?page=1&pageSize=10
    [HttpGet]
    public async Task<IActionResult> GetOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        // Validate via FluentValidation or inline
        // Get userId from JWT claim
        // Call _orderService.GetOrdersAsync
        // Set X-Total-Count header
        // Return Ok(new { items, total, page, size })
    }

    // GET /api/orders/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetOrderDetail(
        Guid id,
        CancellationToken ct = default)
    {
        // Get userId from JWT claim
        // Call _orderService.GetOrderDetailAsync
        // NotFoundException → 404, ForbiddenException → 403
        // Return Ok(dto)
    }
}
```

---

### Security Design

| Concern | Approach |
|---------|---------|
| Authentication | `[Authorize]` on controller class — requires valid Bearer JWT |
| Ownership enforcement | Service compares `Order.UserId` with JWT `sub` claim; throws `ForbiddenException` → 403 |
| Input validation | FluentValidation: `page ≥ 1`, `1 ≤ pageSize ≤ 50` |
| GUID validation | Route constraint `{id:guid}` returns 400 for malformed IDs |
| No sensitive data leak | 403 returned (not 404) when order exists but belongs to another user |

---

### NFR Implementation

| Requirement | Design Approach |
|-------------|----------------|
| < 300 ms p95 | EF Core query with `.Include` + indexed `UserId` + indexed `CreatedAt DESC`; no N+1 |
| Default pageSize=10, max=50 | Validated in FluentValidation; enforced in service |
| Consistent error format | Existing `GlobalExceptionMiddleware` maps `NotFoundException→404`, `ForbiddenException→403` |

---

### Integration Points

| Integration | Direction | Notes |
|-------------|-----------|-------|
| `ApplicationDbContext` | Inbound | Existing EF Core context — inject via constructor |
| `ICurrentUserService` / JWT claims | Inbound | Extract `userId` from `User.FindFirstValue(ClaimTypes.NameIdentifier)` |
| Existing `NotFoundException` | Internal | Throw from service → caught by global middleware |
| Existing `ForbiddenException` | Internal | Throw from service → caught by global middleware |

---

### Service Registration

```csharp
// In Program.cs (existing services section)
builder.Services.AddScoped<IOrderService, OrderService>();
```

---

### Stories → Implementation Mapping

| Story | Controller Method | Service Method |
|-------|------------------|---------------|
| 001-orders-list-endpoint | `GET /api/orders` | `GetOrdersAsync` |
| 002-order-detail-endpoint | `GET /api/orders/{id}` | `GetOrderDetailAsync` |
