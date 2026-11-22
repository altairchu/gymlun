namespace FoodDiary.Configuration;

public sealed class AtlasAppleAuthOptions
{
    public const string SectionName = "AtlasApple";

    public bool Enabled { get; init; }
    public string Authority { get; init; } = string.Empty;
    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;
}
