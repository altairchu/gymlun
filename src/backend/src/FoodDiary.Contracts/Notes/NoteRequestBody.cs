using System.ComponentModel.DataAnnotations;
using FoodDiary.Domain.Enums;
using JetBrains.Annotations;

namespace FoodDiary.Contracts.Notes;

[PublicAPI]
public class NoteRequestBody : IValidatableObject
{
    [Required]
    public required DateOnly? Date { get; init; }

    public TimeOnly? Time { get; init; }

    [EnumDataType(typeof(MealType))]
    public MealType? MealType { get; init; }

    [Range(1, int.MaxValue)]
    public required int ProductId { get; init; }

    [Range(1, 5000, ErrorMessage = "Quantity value must be between 1 and 5000 g")]
    public required int ProductQuantity { get; init; }

    [Range(0, int.MaxValue)]
    public int DisplayOrder { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Time is null && MealType is null)
        {
            yield return new ValidationResult(
                "Either time or meal type is required",
                [nameof(Time), nameof(MealType)]);
        }
    }
}
