namespace FoodDiary.Domain.Abstractions;

public interface IDiaryOwned
{
    const long UnassignedOwnerId = -1;

    long OwnerId { get; set; }
}
