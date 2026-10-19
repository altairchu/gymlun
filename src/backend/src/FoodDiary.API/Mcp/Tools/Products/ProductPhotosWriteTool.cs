using System.ComponentModel;
using FoodDiary.API.Features.ProductPhotos;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace FoodDiary.API.Mcp.Tools.Products;

[McpServerToolType]
[Authorize(Policy = McpExtensions.McpWritePolicy)]
public class ProductPhotosWriteTool(
    PhotoDownloader photoDownloader,
    SetProductPhotoHandler setProductPhotoHandler,
    DeleteProductPhotoHandler deleteProductPhotoHandler)
{
    private const string SetProductPhotoToolDescription =
        """
        Sets the photo shown for one product, by its `id` as returned by `list_products`, replacing the photo it had. The person sees it on the product's card when logging food.

        Pass `photo` when the person attached or took a picture in this chat. Otherwise pass `photoUrl`, a public https link straight to an image file, not to a web page. JPEG, PNG, WebP, GIF, BMP and TIFF images up to 10 MB work; the photo is stored scaled down to 640 pixels on its longer side.
        """;

    private const string RemoveProductPhotoToolDescription =
        """
        Removes the photo of one product, by its `id` as returned by `list_products`. The product itself and its food logs stay as they are.
        """;

    [McpServerTool(Name = "set_product_photo", ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [McpMeta("openai/fileParams", JsonValue = """["photo"]""")]
    [Description(SetProductPhotoToolDescription)]
    public async Task<SetProductPhotoToolResponse> SetProductPhoto(
        [Description("Id of the product.")] int productId,
        // Not nullable: a nullable object renders its strings as nullable too, which ChatGPT does not take for a file
        [Description("The picture the person attached in the chat.")] ChatFile photo = null!,
        [Description("Public https URL of an image, when no picture is attached.")] string? photoUrl = null,
        CancellationToken cancellationToken = default)
    {
        var url = photo?.DownloadUrl ?? photoUrl;

        if (string.IsNullOrWhiteSpace(url))
        {
            throw new McpException("No photo came through. Ask the person to attach the picture in the same message and call again with it in `photo`, or pass `photoUrl`.");
        }

        await using var image = await photoDownloader.Download(url, cancellationToken);
        var result = await setProductPhotoHandler.Handle(productId, image, cancellationToken);

        return result switch
        {
            SetProductPhotoResult.Success => new SetProductPhotoToolResponse(productId, HasPhoto: true),
            SetProductPhotoResult.ProductNotFound => throw new McpException($"Product {productId} not found. Look it up with `list_products`."),
            SetProductPhotoResult.NotAnImage => throw new McpException("That file is not a supported image. Use a JPEG, PNG, WebP, GIF, BMP or TIFF image."),
            _ => throw new McpException($"The photo of product {productId} could not be set.")
        };
    }

    [McpServerTool(Name = "remove_product_photo", ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = false)]
    [Description(RemoveProductPhotoToolDescription)]
    public async Task<RemoveProductPhotoToolResponse> RemoveProductPhoto(
        [Description("Id of the product.")] int productId,
        CancellationToken cancellationToken)
    {
        return await deleteProductPhotoHandler.Handle(productId, cancellationToken)
            ? new RemoveProductPhotoToolResponse(productId, HasPhoto: false)
            : throw new McpException($"Product {productId} not found. Look it up with `list_products`.");
    }
}
