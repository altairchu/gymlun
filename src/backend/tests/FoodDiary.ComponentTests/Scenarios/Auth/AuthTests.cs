using FoodDiary.ComponentTests.Infrastructure;

namespace FoodDiary.ComponentTests.Scenarios.Auth;

public class AuthTests(InfrastructureFixture infrastructure) : BaseTest<AuthContext>(infrastructure)
{
    [Scenario]
    [InlineData("/api/v1/notes")]
    [InlineData("/api/v1/products")]
    [InlineData("/api/v1/categories")]
    public Task I_cannot_use_app_when_my_email_is_not_allowed(string resourcePath)
    {
        return CtxRunner.RunScenarioAsync(
            c => c.Given_authenticated_user("forbidden.user@gmail.com"),
            c => c.When_user_is_trying_to_access_resource(resourcePath),
            c => c.Then_access_is_forbidden());
    }

    [Scenario]
    public Task The_client_can_check_auth_status_and_keep_user_signed_in()
    {
        return CtxRunner.RunScenarioAsync(
            c => c.Given_authenticated_user_with_expired_access_token(),
            c => c.Given_google_identity_provider_is_ready(),
            c => c.Given_user_access_token_can_be_refreshed(),
            c => c.Given_user_info_can_be_retrieved(),
            c => c.When_client_checks_auth_status(),
            c => c.Then_user_is_authenticated());
    }

    [Scenario]
    public Task AtlasApple_sign_in_binds_an_active_account_and_rejects_a_stale_or_revoked_session()
    {
        return CtxRunner.RunScenarioAsync(
            c => c.Given_atlas_apple_is_the_authentication_provider(),
            c => c.When_browser_signs_in_through_atlas_apple(),
            c => c.Then_client_receives_the_current_atlas_account(),
            c => c.Then_a_stale_account_context_cannot_read_the_new_accounts_diary(),
            c => c.Then_revoking_the_atlas_session_revokes_diary_access());
    }
}