using FoodDiary.Domain.Entities;
using FoodDiary.Domain.Repositories.v2;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Repositories.v2;

public class CategoriesRepository(DbSet<Category> categories) : ICategoriesRepository
{
    public Task<Category[]> GetAllOrderedByNameAsync(CancellationToken cancellationToken)
    {
        return categories
            .OrderBy(c => c.Name)
            .ToArrayAsync(cancellationToken);
    }

    public Task<Category[]> FindByIds(IReadOnlyCollection<int> ids, CancellationToken cancellationToken)
    {
        return categories
            .Where(c => ids.Contains(c.Id))
            .ToArrayAsync(cancellationToken);
    }
}
