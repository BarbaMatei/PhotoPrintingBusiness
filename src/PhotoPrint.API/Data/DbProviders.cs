namespace PhotoPrint.API.Data;

/// <summary>
/// EF Core provider names (QUAL-2, review 035-v5). Centralized so the provider-specific
/// branches scattered across the data layer (DbContext model config, OrderNumberService,
/// CartService, StaticShippingService) compare against one constant each and can't drift
/// on a typo'd magic string. These are the <see cref="Microsoft.EntityFrameworkCore.Infrastructure.IDatabaseFacade"/>
/// <c>ProviderName</c> values — the assembly names of the registered providers.
/// </summary>
public static class DbProviders
{
    public const string Postgres = "Npgsql.EntityFrameworkCore.PostgreSQL";
    public const string Sqlite = "Microsoft.EntityFrameworkCore.Sqlite";
    public const string InMemory = "Microsoft.EntityFrameworkCore.InMemory";
}
