using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace FoodDiary.Contracts.Notes;

[PublicAPI]
public class CreateNotesRequest
{
    [Required]
    public required DateOnly? Date { get; init; }

    [Required]
    public required TimeOnly? Time { get; init; }

    [Required]
    [Length(1, 50, ErrorMessage = "From 1 to 50 items can be logged at once")]
    public required IReadOnlyList<CreateNotesRequestItem> Items { get; init; }
}

[PublicAPI]
public class CreateNotesRequestItem
{
    [Range(1, int.MaxValue)]
    public required int ProductId { get; init; }

    [Range(1, 5000, ErrorMessage = "Quantity value must be between 1 and 5000 g")]
    public required int ProductQuantity { get; init; }
}

[PublicAPI]
public record CreateNotesResponse(IReadOnlyList<int> Ids);
