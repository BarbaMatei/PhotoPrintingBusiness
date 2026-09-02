using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace PhotoPrint.API.Data;

public static class PostgresSequences
{
    private static readonly Regex SafeName = new("^[a-z0-9_]{1,63}$", RegexOptions.Compiled);

    public static Task EnsureAsync(
        DatabaseFacade database, string sequenceName, CancellationToken ct = default)
    {
        if (!SafeName.IsMatch(sequenceName))
            throw new ArgumentException(
                $"'{sequenceName}' is not a usable sequence name: lowercase letters, digits and " +
                "underscores only, at most 63 characters.",
                nameof(sequenceName));

        // A concurrent CREATE SEQUENCE IF NOT EXISTS makes the loser raise 42P07, 42710 or 23505; only a subtransaction can swallow that without aborting an enclosing transaction, and only once the name really holds a sequence.
        // A DDL identifier cannot be a SQL parameter, so EF1002 is suppressed and SafeName is the mitigation.
#pragma warning disable EF1002
        return database.ExecuteSqlRawAsync($"""
            DO $$ BEGIN
              CREATE SEQUENCE IF NOT EXISTS "{sequenceName}" START 1 INCREMENT 1;
            EXCEPTION WHEN duplicate_table OR duplicate_object OR unique_violation THEN
              IF NOT EXISTS (
                SELECT 1 FROM pg_class
                WHERE oid = to_regclass('"{sequenceName}"') AND relkind = 'S'
              ) THEN
                RAISE;
              END IF;
            END $$;
            """, ct);
#pragma warning restore EF1002
    }
}
