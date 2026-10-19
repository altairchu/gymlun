using FoodDiary.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.API.Features.ProductPhotos;

public record ProductPhotoFile(byte[] Content, string Version);

public class GetProductPhotoHandler(FoodDiaryContext context)
{
    public async Task<ProductPhotoFile?> Handle(int productId, CancellationToken cancellationToken)
    {
        var photo = await context.ProductPhotos
            .AsNoTracking()
            .Where(p => p.ProductId == productId)
            .Join(
                context.Products,
                photo => photo.ProductId,
                product => product.Id,
                (photo, product) => new { photo.Content, product.PhotoUpdatedAt })
            .FirstOrDefaultAsync(cancellationToken);

        return photo?.PhotoUpdatedAt is { } updatedAt
            ? new ProductPhotoFile(photo.Content, ProductPhotoUrls.ToVersion(updatedAt))
            : null;
    }
}
