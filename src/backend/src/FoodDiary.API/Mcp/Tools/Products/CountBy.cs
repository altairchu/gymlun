using FoodDiary.Domain.Enums;

namespace FoodDiary.API.Mcp.Tools.Products;

internal static class CountBy
{
    public const string Grams = "grams";
    public const string Servings = "servings";

    public static QuantityUnit? ToQuantityUnit(string countBy) => countBy.Trim().ToLowerInvariant() switch
    {
        Grams => QuantityUnit.Grams,
        Servings => QuantityUnit.Servings,
        _ => null
    };
}
