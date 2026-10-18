using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FoodDiary.API.Features.ProductPhotos.Contracts;
using FoodDiary.API.Features.Products.Contracts;
using FoodDiary.ComponentTests.Dsl;
using FoodDiary.ComponentTests.Infrastructure;
using FoodDiary.Domain.Entities;
using JetBrains.Annotations;

namespace FoodDiary.ComponentTests.Scenarios.ProductPhotos;

[UsedImplicitly]
public class ProductPhotosApiContext(FoodDiaryWebApplicationFactory factory) : BaseContext(factory)
{
    private HttpResponseMessage _setPhotoResponse = null!;
    private SetProductPhotoResponse? _setPhoto;
    private HttpResponseMessage _removePhotoResponse = null!;
    private GetProductByIdResponse? _product;
    private HttpResponseMessage _photoResponse = null!;

    public Task Given_products(params Product[] products)
    {
        return Factory.SeedDataAsync(products);
    }

    public Task When_user_sets_png_photo(Product product, int width, int height)
    {
        return SetPhoto(product, TestImages.Png(width, height));
    }

    public Task When_user_sets_text_file_as_photo(Product product)
    {
        return SetPhoto(product, "not an image"u8.ToArray());
    }

    private async Task SetPhoto(Product product, byte[] photo)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(photo) { Headers = { ContentType = new MediaTypeHeaderValue("image/png") } }, "photo", "photo.png");

        _setPhotoResponse = await ApiClient.PutAsync($"/api/v1/products/{product.Id}/photo", content);

        if (_setPhotoResponse.IsSuccessStatusCode)
        {
            _setPhoto = await _setPhotoResponse.Content.ReadFromJsonAsync<SetProductPhotoResponse>();
        }
    }

    public async Task When_user_removes_photo(Product product)
    {
        _removePhotoResponse = await ApiClient.DeleteAsync($"/api/v1/products/{product.Id}/photo");
    }

    public async Task When_user_retrieves_product_by_id(Product product)
    {
        _product = await ApiClient.GetFromJsonAsync<GetProductByIdResponse>($"/api/v1/products/{product.Id}");
    }

    public async Task When_user_downloads_the_photo_url_that_was_set()
    {
        _photoResponse = await ApiClient.GetAsync(_setPhoto!.PhotoUrl);
    }

    public async Task When_user_downloads_photo(Product product)
    {
        _photoResponse = await ApiClient.GetAsync($"/api/v1/products/{product.Id}/photo");
    }

    public Task Then_photo_is_set()
    {
        _setPhotoResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        _setPhoto!.PhotoUrl.Should().MatchRegex(@"^/api/v1/products/\d+/photo\?v=\d+$");
        return Task.CompletedTask;
    }

    public Task Then_product_shows_the_photo_url_that_was_set()
    {
        _product!.PhotoUrl.Should().Be(_setPhoto!.PhotoUrl);
        return Task.CompletedTask;
    }

    public async Task Then_photo_is_cacheable_jpeg_of_size(int width, int height)
    {
        _photoResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        _photoResponse.Content.Headers.ContentType!.MediaType.Should().Be("image/jpeg");
        _photoResponse.Headers.CacheControl!.MaxAge.Should().Be(TimeSpan.FromDays(365));
        _photoResponse.Headers.CacheControl.Private.Should().BeTrue();

        var photo = await _photoResponse.Content.ReadAsByteArrayAsync();
        var size = TestImages.SizeOf(photo);
        (size.Width, size.Height).Should().Be((width, height));
    }

    public Task Then_photo_is_removed()
    {
        _removePhotoResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        return Task.CompletedTask;
    }

    public Task Then_product_has_no_photo()
    {
        _product!.PhotoUrl.Should().BeNull();
        return Task.CompletedTask;
    }

    public Task Then_photo_is_not_found()
    {
        _photoResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        return Task.CompletedTask;
    }

    public Task Then_photo_is_rejected()
    {
        _setPhotoResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        return Task.CompletedTask;
    }
}
