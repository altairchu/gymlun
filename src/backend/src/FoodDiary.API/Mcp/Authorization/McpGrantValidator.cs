using FoodDiary.API.Authentication;

namespace FoodDiary.API.Mcp.Authorization;

internal sealed class McpGrantValidator(
    IConfiguration configuration,
    McpConnectionStore connections,
    AtlasAppleSessionValidator? atlasSessions = null)
{
    public bool UsesAtlas => configuration.GetValue<bool>("AtlasApple:Enabled");

    public async Task<bool> CanUse(AccessGrant grant, CancellationToken cancellationToken)
    {
        if (!UsesAtlas)
        {
            return grant.OwnerId is null && grant.ConnectionId is null;
        }

        return grant.OwnerId is > 0 and var ownerId &&
            grant.AtlasAuthEpoch is >= 0 and var epoch &&
            grant.ConnectionId.HasValue &&
            atlasSessions is not null &&
            await connections.IsCurrent(grant, cancellationToken) &&
            await atlasSessions.IsCurrentAsync(ownerId, epoch, cancellationToken);
    }
}
