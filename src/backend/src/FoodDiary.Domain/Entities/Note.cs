using FoodDiary.Domain.Abstractions;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Domain.Entities;

public class Note : IDiaryOwned
{
    public long OwnerId { get; set; } = IDiaryOwned.UnassignedOwnerId;

    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly Time { get; set; }
    public MealType MealType { get; set; }
    public int ProductId { get; set; }
    public int ProductQuantity { get; set; }
    public int DisplayOrder { get; set; }
    
    public Product? Product { get; set; }
}
