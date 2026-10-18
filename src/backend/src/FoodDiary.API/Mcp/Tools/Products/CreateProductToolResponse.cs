using JetBrains.Annotations;

namespace FoodDiary.API.Mcp.Tools.Products;

[PublicAPI]
public record CreateProductToolResponse(ListProductsToolResponse.Product Product);

[PublicAPI]
public record UpdateProductToolResponse(ListProductsToolResponse.Product Product);

[PublicAPI]
public record DeleteProductToolResponse(int DeletedId);
