using FoodDiary.Domain.Abstractions;

namespace FoodDiary.Domain.Entities;

public sealed class BodyMeasurement : IDiaryOwned
{
    public long Id { get; set; }
    public long OwnerId { get; set; } = IDiaryOwned.UnassignedOwnerId;
    public DateOnly Date { get; set; }
    public string? Source { get; set; }
    public decimal? WeightKg { get; set; }
    public string Data { get; set; } = "{}";
    public long Version { get; set; }
}
