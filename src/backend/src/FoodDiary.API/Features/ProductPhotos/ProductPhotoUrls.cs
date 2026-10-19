using System.Globalization;
using FoodDiary.Domain.Entities;

namespace FoodDiary.API.Features.ProductPhotos;

public static class ProductPhotoUrls
{
    public static string? PhotoUrl(this Product product) => product.PhotoUpdatedAt is { } updatedAt
        ? $"/api/v1/products/{product.Id}/photo?v={ToVersion(updatedAt)}"
        : null;

    public static string ToVersion(DateTimeOffset photoUpdatedAt) =>
        photoUpdatedAt.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture);
}
