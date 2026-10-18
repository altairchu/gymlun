using System.ComponentModel;
using FoodDiary.Infrastructure;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace FoodDiary.API.Mcp.Tools.WeightLogs;

[McpServerToolType]
public class WeightLogsTool(FoodDiaryContext context)
{
    internal const int MaxRangeDays = 366;

    private const string GetWeightLogsToolDescription =
        """
        The person's weight over a date range, oldest first: at most one weight per date, in kilograms. Dates without a weigh-in are left out.

        `from` and `to` are inclusive calendar dates with no time or timezone. The range spans at most 366 days; split longer periods into several calls.
        """;

    private const string DateParameterDescription = "Inclusive calendar date, `yyyy-MM-dd`.";

    [McpServerTool(Name = "get_weight_logs", ReadOnly = true, OpenWorld = false)]
    [Description(GetWeightLogsToolDescription)]
    public async Task<GetWeightLogsToolResponse> GetWeightLogs(
        [Description(DateParameterDescription)] DateOnly from,
        [Description(DateParameterDescription)] DateOnly to,
        CancellationToken cancellationToken)
    {
        if (from > to)
        {
            throw new McpException($"`from` {from:yyyy-MM-dd} is after `to` {to:yyyy-MM-dd}.");
        }

        var rangeDays = to.DayNumber - from.DayNumber + 1;

        if (rangeDays > MaxRangeDays)
        {
            throw new McpException($"Requested {rangeDays} days; the maximum is {MaxRangeDays}. Split the range.");
        }

        var weightLogs = await context.WeightLogs
            .AsNoTracking()
            .Where(wl => wl.Date >= from && wl.Date <= to)
            .OrderBy(wl => wl.Date)
            .Select(wl => new WeightLog(wl.Date, wl.Weight))
            .ToListAsync(cancellationToken);

        return new GetWeightLogsToolResponse(from, to, weightLogs);
    }
}
