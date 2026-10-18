using System.ComponentModel;
using FoodDiary.API.Features.ProductPhotos;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace FoodDiary.API.Mcp.Tools.Products;

[McpServerToolType]
public class ProductPhotosTool(GetProductPhotoHandler getProductPhotoHandler)
{
    private const string GetProductPhotoToolDescription =
        """
        Returns the photo of one product, by its `id` as returned by `list_products`, as a JPEG image. Only products whose `hasPhoto` is true have one.
        """;

    [McpServerTool(Name = "get_product_photo", ReadOnly = true, OpenWorld = false)]
    [Description(GetProductPhotoToolDescription)]
    public async Task<ImageContentBlock> GetProductPhoto(
        [Description("Id of the product.")] int productId,
        CancellationToken cancellationToken)
    {
        var photo = await getProductPhotoHandler.Handle(productId, cancellationToken)
                    ?? throw new McpException($"Product {productId} has no photo, or there is no such product.");

        return ImageContentBlock.FromBytes(photo.Content, "image/jpeg");
    }
}
