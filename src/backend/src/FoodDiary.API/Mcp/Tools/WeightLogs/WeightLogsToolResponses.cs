using JetBrains.Annotations;

namespace FoodDiary.API.Mcp.Tools.WeightLogs;

[PublicAPI]
public record GetWeightLogsToolResponse(DateOnly From, DateOnly To, IReadOnlyList<WeightLog> WeightLogs);

[PublicAPI]
public record WeightLog(DateOnly Date, decimal Weight);

[PublicAPI]
public record LogWeightResponse(DateOnly Date, decimal Weight);

[PublicAPI]
public record UpdateWeightResponse(DateOnly Date, decimal Weight);

[PublicAPI]
public record DeleteWeightResponse(DateOnly DeletedDate);
