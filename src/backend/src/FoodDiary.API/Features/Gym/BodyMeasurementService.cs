using System.Text.Json;
using FoodDiary.Domain.Entities;
using FoodDiary.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.API.Features.Gym;

public sealed class BodyMeasurementService(FoodDiaryContext db)
{
    private static readonly HashSet<string> BodyKeys = new(StringComparer.Ordinal)
    {
        "weightKg", "bmi", "bodyFatPercent", "heartRateBpm", "muscleMassKg", "musclePercent",
        "proteinMassKg", "proteinPercent", "bodyWaterKg", "bodyWaterPercent", "boneMineralKg",
        "boneMineralPercent", "fatMassKg", "skeletalMuscleKg", "visceralFatLevel", "waistHipRatio",
        "skeletalMuscleIndex", "basalMetabolicRateKcal", "bodyAgeYears", "suggestedIntakeKcal",
        "fatFreeMassKg", "bodyScore"
    };

    private static readonly HashSet<string> SegmentKeys = new(StringComparer.Ordinal)
    {
        "leftArm", "rightArm", "trunk", "leftLeg", "rightLeg"
    };

    private sealed record MeasurementData(
        Dictionary<string, decimal?> Metrics,
        BodyProfile? Profile,
        Dictionary<string, BodySegment> Segments,
        Dictionary<string, string> Flags);

    public async Task<IReadOnlyList<BodyMeasurementResult>> List(
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken)
    {
        if (from.HasValue && to.HasValue && from > to)
        {
            throw new GymDataException("The start date must be on or before the end date.");
        }

        var query = db.BodyMeasurements.AsNoTracking();
        if (from.HasValue) query = query.Where(entry => entry.Date >= from.Value);
        if (to.HasValue) query = query.Where(entry => entry.Date <= to.Value);

        var entries = await query.OrderByDescending(entry => entry.Date)
            .ThenByDescending(entry => entry.Id)
            .ToListAsync(cancellationToken);
        return entries.Select(ToResult).ToArray();
    }

    public async Task<BodyMeasurementResult> Get(long id, CancellationToken cancellationToken) =>
        ToResult(await Find(id, cancellationToken));

    public async Task<BodyMeasurementResult> Create(BodyMeasurementInput input, CancellationToken cancellationToken)
    {
        var row = new BodyMeasurement { Version = 1 };
        Apply(row, input);
        db.BodyMeasurements.Add(row);
        await db.SaveChangesAsync(cancellationToken);
        return ToResult(row);
    }

    public async Task<BodyMeasurementResult> Update(long id, BodyMeasurementInput input, CancellationToken cancellationToken)
    {
        var row = await Find(id, cancellationToken);
        if (input.Version.HasValue && input.Version != row.Version)
        {
            throw Conflict();
        }

        Apply(row, input);
        row.Version++;
        await SaveExisting(cancellationToken);
        return ToResult(row);
    }

    public async Task Delete(long id, CancellationToken cancellationToken)
    {
        db.BodyMeasurements.Remove(await Find(id, cancellationToken));
        await SaveExisting(cancellationToken);
    }

    public static BodyMeasurementResult ToResult(BodyMeasurement row)
    {
        var data = JsonSerializer.Deserialize<MeasurementData>(row.Data, JsonSerializerOptions.Web) ??
            throw new InvalidOperationException("Body measurement data is missing.");
        return new BodyMeasurementResult(row.Id, row.Date, row.Source, data.Metrics, data.Profile, data.Segments,
            data.Flags, GymCalculations.DeriveBodyMetrics(data.Metrics, data.Profile), row.Version);
    }

    private async Task<BodyMeasurement> Find(long id, CancellationToken cancellationToken) =>
        await db.BodyMeasurements.SingleOrDefaultAsync(entry => entry.Id == id, cancellationToken)
            ?? throw new GymDataException("Body measurement not found.", StatusCodes.Status404NotFound);

    private static void Apply(BodyMeasurement row, BodyMeasurementInput input)
    {
        var metrics = input.Metrics ?? [];
        var segments = input.Segments ?? [];
        var flags = input.Flags ?? [];
        var profile = input.Profile;

        if (input.Date == default || metrics.Any(metric => !ValidMetric(metric.Key, metric.Value)) ||
            flags.Keys.Any(key => !BodyKeys.Contains(key)) ||
            segments.Any(segment => !SegmentKeys.Contains(segment.Key) || segment.Value is null ||
                segment.Value.FatMassKg is < 0 or > 500 || segment.Value.MuscleMassKg is < 0 or > 500) ||
            profile?.AgeYears is < 0 or > 125 || profile?.HeightCm is < 30 or > 300 ||
            profile?.WaistCm is <= 0 or > 500 || profile?.HipCm is <= 0 or > 500)
        {
            throw new GymDataException("Check the measurement date, keys, values and units.");
        }

        metrics = metrics.Where(metric => metric.Value.HasValue).ToDictionary();
        segments = segments.Where(segment => segment.Value.FatMassKg.HasValue || segment.Value.MuscleMassKg.HasValue)
            .ToDictionary();
        if (metrics.Count == 0 && segments.Count == 0)
        {
            throw new GymDataException("At least one measured value is required.");
        }

        var normalizedFlags = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (key, value) in flags)
        {
            if (metrics.ContainsKey(key) && GymContextService.Trim(value, 40) is { } normalized)
            {
                normalizedFlags.Add(key, normalized);
            }
        }

        var normalizedProfile = profile is null ? null : profile with
        {
            BodyShape = GymContextService.Trim(profile.BodyShape, 100),
            BodyType = GymContextService.Trim(profile.BodyType, 100)
        };
        row.Date = input.Date;
        row.Source = GymContextService.Trim(input.Source, 200);
        row.WeightKg = metrics.GetValueOrDefault("weightKg");
        row.Data = JsonSerializer.Serialize(
            new MeasurementData(metrics, normalizedProfile, segments, normalizedFlags), JsonSerializerOptions.Web);
    }

    private static bool ValidMetric(string key, decimal? value)
    {
        if (!BodyKeys.Contains(key) || value is < 0 or > 15000) return false;
        if (key.EndsWith("Percent", StringComparison.Ordinal) && value is > 100) return false;
        return key != "weightKg" || value is null or > 0 and <= 500;
    }

    private async Task SaveExisting(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw Conflict();
        }
    }

    private static GymDataException Conflict() =>
        new("Another request updated this measurement. Read it again and retry.", StatusCodes.Status409Conflict);
}
