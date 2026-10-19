using FoodDiary.Domain.Entities;
using FoodDiary.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.API.Features.ProductPhotos;

public abstract record SetProductPhotoResult
{
    public record Success(string PhotoUrl) : SetProductPhotoResult;

    public record ProductNotFound : SetProductPhotoResult;

    public record NotAnImage : SetProductPhotoResult;
}

public class SetProductPhotoHandler(
    FoodDiaryContext context,
    ProductPhotoProcessor processor,
    TimeProvider timeProvider)
{
    public async Task<SetProductPhotoResult> Handle(int productId, Stream image, CancellationToken cancellationToken)
    {
        var product = await context.Products
            .Include(p => p.Photo)
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

        if (product is null)
        {
            return new SetProductPhotoResult.ProductNotFound();
        }

        var jpeg = await processor.ToJpeg(image, cancellationToken);

        if (jpeg is null)
        {
            return new SetProductPhotoResult.NotAnImage();
        }

        if (product.Photo is null)
        {
            product.Photo = new ProductPhoto { Content = jpeg };
        }
        else
        {
            product.Photo.Content = jpeg;
        }

        product.PhotoUpdatedAt = timeProvider.GetUtcNow();
        await context.SaveChangesAsync(cancellationToken);
        return new SetProductPhotoResult.Success(product.PhotoUrl()!);
    }
}
