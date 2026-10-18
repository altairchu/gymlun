using FoodDiary.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.API.Features.WeightTracking;

public class DeleteWeightLogHandler(FoodDiaryContext context)
{
    public async Task<IResult> Handle(DateOnly date, CancellationToken cancellationToken)
    {
        var deletedCount = await context.WeightLogs
            .Where(wl => wl.Date == date)
            .ExecuteDeleteAsync(cancellationToken);

        return deletedCount == 0 ? Results.NotFound() : Results.Ok();
    }
}
