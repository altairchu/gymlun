using FoodDiary.API.Features.Workouts.Contracts;
using FoodDiary.Domain.Workouts;

namespace FoodDiary.API.Features.Workouts;

public static class Mappings
{
    public static WorkoutSetItem ToWorkoutSetItem(this WorkoutSet set) =>
        new(set.Id, set.Date, set.Exercise, set.Reps, set.WeightKg);
}
