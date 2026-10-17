using FoodDiary.Domain.Enums;

namespace FoodDiary.Domain.Entities;

/// <summary>
/// Eatable product (or dish) to store in diary  
/// </summary>
public class Product
{
    public int Id { get; set; }

    /// <summary>
    /// Product name
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Describes, how much calories the product would cost if its quantity is 100 g 
    /// </summary>
    public int CaloriesCost { get; set; }

    /// <summary>
    /// Grams in one serving or piece. For a product counted in grams, it is also the usual portion to log
    /// </summary>
    public int DefaultQuantity { get; set; } = 100;

    public QuantityUnit QuantityUnit { get; set; } = QuantityUnit.Grams;

    /// <summary>
    /// Marks a product the person eats often, so it can be logged in one tap
    /// </summary>
    public bool IsFavorite { get; set; }

    public required decimal? Protein { get; set; }
    public required decimal? Fats { get; set; }
    public required decimal? Carbs { get; set; }
    public required decimal? Sugar { get; set; }
    public required decimal? Salt { get; set; }

    public virtual ICollection<Category> Categories { get; set; } = [];

    public virtual ICollection<Note> Notes { get; set; } = [];
}
