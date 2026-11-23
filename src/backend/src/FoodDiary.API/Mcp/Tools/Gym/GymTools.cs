using System.ComponentModel;
using FoodDiary.API.Features.Gym;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace FoodDiary.API.Mcp.Tools.Gym;

public sealed class GymTools(GymContextService gym, BodyMeasurementService measurements)
{
    public const string ContextTool = "get_gym_context";

    [McpServerTool(Name = ContextTool, ReadOnly = true, OpenWorld = false)]
    [Description("Call this first in each GymLun conversation. Read the AtlasApple account's saved goal, current eating approach, latest available weight and daily needs estimate. Missing inputs and estimates are separate from the user's chosen targets.")]
    public Task<GymContext> GetGymContext(CancellationToken cancellationToken) => gym.Get(cancellationToken);

    [McpServerTool(Name = "update_gym_context", ReadOnly = false, Destructive = false, OpenWorld = false)]
    [Authorize(Policy = McpExtensions.McpWritePolicy)]
    [Description("Replace the account's goal, current eating approach and optional targets. Start with get_gym_context, carry forward all unchanged settings and pass its version. Omit a nullable field only to clear it.")]
    public Task<GymContext> UpdateGymContext(
        [Description("Saved goal and diet fields with the current version from get_gym_context. Goals: lose, maintain, gain, custom. Activity is a user-specified factor.")] GymContextInput settings,
        CancellationToken cancellationToken) => Run(() => gym.Set(settings, cancellationToken));

    [McpServerTool(Name = "list_body_composition", ReadOnly = true, OpenWorld = false)]
    [Description("Read the account's body composition observations, newest first. Use optional inclusive local calendar dates. Measured and derived readings are returned separately.")]
    public Task<IReadOnlyList<BodyMeasurementResult>> ListBodyComposition(
        DateOnly? from = null, DateOnly? to = null, CancellationToken cancellationToken = default) =>
        Run(() => measurements.List(from, to, cancellationToken));

    [McpServerTool(Name = "get_body_composition", ReadOnly = true, OpenWorld = false)]
    [Description("Read one body composition observation by ID from list_body_composition.")]
    public Task<BodyMeasurementResult> GetBodyComposition(long id, CancellationToken cancellationToken) =>
        Run(() => measurements.Get(id, cancellationToken));

    [McpServerTool(Name = "log_body_composition", ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false)]
    [Authorize(Policy = McpExtensions.McpWritePolicy)]
    [Description("Save a measured body composition observation. Leave unavailable fields and optional segments empty. Supply measured values with their correct units, not inferred device classifications. Existing weight diary entries are separate.")]
    public Task<BodyMeasurementResult> LogBodyComposition(
        [Description("Local measurement date, optional source, profile and body measurements. Metric keys: weightKg, bmi, bodyFatPercent, heartRateBpm, muscleMassKg, musclePercent, proteinMassKg, proteinPercent, bodyWaterKg, bodyWaterPercent, boneMineralKg, boneMineralPercent, fatMassKg, skeletalMuscleKg, visceralFatLevel, waistHipRatio, skeletalMuscleIndex, basalMetabolicRateKcal, bodyAgeYears, suggestedIntakeKcal, fatFreeMassKg, bodyScore. Optional segment keys: leftArm, rightArm, trunk, leftLeg, rightLeg.")] BodyMeasurementInput measurement,
        CancellationToken cancellationToken) => Run(() => measurements.Create(measurement, cancellationToken));

    [McpServerTool(Name = "update_body_composition", ReadOnly = false, Destructive = true, OpenWorld = false)]
    [Authorize(Policy = McpExtensions.McpWritePolicy)]
    [Description("Replace an observation. Read it first, provide the current version and carry forward any unchanged measured fields. Derived readings are calculated again.")]
    public Task<BodyMeasurementResult> UpdateBodyComposition(long id, BodyMeasurementInput measurement,
        CancellationToken cancellationToken) => Run(() => measurements.Update(id, measurement, cancellationToken));

    [McpServerTool(Name = "delete_body_composition", ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = false)]
    [Authorize(Policy = McpExtensions.McpWritePolicy)]
    [Description("Delete the identified body composition observation after the owner asks for deletion.")]
    public Task<long> DeleteBodyComposition(long id, CancellationToken cancellationToken) =>
        Run(async () =>
        {
            await measurements.Delete(id, cancellationToken);
            return id;
        });

    private static async Task<T> Run<T>(Func<Task<T>> operation)
    {
        try
        {
            return await operation();
        }
        catch (GymDataException error)
        {
            throw new McpException(error.Message);
        }
    }
}
