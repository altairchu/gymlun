namespace FoodDiary.Domain.Entities;

/// <summary>
/// Kept apart from <see cref="Product"/>, so loading products never loads their photos
/// </summary>
public class ProductPhoto
{
    public int ProductId { get; set; }

    /// <summary>
    /// JPEG image
    /// </summary>
    public required byte[] Content { get; set; }
}
