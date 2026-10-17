using FoodDiary.ComponentTests.Infrastructure;
using FoodDiary.Domain.Enums;

namespace FoodDiary.ComponentTests.Scenarios.Migrations;

public class MigrationsTests(InfrastructureFixture infrastructure) : BaseTest<MigrationsContext>(infrastructure)
{
    private const string SingleCategoryPerProduct = "AddProductIsFavorite";
    private const string ManyCategoriesPerProduct = "AddProductCategoriesUnitsAndNoteTime";

    [Scenario]
    public Task Products_keep_their_category_and_food_logs_get_time_of_their_meal()
    {
        return CtxRunner.RunScenarioAsync(
            c => c.Given_database_migrated_to(SingleCategoryPerProduct),
            c => c.Given_category(1, "飯"),
            c => c.Given_category(2, "肉類"),
            c => c.Given_product_in_category(10, "White rice", 1),
            c => c.Given_product_in_category(11, "Beef", 2),
            c => c.Given_note(100, 10, MealType.Breakfast),
            c => c.Given_note(101, 10, MealType.SecondBreakfast),
            c => c.Given_note(102, 11, MealType.Lunch),
            c => c.Given_note(103, 11, MealType.AfternoonSnack),
            c => c.Given_note(104, 11, MealType.Dinner),
            c => c.When_database_is_migrated_to(ManyCategoriesPerProduct),
            c => c.Then_product_is_in_categories(10, 1),
            c => c.Then_product_is_in_categories(11, 2),
            c => c.Then_product_is_counted_in_grams(10),
            c => c.Then_note_is_eaten_at(100, "08:00"),
            c => c.Then_note_is_eaten_at(101, "10:30"),
            c => c.Then_note_is_eaten_at(102, "12:30"),
            c => c.Then_note_is_eaten_at(103, "15:30"),
            c => c.Then_note_is_eaten_at(104, "19:00"));
    }

    [Scenario]
    public Task Reverting_keeps_one_category_per_product()
    {
        return CtxRunner.RunScenarioAsync(
            c => c.Given_database_migrated_to(ManyCategoriesPerProduct),
            c => c.Given_category(501, "飯"),
            c => c.Given_category(502, "肉類"),
            c => c.Given_product_in_categories(10, "Fried rice with pork", 502, 501),
            c => c.Given_product_in_categories(11, "Water"),
            c => c.When_database_is_migrated_to(SingleCategoryPerProduct),
            c => c.Then_product_is_in_single_category(10, "飯"),
            c => c.Then_product_is_in_single_category(11, "Uncategorized"));
    }
}
