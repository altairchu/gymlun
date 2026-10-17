using FoodDiary.Domain.Entities;
using FoodDiary.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.API.Features.Products;

public record SearchProductsResult(IReadOnlyCollection<Product> Products);

public class SearchProductsHandler(FoodDiaryContext context)
{
    public async Task<SearchProductsResult> Handle(CancellationToken cancellationToken)
    {
        var products = await context.Products
            .AsNoTracking()
            .Include(p => p.Categories)
            // Postgres sorts nulls first in descending order: never logged products have to go last
            .OrderByDescending(p => p.Notes.Max(n => (DateOnly?)n.Date).HasValue)
            .ThenByDescending(p => p.Notes.Max(n => (DateOnly?)n.Date))
            .ThenBy(p => p.Name)
            .ToListAsync(cancellationToken);

        return new SearchProductsResult(products);
    }
}
