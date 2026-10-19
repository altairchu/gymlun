using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FoodDiary.API.Mcp.Tools.Products;
using FoodDiary.ComponentTests.Dsl;
using FoodDiary.Domain.Entities;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace FoodDiary.ComponentTests.Scenarios.Mcp;

public partial class McpApiContext
{
    private Tool _setProductPhotoTool = null!;

    public Task Given_png_photo_can_be_downloaded_from(string url, int width, int height)
    {
        var photo = TestImages.Png(width, height);

        Factory = Factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services => services
            .AddHttpClient(PhotoDownloader.HttpClientName)
            .ConfigurePrimaryHttpMessageHandler(() => new PhotoServer(new Uri(url), photo))));

        return Task.CompletedTask;
    }

    public async Task When_mcp_client_calls_set_product_photo_with_photo_attached_in_chat(Product product, string downloadUrl)
    {
        await When_mcp_client_calls_listed_tool("set_product_photo", new Dictionary<string, object?>
        {
            ["productId"] = product.Id,
            ["photo"] = JsonSerializer.SerializeToElement(new Dictionary<string, string>
            {
                ["download_url"] = downloadUrl,
                ["file_id"] = "file_0001"
            })
        });
    }

    public Task When_mcp_client_calls_set_product_photo_with_photo_url(Product product, string photoUrl)
    {
        return When_mcp_client_calls_listed_tool("set_product_photo", new Dictionary<string, object?>
        {
            ["productId"] = product.Id,
            ["photoUrl"] = photoUrl
        });
    }

    public Task When_mcp_client_calls_get_product_photo(Product product)
    {
        return When_mcp_client_calls_listed_tool("get_product_photo", new Dictionary<string, object?>
        {
            ["productId"] = product.Id
        });
    }

    public Task When_mcp_client_calls_remove_product_photo(Product product)
    {
        return When_mcp_client_calls_listed_tool("remove_product_photo", new Dictionary<string, object?>
        {
            ["productId"] = product.Id
        });
    }

    public async Task When_mcp_client_lists_set_product_photo_tool()
    {
        await using var mcpClient = await ConnectMcpClient();

        var tools = await mcpClient.ListToolsAsync();
        _listedToolNames = tools.Select(tool => tool.Name).ToList();
        _setProductPhotoTool = tools.Single(tool => tool.Name == "set_product_photo").ProtocolTool;
    }

    public Task Then_set_product_photo_tool_takes_photo_attached_in_chat()
    {
        _setProductPhotoTool.Meta!["openai/fileParams"]!.AsArray()
            .Select(param => param!.GetValue<string>())
            .Should().Equal("photo");

        var photo = _setProductPhotoTool.InputSchema.GetProperty("properties").GetProperty("photo");
        photo.GetProperty("properties").EnumerateObject().Select(property => property.Name)
            .Should().BeEquivalentTo("download_url", "file_id", "mime_type", "file_name");
        photo.GetProperty("required").EnumerateArray().Select(property => property.GetString())
            .Should().BeEquivalentTo("download_url", "file_id");

        return Task.CompletedTask;
    }

    public async Task Then_product_photo_is_saved_scaled_down(Product product, int width, int height)
    {
        _toolResult.IsError.Should().NotBe(true, ToolResultText());

        using var response = JsonDocument.Parse(ToolResultText());
        response.RootElement.GetProperty("productId").GetInt32().Should().Be(product.Id);
        response.RootElement.GetProperty("hasPhoto").GetBoolean().Should().BeTrue();

        var photo = await QueryDatabase(db => db.ProductPhotos.AsNoTracking().SingleAsync(p => p.ProductId == product.Id));
        var size = TestImages.SizeOf(photo.Content);
        (size.Width, size.Height).Should().Be((width, height));
    }

    public Task Then_product_photo_is_returned_as_jpeg()
    {
        _toolResult.IsError.Should().NotBe(true);

        var image = _toolResult.Content.OfType<ImageContentBlock>().Single();
        image.MimeType.Should().Be("image/jpeg");
        TestImages.SizeOf(image.DecodedData.ToArray()).Width.Should().BePositive();

        return Task.CompletedTask;
    }

    public async Task Then_product_has_no_photo(Product product)
    {
        _toolResult.IsError.Should().NotBe(true, ToolResultText());

        var savedProduct = await QueryDatabase(db => db.Products.AsNoTracking().SingleAsync(p => p.Id == product.Id));
        savedProduct.PhotoUpdatedAt.Should().BeNull();
        (await QueryDatabase(db => db.ProductPhotos.AnyAsync(p => p.ProductId == product.Id))).Should().BeFalse();
    }

    public Task Then_products_show_photo()
    {
        _toolResult.IsError.Should().NotBe(true, ToolResultText());

        using var products = JsonDocument.Parse(ToolResultText());
        products.RootElement.GetProperty("products").EnumerateArray().Single()
            .GetProperty("hasPhoto").GetBoolean().Should().BeTrue();

        return Task.CompletedTask;
    }

    private sealed class PhotoServer(Uri url, byte[] photo) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(request.RequestUri == url
                ? new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(photo) { Headers = { ContentType = new MediaTypeHeaderValue("image/png") } }
                }
                : new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }
}
