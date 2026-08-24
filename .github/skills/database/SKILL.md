---
name: database
description: Database design, EF Core migrations, PostgreSQL schema conventions, and data modeling for FotoTipar. Use this skill when creating or modifying entities, writing migrations, designing queries, or working with the database schema.
---

## Tech Stack

- **PostgreSQL 16** (production + development via Docker)
- **Entity Framework Core 8** (Code-First)
- **Provider**: `Npgsql.EntityFrameworkCore.PostgreSQL`

## Schema Conventions

### General Rules

- All table names: `PascalCase` (EF Core default from entity names)
- All column names: `PascalCase` (EF Core default from property names)
- All primary keys: `Id` column of type `Guid` (UUID v4)
- All entities include: `CreatedAt` (DateTimeOffset), `UpdatedAt` (DateTimeOffset, nullable)
- Soft deletes where applicable: `DeletedAt` (DateTimeOffset, nullable)
- No auto-increment integer IDs — UUID only

### Naming Patterns

- Foreign keys: `EntityNameId` (e.g., `UserId`, `OrderId`)
- Navigation properties: singular for 1:1, collection for 1:N (e.g., `User`, `OrderItems`)
- Junction tables: `Entity1Entity2` or descriptive name (e.g., `OrderItem`)
- Indexes: `IX_TableName_ColumnName`

## Core Entities

```
Users
  - Id (Guid PK)
  - Email (varchar 320, unique, not null)
  - PasswordHash (varchar 500, nullable — null for Google OAuth users)
  - FullName (varchar 200, not null)
  - Phone (varchar 20, nullable)
  - Role (varchar 20, default 'Customer')
  - GoogleId (varchar 100, nullable, unique)
  - GdprConsentAt (DateTimeOffset, not null)
  - EmailConfirmedAt (DateTimeOffset, nullable)
  - CreatedAt, UpdatedAt

GuestSessions
  - Id (Guid PK)
  - Token (varchar 64, unique, indexed)
  - TokenHash (varchar 128, unique, indexed)
  - ExpiresAt (DateTimeOffset)
  - ConvertedToUserId (Guid FK, nullable)
  - CreatedAt

RefreshTokens
  - Id (Guid PK)
  - UserId (Guid FK)
  - TokenHash (varchar 128, not null)
  - ExpiresAt (DateTimeOffset)
  - RevokedAt (DateTimeOffset, nullable)
  - ReplacedByTokenId (Guid, nullable)
  - CreatedAt

Products (photo print formats)
  - Id (Guid PK)
  - Name (varchar 100 — e.g., "10×15 mat")
  - Width_mm, Height_mm (int)
  - Finish (varchar 20 — "mat"/"lucios")
  - BasePrice (decimal 18,2 — RON)
  - SortOrder (int)
  - IsActive (bool)
  - CreatedAt, UpdatedAt

CartItems
  - Id (Guid PK)
  - UserId (Guid FK, nullable)
  - GuestSessionId (Guid FK, nullable)
  - ProductId (Guid FK)
  - FileName (varchar 500)
  - StoragePath (varchar 1000)
  - Quantity (int, min 1)
  - UnitPrice (decimal 18,2)
  - CropData (jsonb, nullable)
  - CreatedAt

Orders
  - Id (Guid PK)
  - OrderNumber (varchar 20, unique — format: FT-YYYYNNNN)
  - UserId (Guid FK, nullable)
  - GuestEmail (varchar 320, nullable)
  - Status (varchar 30)
  - DeliveryMethod (varchar 20 — "easybox"/"courier")
  - ShippingAddress (jsonb)
  - EasyboxLockerId (varchar 50, nullable)
  - SubTotal, ShippingCost, Total (decimal 18,2)
  - PaymentIntentId (varchar 200, nullable)
  - PaidAt (DateTimeOffset, nullable)
  - Notes (text, nullable)
  - CreatedAt, UpdatedAt

OrderItems
  - Id (Guid PK)
  - OrderId (Guid FK)
  - ProductId (Guid FK)
  - FileName, StoragePath (varchar)
  - Quantity, UnitPrice, LineTotal (decimal)
  - CropData (jsonb, nullable)

Addresses (saved user addresses)
  - Id (Guid PK)
  - UserId (Guid FK)
  - Label (varchar 100 — e.g., "Acasă")
  - FullName, Phone (varchar)
  - Street, City, County, PostalCode (varchar)
  - IsDefault (bool)
  - CreatedAt, UpdatedAt
```

## EF Core Configuration

### DbContext

```csharp
public class PhotoPrintDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    // ... etc
}
```

### Entity Configuration

- Use `IEntityTypeConfiguration<T>` in separate files per entity
- Configure indexes, unique constraints, and column types explicitly
- Use `.HasColumnType("jsonb")` for JSON columns in PostgreSQL
- Use `.HasIndex(e => e.Email).IsUnique()` for unique constraints

### Migrations

- `dotnet ef migrations add MigrationName`
- `dotnet ef database update`
- Never edit generated migrations manually (except for data migrations)
- Name migrations descriptively: `AddOrdersTable`, `AddIndexOnUserEmail`

## Query Patterns

- Use `IQueryable<T>` for composable queries
- Use `.AsNoTracking()` for read-only queries
- Use `.Include()` only when navigation data is needed (avoid N+1)
- Pagination: `.Skip((page-1) * size).Take(size)` with total count
- Filter by `DeletedAt == null` for soft-deleted entities
- Use `DateTimeOffset.UtcNow` for all timestamps

## Performance

- Add indexes on: foreign keys, unique fields, frequently filtered columns
- Composite index on `(Status, CreatedAt DESC)` for order queries
- Use `.AsSplitQuery()` for complex includes to avoid cartesian explosion
- Monitor with EF Core logging (log SQL in development only)
