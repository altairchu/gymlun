using FoodDiary.API.Features.Workouts;

namespace FoodDiary.UnitTests.Workouts;

public class WorkoutsTests
{
    private static readonly DateOnly September1 = new(2026, 9, 1);
    private static readonly DateOnly September2 = new(2026, 9, 2);

    [Fact]
    public void ExerciseName_MatchingLoggedOneIgnoringCase_TakesLoggedSpelling()
    {
        var names = ExerciseNames.ReuseSpellings(["  bench PRESS ", "Squat"], ["Bench Press"]);

        names.Should().Equal("Bench Press", "Squat");
    }

    [Fact]
    public void ExerciseName_MatchingSeveralLoggedOnes_TakesTheFirst()
    {
        var names = ExerciseNames.ReuseSpellings(["bench press"], ["Bench press", "Bench Press"]);

        names.Should().Equal("Bench press");
    }

    [Fact]
    public void NewExerciseName_RepeatedInDifferentCase_KeepsItsFirstSpelling()
    {
        var names = ExerciseNames.ReuseSpellings(["Pull-up ", "PULL-UP"], []);

        names.Should().Equal("Pull-up", "Pull-up");
    }

    [Fact]
    public void Exercises_RunMostRecentlyDoneFirst_ThenByLastSetLogged()
    {
        ExerciseDay[] days =
        [
            new("Deadlift", September1, 1),
            new("Bench Press", September2, 3),
            new("Squat", September2, 2),
            new("Bench Press", September1, 4)
        ];

        var exercises = ExerciseHistory.Summarize(days);

        exercises.Should().Equal(
            new ExerciseSummary("Bench Press", September2, 2),
            new ExerciseSummary("Squat", September2, 1),
            new ExerciseSummary("Deadlift", September1, 1));
    }

    [Fact]
    public void Exercises_AreAtMost200()
    {
        var days = Enumerable.Range(1, 250).Select(id => new ExerciseDay($"Exercise {id}", September1, id));

        var exercises = ExerciseHistory.Summarize(days);

        exercises.Should().HaveCount(ExerciseHistory.MaxExercises);
        exercises[0].Name.Should().Be("Exercise 250");
    }

    [Theory]
    [InlineData(62.504, 62.5)]
    [InlineData(62.505, 62.51)]
    [InlineData(0, 0)]
    public void WeightKg_IsRoundedToHundredthsHalfAwayFromZero(double weightKg, double expected)
    {
        WorkoutSetRules.RoundWeightKg((decimal)weightKg).Should().Be((decimal)expected);
    }

    [Theory]
    [InlineData("2025-09-01", "2026-09-01", null)]
    [InlineData("2025-09-01", "2026-09-02", "Requested 367 days; the maximum is 366. Split the range.")]
    [InlineData("2026-09-02", "2026-09-01", "`from` 2026-09-02 is after `to` 2026-09-01.")]
    public void Range_SpansAtMost366Days(string from, string to, string? error)
    {
        WorkoutSetRules.RangeError(DateOnly.Parse(from), DateOnly.Parse(to)).Should().Be(error);
    }
}
