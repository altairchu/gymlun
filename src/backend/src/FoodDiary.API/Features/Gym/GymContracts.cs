namespace FoodDiary.API.Features.Gym;

public record GymContextInput(
    string? Goal = null,
    string? EatingApproach = null,
    decimal? TargetWeightKg = null,
    int? TargetKcal = null,
    decimal? ProteinPerKg = null,
    decimal? TargetCarbsG = null,
    decimal? TargetFatG = null,
    int? AgeYears = null,
    decimal? HeightCm = null,
    string? Sex = null,
    decimal? ActivityFactor = null,
    long Version = 0);

public sealed record LatestWeight(DateOnly Date, decimal WeightKg, string Source);

public sealed record DailyNeeds(
    int? RestingKcal,
    int? MaintenanceKcal,
    decimal? TargetProteinG,
    string[] Missing,
    string Method);

public sealed record GymContext(
    string? Goal,
    string? EatingApproach,
    decimal? TargetWeightKg,
    int? TargetKcal,
    decimal? ProteinPerKg,
    decimal? TargetCarbsG,
    decimal? TargetFatG,
    int? AgeYears,
    decimal? HeightCm,
    string? Sex,
    decimal? ActivityFactor,
    long Version,
    LatestWeight? LatestWeight,
    DailyNeeds DailyNeeds)
    : GymContextInput(Goal, EatingApproach, TargetWeightKg, TargetKcal, ProteinPerKg, TargetCarbsG, TargetFatG,
        AgeYears, HeightCm, Sex, ActivityFactor, Version);

public sealed record BodyProfile(
    int? AgeYears = null,
    decimal? HeightCm = null,
    decimal? WaistCm = null,
    decimal? HipCm = null,
    string? BodyShape = null,
    string? BodyType = null);

public sealed record BodySegment(decimal? FatMassKg = null, decimal? MuscleMassKg = null);

public sealed record BodyMeasurementInput(
    DateOnly Date,
    string? Source = null,
    Dictionary<string, decimal?>? Metrics = null,
    BodyProfile? Profile = null,
    Dictionary<string, BodySegment>? Segments = null,
    Dictionary<string, string>? Flags = null,
    long? Version = null);

public sealed record BodyMeasurementResult(
    long Id,
    DateOnly Date,
    string? Source,
    Dictionary<string, decimal?> Metrics,
    BodyProfile? Profile,
    Dictionary<string, BodySegment> Segments,
    Dictionary<string, string> Flags,
    Dictionary<string, decimal> DerivedMetrics,
    long Version);

internal sealed class GymDataException(string message, int status = StatusCodes.Status400BadRequest) : Exception(message)
{
    public int Status { get; } = status;
}
