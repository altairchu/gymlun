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
        Logs the person's weight in kilograms on one date, and returns it. A date holds one weight: once one is logged for a date, another cannot be.

        `date` is a calendar date with no time or timezone: use the person's local date. Convert pounds to kilograms first (1 lb = 0.4536 kg).
        """;

    [McpServerTool(Name = "log_weight", ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false)]
    [Description(LogWeightToolDescription)]
    public async Task<LogWeightResponse> LogWeight(
        [Description("Calendar date, `yyyy-MM-dd`.")] DateOnly date,
        [Description("Weight in kilograms, from 1 to 500, e.g. 72.4.")] decimal weight,
        CancellationToken cancellationToken)
    {
        if (weight is < MinWeight or > MaxWeight)
        {
            throw new McpException($"Requested weight {weight}; it must be from {MinWeight} to {MaxWeight} kg.");
        }

        var logged = await context.WeightLogs.AsNoTracking().FirstOrDefaultAsync(wl => wl.Date == date, cancellationToken);

        if (logged is not null)
        {
            throw new McpException($"{logged.Weight} kg is already logged for {date:yyyy-MM-dd}.");
        }

        var weightLog = new DomainWeightLog
        {
            Date = date,
            Weight = Math.Round(weight, 2, MidpointRounding.AwayFromZero)
        };

        context.WeightLogs.Add(weightLog);
        await context.SaveChangesAsync(cancellationToken);

        return new LogWeightResponse(weightLog.Date, weightLog.Weight);
    }
}
