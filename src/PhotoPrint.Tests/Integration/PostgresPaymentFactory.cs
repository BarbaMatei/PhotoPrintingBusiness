using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PhotoPrint.API.Data;
using PhotoPrint.Tests.Helpers;

namespace PhotoPrint.Tests.Integration;

/// <summary>
/// Like <see cref="PaymentFactory"/> but backs the API with a real PostgreSQL database instead
/// of the EF InMemory provider. InMemory does not enforce unique indexes, so it cannot exercise
/// the cross-tenant idempotency-key collision → 409 path at the HTTP layer; PostgreSQL enforces
/// <c>ix_orders_idempotency_key</c>.
/// </summary>
public sealed class PostgresPaymentFactory : PaymentFactory
{
    private readonly PostgresTestDatabase _database = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        // ConfigureTestServices runs after the app's and the base factory's registrations,
        // so swap the InMemory DbContext (added by UploadFactory) for PostgreSQL here.
        builder.ConfigureTestServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<PhotoPrintDbContext>));
            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddDbContext<PhotoPrintDbContext>(
                options => options.UseNpgsql(_database.ConnectionString));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _database.Dispose();
    }
}
