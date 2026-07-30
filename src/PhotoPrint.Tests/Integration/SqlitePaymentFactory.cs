using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PhotoPrint.API.Data;

namespace PhotoPrint.Tests.Integration;

/// <summary>
/// Like <see cref="PaymentFactory"/> but backs the API with a real SQLite database
/// (one shared in-memory connection kept open for the factory's lifetime) instead of
/// the EF InMemory provider. InMemory does not enforce unique indexes, so it cannot
/// exercise the cross-tenant idempotency-key collision → 409 path at the HTTP layer
/// SQLite enforces <c>ix_orders_idempotency_key</c>.
/// </summary>
public sealed class SqlitePaymentFactory : PaymentFactory
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open(); // keep open so the in-memory database survives the run

        base.ConfigureWebHost(builder);

        // ConfigureTestServices runs after the app's and the base factory's registrations,
        // so swap the InMemory DbContext (added by UploadFactory) for SQLite here.
        builder.ConfigureTestServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<PhotoPrintDbContext>));
            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddDbContext<PhotoPrintDbContext>(options => options.UseSqlite(_connection));
            // NB: the REAL OrderNumberService runs here — it has a SQLite branch.
        });
    }

    // Build the schema on the shared connection once the host (and final DI graph) exists.
    // Independent of the Program.cs startup EnsureCreated block, whose DatabaseProvider
    // gate isn't visible this early under WebApplicationFactory.
    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PhotoPrintDbContext>();
        db.Database.EnsureCreated();

        return host;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _connection.Dispose();
    }
}
