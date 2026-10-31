using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Dapper;
using Npgsql;
using NlwcEms.Api.Identity;

namespace NlwcEms.Api.Access;

/// <summary>Append-only audit trail. Each row's hash covers the previous hash, so edits or removals are detectable.</summary>
public sealed class AuditService(NpgsqlDataSource db)
{
    private const long ChainLock = 727002;
    public const string Genesis = "genesis";

    private static string Hash(string prev, Guid id, DateTime at, Guid? actor, string action, string? resource, Guid? edition, string? reason, string detail) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(
            string.Join('|', prev, id, at.ToUniversalTime().ToString("O"), actor, action, resource, edition, reason, detail))));

    public async Task WriteAsync(Guid? actor, string action, string? resource = null, Guid? editionId = null, string? reason = null, object? detail = null)
    {
        await using var conn = await db.OpenConnectionAsync();
        await using var tx = await conn.BeginTransactionAsync();
        await conn.ExecuteAsync($"SELECT pg_advisory_xact_lock({ChainLock})", transaction: tx);
        var prev = await conn.ExecuteScalarAsync<string?>("SELECT hash FROM audit_event ORDER BY seq DESC LIMIT 1", transaction: tx) ?? Genesis;
        var id = Crypto.NewId();
        // Truncate to microseconds so the stored value round-trips to the same hash.
        var at = new DateTime(DateTime.UtcNow.Ticks / 10 * 10, DateTimeKind.Utc);
        var json = JsonSerializer.Serialize(detail ?? new { });
        var hash = Hash(prev, id, at, actor, action, resource, editionId, reason, json);
        await conn.ExecuteAsync(
            "INSERT INTO audit_event(id, occurred_at, actor_account_id, action, resource, edition_id, reason, detail, prev_hash, hash) VALUES (@id, @at, @actor, @action, @resource, @editionId, @reason, @json, @prev, @hash)",
            new { id, at, actor, action, resource, editionId, reason, json, prev, hash }, tx);
        await tx.CommitAsync();
    }

    private sealed record Row(long Seq, Guid Id, DateTime OccurredAt, Guid? ActorAccountId, string Action, string? Resource, Guid? EditionId, string? Reason, string Detail, string PrevHash, string Hash);

    /// <summary>Recomputes the chain. Returns the seq of the first broken row, or null if intact.</summary>
    public async Task<long?> VerifyChainAsync()
    {
        await using var conn = await db.OpenConnectionAsync();
        var rows = await conn.QueryAsync<Row>(
            "SELECT seq, id, occurred_at AS OccurredAt, actor_account_id AS ActorAccountId, action, resource, edition_id AS EditionId, reason, detail, prev_hash AS PrevHash, hash FROM audit_event ORDER BY seq");
        var prev = Genesis;
        foreach (var r in rows)
        {
            if (r.PrevHash != prev || r.Hash != Hash(prev, r.Id, r.OccurredAt, r.ActorAccountId, r.Action, r.Resource, r.EditionId, r.Reason, r.Detail))
                return r.Seq;
            prev = r.Hash;
        }
        return null;
    }
}
