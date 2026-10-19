using FoodDiary.API.Mcp.Tools.Workouts;
using FoodDiary.Domain.Workouts;
using ModelContextProtocol;

namespace FoodDiary.UnitTests.Mcp;

public class WorkoutsToolTests
{
    private static readonly DateOnly September1 = new(2026, 9, 1);
    private static readonly DateOnly September2 = new(2026, 9, 2);

    private static WorkoutSet Set(int id, DateOnly date, string exercise, int reps = 10, decimal weightKg = 60m) => new()
    {
        Id = id,
        Date = date,
        Exercise = exercise,
        Reps = reps,
        WeightKg = weightKg
    };

    private static LogWorkoutExercise Exercise(string exercise, params (int Reps, decimal WeightKg)[] sets) => new()
    {
        Exercise = exercise,
        Sets = sets.Select(set => new LogWorkoutSet { Reps = set.Reps, WeightKg = set.WeightKg }).ToArray()
    };

    [Fact]
    public void WorkoutDays_RunOldestFirst()
    {
        var days = new[] { Set(1, September2, "Squat"), Set(2, September1, "Bench Press") }.ToWorkoutDays();

        days.Select(day => day.Date).Should().Equal(September1, September2);
    }

    [Fact]
    public void Exercises_RunInOrderOfTheirFirstSet_AndKeepTheirSetsInOrderOfId()
    {
        WorkoutSet[] sets =
        [
            Set(4, September1, "Bench Press", reps: 6),
            Set(2, September1, "Pull-up", reps: 8),
            Set(1, September1, "Bench Press", reps: 10),
            Set(3, September1, "Pull-up", reps: 7)
        ];

        var day = sets.ToWorkoutDays().Single();

        day.Exercises.Select(exercise => exercise.Exercise).Should().Equal("Bench Press", "Pull-up");
        day.Exercises[0].Sets.Select(set => (set.Id, set.Reps)).Should().Equal((1, 10), (4, 6));
        day.Exercises[1].Sets.Select(set => (set.Id, set.Reps)).Should().Equal((2, 8), (3, 7));
    }

    [Fact]
    public void LogWorkout_WithinLimits_IsValid()
    {
        var exercises = Enumerable.Range(0, LogWorkoutArgument.MaxExercises)
            .Select(index => Exercise(new string('x', WorkoutSet.MaxExerciseLength), (1, 0m), (1000, 1000m), (5, 0.5m), (5, 2.5m), (5, 1m)))
            .ToArray();

        var act = () => LogWorkoutArgument.Validate(exercises);

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    public void LogWorkout_WithExerciseCountOutOfRange_IsRejected(int count)
    {
        var exercises = Enumerable.Range(0, count).Select(_ => Exercise("Squat", (5, 100m))).ToArray();

        var act = () => LogWorkoutArgument.Validate(exercises);

        act.Should().ThrowExactly<McpException>().WithMessage($"Requested {count} exercises; pass 1 to 20.");
    }

    [Fact]
    public void LogWorkout_WithMoreThan100SetsInTotal_IsRejected()
    {
        var sets = Enumerable.Repeat((5, 100m), 51).ToArray();

        var act = () => LogWorkoutArgument.Validate([Exercise("Squat", sets), Exercise("Deadlift", sets)]);

        act.Should().ThrowExactly<McpException>().WithMessage("Requested 102 sets in total; pass at most 100.*");
    }

    [Fact]
    public void LogWorkout_WithExerciseWithoutSets_IsRejected()
    {
        var act = () => LogWorkoutArgument.Validate([Exercise("Squat", (5, 100m)), Exercise(" Deadlift ")]);

        act.Should().ThrowExactly<McpException>().WithMessage("`exercises[1]` (Deadlift): pass at least 1 set.*");
    }

    [Theory]
    [InlineData("  ", "Exercise is required.")]
    [InlineData(null, "Exercise is required.")]
    public void LogWorkout_WithoutExerciseName_IsRejected(string? name, string message)
    {
        var act = () => LogWorkoutArgument.Validate([Exercise(name!, (5, 100m))]);

        act.Should().ThrowExactly<McpException>().WithMessage($"`exercises[0]`: {message}*");
    }

    [Fact]
    public void LogWorkout_WithExerciseNameLongerThan100Characters_IsRejected()
    {
        var act = () => LogWorkoutArgument.Validate([Exercise(new string('x', 101), (5, 100m))]);

        act.Should().ThrowExactly<McpException>()
            .WithMessage("`exercises[0]`: Exercise is 101 characters long; it must be at most 100.*");
    }

    [Theory]
    [InlineData(0, 60, "Requested 0 reps; they must be from 1 to 1000.")]
    [InlineData(1001, 60, "Requested 1001 reps; they must be from 1 to 1000.")]
    [InlineData(10, -0.5, "Requested weight -0.5; it must be from 0 to 1000 kg.")]
    [InlineData(10, 1000.5, "Requested weight 1000.5; it must be from 0 to 1000 kg.")]
    public void LogWorkout_WithSetOutOfRange_IsRejectedNamingTheSet(int reps, double weightKg, string message)
    {
        var act = () => LogWorkoutArgument.Validate(
            [Exercise("Bench Press", (10, 60m)), Exercise("Squat", (5, 100m), (reps, (decimal)weightKg))]);

        act.Should().ThrowExactly<McpException>().WithMessage($"`exercises[1].sets[1]` (Squat): {message} Nothing was logged.");
    }
}
