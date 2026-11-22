using FoodDiary.Domain.Entities;
using FoodDiary.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.API.Mcp.Authorization;

internal sealed class McpConnectionStore(FoodDiaryContext db, TimeProvider timeProvider)
{
    public async Task<AccessGrant> Create(
        AccessGrant grant,
        string? clientName,
        CancellationToken cancellationToken)
    {
        var connection = new McpConnection
        {
            Id = Guid.NewGuid(),
            ClientId = grant.ClientId,
            ClientName = string.IsNullOrWhiteSpace(clientName) ? "AI client" : clientName[..Math.Min(clientName.Length, 200)],
            AuthorizedAt = timeProvider.GetUtcNow()
        };
        db.McpConnections.Add(connection);
        await db.SaveChangesAsync(cancellationToken);
        return grant with { ConnectionId = connection.Id, RefreshVersion = 0 };
    }

    public Task<bool> IsCurrent(AccessGrant grant, CancellationToken cancellationToken) =>
        FromProtectedGrant(grant).AnyAsync(cancellationToken);

    public async Task<AccessGrant?> RotateRefresh(AccessGrant grant, CancellationToken cancellationToken)
    {
        var updated = await FromProtectedGrant(grant)
            .Where(entry => entry.RefreshVersion == grant.RefreshVersion)
            .ExecuteUpdateAsync(
                updates => updates.SetProperty(entry => entry.RefreshVersion, entry => entry.RefreshVersion + 1),
                cancellationToken);
        return updated == 1 ? grant with { RefreshVersion = grant.RefreshVersion + 1 } : null;
    }

    public async Task<IReadOnlyList<McpConnectionItem>> List(CancellationToken cancellationToken) =>
        await db.McpConnections.AsNoTracking()
            .Where(entry => entry.RevokedAt == null)
            .OrderByDescending(entry => entry.AuthorizedAt)
            .Select(entry => new McpConnectionItem(entry.Id, entry.ClientName, entry.AuthorizedAt))
            .ToArrayAsync(cancellationToken);

    public async Task<bool> Revoke(Guid id, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var updated = await db.McpConnections.Where(entry => entry.Id == id && entry.RevokedAt == null)
            .ExecuteUpdateAsync(updates => updates.SetProperty(entry => entry.RevokedAt, now), cancellationToken);
        return updated == 1;
    }

    // Used only on claims read from a server-protected token, before a request principal can exist.
    private IQueryable<McpConnection> FromProtectedGrant(AccessGrant grant) =>
        db.McpConnections.IgnoreQueryFilters().Where(entry =>
            grant.OwnerId > 0 && grant.ConnectionId.HasValue &&
            entry.Id == grant.ConnectionId && entry.OwnerId == grant.OwnerId &&
            entry.ClientId == grant.ClientId && entry.RevokedAt == null);
}

internal sealed record McpConnectionItem(Guid Id, string ClientName, DateTimeOffset AuthorizedAt);
