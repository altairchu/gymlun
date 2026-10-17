namespace FoodDiary.Domain.Enums;

public static class MealTypeTimes
{
    private static readonly TimeOnly SecondBreakfastStart = new(10, 0);
    private static readonly TimeOnly BreakfastStart = new(4, 0);
    private static readonly TimeOnly LunchStart = new(12, 0);
    private static readonly TimeOnly AfternoonSnackStart = new(15, 0);
    private static readonly TimeOnly DinnerStart = new(18, 0);

    extension(MealType mealType)
    {
        /// <summary>
        /// The time a food log of this meal is given when no time of day is known
        /// </summary>
        public TimeOnly DefaultTime => mealType switch
        {
            MealType.Breakfast => new TimeOnly(8, 0),
            MealType.SecondBreakfast => new TimeOnly(10, 30),
            MealType.Lunch => new TimeOnly(12, 30),
            MealType.AfternoonSnack => new TimeOnly(15, 30),
            MealType.Dinner => new TimeOnly(19, 0),
            _ => throw new ArgumentOutOfRangeException(nameof(mealType), mealType, "Unknown meal type")
        };
    }

    extension(MealType)
    {
        public static MealType FromTime(TimeOnly time) => time switch
        {
            _ when time >= BreakfastStart && time < SecondBreakfastStart => MealType.Breakfast,
            _ when time >= SecondBreakfastStart && time < LunchStart => MealType.SecondBreakfast,
            _ when time >= LunchStart && time < AfternoonSnackStart => MealType.Lunch,
            _ when time >= AfternoonSnackStart && time < DinnerStart => MealType.AfternoonSnack,
            _ => MealType.Dinner
        };

        /// <summary>
        /// Completes a food log's time of day and meal from whichever of them is known.
        /// Returns null when neither is.
        /// </summary>
        public static (TimeOnly Time, MealType MealType)? ResolveTimeAndMeal(TimeOnly? time, MealType? mealType) =>
            (time, mealType) switch
            {
                ({ } t, { } m) => (t, m),
                ({ } t, null) => (t, MealType.FromTime(t)),
                (null, { } m) => (m.DefaultTime, m),
                _ => null
            };
    }
}
