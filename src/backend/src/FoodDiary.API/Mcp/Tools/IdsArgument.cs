using ModelContextProtocol;

namespace FoodDiary.API.Mcp.Tools;

internal static class IdsArgument
{
    public static IReadOnlyList<int> Distinct(int[] ids, int maxCount)
    {
        var distinctIds = ids.Distinct().ToList();

        return distinctIds.Count >= 1 && distinctIds.Count <= maxCount
            ? distinctIds
            : throw new McpException($"Requested {distinctIds.Count} ids; pass 1 to {maxCount}.");
    }

    public static string Join(IEnumerable<int> ids) => string.Join(", ", ids);
}
