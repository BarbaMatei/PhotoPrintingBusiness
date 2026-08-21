using Microsoft.EntityFrameworkCore;
using PhotoPrint.API.Data;

namespace PhotoPrint.API.Services.Invoicing;

public static class InvoiceUniqueViolation
{
    private const string UniqueViolation = "23505";

    /// <summary>True when the order already has an invoice — a concurrent delivery won the race.</summary>
    public static bool IsOrderIdViolation(DbUpdateException ex)
        => ex.InnerException is Npgsql.PostgresException pg
           && pg.SqlState == UniqueViolation
           && pg.ConstraintName == PhotoPrintDbContext.InvoiceOrderIdIndexName;

    /// <summary>True when the allocated number is taken; both indexes guard that, so either answers to a fresh number.</summary>
    public static bool IsNumberViolation(DbUpdateException ex)
        => ex.InnerException is Npgsql.PostgresException pg
           && pg.SqlState == UniqueViolation
           && (pg.ConstraintName == PhotoPrintDbContext.InvoiceNumberIndexName
               || pg.ConstraintName == PhotoPrintDbContext.InvoiceSeriesYearNumberIndexName);
}
