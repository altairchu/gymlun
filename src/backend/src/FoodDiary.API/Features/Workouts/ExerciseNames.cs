using FoodDiary.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.API.Features.Workouts;

public class ExerciseNames(FoodDiaryContext context)
{
    /// <summary>
    /// Trims each name and swaps it for the spelling already logged when one matches it, ignoring case.
    /// The name of the set <c>exceptSetId</c> does not count as logged, so that set can be respelled
    /// </summary>
    public async Task<IReadOnlyList<string>> ReuseLoggedSpellings(
        IReadOnlyCollection<string> exercises,
        int? exceptSetId,
        CancellationToken cancellationToken)
    {
        var lowerCaseNames = exercises.Select(exercise => exercise.Trim().ToLowerInvariant()).Distinct().ToList();

        var loggedSpellings = await context.WorkoutSets
            .AsNoTracking()
            .Where(s => s.Id != exceptSetId && lowerCaseNames.Contains(s.Exercise.ToLower()))
            .GroupBy(s => s.Exercise)
            .Select(g => new { Exercise = g.Key, FirstSetId = g.Min(s => s.Id) })
            .ToListAsync(cancellationToken);

        return ReuseSpellings(exercises, loggedSpellings.OrderBy(s => s.FirstSetId).Select(s => s.Exercise));
    }

    /// <summary>
    /// When several of <c>loggedSpellings</c> match a name, the first one wins. A name that matches none of them
    /// takes the spelling of its first occurrence in <c>exercises</c>
    /// </summary>
    public static IReadOnlyList<string> ReuseSpellings(IEnumerable<string> exercises, IEnumerable<string> loggedSpellings)
    {
        var spellings = new Dictionary<string, string>();

        foreach (var spelling in loggedSpellings)
        {
            spellings.TryAdd(spelling.ToLowerInvariant(), spelling);
        }

        return exercises
            .Select(exercise => exercise.Trim())
            .Select(exercise =>
            {
                var key = exercise.ToLowerInvariant();
                spellings.TryAdd(key, exercise);
                return spellings[key];
            })
            .ToList();
    }
}
