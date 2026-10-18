using FoodDiary.ComponentTests.Dsl;
using FoodDiary.ComponentTests.Infrastructure;
using FoodDiary.Domain.WeightTracking;

namespace FoodDiary.ComponentTests.Scenarios.Mcp;

public class McpApiTests(InfrastructureFixture infrastructure) : BaseTest<McpApiContext>(infrastructure)
{
    private static readonly string[] ReadTools = ["get_food_logs", "list_products", "list_categories", "get_weight_logs"];

    private static readonly string[] AllTools =
    [
        ..ReadTools,
        "log_food", "update_food_log", "delete_food_log",
        "create_product", "update_product", "delete_product",
        "create_category", "rename_category", "delete_category",
        "log_weight"
    ];

    [Scenario]
    public Task The_client_can_discover_authorization_server()
    {
        return CtxRunner.RunScenarioAsync(
            c => c.When_client_requests_authorization_server_metadata(),
            c => c.Then_authorization_server_metadata_is_built_from_base_url());
    }

    [Scenario]
    public Task The_client_without_access_token_is_challenged_to_discover_protected_resource_metadata()
    {
        return CtxRunner.RunScenarioAsync(
            c => c.When_client_calls_mcp_without_access_token(),
            c => c.When_client_requests_resource_metadata_from_challenge(),
            c => c.Then_client_is_challenged_with_resource_metadata_and_scope(),
            c => c.Then_resource_metadata_is_built_from_base_url());
    }

    [Scenario]
    [InlineData("POST", "/mcp")]
    [InlineData("GET", "/.well-known/oauth-protected-resource/mcp")]
    [InlineData("GET", "/.well-known/oauth-authorization-server")]
    [InlineData("GET", "/authorize")]
    [InlineData("POST", "/token")]
    [InlineData("POST", "/register")]
    public Task The_client_cannot_find_mcp_server_when_it_is_disabled(string method, string path)
    {
        return CtxRunner.RunScenarioAsync(
            c => c.Given_mcp_is_disabled(),
            c => c.When_client_requests(method, path),
            c => c.Then_response_is_not_found());
    }

    [Scenario]
    public Task The_client_with_access_token_can_connect()
    {
        return CtxRunner.RunScenarioAsync(
            c => c.Given_access_token_was_issued(),
            c => c.When_mcp_client_connects(),
            c => c.Then_mcp_client_is_connected());
    }

    [Scenario]
    public Task The_client_can_get_food_logs()
    {
        var note = Create.Note().WithDate("2026-09-01").WithProduct("Oatmeal", quantity: 60).Please();

        return CtxRunner.RunScenarioAsync(
            c => c.Given_notes(note),
            c => c.Given_access_token_was_issued(),
            c => c.When_mcp_client_calls_get_food_logs("2026-09-01", "2026-09-01"),
            c => c.Then_mcp_client_lists_tools(AllTools),
            c => c.Then_food_logs_contain(note));
    }

    [Scenario]
    public Task The_client_is_told_to_split_food_logs_range_wider_than_31_days()
    {
        return CtxRunner.RunScenarioAsync(
            c => c.Given_access_token_was_issued(),
            c => c.When_mcp_client_calls_get_food_logs("2026-09-01", "2026-10-02"),
            c => c.Then_tool_call_is_error("Requested 32 days; the maximum is 31. Split the range."));
    }

    [Scenario]
    public Task The_client_can_list_products()
    {
        var oatmeal = Create.Product("Oatmeal").Please();
        var apple = Create.Product("Apple").Please();

        return CtxRunner.RunScenarioAsync(
            c => c.Given_products(oatmeal, apple),
            c => c.Given_access_token_was_issued(),
            c => c.When_mcp_client_calls_list_products("oat"),
            c => c.Then_mcp_client_lists_tools(AllTools),
            c => c.Then_products_contain(oatmeal));
    }

    [Scenario]
    public Task The_client_can_log_food()
    {
        var dumplings = Create.Product("餃子").Please();

        return CtxRunner.RunScenarioAsync(
            c => c.Given_products(dumplings),
            c => c.Given_access_token_was_issued(),
            c => c.When_mcp_client_calls_log_food("2026-09-01", "Lunch", dumplings),
            c => c.Then_mcp_client_lists_tools(AllTools),
            c => c.Then_food_log_is_logged("2026-09-01", "Lunch", dumplings));
    }

    [Scenario]
    public Task The_client_can_log_food_by_time_and_servings()
    {
        var dumplings = Create.Product("餃子").CountedInServingsOf(25).Please();

        return CtxRunner.RunScenarioAsync(
            c => c.Given_products(dumplings),
            c => c.Given_access_token_was_issued(),
            c => c.When_mcp_client_calls_log_food_by_time_and_servings("2026-09-01", "12:45", 10, dumplings),
            c => c.Then_mcp_client_lists_tools(AllTools),
            c => c.Then_food_log_is_logged_at("2026-09-01", "12:45", "Lunch", dumplings, 250));
    }

    [Scenario]
    public Task The_client_can_delete_food_log()
    {
        var note = Create.Note().WithDate("2026-09-01").WithProduct("Oatmeal", quantity: 60).Please();

        return CtxRunner.RunScenarioAsync(
            c => c.Given_notes(note),
            c => c.Given_access_token_was_issued(),
            c => c.When_mcp_client_calls_delete_food_log(note),
            c => c.Then_mcp_client_lists_tools(AllTools),
            c => c.Then_food_log_is_deleted(note));
    }

    [Scenario]
    public Task The_client_can_update_food_log()
    {
        var note = Create.Note().WithDate("2026-09-01").WithTime("12:30").WithProduct("Oatmeal", quantity: 60).Please();

        return CtxRunner.RunScenarioAsync(
            c => c.Given_notes(note),
            c => c.Given_access_token_was_issued(),
            c => c.When_mcp_client_calls_update_food_log(note, time: "15:45", quantity: 80),
            c => c.Then_mcp_client_lists_tools(AllTools),
            c => c.Then_food_log_is_changed_to(note, "15:45", "AfternoonSnack", 80));
    }

    [Scenario]
    public Task The_client_can_list_products_in_category()
    {
        var meat = Create.Category("肉類").Please();
        var chicken = Create.Product("雞胸肉").WithCategories(meat).Please();
        var rice = Create.Product("白飯").Please();

        return CtxRunner.RunScenarioAsync(
            c => c.Given_products(chicken, rice),
            c => c.Given_access_token_was_issued(),
            c => c.When_mcp_client_calls_list_products_in_category(meat),
            c => c.Then_mcp_client_lists_tools(AllTools),
            c => c.Then_products_contain(chicken));
    }

    [Scenario]
    public Task The_client_can_update_product()
    {
        var chicken = Create.Product("雞胸肉").Please();
        string[] categories = ["肉類", "高蛋白"];

        return CtxRunner.RunScenarioAsync(
            c => c.Given_products(chicken),
            c => c.Given_access_token_was_issued(),
            c => c.When_mcp_client_calls_update_product(chicken, caloriesPer100g: 165, categories),
            c => c.Then_mcp_client_lists_tools(AllTools),
            c => c.Then_product_is_changed_to(chicken, 165, categories));
    }

    [Scenario]
    public Task The_client_can_delete_product_with_its_food_logs()
    {
        var note = Create.Note().WithDate("2026-09-01").WithProduct("Oatmeal", quantity: 60).Please();

        return CtxRunner.RunScenarioAsync(
            c => c.Given_notes(note),
            c => c.Given_access_token_was_issued(),
            c => c.When_mcp_client_calls_delete_product(note.Product!),
            c => c.Then_mcp_client_lists_tools(AllTools),
            c => c.Then_product_and_its_food_logs_are_deleted(note.Product!));
    }

    [Scenario]
    public Task The_client_can_list_categories()
    {
        var meat = Create.Category("Meat").Please();
        var frozen = Create.Category("Frozen").Please();
        var dumplings = Create.Product("餃子").WithCategories(meat, frozen).Please();
        var chicken = Create.Product("雞胸肉").WithCategories(meat).Please();
        (string Name, int ProductCount)[] expectedCategories = [("Frozen", 1), ("Meat", 2)];

        return CtxRunner.RunScenarioAsync(
            c => c.Given_products(dumplings, chicken),
            c => c.Given_access_token_was_issued(),
            c => c.When_mcp_client_calls_list_categories(),
            c => c.Then_mcp_client_lists_tools(AllTools),
            c => c.Then_categories_are_listed_with_product_counts(expectedCategories));
    }

    [Scenario]
    public Task The_client_can_create_category()
    {
        return CtxRunner.RunScenarioAsync(
            c => c.Given_access_token_was_issued(),
            c => c.When_mcp_client_calls_create_category("飲品"),
            c => c.Then_mcp_client_lists_tools(AllTools),
            c => c.Then_category_is_saved_as("飲品"));
    }

    [Scenario]
    public Task The_client_can_rename_category()
    {
        var category = Create.Category("Meat").Please();

        return CtxRunner.RunScenarioAsync(
            c => c.Given_categories(category),
            c => c.Given_access_token_was_issued(),
            c => c.When_mcp_client_calls_rename_category(category, "肉類"),
            c => c.Then_mcp_client_lists_tools(AllTools),
            c => c.Then_category_is_saved_as("肉類"));
    }

    [Scenario]
    public Task The_client_can_delete_category_and_its_products_stay()
    {
        var meat = Create.Category("肉類").Please();
        var chicken = Create.Product("雞胸肉").WithCategories(meat).Please();

        return CtxRunner.RunScenarioAsync(
            c => c.Given_products(chicken),
            c => c.Given_access_token_was_issued(),
            c => c.When_mcp_client_calls_delete_category(meat),
            c => c.Then_mcp_client_lists_tools(AllTools),
            c => c.Then_category_is_deleted_and_its_products_stay(meat, chicken));
    }

    [Scenario]
    public Task The_client_can_get_weight_logs()
    {
        WeightLog[] weightLogs =
        [
            new() { Date = DateOnly.Parse("2026-08-31"), Weight = 73.1m },
            new() { Date = DateOnly.Parse("2026-09-01"), Weight = 72.8m },
            new() { Date = DateOnly.Parse("2026-09-03"), Weight = 72.4m }
        ];
        (string Date, decimal Weight)[] expectedWeightLogs = [("2026-09-01", 72.8m), ("2026-09-03", 72.4m)];

        return CtxRunner.RunScenarioAsync(
            c => c.Given_weight_logs(weightLogs),
            c => c.Given_access_token_was_issued(),
            c => c.When_mcp_client_calls_get_weight_logs("2026-09-01", "2026-09-07"),
            c => c.Then_mcp_client_lists_tools(AllTools),
            c => c.Then_weight_logs_are(expectedWeightLogs));
    }

    [Scenario]
    public Task The_client_can_log_weight()
    {
        return CtxRunner.RunScenarioAsync(
            c => c.Given_access_token_was_issued(),
            c => c.When_mcp_client_calls_log_weight("2026-09-01", 72.4m),
            c => c.Then_mcp_client_lists_tools(AllTools),
            c => c.Then_weight_is_logged("2026-09-01", 72.4m));
    }

    [Scenario]
    public Task The_client_can_create_product_from_serving_nutrition()
    {
        return CtxRunner.RunScenarioAsync(
            c => c.Given_access_token_was_issued(),
            c => c.When_mcp_client_calls_create_product("豬肉韭菜餃", "Dumplings", servingGrams: 250, calories: 550, protein: 22.5m),
            c => c.Then_mcp_client_lists_tools(AllTools),
            c => c.Then_created_product_is_favorite_with_nutrition_per_100g("豬肉韭菜餃", "Dumplings", 250, 220, 9m));
    }

    [Scenario]
    public Task The_client_can_create_product_counted_in_servings_with_many_categories()
    {
        string[] categories = ["餃子", "Frozen"];

        return CtxRunner.RunScenarioAsync(
            c => c.Given_access_token_was_issued(),
            c => c.When_mcp_client_calls_create_product_counted_in_servings("豬肉韭菜餃", categories, 25, 55),
            c => c.Then_mcp_client_lists_tools(AllTools),
            c => c.Then_created_product_is_counted_in_servings("豬肉韭菜餃", categories, 25));
    }

    [Scenario]
    public Task The_client_with_read_only_access_token_cannot_see_write_tools()
    {
        return CtxRunner.RunScenarioAsync(
            c => c.Given_read_only_access_token_was_issued(),
            c => c.When_mcp_client_lists_tools(),
            c => c.Then_mcp_client_lists_tools(ReadTools));
    }

    [Scenario]
    public Task The_client_can_get_nutrition_report_prompt()
    {
        return CtxRunner.RunScenarioAsync(
            c => c.Given_access_token_was_issued(),
            c => c.When_mcp_client_gets_nutrition_report_prompt("last 14 days"),
            c => c.Then_mcp_client_lists_prompts("nutrition_report"),
            c => c.Then_prompt_mentions("last 14 days"));
    }

    [Scenario]
    public Task The_client_with_access_token_of_user_no_longer_allowed_is_forbidden()
    {
        return CtxRunner.RunScenarioAsync(
            c => c.Given_access_token_was_issued_to("removed.user@gmail.com"),
            c => c.When_client_calls_mcp_with_access_token(),
            c => c.Then_access_is_forbidden());
    }

    [Scenario]
    public Task The_client_can_exchange_authorization_code_for_access_and_refresh_tokens()
    {
        return CtxRunner.RunScenarioAsync(
            c => c.Given_authenticated_user(),
            c => c.When_client_requests_authorization(),
            c => c.When_client_exchanges_authorization_code(),
            c => c.Then_authorization_response_carries_state_and_issuer(),
            c => c.Then_client_receives_access_and_refresh_tokens());
    }

    [Scenario]
    public Task The_client_is_redirected_back_with_error_and_issuer_when_PKCE_is_not_S256()
    {
        return CtxRunner.RunScenarioAsync(
            c => c.When_client_requests_authorization_with_code_challenge_method("plain"),
            c => c.Then_authorization_error_response_carries_state_and_issuer("invalid_request"));
    }

    [Scenario]
    public Task The_client_cannot_exchange_one_authorization_code_twice()
    {
        return CtxRunner.RunScenarioAsync(
            c => c.Given_authorization_code_was_issued(),
            c => c.When_client_exchanges_authorization_code(),
            c => c.When_client_exchanges_authorization_code(),
            c => c.Then_only_the_first_code_exchange_succeeds());
    }

    [Scenario]
    public Task The_client_can_register_itself_and_connect_once_the_owner_allows_it()
    {
        return CtxRunner.RunScenarioAsync(
            c => c.Given_authenticated_user(),
            c => c.When_client_registers("ChatGPT", "https://chatgpt.com/connector_platform_oauth_redirect", "none"),
            c => c.When_registered_client_requests_authorization(),
            c => c.Then_client_is_registered_as("ChatGPT", "none"),
            c => c.Then_owner_is_asked_to_allow("ChatGPT"),
            c => c.When_owner_allows_the_connection(),
            c => c.When_registered_client_exchanges_authorization_code(),
            c => c.When_mcp_client_connects(),
            c => c.Then_authorization_response_carries_state_and_issuer(),
            c => c.Then_mcp_client_is_connected());
    }

    [Scenario]
    public Task The_registered_client_with_secret_can_exchange_authorization_code_with_basic_authentication()
    {
        return CtxRunner.RunScenarioAsync(
            c => c.Given_authenticated_user(),
            c => c.When_client_registers("Some AI app", "https://ai.example/oauth/callback", "client_secret_basic"),
            c => c.When_registered_client_requests_authorization(),
            c => c.When_owner_allows_the_connection(),
            c => c.When_registered_client_exchanges_authorization_code(),
            c => c.Then_client_is_registered_as("Some AI app", "client_secret_basic"),
            c => c.Then_client_receives_access_and_refresh_tokens());
    }

    [Scenario]
    public Task The_registered_client_is_redirected_back_with_access_denied_when_the_owner_does_not_allow_it()
    {
        return CtxRunner.RunScenarioAsync(
            c => c.Given_authenticated_user(),
            c => c.When_client_registers("Unknown app", "https://unknown.example/callback", "none"),
            c => c.When_registered_client_requests_authorization(),
            c => c.When_owner_does_not_allow_the_connection(),
            c => c.Then_authorization_error_response_carries_state_and_issuer("access_denied"));
    }

    [Scenario]
    [InlineData("http://attacker.example/callback")]
    [InlineData("javascript:alert(1)")]
    [InlineData("/relative/callback")]
    public Task The_client_cannot_register_unsafe_redirect_uri(string redirectUri)
    {
        return CtxRunner.RunScenarioAsync(
            c => c.When_client_registers("Some AI app", redirectUri, "none"),
            c => c.Then_registration_is_rejected_with("invalid_redirect_uri"));
    }

    [Scenario]
    public Task The_client_can_refresh_access_token()
    {
        return CtxRunner.RunScenarioAsync(
            c => c.Given_authorization_code_was_issued(),
            c => c.When_client_exchanges_authorization_code(),
            c => c.When_client_refreshes_access_token(),
            c => c.Then_client_receives_new_access_token());
    }
}
