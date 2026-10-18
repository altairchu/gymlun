using System.ComponentModel;
using FoodDiary.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using DomainWeightLog = FoodDiary.Domain.WeightTracking.WeightLog;

namespace FoodDiary.API.Mcp.Tools.WeightLogs;

[McpServerToolType]
[Authorize(Policy = McpExtensions.McpWritePolicy)]
public class WeightLogsWriteTool(FoodDiaryContext context)
{
    internal const decimal MinWeight = 1;
    internal const decimal MaxWeight = 500;

    private const string LogWeightToolDescription =
        """
        Logs the person's weight in kilograms on one date, and returns it. A date holds one weight: to change the weight already logged for a date, use `update_weight`.

        `date` is a calendar date with no time or timezone: use the person's local date. Convert pounds to kilograms first (1 lb = 0.4536 kg).
        """;

    private const string UpdateWeightToolDescription =
        """
        Changes the person's weight logged on one date, as returned by `get_weight_logs` or `log_weight`, and returns it as it is now. Use it to fix a weight logged wrong, or when the person weighs in again on the same day.

        `date` is a calendar date with no time or timezone. Convert pounds to kilograms first (1 lb = 0.4536 kg).
        """;

    private const string DeleteWeightToolDescription =
        """
        Deletes the person's weight logged on one date, as returned by `get_weight_logs` or `log_weight`. Use it to undo a weight logged on the wrong date; to fix a wrong value, use `update_weight` instead.
        """;

    private const string DateParameterDescription = "Calendar date, `yyyy-MM-dd`.";

    [McpServerTool(Name = "log_weight", ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false)]
    [Description(LogWeightToolDescription)]
    public async Task<LogWeightResponse> LogWeight(
        [Description(DateParameterDescription)] DateOnly date,
        [Description("Weight in kilograms, from 1 to 500, e.g. 72.4.")] decimal weight,
        CancellationToken cancellationToken)
    {
        var roundedWeight = ValidateWeight(weight);
        var logged = await context.WeightLogs.AsNoTracking().FirstOrDefaultAsync(wl => wl.Date == date, cancellationToken);

        if (logged is not null)
        {
            throw new McpException($"{logged.Weight} kg is already logged for {date:yyyy-MM-dd}. Change it with `update_weight`.");
        }

        var weightLog = new DomainWeightLog
        {
            Date = date,
            Weight = roundedWeight
        };

        context.WeightLogs.Add(weightLog);
        await context.SaveChangesAsync(cancellationToken);

        return new LogWeightResponse(weightLog.Date, weightLog.Weight);
    }

    [McpServerTool(Name = "update_weight", ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = false)]
    [Description(UpdateWeightToolDescription)]
    public async Task<UpdateWeightResponse> UpdateWeight(
        [Description(DateParameterDescription)] DateOnly date,
        [Description("New weight in kilograms, from 1 to 500, e.g. 72.4.")] decimal weight,
        CancellationToken cancellationToken)
    {
        var roundedWeight = ValidateWeight(weight);

        var updatedCount = await context.WeightLogs
            .Where(wl => wl.Date == date)
            .ExecuteUpdateAsync(setters => setters.SetProperty(wl => wl.Weight, roundedWeight), cancellationToken);

        if (updatedCount == 0)
        {
            throw new McpException($"No weight is logged for {date:yyyy-MM-dd}. Log one with `log_weight`.");
        }

        return new UpdateWeightResponse(date, roundedWeight);
    }

    [McpServerTool(Name = "delete_weight", ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = false)]
    [Description(DeleteWeightToolDescription)]
    public async Task<DeleteWeightResponse> DeleteWeight(
        [Description(DateParameterDescription)] DateOnly date,
        CancellationToken cancellationToken)
    {
        var deletedCount = await context.WeightLogs
            .Where(wl => wl.Date == date)
            .ExecuteDeleteAsync(cancellationToken);

        if (deletedCount == 0)
        {
            throw new McpException($"No weight is logged for {date:yyyy-MM-dd}.");
        }

        return new DeleteWeightResponse(date);
    }

    private static decimal ValidateWeight(decimal weight) => weight is < MinWeight or > MaxWeight
        ? throw new McpException($"Requested weight {weight}; it must be from {MinWeight} to {MaxWeight} kg.")
        : Math.Round(weight, 2, MidpointRounding.AwayFromZero);
}
