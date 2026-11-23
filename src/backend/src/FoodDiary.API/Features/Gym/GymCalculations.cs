namespace FoodDiary.API.Features.Gym;

public static class GymCalculations
{
    private static readonly (string Mass, string Percent)[] CompositionPairs =
    [
        ("fatMassKg", "bodyFatPercent"),
        ("muscleMassKg", "musclePercent"),
        ("proteinMassKg", "proteinPercent"),
        ("bodyWaterKg", "bodyWaterPercent"),
        ("boneMineralKg", "boneMineralPercent")
    ];

    public static DailyNeeds CalculateDailyNeeds(GymContextInput input, LatestWeight? latestWeight, decimal? measuredHeightCm)
    {
        var weight = latestWeight?.WeightKg;
        var height = input.HeightCm ?? measuredHeightCm;
        int? sexOffset = input.Sex switch { "m" => 5, "f" => -161, _ => null };
        var missing = new List<string>(5);
        if (weight is null or <= 0) missing.Add("weightKg");
        if (height is null or <= 0) missing.Add("heightCm");
        if (input.AgeYears is null or < 18) missing.Add("ageYears");
        if (sexOffset is null) missing.Add("sex");

        var resting = missing.Count == 0
            ? decimal.Max(0, 10 * weight!.Value + 6.25m * height!.Value - 5 * input.AgeYears!.Value + sexOffset!.Value)
            : (decimal?)null;
        if (input.ActivityFactor is null) missing.Add("activityFactor");
        var maintenance = resting * input.ActivityFactor;
        var targetProtein = weight * input.ProteinPerKg;

        return new DailyNeeds(
            resting is > 0 ? (int)decimal.Round(resting.Value) : null,
            maintenance is > 0 ? (int)decimal.Round(maintenance.Value) : null,
            targetProtein is > 0 ? decimal.Round(targetProtein.Value, 1) : null,
            [.. missing],
            "mifflin_st_jeor_user_activity_factor");
    }

    public static Dictionary<string, decimal> DeriveBodyMetrics(
        IReadOnlyDictionary<string, decimal?> recorded,
        BodyProfile? profile)
    {
        var derived = new Dictionary<string, decimal>(StringComparer.Ordinal);
        decimal? Get(string key) => recorded.GetValueOrDefault(key) ??
            (derived.TryGetValue(key, out var value) ? value : null);

        void Fill(string key, decimal value, int precision)
        {
            if (recorded.GetValueOrDefault(key) is null)
            {
                derived[key] = decimal.Round(value, precision);
            }
        }

        if (Get("weightKg") is > 0 and var weight)
        {
            if (profile?.HeightCm is > 0 and var height)
            {
                var meters = height / 100;
                Fill("bmi", weight / (meters * meters), 1);
            }

            foreach (var (massKey, percentKey) in CompositionPairs)
            {
                if (Get(massKey) is >= 0 and var mass && mass <= weight)
                {
                    Fill(percentKey, 100 * mass / weight, 1);
                }

                if (Get(percentKey) is >= 0 and <= 100 and var percent)
                {
                    Fill(massKey, weight * percent / 100, 2);
                }
            }

            if (Get("fatMassKg") is >= 0 and var fat && fat <= weight)
            {
                Fill("fatFreeMassKg", weight - fat, 2);
            }
        }

        if (profile?.WaistCm is > 0 and var waist && profile.HipCm is > 0 and var hip)
        {
            Fill("waistHipRatio", waist / hip, 2);
        }

        return derived;
    }
}
