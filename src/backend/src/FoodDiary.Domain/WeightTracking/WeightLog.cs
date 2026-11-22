using FoodDiary.Domain.Abstractions;

namespace FoodDiary.Domain.WeightTracking;

public class WeightLog : IDiaryOwned
{
    public long OwnerId { get; set; } = IDiaryOwned.UnassignedOwnerId;

    public required DateOnly Date { get; init; }
    public required decimal Weight { get; init; }
}