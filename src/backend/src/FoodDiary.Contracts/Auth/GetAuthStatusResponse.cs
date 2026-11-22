using JetBrains.Annotations;

namespace FoodDiary.Contracts.Auth;

[PublicAPI]
public class GetAuthStatusResponse
{
    public bool IsAuthenticated { get; init; }
    public string Provider { get; init; } = "google";
    public string? AccountId { get; init; }
}