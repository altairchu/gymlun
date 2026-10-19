using FoodDiary.API.Features.Workouts.Contracts;

namespace FoodDiary.API.Features.Workouts;

public class GetExercisesHandler(ExerciseHistory exerciseHistory)
{
    public async Task<IResult> Handle(CancellationToken cancellationToken)
    {
        var exercises = await exerciseHistory.MostRecentlyDoneFirst(cancellationToken);

        return Results.Ok(new GetExercisesResponse(exercises.Select(exercise => exercise.Name).ToList()));
    }
}
