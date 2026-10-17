using FoodDiary.Domain.Entities;

namespace FoodDiary.Domain.Repositories.v2;

public interface ICategoriesRepository
{
    Task<Category[]> GetAllOrderedByNameAsync(CancellationToken cancellationToken);

    Task<Category[]> FindByIds(IReadOnlyCollection<int> ids, CancellationToken cancellationToken);
}
