using FoodDiary.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.API.Features.ProductPhotos;

public class DeleteProductPhotoHandler(FoodDiaryContext context)
{
    /// <returns>False when there is no such product</returns>
    public async Task<bool> Handle(int productId, CancellationToken cancellationToken)
    {
        var product = await context.Products
            .Include(p => p.Photo)
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

        if (product is null)
        {
            return false;
        }

        product.Photo = null;
        product.PhotoUpdatedAt = null;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
