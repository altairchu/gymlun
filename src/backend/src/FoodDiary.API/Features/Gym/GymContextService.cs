using FoodDiary.Domain.Entities;
using FoodDiary.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FoodDiary.API.Features.Gym;

public sealed class GymContextService(FoodDiaryContext db)
{
    public async Task<GymContext> Get(CancellationToken cancellationToken)
    {
        var settings = await db.GoalSettings.AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        return await ToContext(ToInput(settings), cancellationToken);
    }

    public async Task<GymContext> Set(GymContextInput input, CancellationToken cancellationToken)
    {
        var normalized = Validate(input);
        var settings = await db.GoalSettings.SingleOrDefaultAsync(cancellationToken);
        if ((settings?.Version ?? 0) != normalized.Version)
        {
            throw Conflict();
        }

        if (settings is null)
        {
            settings = new GoalSettings();
            db.GoalSettings.Add(settings);
        }

        settings.Version = normalized.Version + 1;
        settings.Goal = normalized.Goal;
        settings.EatingApproach = normalized.EatingApproach;
        settings.TargetWeightKg = normalized.TargetWeightKg;
        settings.TargetKcal = normalized.TargetKcal;
        settings.ProteinPerKg = normalized.ProteinPerKg;
        settings.TargetCarbsG = normalized.TargetCarbsG;
        settings.TargetFatG = normalized.TargetFatG;
        settings.AgeYears = normalized.AgeYears;
        settings.HeightCm = normalized.HeightCm;
        settings.Sex = normalized.Sex;
        settings.ActivityFactor = normalized.ActivityFactor;

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException error) when (error is DbUpdateConcurrencyException ||
            error.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw Conflict();
        }

        return await ToContext(ToInput(settings), cancellationToken);
    }

    private async Task<GymContext> ToContext(GymContextInput input, CancellationToken cancellationToken)
    {
        var logged = await db.WeightLogs.AsNoTracking()
            .OrderByDescending(entry => entry.Date)
            .Select(entry => new LatestWeight(entry.Date, entry.Weight, "weightDiary"))
            .FirstOrDefaultAsync(cancellationToken);
        var report = await db.BodyMeasurements.AsNoTracking()
            .Where(entry => entry.WeightKg != null)
            .OrderByDescending(entry => entry.Date)
            .ThenByDescending(entry => entry.Id)
            .Select(entry => new LatestWeight(entry.Date, entry.WeightKg!.Value, "bodyComposition"))
            .FirstOrDefaultAsync(cancellationToken);
        var latest = logged is not null && (report is null || logged.Date >= report.Date) ? logged : report;
        var newestBody = await db.BodyMeasurements.AsNoTracking()
            .OrderByDescending(entry => entry.Date)
            .ThenByDescending(entry => entry.Id)
            .FirstOrDefaultAsync(cancellationToken);
        var height = newestBody is null ? null : BodyMeasurementService.ToResult(newestBody).Profile?.HeightCm;

        return new GymContext(input.Goal, input.EatingApproach, input.TargetWeightKg, input.TargetKcal,
            input.ProteinPerKg, input.TargetCarbsG, input.TargetFatG, input.AgeYears, input.HeightCm, input.Sex,
            input.ActivityFactor, input.Version, latest, GymCalculations.CalculateDailyNeeds(input, latest, height));
    }

    private static GymContextInput ToInput(GoalSettings? row) => row is null ? new GymContextInput() : new(
        row.Goal, row.EatingApproach, row.TargetWeightKg, row.TargetKcal, row.ProteinPerKg,
        row.TargetCarbsG, row.TargetFatG, row.AgeYears, row.HeightCm, row.Sex, row.ActivityFactor, row.Version);

    private static GymContextInput Validate(GymContextInput input)
    {
        var goal = Trim(input.Goal, 16);
        var sex = Trim(input.Sex, 1);
        if (goal is not (null or "lose" or "maintain" or "gain" or "custom") ||
            sex is not (null or "m" or "f") ||
            input.TargetWeightKg is < 1 or > 500 || input.TargetKcal is < 1 or > 15000 ||
            input.ProteinPerKg is <= 0 or > 10 || input.TargetCarbsG is < 0 or > 2000 ||
            input.TargetFatG is < 0 or > 2000 || input.AgeYears is < 18 or > 125 ||
            input.HeightCm is < 50 or > 300 || input.ActivityFactor is < 1 or > 3 || input.Version < 0)
        {
            throw new GymDataException("Check the goal, measurement bounds and estimate inputs.");
        }

        return input with { Goal = goal, Sex = sex, EatingApproach = Trim(input.EatingApproach, 1000) };
    }

    internal static string? Trim(string? value, int maximum)
    {
        var text = value?.Trim();
        if (text?.Length > maximum)
        {
            throw new GymDataException($"Text must contain at most {maximum} characters.");
        }

        return string.IsNullOrEmpty(text) ? null : text;
    }

    private static GymDataException Conflict() =>
        new("Another request updated these settings. Read the latest settings and retry.", StatusCodes.Status409Conflict);
}
