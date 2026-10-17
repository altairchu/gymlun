using FoodDiary.API.Features.Products.Contracts;
using FoodDiary.API.Features.Products.Extensions;
using FoodDiary.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.API.Features.Products;

public abstract record GetProductByIdHandlerResult
{
    public record Success(GetProductByIdResponse Product) : GetProductByIdHandlerResult;

    public record NotFound : GetProductByIdHandlerResult;
}

public class GetProductByIdHandler(FoodDiaryContext context)
{
    public async Task<GetProductByIdHandlerResult> Handle(int id, CancellationToken cancellationToken)
    {
        var product = await context.Products
            .AsNoTracking()
            .Include(p => p.Categories)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        return product is null
            ? new GetProductByIdHandlerResult.NotFound()
            : new GetProductByIdHandlerResult.Success(product.ToGetProductByIdResponse());
    }
}
