using JetBrains.Annotations;

namespace FoodDiary.API.Mcp.Tools.Products;

[PublicAPI]
public record SetProductPhotoToolResponse(int ProductId, bool HasPhoto);

[PublicAPI]
public record RemoveProductPhotoToolResponse(int ProductId, bool HasPhoto);
