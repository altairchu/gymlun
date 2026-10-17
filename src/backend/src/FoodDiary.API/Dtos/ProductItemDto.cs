using FoodDiary.API.Features.Products.Contracts;
using FoodDiary.Domain.Enums;

namespace FoodDiary.API.Dtos;

public class ProductItemDto
{
    public int Id { get; init; }

    public required string Name { get; init; }

    public int CaloriesCost { get; init; }

    public int DefaultQuantity { get; init; }

    public QuantityUnit QuantityUnit { get; init; }

    public required IReadOnlyList<Category> Categories { get; init; }
    
    public required decimal? Protein { get; init; }
    public required decimal? Fats { get; init; }
    public required decimal? Carbs { get; init; }
    public required decimal? Sugar { get; init; }
    public required decimal? Salt { get; init; }
    public bool IsFavorite { get; init; }
}
