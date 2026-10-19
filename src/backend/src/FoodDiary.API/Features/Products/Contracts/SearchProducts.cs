using FoodDiary.Domain.Enums;
using JetBrains.Annotations;

namespace FoodDiary.API.Features.Products.Contracts;

[PublicAPI]
public record SearchProductsResponse(IReadOnlyCollection<SearchProductsResponse.Product> Products)
{
    public record Product(
        int Id,
        string Name,
        int DefaultQuantity,
        QuantityUnit QuantityUnit,
        IReadOnlyList<int> CategoryIds,
        int Calories,
        decimal? Protein,
        decimal? Fats,
        decimal? Carbs,
        decimal? Sugar,
        decimal? Salt,
        bool IsFavorite,
        string? PhotoUrl);
}
