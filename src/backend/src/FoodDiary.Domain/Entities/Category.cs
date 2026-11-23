using FoodDiary.Domain.Abstractions;

namespace FoodDiary.Domain.Entities;

/// <summary>
/// Category of product (or dish) stored in diary
/// </summary>
public class Category : IDiaryOwned
{
    public long OwnerId { get; set; } = IDiaryOwned.UnassignedOwnerId;

    public int Id { get; set; }

    /// <summary>
    /// Category name
    /// </summary>
    public required string Name { get; set; }

    public virtual ICollection<Product> Products { get; set; } = [];
}