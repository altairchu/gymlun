using FoodDiary.ComponentTests.Dsl;
using FoodDiary.ComponentTests.Infrastructure;

namespace FoodDiary.ComponentTests.Scenarios.Products;

public class ProductsApiTests(InfrastructureFixture infrastructure)
    : BaseTest<ProductsApiContext>(infrastructure)
{
    [Scenario]
    public Task I_can_retrieve_products_list()
    {
        var apple = Create.Product("Apple").Please();
        var chicken = Create.Product("Chicken").Please();
        var milk = Create.Product("Milk").Please();
        
        return CtxRunner.RunScenarioAsync(
            c => c.Given_authenticated_user(),
            c => c.Given_products(chicken, apple, milk),
            c => c.When_user_retrieves_products_list(),
            c => c.Then_products_list_contains_items(chicken, apple, milk));
    }
    
    [Scenario]
    public Task I_can_get_product_by_id()
    {
        var apple = Create.Product("Apple").Please();
        
        return CtxRunner.RunScenarioAsync(
            c => c.Given_authenticated_user(),
            c => c.Given_products(apple),
            c => c.When_user_retrieves_product_by_id(apple.Id),
            c => c.Then_product_is_successfully_retrieved(apple));
    }

    [Scenario]
    public Task I_can_search_products_by_name()
    {
        var apple = Create.Product("Apple").Please();
        var mozzarellaCheese = Create.Product("Mozzarella cheese").Please();
        var milk = Create.Product("Milk").Please();
        var chicken = Create.Product("Chicken").Please();
        
        return CtxRunner.RunScenarioAsync(
            c => c.Given_authenticated_user(),
            c => c.Given_products(apple, mozzarellaCheese, milk, chicken),
            c => c.When_user_searches_products_by_name("ch"),
            c => c.Then_products_list_contains_items(chicken, mozzarellaCheese));
    }

    [Scenario]
    public Task I_can_search_products_for_autocomplete()
    {
        return CtxRunner.RunScenarioAsync(
            c => c.Given_authenticated_user(),
            c => c.Given_product("Chicken"),
            c => c.Given_product_logged_yesterday("Beef"),
            c => c.Given_product_logged_yesterday("Apple"),
            c => c.Given_product_logged_today("Eggs"),
            c => c.Given_product_logged_today("Apple"),
            c => c.When_user_searches_products_for_autocomplete(),
            c => c.Then_products_list_for_autocomplete_contains_items("Apple", "Eggs", "Beef", "Chicken"));
    }

    [Scenario]
    public Task I_can_create_product()
    {
        var chicken = Create.Product("Chicken").Please();
        
        return CtxRunner.RunScenarioAsync(
            c => c.Given_authenticated_user(),
            c => c.Given_categories(chicken.Categories.ToArray()),
            c => c.When_user_creates_product(chicken),
            c => c.Then_product_is_successfully_created(),
            c => c.When_user_retrieves_created_product_by_id(),
            c => c.Then_product_is_successfully_retrieved(chicken));
    }

    [Scenario]
    public Task I_can_create_product_with_many_categories_counted_in_servings()
    {
        var rice = Create.Category("飯").Please();
        var meat = Create.Category("肉類").Please();
        var dumplings = Create.Product("豬肉韭菜餃")
            .WithCategories(rice, meat)
            .CountedInServingsOf(25)
            .Please();

        return CtxRunner.RunScenarioAsync(
            c => c.Given_authenticated_user(),
            c => c.Given_categories(rice, meat),
            c => c.When_user_creates_product(dumplings),
            c => c.Then_product_is_successfully_created(),
            c => c.When_user_retrieves_created_product_by_id(),
            c => c.Then_product_is_successfully_retrieved(dumplings));
    }

    [Scenario]
    public Task I_can_create_product_without_categories()
    {
        var water = Create.Product("Water").WithoutCategories().Please();

        return CtxRunner.RunScenarioAsync(
            c => c.Given_authenticated_user(),
            c => c.When_user_creates_product(water),
            c => c.Then_product_is_successfully_created(),
            c => c.When_user_retrieves_created_product_by_id(),
            c => c.Then_product_is_successfully_retrieved(water));
    }

    [Scenario]
    public Task I_can_create_product_from_old_client_sending_single_category()
    {
        var dairy = Create.Category("Dairy").Please();
        var milk = Create.Product("Milk").WithCategories(dairy).Please();

        return CtxRunner.RunScenarioAsync(
            c => c.Given_authenticated_user(),
            c => c.Given_categories(dairy),
            c => c.When_old_client_creates_product_with_single_category(milk, dairy),
            c => c.Then_product_is_successfully_created(),
            c => c.When_user_retrieves_created_product_by_id(),
            c => c.Then_product_is_successfully_retrieved(milk));
    }

    [Scenario]
    public Task I_cannot_create_product_with_unknown_category()
    {
        var dairy = Create.Category("Dairy").Please();
        var milk = Create.Product("Milk").WithCategories(dairy).Please();

        return CtxRunner.RunScenarioAsync(
            c => c.Given_authenticated_user(),
            c => c.Given_categories(dairy),
            c => c.When_user_creates_product_with_categories(milk, dairy.Id, dairy.Id + 1),
            c => c.Then_product_is_not_created_because_categories_are_not_found(),
            c => c.When_user_retrieves_products_list(),
            c => c.Then_products_list_is_empty());
    }

    [Scenario]
    public Task I_can_change_product_categories()
    {
        var rice = Create.Category("飯").Please();
        var meat = Create.Category("肉類").Please();
        var frozen = Create.Category("Frozen").Please();
        var dumplings = Create.Product("Dumplings").WithCategories(rice, meat).Please();
        var dumplingsInNewCategories = Create.Product().From(dumplings).WithCategories(meat, frozen).Please();

        return CtxRunner.RunScenarioAsync(
            c => c.Given_authenticated_user(),
            c => c.Given_categories(frozen),
            c => c.Given_products(dumplings),
            c => c.When_user_changes_product_categories(dumplings, meat, frozen),
            c => c.Then_product_is_successfully_updated(),
            c => c.When_user_retrieves_product_by_id(dumplings.Id),
            c => c.Then_product_is_successfully_retrieved(dumplingsInNewCategories));
    }

    [Scenario]
    public Task I_can_filter_products_by_one_of_their_categories()
    {
        var rice = Create.Category("飯").Please();
        var meat = Create.Category("肉類").Please();
        var plainRice = Create.Product("White rice").WithCategories(rice).Please();
        var beef = Create.Product("Beef").WithCategories(meat).Please();
        var friedRice = Create.Product("Fried rice with pork").WithCategories(rice, meat).Please();

        return CtxRunner.RunScenarioAsync(
            c => c.Given_authenticated_user(),
            c => c.Given_products(plainRice, beef, friedRice),
            c => c.When_user_filters_products_by_category(meat),
            c => c.Then_products_list_contains_items(beef, friedRice));
    }

    [Scenario]
    public Task I_keep_products_when_their_category_is_deleted()
    {
        var rice = Create.Category("飯").Please();
        var meat = Create.Category("肉類").Please();
        var friedRice = Create.Product("Fried rice with pork").WithCategories(rice, meat).Please();
        var friedRiceWithoutMeat = Create.Product().From(friedRice).WithCategories(rice).Please();

        return CtxRunner.RunScenarioAsync(
            c => c.Given_authenticated_user(),
            c => c.Given_products(friedRice),
            c => c.When_user_deletes_category(meat),
            c => c.Then_category_is_successfully_deleted(),
            c => c.When_user_retrieves_product_by_id(friedRice.Id),
            c => c.Then_product_is_successfully_retrieved(friedRiceWithoutMeat));
    }

    [Scenario]
    public Task I_can_see_product_categories_and_unit_for_autocomplete()
    {
        var rice = Create.Category("飯").Please();
        var meat = Create.Category("肉類").Please();
        var dumplings = Create.Product("Dumplings")
            .WithCategories(rice, meat)
            .CountedInServingsOf(25)
            .Please();

        return CtxRunner.RunScenarioAsync(
            c => c.Given_authenticated_user(),
            c => c.Given_products(dumplings),
            c => c.When_user_searches_products_for_autocomplete(),
            c => c.Then_products_list_for_autocomplete_contains(dumplings));
    }

    [Scenario]
    public Task I_can_update_product()
    {
        var chicken = Create.Product("Chicken").Please();
        var boiledChicken = Create.Product().From(chicken).WithName("Boiled chicken").Please();
        
        return CtxRunner.RunScenarioAsync(
            c => c.Given_authenticated_user(),
            c => c.Given_products(chicken),
            c => c.When_user_renames_product(chicken, "Boiled chicken"),
            c => c.Then_product_is_successfully_updated(),
            c => c.When_user_retrieves_product_by_id(boiledChicken.Id),
            c => c.Then_product_is_successfully_retrieved(boiledChicken));
    }

    [Scenario]
    public Task I_can_delete_product()
    {
        var milk = Create.Product("Milk").Please();
        
        return CtxRunner.RunScenarioAsync(
            c => c.Given_authenticated_user(),
            c => c.Given_products(milk),
            c => c.When_user_deletes_product(milk),
            c => c.Then_product_is_successfully_deleted(),
            c => c.When_user_retrieves_products_list(),
            c => c.Then_products_list_is_empty());
    }
    
    [Scenario]
    public Task I_can_delete_multiple_products()
    {
        var oats = Create.Product("Oats").Please();
        var milk = Create.Product("Milk").Please();
        
        return CtxRunner.RunScenarioAsync(
            c => c.Given_authenticated_user(),
            c => c.Given_products(milk),
            c => c.When_user_deletes_products(oats, milk),
            c => c.Then_multiple_products_are_successfully_deleted(),
            c => c.When_user_retrieves_products_list(),
            c => c.Then_products_list_is_empty());
    }

    [Scenario]
    public Task I_can_get_nutrition_suggestion()
    {
        var givenNutrition = Create.SuggestNutritionModelResponse()
            .WithCalories(402)
            .WithNutritionComponents(protein: 25, fats: 33.1m, carbs: 1.3m, sugar: null, salt: 1.8m)
            .Please();

        return CtxRunner.RunScenarioAsync(
            c => c.Given_OpenAI_api_is_ready(),
            c => c.Given_OpenAI_api_can_suggest_nutrition(givenNutrition),
            c => c.Given_authenticated_user(),
            c => c.When_user_requests_nutrition_suggestion("Cheddar cheese"),
            c => c.Then_nutrition_suggestion_is(402, 25, 33.1m, 1.3m, null, 1.8m));
    }
}