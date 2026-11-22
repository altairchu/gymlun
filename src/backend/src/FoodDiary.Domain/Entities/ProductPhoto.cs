using FoodDiary.Domain.Abstractions;

namespace FoodDiary.Domain.Entities;

/// <summary>
/// Kept apart from <see cref="Product"/>, so loading products never loads their photos
/// </summary>
public class ProductPhoto : IDiaryOwned
{
    public long OwnerId { get; set; } = IDiaryOwned.UnassignedOwnerId;

    public int ProductId { get; set; }

    /// <summary>
    /// JPEG image
    /// </summary>
    public required byte[] Content { get; set; }
}
