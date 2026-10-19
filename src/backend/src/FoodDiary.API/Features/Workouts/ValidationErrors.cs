namespace FoodDiary.API.Features.Workouts;

internal class ValidationErrors
{
    private readonly Dictionary<string, string[]> _errors = [];

    public void Add(string field, string? error)
    {
        if (error is not null)
        {
            _errors.TryAdd(field, [error]);
        }
    }

    public IResult? ToResult() => _errors.Count > 0 ? Results.ValidationProblem(_errors) : null;
}
