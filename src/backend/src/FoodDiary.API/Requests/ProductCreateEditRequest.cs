using System.ComponentModel.DataAnnotations;
using FoodDiary.Domain.Enums;
using JetBrains.Annotations;

namespace FoodDiary.API.Requests;

[PublicAPI]
public class ProductCreateEditRequest : IValidatableObject
{
    private const int MaxCategories = 10;

    [Required(ErrorMessage = "Product name is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Product name must be between 1 and 100 characters")]
    public required string Name { get; init; }

    [Range(1, 1000, ErrorMessage = "Calories cost value must be between 1 and 1000")]
    public int CaloriesCost { get; init; }

    [Range(1, 1000, ErrorMessage = "Default quantity must be between 1 and 1000 g")]
    public int DefaultQuantity { get; init; }

    [EnumDataType(typeof(QuantityUnit))]
    public QuantityUnit QuantityUnit { get; init; } = QuantityUnit.Grams;

    public IReadOnlyList<int>? CategoryIds { get; init; }

    /// <summary>
    /// Single category sent by clients older than <see cref="CategoryIds"/>; used only when <see cref="CategoryIds"/> is absent
    /// </summary>
    [Range(1, int.MaxValue)]
    public int? CategoryId { get; init; }
    
    [Range(0, 1000)]
    public decimal? Protein { get; init; }
    
    [Range(0, 1000)]
    public decimal? Fats { get; init; }
    
    [Range(0, 1000)]
    public decimal? Carbs { get; init; }
    
    [Range(0, 1000)]
    public decimal? Sugar { get; init; }
    
    [Range(0, 1000)]
    public decimal? Salt { get; init; }

    public bool IsFavorite { get; init; }

    public IReadOnlyList<int> GetCategoryIds() =>
        CategoryIds ?? (CategoryId is { } categoryId ? [categoryId] : []);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CategoryIds is null)
        {
            yield break;
        }

        if (CategoryIds.Count > MaxCategories)
        {
            yield return new ValidationResult(
                $"A product can have at most {MaxCategories} categories",
                [nameof(CategoryIds)]);
        }

        if (CategoryIds.Any(id => id < 1))
        {
            yield return new ValidationResult("Category ids must be positive", [nameof(CategoryIds)]);
        }

        if (CategoryIds.Distinct().Count() != CategoryIds.Count)
        {
            yield return new ValidationResult("Category ids must be distinct", [nameof(CategoryIds)]);
        }
    }
}
