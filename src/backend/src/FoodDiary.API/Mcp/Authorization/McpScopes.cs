namespace FoodDiary.API.Mcp.Authorization;

public static class McpScopes
{
    public const string ClaimType = "scope";

    public const string FoodRead = "food:read";
    public const string FoodWrite = "food:write";

    public static readonly string[] Supported = [FoodRead, FoodWrite];
}
