using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using FoodDiary.API.Authentication;
using FoodDiary.ComponentTests.Infrastructure;
using FoodDiary.ComponentTests.Infrastructure.ExternalServices;
using FoodDiary.Configuration;
using FoodDiary.Contracts.Auth;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FoodDiary.ComponentTests.Scenarios.Auth;

[UsedImplicitly]
public class AuthContext(
    FoodDiaryWebApplicationFactory factory,
    ExternalServicesFixture externalServices) : BaseContext(factory)
{
    private GoogleIdentityProvider GoogleIdentityProvider => externalServices.GoogleIdentityProvider;
    private HttpResponseMessage? _response;
    private GetAuthStatusResponse? _getStatusResponse;
    private const long AtlasOwner = 506180001;
    private const long AtlasEpoch = 3;
    private const string SyntheticCode = "atlas-test-code";
    private const string AtlasOrigin = "https://atlas.example.test";
    private HttpClient? _atlasBrowser;
    private bool _atlasSessionActive = true;
    private string? _atlasChallenge;

    public Task Given_authenticated_user_with_expired_access_token()
    {
        var timeProvider = Factory.Services.GetRequiredService<TimeProvider>();

        Factory = WithAuthenticatedUser(
            tokenIssuedOn: timeProvider.GetUtcNow()
                .Subtract(Constants.AuthenticationParameters.AccessTokenRefreshInterval)
                .Subtract(TimeSpan.FromMinutes(15)));

        return Task.CompletedTask;
    }

    public Task Given_google_identity_provider_is_ready()
    {
        return GoogleIdentityProvider.Start();
    }

    public Task Given_user_access_token_can_be_refreshed()
    {
        return GoogleIdentityProvider.SetupAccessTokenSuccessfullyRefreshed();
    }

    public Task Given_user_info_can_be_retrieved()
    {
        return GoogleIdentityProvider.SetupUserInfoSuccessfullyReceived();
    }

    public async Task When_user_is_trying_to_access_resource(string resource)
    {
        _response = await ApiClient.GetAsync(resource);
    }

    public async Task When_client_checks_auth_status()
    {
        _getStatusResponse = await ApiClient.GetFromJsonAsync<GetAuthStatusResponse>("/api/v1/auth/status");
    }

    public Task Then_access_is_forbidden()
    {
        _response!.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        return Task.CompletedTask;
    }

    public Task Then_user_is_authenticated()
    {
        _getStatusResponse?.IsAuthenticated.Should().BeTrue();
        return Task.CompletedTask;
    }

    public Task Given_atlas_apple_is_the_authentication_provider()
    {
        Factory = Factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AtlasApple:Enabled"] = "true",
                ["AtlasApple:Authority"] = AtlasOrigin,
                ["AtlasApple:ClientId"] = "gymlun-test",
                ["AtlasApple:ClientSecret"] = "synthetic-atlas-secret-for-component-test",
            }));
            builder.ConfigureTestServices(services =>
            {
                services.Configure<OAuthOptions>(Constants.AuthenticationSchemes.AtlasApple, options =>
                    options.Backchannel = new HttpClient(new AtlasBackchannelStub(ExchangeAtlasCodeAsync)));
                services.AddTransient<AtlasAppleSessionValidator>(provider => new AtlasAppleSessionValidator(
                    new HttpClient(new AtlasBackchannelStub(CheckAtlasSessionAsync)),
                    provider.GetRequiredService<IOptions<AtlasAppleAuthOptions>>()));
            });
        });
        return Task.CompletedTask;
    }

    public async Task When_browser_signs_in_through_atlas_apple()
    {
        _atlasBrowser = Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false,
            BaseAddress = new Uri("https://gym.example.test"),
        });
        using var challenge = await _atlasBrowser.GetAsync("/api/v1/auth/login?returnUrl=%2Fbody");
        challenge.StatusCode.Should().Be(HttpStatusCode.Redirect);
        var location = challenge.Headers.Location!;
        location.GetLeftPart(UriPartial.Authority).Should().Be(AtlasOrigin);
        location.AbsolutePath.Should().Be("/api/gym-lun/sso/authorize");
        var requestParameters = QueryHelpers.ParseQuery(location.Query);
        requestParameters["client_id"].ToString().Should().Be("gymlun-test");
        requestParameters["redirect_uri"].ToString().Should().Be("https://gym.example.test/signin-atlasapple");
        requestParameters["scope"].ToString().Should().Be("gymlun.identity");
        requestParameters["code_challenge_method"].ToString().Should().Be("S256");
        _atlasChallenge = requestParameters["code_challenge"].ToString();
        _atlasChallenge.Length.Should().Be(43);
        var correlation = challenge.Headers.GetValues("Set-Cookie")
            .Single(value => value.StartsWith(".AspNetCore.Correlation.", StringComparison.Ordinal));
        correlation.Should().Contain("path=/signin-atlasapple").And.Contain("secure").And.Contain("httponly");
        using var callbackRequest = new HttpRequestMessage(HttpMethod.Get, QueryHelpers.AddQueryString("/signin-atlasapple",
            new Dictionary<string, string?>
            {
                ["code"] = SyntheticCode,
                ["state"] = requestParameters["state"].ToString(),
            }));
        callbackRequest.Headers.Add("Cookie", correlation.Split(';', 2)[0]);
        using var callback = await _atlasBrowser.SendAsync(callbackRequest);
        callback.StatusCode.Should().Be(HttpStatusCode.Redirect);
        var cookie = callback.Headers.GetValues("Set-Cookie")
            .Single(value => value.StartsWith("__Host-GymLunAtlas=", StringComparison.Ordinal));
        cookie.Should().Contain("path=/").And.Contain("secure").And.Contain("httponly");
        _atlasBrowser.DefaultRequestHeaders.Add("Cookie", cookie.Split(';', 2)[0]);
        using var loginReturn = await _atlasBrowser.GetAsync(callback.Headers.Location);
        loginReturn.Headers.Location!.OriginalString.Should().Be("/#/post-login?returnUrl=%2Fbody");
    }

    public async Task Then_client_receives_the_current_atlas_account()
    {
        var status = await _atlasBrowser!.GetFromJsonAsync<GetAuthStatusResponse>("/api/v1/auth/status");
        status!.IsAuthenticated.Should().BeTrue();
        status.Provider.Should().Be("atlasApple");
        status.AccountId.Should().Be(AtlasOwner.ToString(CultureInfo.InvariantCulture));
    }

    public async Task Then_a_stale_account_context_cannot_read_the_new_accounts_diary()
    {
        using var staleRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/products");
        staleRequest.Headers.Add("X-Gym-Expected-Account", "506180002");
        using var response = await _atlasBrowser!.SendAsync(staleRequest);
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        response.Headers.GetValues("X-Gym-Account-Changed").Should().ContainSingle().Which.Should().Be("true");
        response.Headers.CacheControl!.NoStore.Should().BeTrue();
    }

    public async Task Then_revoking_the_atlas_session_revokes_diary_access()
    {
        _atlasSessionActive = false;
        var status = await _atlasBrowser!.GetFromJsonAsync<GetAuthStatusResponse>("/api/v1/auth/status");
        status!.IsAuthenticated.Should().BeFalse();
        status.AccountId.Should().BeNull();
        using var response = await _atlasBrowser!.GetAsync("/api/v1/products");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<HttpResponseMessage> ExchangeAtlasCodeAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.RequestUri!.AbsoluteUri.Should().Be($"{AtlasOrigin}/api/gym-lun/sso/token");
        var content = await request.Content!.ReadAsStringAsync(cancellationToken);
        var form = QueryHelpers.ParseQuery(content);
        form["code"].ToString().Should().Be(SyntheticCode);
        form["client_secret"].ToString().Should().Be("synthetic-atlas-secret-for-component-test");
        var verifier = Encoding.ASCII.GetBytes(form["code_verifier"].ToString());
        WebEncoders.Base64UrlEncode(SHA256.HashData(verifier)).Should().Be(_atlasChallenge);
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new
            {
                access_token = "inert-test-value",
                token_type = "Bearer",
                expires_in = 60,
                gym_owner_id = AtlasOwner,
                atlas_auth_epoch = AtlasEpoch,
                atlas_expires_at = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
            }),
        };
    }

    private async Task<HttpResponseMessage> CheckAtlasSessionAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.RequestUri!.AbsoluteUri.Should().Be($"{AtlasOrigin}/api/gym-lun/sso/session");
        var form = QueryHelpers.ParseQuery(await request.Content!.ReadAsStringAsync(cancellationToken));
        form["gym_owner_id"].ToString().Should().Be(AtlasOwner.ToString(CultureInfo.InvariantCulture));
        form["atlas_auth_epoch"].ToString().Should().Be(AtlasEpoch.ToString(CultureInfo.InvariantCulture));
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new { active = _atlasSessionActive }) };
    }

    private sealed class AtlasBackchannelStub(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            respond(request, cancellationToken);
    }
}