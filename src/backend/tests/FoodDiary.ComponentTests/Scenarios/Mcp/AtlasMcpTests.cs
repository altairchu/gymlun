using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using FoodDiary.API.Authentication;
using FoodDiary.API.Features.Gym;
using FoodDiary.API.Mcp.Authorization;
using FoodDiary.ComponentTests.Infrastructure;
using FoodDiary.Domain.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace FoodDiary.ComponentTests.Scenarios.Mcp;

[Collection(nameof(InfrastructureCollection))]
public sealed class AtlasMcpTests(InfrastructureFixture infrastructure) : IAsyncLifetime
{
    private const string ClientId = "component-tests";
    private const string ClientSecret = "component-tests-secret";
    private const string Verifier = "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk";
    private const string Challenge = "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM";
    private static readonly Uri BaseUrl = new("https://localhost");
    private static readonly Uri Callback = new(BaseUrl, "/oauth/callback");
    private static readonly Uri Resource = new(BaseUrl, "/mcp");

    public Task InitializeAsync() => infrastructure.Database.Clear();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Atlas_connection_requires_context_and_keeps_web_and_mcp_records_separate_by_account()
    {
        await using var factory = AtlasFactory();
        using var first = WebClient(factory, 5101);
        using var second = WebClient(factory, 5102);
        using var oauth = factory.CreateClient(ClientOptions());

        var empty = await first.GetFromJsonAsync<GymContext>("/api/v1/gym-context");
        Assert.Equal(0, empty!.Version);
        using var saved = await first.PutAsJsonAsync("/api/v1/gym-context",
            new GymContextInput(Goal: "maintain", EatingApproach: "Two meals", TargetKcal: 2050));
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        Assert.Equal(1, (await saved.Content.ReadFromJsonAsync<GymContext>())!.Version);
        Assert.Null((await second.GetFromJsonAsync<GymContext>("/api/v1/gym-context"))!.Goal);

        var tokens = await Authorize(first, oauth);
        await using var client = await Connect(factory, tokens.Access);
        Assert.True((await client.CallToolAsync("list_body_composition")).IsError);

        var context = ReadTool<GymContext>(await client.CallToolAsync("get_gym_context"));
        Assert.Equal("Two meals", context.EatingApproach);
        Assert.Null(context.LatestWeight);
        var created = ReadTool<BodyMeasurementResult>(await client.CallToolAsync("log_body_composition",
            new Dictionary<string, object?>
            {
                ["measurement"] = new BodyMeasurementInput(new DateOnly(2026, 1, 4),
                    Metrics: new Dictionary<string, decimal?> { ["weightKg"] = 76, ["bodyFatPercent"] = 25 }),
            }));
        Assert.Equal(19m, created.DerivedMetrics["fatMassKg"]);

        var visible = await first.GetFromJsonAsync<BodyMeasurementResult[]>("/api/v1/body-composition");
        Assert.Equal(created.Id, Assert.Single(visible!).Id);
        Assert.Equal(76m, (await first.GetFromJsonAsync<GymContext>("/api/v1/gym-context"))!.LatestWeight!.WeightKg);
        Assert.Empty((await second.GetFromJsonAsync<BodyMeasurementResult[]>("/api/v1/body-composition"))!);
        using var forbidden = await second.DeleteAsync($"/api/v1/body-composition/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, forbidden.StatusCode);

        await using var nextSession = await Connect(factory, tokens.Access);
        Assert.True((await nextSession.CallToolAsync("list_body_composition")).IsError);

        var fresh = await Refresh(oauth, tokens.Refresh);
        using var reusedRefresh = await SendRefresh(oauth, tokens.Refresh);
        Assert.Equal(HttpStatusCode.BadRequest, reusedRefresh.StatusCode);
        await using var renewed = await Connect(factory, fresh.Access);
        Assert.True((await renewed.CallToolAsync("list_body_composition")).IsError);
        Assert.Equal(76m, ReadTool<GymContext>(await renewed.CallToolAsync("get_gym_context")).LatestWeight!.WeightKg);

        var connections = await first.GetFromJsonAsync<JsonElement>("/api/v1/mcp-connections");
        var id = Assert.Single(connections.GetProperty("connections").EnumerateArray()).GetProperty("id").GetGuid();
        using var otherOwner = await second.DeleteAsync($"/api/v1/mcp-connections/{id}");
        Assert.Equal(HttpStatusCode.NotFound, otherOwner.StatusCode);
        using var revoke = await first.DeleteAsync($"/api/v1/mcp-connections/{id}");
        Assert.Equal(HttpStatusCode.NoContent, revoke.StatusCode);
        using var revokedRefresh = await SendRefresh(oauth, fresh.Refresh);
        Assert.Equal(HttpStatusCode.BadRequest, revokedRefresh.StatusCode);

        using var check = new HttpRequestMessage(HttpMethod.Post, Resource);
        check.Headers.Authorization = new AuthenticationHeaderValue("Bearer", fresh.Access);
        using var revokedAccess = await oauth.SendAsync(check);
        Assert.Equal(HttpStatusCode.Unauthorized, revokedAccess.StatusCode);

        var legacy = factory.Services.GetRequiredService<McpTokenService>()
            .IssueAccessToken(new AccessGrant(ClientId, "fake.user@gmail.com", "food:read food:write", Resource.AbsoluteUri));
        using var legacyRequest = new HttpRequestMessage(HttpMethod.Post, Resource);
        legacyRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", legacy);
        using var legacyResponse = await oauth.SendAsync(legacyRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, legacyResponse.StatusCode);
    }

    private WebApplicationFactory<FoodDiary.API.Startup> AtlasFactory() =>
        new FoodDiaryWebApplicationFactory(infrastructure.Database).WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["AtlasApple:Enabled"] = "true",
                    ["AtlasApple:Authority"] = "https://atlas-tests.invalid",
                    ["AtlasApple:ClientId"] = "atlas-component-tests",
                    ["AtlasApple:ClientSecret"] = new string('t', 40),
                    ["Mcp:BaseUrl"] = BaseUrl.AbsoluteUri.TrimEnd('/'),
                    ["Mcp:Clients:0:RedirectUri"] = Callback.AbsoluteUri,
                }));
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IFoodDiaryOwner>();
                services.AddScoped<IFoodDiaryOwner, AuthenticatedDiaryOwner>();
                services.AddHttpClient<AtlasAppleSessionValidator>()
                    .ConfigurePrimaryHttpMessageHandler(() => new ValidAtlasSession());
            });
        });

    private static WebApplicationFactoryClientOptions ClientOptions() => new()
    {
        BaseAddress = BaseUrl,
        AllowAutoRedirect = false
    };

    private static HttpClient WebClient(WebApplicationFactory<FoodDiary.API.Startup> factory, long ownerId)
    {
        var scheme = Constants.AuthenticationSchemes.Cookie;
        var options = factory.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get(scheme);
        var claims = new ClaimsIdentity(
            [new Claim(AtlasAppleAuthentication.OwnerClaim, ownerId.ToString(CultureInfo.InvariantCulture)),
                new Claim(AtlasAppleAuthentication.AuthEpochClaim, "3")], scheme);
        var now = (options.TimeProvider ?? TimeProvider.System).GetUtcNow();
        var properties = new AuthenticationProperties { IssuedUtc = now, ExpiresUtc = now.AddHours(1) };
        var encrypted = options.TicketDataFormat.Protect(new AuthenticationTicket(new ClaimsPrincipal(claims), properties, scheme));
        var client = factory.CreateClient(ClientOptions());
        client.DefaultRequestHeaders.Add("Cookie", $"{options.Cookie.Name}={encrypted}");
        return client;
    }

    private static async Task<(string Access, string Refresh)> Authorize(HttpClient browser, HttpClient tokenClient)
    {
        var request = QueryHelpers.AddQueryString("/authorize", new Dictionary<string, string?>
        {
            ["response_type"] = "code",
            ["client_id"] = ClientId,
            ["redirect_uri"] = Callback.AbsoluteUri,
            ["code_challenge"] = Challenge,
            ["code_challenge_method"] = "S256",
            ["scope"] = "food:read",
            ["resource"] = Resource.AbsoluteUri
        });
        using var page = await browser.GetAsync(request);
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        var ticket = Regex.Match(await page.Content.ReadAsStringAsync(), "name=\"consent_ticket\" value=\"([^\"]+)\"")
            .Groups[1].Value;
        Assert.NotEmpty(ticket);
        using var approvalBody = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["consent_ticket"] = WebUtility.HtmlDecode(ticket),
            ["decision"] = "allow"
        });
        using var consent = await browser.PostAsync("/authorize", approvalBody);
        Assert.Equal(HttpStatusCode.Redirect, consent.StatusCode);
        var code = QueryHelpers.ParseQuery(consent.Headers.Location!.Query)["code"].ToString();
        using var exchangeBody = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = ClientId,
            ["client_secret"] = ClientSecret,
            ["redirect_uri"] = Callback.AbsoluteUri,
            ["code_verifier"] = Verifier,
            ["code"] = code,
        });
        using var exchange = await tokenClient.PostAsync("/token", exchangeBody);
        return await ReadTokens(exchange);
    }

    private static async Task<(string Access, string Refresh)> Refresh(HttpClient client, string token)
    {
        using var response = await SendRefresh(client, token);
        return await ReadTokens(response);
    }

    private static async Task<HttpResponseMessage> SendRefresh(HttpClient client, string token)
    {
        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = ClientId,
            ["client_secret"] = ClientSecret,
            ["refresh_token"] = token,
        });
        return await client.PostAsync("/token", form);
    }

    private static async Task<(string Access, string Refresh)> ReadTokens(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("food:write", json.GetProperty("scope").GetString());
        return (json.GetProperty("access_token").GetString()!, json.GetProperty("refresh_token").GetString()!);
    }

    private static Task<McpClient> Connect(WebApplicationFactory<FoodDiary.API.Startup> factory, string bearer) =>
        McpClient.CreateAsync(new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = Resource,
            TransportMode = HttpTransportMode.StreamableHttp,
            AdditionalHeaders = new Dictionary<string, string> { ["Authorization"] = $"Bearer {bearer}" }
        }, factory.CreateClient(ClientOptions())));

    private static T ReadTool<T>(CallToolResult response)
    {
        var text = Assert.Single(response.Content.OfType<TextContentBlock>()).Text;
        Assert.False(response.IsError == true, text);
        return JsonSerializer.Deserialize<T>(text, JsonSerializerOptions.Web)!;
    }

    private sealed class ValidAtlasSession : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.Equal("/api/gym-lun/sso/session", request.RequestUri!.AbsolutePath);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { active = true })
            });
        }
    }
}
