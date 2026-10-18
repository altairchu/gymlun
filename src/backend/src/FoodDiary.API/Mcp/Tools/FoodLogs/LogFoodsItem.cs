using System.ComponentModel;
using JetBrains.Annotations;

namespace FoodDiary.API.Mcp.Tools.FoodLogs;

[PublicAPI]
public record LogFoodsItem
{
    [Description("Id of the product eaten.")]
    public required int ProductId { get; init; }

    [Description("Grams eaten, from 1 to 5000. Omit it when passing `servings`.")]
    public int? Quantity { get; init; }

    [Description("Pieces or servings eaten, more than 0 and at most 100, e.g. 1.5. Omit it when passing `quantity`.")]
    public decimal? Servings { get; init; }
}
