using JetBrains.Annotations;

namespace FoodDiary.API.Mcp.Tools.FoodLogs;

[PublicAPI]
public record LogFoodResponse(DateOnly Date, string MealType, FoodLog FoodLog);

[PublicAPI]
public record DeleteFoodLogResponse(int DeletedId);
