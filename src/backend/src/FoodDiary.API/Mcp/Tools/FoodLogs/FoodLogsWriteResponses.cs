using JetBrains.Annotations;

namespace FoodDiary.API.Mcp.Tools.FoodLogs;

[PublicAPI]
public record LogFoodResponse(DateOnly Date, string MealType, FoodLog FoodLog);

[PublicAPI]
public record LogFoodsResponse(DateOnly Date, string MealType, IReadOnlyList<FoodLog> FoodLogs);

[PublicAPI]
public record UpdateFoodLogResponse(DateOnly Date, string MealType, FoodLog FoodLog);

[PublicAPI]
public record DeleteFoodLogsResponse(IReadOnlyList<int> DeletedIds);
