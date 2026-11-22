using FoodDiary.Domain.Abstractions;

namespace FoodDiary.Infrastructure.EntityConfigurations;

internal sealed class ProductCategory : IDiaryOwned
{
    public long OwnerId { get; set; } = IDiaryOwned.UnassignedOwnerId;
    public int ProductId { get; set; }
    public int CategoryId { get; set; }
}
