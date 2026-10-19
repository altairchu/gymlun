using FoodDiary.Domain.Workouts;
using LightBDD.Core.Formatting.Values;

namespace FoodDiary.ComponentTests.Formatting;

public class WorkoutSetFormatter : IValueFormatter
{
    public string FormatValue(object value, IValueFormattingService formattingService)
    {
        if (value is not WorkoutSet workoutSet)
        {
            throw new FormatterNotRegisteredException(value);
        }

        return $"{workoutSet.Date:R} - {workoutSet.Exercise} {workoutSet.Reps} x {workoutSet.WeightKg} kg";
    }
}
