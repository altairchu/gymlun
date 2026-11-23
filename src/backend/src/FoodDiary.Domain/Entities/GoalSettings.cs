using FoodDiary.Domain.Abstractions;

namespace FoodDiary.Domain.Entities;

public sealed class GoalSettings : IDiaryOwned
{
    public long OwnerId { get; set; } = IDiaryOwned.UnassignedOwnerId;
    public long Version { get; set; }
    public string? Goal { get; set; }
    public string? EatingApproach { get; set; }
    public decimal? TargetWeightKg { get; set; }
    public int? TargetKcal { get; set; }
    public decimal? ProteinPerKg { get; set; }
    public decimal? TargetCarbsG { get; set; }
    public decimal? TargetFatG { get; set; }
    public int? AgeYears { get; set; }
    public decimal? HeightCm { get; set; }
    public string? Sex { get; set; }
    public decimal? ActivityFactor { get; set; }
}
