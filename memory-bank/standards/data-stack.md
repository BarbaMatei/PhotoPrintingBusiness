# Data Stack

## Overview
PostgreSQL 16 serves as the single relational database, accessed via Entity Framework Core 8 with a Code-First migration approach. All data is modeled relationally with UUID primary keys.

## Database

**PostgreSQL 16** — self-hosted via Docker (dev), managed service (prod)

PostgreSQL was chosen for its robustness, JSON support (JSONB for flexible fields like shipping addresses and crop data), excellent indexing, and wide hosting availability. The data model is fully relational — users, orders, products, cart items — with clear foreign key relationships.

### Key Characteristics
- All primary keys: UUID (Guid) — no auto-increment integers
- All entities include `CreatedAt` (DateTimeOffset) and `UpdatedAt` (DateTimeOffset, nullable)
- Soft deletes where applicable via `DeletedAt` (DateTimeOffset, nullable)
- JSONB columns for semi-structured data: `ShippingAddress`, `CropData`
- Currency stored as `decimal(18,2)` in RON

### Core Tables
- `Users` — accounts with email, password hash, role, Google ID
- `GuestSessions` — anonymous sessions with hashed tokens, 7-day TTL
- `RefreshTokens` — SHA-256 hashed, with rotation tracking
- `Products` — print format × finish combinations (6 products)
- `CartItems` — per-user or per-guest-session, linked to product and upload
- `Orders` — payment, shipping, status tracking with `FT-YYYYNNNN` order numbers
- `OrderItems` — individual photos in an order with quantity
- `EasyboxLockers` — Sameday locker locations with coordinates

## ORM / Database Client

**Entity Framework Core 8** (Code-First) with `Npgsql.EntityFrameworkCore.PostgreSQL` provider

EF Core was chosen for its tight integration with ASP.NET Core, LINQ-based query composition, automatic migration generation, and strong typing via navigation properties.

### Conventions
- DbContext: `PhotoPrintDbContext` with `DbSet<T>` for each entity
- Entities are POCO classes in `Models/` folder with navigation properties
- Use `IQueryable<T>` for composable queries; materialize with `ToListAsync()`
- Never expose entities directly — map to/from DTOs
- Never use raw SQL with user input — always parameterized queries
- Composite indexes on frequently filtered columns (e.g., `Status` + `CreatedAt`)
- Table names: PascalCase (EF Core default)
- Column names: PascalCase (EF Core default)
- Foreign keys: `EntityNameId` pattern (e.g., `UserId`, `OrderId`)

### Migration Strategy
- Development: `dotnet ef database update`
- Production: apply migrations at startup or via CI/CD pipeline
- Rollback: `dotnet ef database update <previous-migration>`
- Never drop tables in production — use data-preserving migrations

## Decision Relationships
- PostgreSQL JSONB is used for `ShippingAddress` and `CropData` to avoid excessive normalization for address fields
- EF Core Code-First allows the schema to evolve with the domain model during development
- UUID keys prevent enumeration attacks and simplify distributed ID generation
