using FoodDiary.Domain.Abstractions;

namespace FoodDiary.Domain.Entities;

public sealed class McpConnection : IDiaryOwned
{
    public Guid Id { get; set; }
    public long OwnerId { get; set; } = IDiaryOwned.UnassignedOwnerId;
    public string ClientId { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public DateTimeOffset AuthorizedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public long RefreshVersion { get; set; }
}
