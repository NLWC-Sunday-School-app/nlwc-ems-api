using System.Reflection;
using Npgsql;

namespace NlwcEms.Api.Db;

/// <summary>Forward-only SQL migrations. One transaction in total, serialised by an advisory lock.</summary>
public static class Migrator
{
    private const long LockId = 727001;

    public static async Task<IReadOnlyList<string>> RunAsync(NpgsqlDataSource ds, CancellationToken ct = default)
    {
        var asm = Assembly.GetExecutingAssembly();
        var names = asm.GetManifestResourceNames().Where(n => n.StartsWith("migrations/")).OrderBy(n => n, StringComparer.Ordinal).ToList();
        var applied = new List<string>();

        await using var conn = await ds.OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        async Task Exec(string sql, params (string, object)[] ps)
        {
            await using var cmd = new NpgsqlCommand(sql, conn, tx);
            foreach (var (k, v) in ps) cmd.Parameters.AddWithValue(k, v);
            await cmd.ExecuteNonQueryAsync(ct);
        }

        await Exec($"SELECT pg_advisory_xact_lock({LockId})");
        await Exec("CREATE TABLE IF NOT EXISTS schema_migration (name text PRIMARY KEY, applied_at timestamptz NOT NULL DEFAULT now())");

        var done = new HashSet<string>();
        await using (var cmd = new NpgsqlCommand("SELECT name FROM schema_migration", conn, tx))
        await using (var r = await cmd.ExecuteReaderAsync(ct))
            while (await r.ReadAsync(ct)) done.Add(r.GetString(0));

        foreach (var n in names)
        {
            var file = n["migrations/".Length..];
            if (done.Contains(file)) continue;
            using var s = new StreamReader(asm.GetManifestResourceStream(n)!);
            await Exec(await s.ReadToEndAsync(ct));
            await Exec("INSERT INTO schema_migration (name) VALUES (@n)", ("n", file));
            applied.Add(file);
        }
        await tx.CommitAsync(ct);
        return applied;
    }
}
