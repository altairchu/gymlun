using FoodDiary.ComponentTests.Dsl;
using FoodDiary.ComponentTests.Infrastructure;

namespace FoodDiary.ComponentTests.Scenarios.Mcp;

public class McpApiTests(InfrastructureFixture infrastructure) : BaseTest<McpApiContext>(infrastructure)
{
    private static readonly string[] ReadTools = ["get_food_logs", "list_products"];
    private static readonly string[] AllTools = [..ReadTools, "log_food", "delete_food_log", "create_product"];

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
    public Task The_client_can_refresh_access_token()
    {
        return CtxRunner.RunScenarioAsync(
            c => c.Given_authorization_code_was_issued(),
            c => c.When_client_exchanges_authorization_code(),
            c => c.When_client_refreshes_access_token(),
            c => c.Then_client_receives_new_access_token());
    }
}
