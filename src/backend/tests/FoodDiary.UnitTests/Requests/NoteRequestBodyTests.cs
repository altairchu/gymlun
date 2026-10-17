using System.ComponentModel.DataAnnotations;
using FoodDiary.Contracts.Notes;
using FoodDiary.Domain.Enums;

namespace FoodDiary.UnitTests.Requests;

public class NoteRequestBodyTests
{
    private static NoteRequestBody Body(TimeOnly? time = null, MealType? mealType = null, int productQuantity = 100) => new()
    {
        Date = new DateOnly(2026, 10, 17),
        Time = time,
        MealType = mealType,
        ProductId = 1,
        ProductQuantity = productQuantity
    };

    private static List<ValidationResult> Validate(object request)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void NeitherTimeNorMealType_IsRejected()
    {
        Validate(Body()).Should().ContainSingle()
            .Which.MemberNames.Should().Equal(nameof(NoteRequestBody.Time), nameof(NoteRequestBody.MealType));
    }

    [Fact]
    public void TimeOrMealType_IsEnough()
    {
        Validate(Body(time: new TimeOnly(12, 0))).Should().BeEmpty();
        Validate(Body(mealType: MealType.Dinner)).Should().BeEmpty();
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(5000, true)]
    [InlineData(5001, false)]
    public void ProductQuantity_IsFrom1To5000(int productQuantity, bool isValid)
    {
        Validate(Body(mealType: MealType.Lunch, productQuantity: productQuantity)).Should().HaveCount(isValid ? 0 : 1);
    }
}
