using FoodDiary.API.Features.WeightTracking.Contracts;
using FoodDiary.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.API.Features.WeightTracking;

public class UpdateWeightLogHandler(FoodDiaryContext context)
{
    public async Task<IResult> Handle(DateOnly date, UpdateWeightLogBody request, CancellationToken cancellationToken)
    {
        var updatedCount = await context.WeightLogs
            .Where(wl => wl.Date == date)
            .ExecuteUpdateAsync(setters => setters.SetProperty(wl => wl.Weight, request.Value), cancellationToken);

        return updatedCount == 0 ? Results.NotFound() : Results.Ok();
    }
}
