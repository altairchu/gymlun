using FoodDiary.Domain.Enums;

namespace FoodDiary.UnitTests.Notes;

public class MealTypeTimesTests
{
    [Theory]
    [InlineData("00:00", MealType.Dinner)]
    [InlineData("03:59", MealType.Dinner)]
    [InlineData("04:00", MealType.Breakfast)]
    [InlineData("09:59", MealType.Breakfast)]
    [InlineData("10:00", MealType.SecondBreakfast)]
    [InlineData("11:59", MealType.SecondBreakfast)]
    [InlineData("12:00", MealType.Lunch)]
    [InlineData("14:59", MealType.Lunch)]
    [InlineData("15:00", MealType.AfternoonSnack)]
    [InlineData("17:59", MealType.AfternoonSnack)]
    [InlineData("18:00", MealType.Dinner)]
    [InlineData("23:59", MealType.Dinner)]
    public void FromTime_DerivesMealFromStartInclusiveBoundaries(string time, MealType expected)
    {
        MealType.FromTime(TimeOnly.Parse(time)).Should().Be(expected);
    }

    [Theory]
    [InlineData(MealType.Breakfast, "08:00")]
    [InlineData(MealType.SecondBreakfast, "10:30")]
    [InlineData(MealType.Lunch, "12:30")]
    [InlineData(MealType.AfternoonSnack, "15:30")]
    [InlineData(MealType.Dinner, "19:00")]
    public void DefaultTime_FallsWithinItsOwnMeal(MealType mealType, string expected)
    {
        mealType.DefaultTime.Should().Be(TimeOnly.Parse(expected));
        MealType.FromTime(mealType.DefaultTime).Should().Be(mealType);
    }

    [Fact]
    public void ResolveTimeAndMeal_TimeOnly_DerivesMeal()
    {
        MealType.ResolveTimeAndMeal(new TimeOnly(16, 20), null)
            .Should().Be((new TimeOnly(16, 20), MealType.AfternoonSnack));
    }

    [Fact]
    public void ResolveTimeAndMeal_MealOnly_UsesMealDefaultTime()
    {
        MealType.ResolveTimeAndMeal(null, MealType.Lunch)
            .Should().Be((new TimeOnly(12, 30), MealType.Lunch));
    }

    [Fact]
    public void ResolveTimeAndMeal_Both_KeepsBothAsGiven()
    {
        MealType.ResolveTimeAndMeal(new TimeOnly(15, 30), MealType.Lunch)
            .Should().Be((new TimeOnly(15, 30), MealType.Lunch));
    }

    [Fact]
    public void ResolveTimeAndMeal_Neither_IsNull()
    {
        MealType.ResolveTimeAndMeal(null, null).Should().BeNull();
    }
}
