using System.ComponentModel.DataAnnotations;

namespace FoodDiary.API.Features.WeightTracking.Contracts;

public class UpdateWeightLogBody
{
    [Range(1, 700)]
    public decimal Value { get; init; }
}
