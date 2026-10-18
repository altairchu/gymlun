using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using FoodDiary.API.Mcp;
using FoodDiary.API.Mcp.Authorization;
using FoodDiary.ComponentTests.Infrastructure;
using FoodDiary.Domain.Entities;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.WeightTracking;
using FoodDiary.Infrastructure;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace FoodDiary.ComponentTests.Scenarios.Mcp;

[UsedImplicitly]
public class McpApiContext(FoodDiaryWebApplicationFactory factory) : BaseContext(factory)
{
    // PKCE example values from RFC 7636, Appendix B
    private const string CodeVerifier = "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk";
    private const string CodeChallenge = "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM";

    private const string State = "af0ifjsldkj";

    private const string AllowedUserEmail = "fake.user@gmail.com";

    private readonly McpOptions _mcpOptions = factory.Services.GetRequiredService<IOptions<McpOptions>>().Value;
    private HttpResponseMessage _metadataResponse = null!;
    private HttpResponseMessage _authorizeResponse = null!;
    private string? _authorizationCode;
    private readonly List<HttpResponseMessage> _codeExchangeResponses = [];
    private HttpResponseMessage _refreshResponse = null!;
    private HttpResponseMessage _mcpResponse = null!;
    private HttpResponseMessage _resourceMetadataResponse = null!;
    private HttpResponseMessage _response = null!;
    private string? _accessToken;
    private HttpResponseMessage _registrationResponse = null!;
    private RegisteredClientCredentials? _registeredClient;
    private HttpResponseMessage _consentPageResponse = null!;
    private Implementation _connectedServer = null!;
    private List<string> _listedToolNames = [];
    private CallToolResult _toolResult = null!;
    private List<string> _listedPromptNames = [];
    private GetPromptResult _promptResult = null!;

    private McpClientRegistration Client => _mcpOptions.Clients[0];
    private string McpResource => _mcpOptions.McpResource;
    private McpTokenService TokenService => Factory.Services.GetRequiredService<McpTokenService>();

    private HttpClient OAuthClient => field ??= Factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false
    });

    public Task Given_authorization_code_was_issued()
    {
        _authorizationCode = TokenService.IssueAuthorizationCode(
            new AuthorizationCodeGrant(AccessGrantFor(AllowedUserEmail), Client.RedirectUri!, CodeChallenge));

        return Task.CompletedTask;
    }

    public Task Given_access_token_was_issued()
    {
        return Given_access_token_was_issued_to(AllowedUserEmail);
    }

    public Task Given_access_token_was_issued_to(string email)
    {
        _accessToken = TokenService.IssueAccessToken(AccessGrantFor(email));
        return Task.CompletedTask;
    }

    public Task Given_read_only_access_token_was_issued()
    {
        _accessToken = TokenService.IssueAccessToken(AccessGrantFor(AllowedUserEmail, "food:read"));
        return Task.CompletedTask;
    }

    public Task Given_mcp_is_disabled()
    {
        Factory = Factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Mcp:Enabled"] = "false"
            })));

        return Task.CompletedTask;
    }

    public Task Given_notes(params Note[] notes)
    {
        return Factory.SeedDataAsync(notes);
    }

    public Task Given_products(params Product[] products)
    {
        return Factory.SeedDataAsync(products);
    }

    public Task Given_categories(params Category[] categories)
    {
        return Factory.SeedDataAsync(categories);
    }

    public Task Given_weight_logs(params WeightLog[] weightLogs)
    {
        return Factory.SeedDataAsync(weightLogs);
    }

    public async Task When_mcp_client_connects()
    {
        await using var mcpClient = await ConnectMcpClient();
        _connectedServer = mcpClient.ServerInfo;
    }

    public Task When_mcp_client_calls_get_food_logs(string from, string to)
    {
        return When_mcp_client_calls_listed_tool("get_food_logs", new Dictionary<string, object?>
        {
            ["from"] = from,
            ["to"] = to
        });
    }

    public Task When_mcp_client_calls_list_products(string productName)
    {
        return When_mcp_client_calls_listed_tool("list_products", new Dictionary<string, object?>
        {
            ["pageNumber"] = 1,
            ["pageSize"] = 100,
            ["productName"] = productName
        });
    }

    public Task When_mcp_client_calls_log_food(string date, string mealType, Product product)
    {
        return When_mcp_client_calls_listed_tool("log_food", new Dictionary<string, object?>
        {
            ["date"] = date,
            ["mealType"] = mealType,
            ["productId"] = product.Id
        });
    }

    public Task When_mcp_client_calls_log_food_by_time_and_servings(string date, string time, decimal servings, Product product)
    {
        return When_mcp_client_calls_listed_tool("log_food", new Dictionary<string, object?>
        {
            ["date"] = date,
            ["time"] = time,
            ["servings"] = servings,
            ["productId"] = product.Id
        });
    }

    public Task When_mcp_client_calls_update_food_log(Note note, string time, int quantity)
    {
        return When_mcp_client_calls_listed_tool("update_food_log", new Dictionary<string, object?>
        {
            ["id"] = note.Id,
            ["time"] = time,
            ["quantity"] = quantity
        });
    }

    public Task When_mcp_client_calls_list_products_in_category(Category category)
    {
        return When_mcp_client_calls_listed_tool("list_products", new Dictionary<string, object?>
        {
            ["pageNumber"] = 1,
            ["pageSize"] = 100,
            ["categoryId"] = category.Id
        });
    }

    public Task When_mcp_client_calls_update_product(Product product, int caloriesPer100g, string[] categories)
    {
        return When_mcp_client_calls_listed_tool("update_product", new Dictionary<string, object?>
        {
            ["id"] = product.Id,
            ["caloriesPer100g"] = caloriesPer100g,
            ["categories"] = categories
        });
    }

    public Task When_mcp_client_calls_delete_product(Product product)
    {
        return When_mcp_client_calls_listed_tool("delete_product", new Dictionary<string, object?>
        {
            ["id"] = product.Id
        });
    }

    public Task When_mcp_client_calls_list_categories()
    {
        return When_mcp_client_calls_listed_tool("list_categories", new Dictionary<string, object?>());
    }

    public Task When_mcp_client_calls_create_category(string name)
    {
        return When_mcp_client_calls_listed_tool("create_category", new Dictionary<string, object?>
        {
            ["name"] = name
        });
    }

    public Task When_mcp_client_calls_rename_category(Category category, string name)
    {
        return When_mcp_client_calls_listed_tool("rename_category", new Dictionary<string, object?>
        {
            ["id"] = category.Id,
            ["name"] = name
        });
    }

    public Task When_mcp_client_calls_delete_category(Category category)
    {
        return When_mcp_client_calls_listed_tool("delete_category", new Dictionary<string, object?>
        {
            ["id"] = category.Id
        });
    }

    public Task When_mcp_client_calls_get_weight_logs(string from, string to)
    {
        return When_mcp_client_calls_listed_tool("get_weight_logs", new Dictionary<string, object?>
        {
            ["from"] = from,
            ["to"] = to
        });
    }

    public Task When_mcp_client_calls_log_weight(string date, decimal weight)
    {
        return When_mcp_client_calls_listed_tool("log_weight", new Dictionary<string, object?>
        {
            ["date"] = date,
            ["weight"] = weight
        });
    }

    public Task When_mcp_client_calls_delete_food_log(Note note)
    {
        return When_mcp_client_calls_listed_tool("delete_food_log", new Dictionary<string, object?>
        {
            ["id"] = note.Id
        });
    }

    public Task When_mcp_client_calls_create_product(string name, string category, int servingGrams, decimal calories, decimal protein)
    {
        return When_mcp_client_calls_listed_tool("create_product", new Dictionary<string, object?>
        {
            ["name"] = name,
            ["categories"] = new[] { category },
            ["servingGrams"] = servingGrams,
            ["calories"] = calories,
            ["protein"] = protein
        });
    }

    public Task When_mcp_client_calls_create_product_counted_in_servings(
        string name,
        string[] categories,
        int servingGrams,
        decimal calories)
    {
        return When_mcp_client_calls_listed_tool("create_product", new Dictionary<string, object?>
        {
            ["name"] = name,
            ["categories"] = categories,
            ["countBy"] = "servings",
            ["servingGrams"] = servingGrams,
            ["calories"] = calories
        });
    }

    public async Task When_mcp_client_lists_tools()
    {
        await using var mcpClient = await ConnectMcpClient();

        var tools = await mcpClient.ListToolsAsync();
        _listedToolNames = tools.Select(tool => tool.Name).ToList();
    }

    private async Task When_mcp_client_calls_listed_tool(string toolName, Dictionary<string, object?> arguments)
    {
        await using var mcpClient = await ConnectMcpClient();

        var tools = await mcpClient.ListToolsAsync();
        _listedToolNames = tools.Select(tool => tool.Name).ToList();

        _toolResult = await tools.Single(tool => tool.Name == toolName).CallAsync(arguments);
    }

    public async Task When_mcp_client_gets_nutrition_report_prompt(string period)
    {
        await using var mcpClient = await ConnectMcpClient();

        var prompts = await mcpClient.ListPromptsAsync();
        _listedPromptNames = prompts.Select(prompt => prompt.Name).ToList();

        _promptResult = await prompts.Single(prompt => prompt.Name == "nutrition_report")
            .GetAsync(new Dictionary<string, object?> { ["period"] = period });
    }

    public async Task When_client_requests_authorization_server_metadata()
    {
        _metadataResponse = await OAuthClient.GetAsync("/.well-known/oauth-authorization-server");
    }

    public async Task When_client_requests(string method, string path)
    {
        _response = await OAuthClient.SendAsync(new HttpRequestMessage(new HttpMethod(method), path));
    }

    public Task When_client_calls_mcp_without_access_token()
    {
        return CallMcp(accessToken: null);
    }

    public Task When_client_calls_mcp_with_access_token()
    {
        return CallMcp(_accessToken);
    }

    private async Task CallMcp(string? accessToken)
    {
        var initializeRequest = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent(
                """
                {
                  "jsonrpc": "2.0",
                  "id": 1,
                  "method": "initialize",
                  "params": {
                    "protocolVersion": "2025-11-25",
                    "capabilities": {},
                    "clientInfo": { "name": "component-tests", "version": "1.0.0" }
                  }
                }
                """,
                Encoding.UTF8,
                "application/json")
        };

        initializeRequest.Headers.Accept.ParseAdd("application/json");
        initializeRequest.Headers.Accept.ParseAdd("text/event-stream");

        if (accessToken is not null)
        {
            initializeRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        _mcpResponse = await OAuthClient.SendAsync(initializeRequest);
    }

    public async Task When_client_requests_resource_metadata_from_challenge()
    {
        var challengeParameters = _mcpResponse.Headers.WwwAuthenticate.FirstOrDefault()?.Parameter ?? string.Empty;
        var resourceMetadataUrl = Regex.Match(challengeParameters, "resource_metadata=\"([^\"]+)\"").Groups[1].Value;

        _resourceMetadataResponse = await OAuthClient.GetAsync(resourceMetadataUrl);
    }

    public Task When_client_requests_authorization()
    {
        return When_client_requests_authorization_with_code_challenge_method("S256");
    }

    public async Task When_client_requests_authorization_with_code_challenge_method(string codeChallengeMethod)
    {
        var authorizeUrl = QueryHelpers.AddQueryString("/authorize", new Dictionary<string, string?>
        {
            ["response_type"] = "code",
            ["client_id"] = Client.ClientId,
            ["redirect_uri"] = Client.RedirectUri,
            ["code_challenge"] = CodeChallenge,
            ["code_challenge_method"] = codeChallengeMethod,
            ["scope"] = "food:read",
            ["resource"] = McpResource,
            ["state"] = State
        });

        _authorizeResponse = await OAuthClient.GetAsync(authorizeUrl);
    }

    public async Task When_client_exchanges_authorization_code()
    {
        _authorizationCode ??= AuthorizationResponseParameter("code");

        var response = await OAuthClient.PostAsync("/token", new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = _authorizationCode,
                ["redirect_uri"] = Client.RedirectUri!,
                ["code_verifier"] = CodeVerifier,
                ["client_id"] = Client.ClientId!,
                ["client_secret"] = Client.ClientSecret!,
                ["resource"] = McpResource
            }));

        _codeExchangeResponses.Add(response);
    }

    public async Task When_client_refreshes_access_token()
    {
        using var tokens = await ReadJson(_codeExchangeResponses.Single());

        _refreshResponse = await OAuthClient.PostAsync("/token", new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = tokens.RootElement.GetProperty("refresh_token").GetString()!,
                ["client_id"] = Client.ClientId!,
                ["client_secret"] = Client.ClientSecret!
            }));
    }

    public Task Then_authorization_response_carries_state_and_issuer()
    {
        AuthorizationResponseParameter("state").Should().Be(State);
        AuthorizationResponseParameter("iss").Should().Be(_mcpOptions.BaseUrl);
        return Task.CompletedTask;
    }

    public Task Then_authorization_error_response_carries_state_and_issuer(string error)
    {
        AuthorizationResponseParameter("error").Should().Be(error);
        AuthorizationResponseParameter("code").Should().BeEmpty();
        AuthorizationResponseParameter("state").Should().Be(State);
        AuthorizationResponseParameter("iss").Should().Be(_mcpOptions.BaseUrl);
        return Task.CompletedTask;
    }

    public async Task Then_client_receives_access_and_refresh_tokens()
    {
        var response = _codeExchangeResponses.Single();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.CacheControl?.NoStore.Should().BeTrue();

        using var tokens = await ReadJson(response);
        tokens.RootElement.GetProperty("access_token").GetString().Should().NotBeNullOrWhiteSpace();
        tokens.RootElement.GetProperty("refresh_token").GetString().Should().NotBeNullOrWhiteSpace();
        tokens.RootElement.GetProperty("token_type").GetString().Should().Be("Bearer");
        tokens.RootElement.GetProperty("expires_in").GetInt32().Should().Be(3600);
    }

    public async Task Then_authorization_server_metadata_is_built_from_base_url()
    {
        _metadataResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        _metadataResponse.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var metadata = JsonNode.Parse(await _metadataResponse.Content.ReadAsStringAsync());
        var expectedMetadata = JsonNode.Parse(
            """
            {
              "issuer": "http://localhost",
              "authorization_endpoint": "http://localhost/authorize",
              "token_endpoint": "http://localhost/token",
              "registration_endpoint": "http://localhost/register",
              "response_types_supported": ["code"],
              "grant_types_supported": ["authorization_code", "refresh_token"],
              "code_challenge_methods_supported": ["S256"],
              "token_endpoint_auth_methods_supported": ["client_secret_post", "client_secret_basic", "none"],
              "scopes_supported": ["food:read", "food:write"],
              "authorization_response_iss_parameter_supported": true
            }
            """);

        JsonNode.DeepEquals(metadata, expectedMetadata)
            .Should().BeTrue("metadata must match RFC 8414 document in the spec, but was {0}", metadata?.ToJsonString());
    }

    public Task Then_client_is_challenged_with_resource_metadata_and_scope()
    {
        _mcpResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        _mcpResponse.Headers.WwwAuthenticate.Select(challenge => challenge.ToString()).Should().Equal(
            """
            Bearer resource_metadata="http://localhost/.well-known/oauth-protected-resource/mcp", scope="food:read food:write"
            """);

        return Task.CompletedTask;
    }

    public Task Then_response_is_not_found()
    {
        _response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        return Task.CompletedTask;
    }

    public Task Then_access_is_forbidden()
    {
        _mcpResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        return Task.CompletedTask;
    }

    public Task Then_mcp_client_is_connected()
    {
        _connectedServer.Name.Should().NotBeNullOrWhiteSpace();
        return Task.CompletedTask;
    }

    public async Task Then_resource_metadata_is_built_from_base_url()
    {
        _resourceMetadataResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        _resourceMetadataResponse.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        using var metadata = await ReadJson(_resourceMetadataResponse);
        metadata.RootElement.GetProperty("resource").GetString().Should().Be("http://localhost/mcp");
        metadata.RootElement.GetProperty("authorization_servers").EnumerateArray()
            .Select(server => server.GetString()).Should().Equal("http://localhost");
        metadata.RootElement.GetProperty("scopes_supported").EnumerateArray()
            .Select(scope => scope.GetString()).Should().Equal("food:read", "food:write");
    }

    public async Task Then_client_receives_new_access_token()
    {
        _refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var tokens = await ReadJson(_refreshResponse);
        tokens.RootElement.GetProperty("access_token").GetString().Should().NotBeNullOrWhiteSpace();
        tokens.RootElement.GetProperty("token_type").GetString().Should().Be("Bearer");
        tokens.RootElement.GetProperty("expires_in").GetInt32().Should().Be(3600);
    }

    public async Task Then_only_the_first_code_exchange_succeeds()
    {
        _codeExchangeResponses.Select(response => response.StatusCode)
            .Should().Equal(HttpStatusCode.OK, HttpStatusCode.BadRequest);

        using var error = await ReadJson(_codeExchangeResponses[1]);
        error.RootElement.GetProperty("error").GetString().Should().Be("invalid_grant");
    }

    public Task Then_mcp_client_lists_tools(params string[] toolNames)
    {
        _listedToolNames.Should().BeEquivalentTo(toolNames);
        return Task.CompletedTask;
    }

    public Task Then_mcp_client_lists_prompts(params string[] promptNames)
    {
        _listedPromptNames.Should().BeEquivalentTo(promptNames);
        return Task.CompletedTask;
    }

    public Task Then_prompt_mentions(string text)
    {
        _promptResult.Messages.Select(message => message.Content).OfType<TextContentBlock>().Single().Text
            .Should().Contain(text);
        return Task.CompletedTask;
    }

    public Task Then_food_logs_contain(Note note)
    {
        _toolResult.IsError.Should().NotBe(true);

        using var foodLogs = JsonDocument.Parse(ToolResultText());
        var day = foodLogs.RootElement.GetProperty("days").EnumerateArray().Single();
        day.GetProperty("date").GetString().Should().Be(note.Date.ToString("yyyy-MM-dd"));

        var item = day.GetProperty("meals").EnumerateArray().Single().GetProperty("items").EnumerateArray().Single();
        item.GetProperty("time").GetString().Should().Be(note.Time.ToString("HH:mm", CultureInfo.InvariantCulture));
        item.GetProperty("product").GetProperty("name").GetString().Should().Be(note.Product!.Name);
        item.GetProperty("quantity").GetInt32().Should().Be(note.ProductQuantity);

        return Task.CompletedTask;
    }

    public Task Then_products_contain(Product product)
    {
        _toolResult.IsError.Should().NotBe(true);

        using var products = JsonDocument.Parse(ToolResultText());
        products.RootElement.GetProperty("totalCount").GetInt32().Should().Be(1);

        var listedProduct = products.RootElement.GetProperty("products").EnumerateArray().Single();
        listedProduct.GetProperty("name").GetString().Should().Be(product.Name);
        listedProduct.GetProperty("categories").EnumerateArray()
            .Select(category => category.GetProperty("name").GetString())
            .Should().Equal(product.Categories.Select(category => category.Name));
        listedProduct.GetProperty("countBy").GetString().Should().Be("grams");

        return Task.CompletedTask;
    }

    public Task Then_food_log_is_logged(string date, string mealType, Product product)
    {
        _toolResult.IsError.Should().NotBe(true, ToolResultText());

        using var response = JsonDocument.Parse(ToolResultText());
        response.RootElement.GetProperty("date").GetString().Should().Be(date);
        response.RootElement.GetProperty("mealType").GetString().Should().Be(mealType);

        var foodLog = response.RootElement.GetProperty("foodLog");
        foodLog.GetProperty("id").GetInt32().Should().BePositive();
        foodLog.GetProperty("product").GetProperty("name").GetString().Should().Be(product.Name);
        foodLog.GetProperty("quantity").GetInt32().Should().Be(product.DefaultQuantity);

        return Task.CompletedTask;
    }

    public Task Then_food_log_is_logged_at(string date, string time, string mealType, Product product, int quantity)
    {
        _toolResult.IsError.Should().NotBe(true, ToolResultText());

        using var response = JsonDocument.Parse(ToolResultText());
        response.RootElement.GetProperty("date").GetString().Should().Be(date);
        response.RootElement.GetProperty("mealType").GetString().Should().Be(mealType);

        var foodLog = response.RootElement.GetProperty("foodLog");
        foodLog.GetProperty("time").GetString().Should().Be(time);
        foodLog.GetProperty("product").GetProperty("name").GetString().Should().Be(product.Name);
        foodLog.GetProperty("quantity").GetInt32().Should().Be(quantity);

        return Task.CompletedTask;
    }

    public Task Then_food_log_is_deleted(Note note)
    {
        _toolResult.IsError.Should().NotBe(true, ToolResultText());

        using var response = JsonDocument.Parse(ToolResultText());
        response.RootElement.GetProperty("deletedId").GetInt32().Should().Be(note.Id);

        return Task.CompletedTask;
    }

    public Task Then_created_product_is_favorite_with_nutrition_per_100g(
        string name,
        string category,
        int defaultQuantity,
        int caloriesPer100g,
        decimal proteinPer100g)
    {
        _toolResult.IsError.Should().NotBe(true, ToolResultText());

        using var response = JsonDocument.Parse(ToolResultText());
        var product = response.RootElement.GetProperty("product");
        product.GetProperty("name").GetString().Should().Be(name);
        product.GetProperty("categories").EnumerateArray()
            .Select(c => c.GetProperty("name").GetString())
            .Should().Equal(category);
        product.GetProperty("countBy").GetString().Should().Be("grams");
        product.GetProperty("defaultQuantity").GetInt32().Should().Be(defaultQuantity);
        product.GetProperty("isFavorite").GetBoolean().Should().BeTrue();
        product.GetProperty("per100g").GetProperty("calories").GetInt32().Should().Be(caloriesPer100g);
        product.GetProperty("per100g").GetProperty("protein").GetDecimal().Should().Be(proteinPer100g);

        return Task.CompletedTask;
    }

    public Task Then_created_product_is_counted_in_servings(string name, string[] categories, int servingGrams)
    {
        _toolResult.IsError.Should().NotBe(true, ToolResultText());

        using var response = JsonDocument.Parse(ToolResultText());
        var product = response.RootElement.GetProperty("product");
        product.GetProperty("name").GetString().Should().Be(name);
        product.GetProperty("categories").EnumerateArray()
            .Select(c => c.GetProperty("name").GetString())
            .Should().BeEquivalentTo(categories);
        product.GetProperty("countBy").GetString().Should().Be("servings");
        product.GetProperty("defaultQuantity").GetInt32().Should().Be(servingGrams);

        return Task.CompletedTask;
    }

    public async Task Then_food_log_is_changed_to(Note note, string time, string mealType, int quantity)
    {
        _toolResult.IsError.Should().NotBe(true, ToolResultText());

        using var response = JsonDocument.Parse(ToolResultText());
        response.RootElement.GetProperty("mealType").GetString().Should().Be(mealType);
        response.RootElement.GetProperty("foodLog").GetProperty("time").GetString().Should().Be(time);

        var savedNote = await QueryDatabase(db => db.Notes.AsNoTracking().SingleAsync(n => n.Id == note.Id));
        savedNote.Should().BeEquivalentTo(new
        {
            note.Date,
            Time = TimeOnly.Parse(time, CultureInfo.InvariantCulture),
            MealType = Enum.Parse<MealType>(mealType),
            ProductQuantity = quantity
        });
    }

    public async Task Then_product_is_changed_to(Product product, int caloriesPer100g, string[] categories)
    {
        _toolResult.IsError.Should().NotBe(true, ToolResultText());

        using var response = JsonDocument.Parse(ToolResultText());
        var changedProduct = response.RootElement.GetProperty("product");
        changedProduct.GetProperty("name").GetString().Should().Be(product.Name);
        changedProduct.GetProperty("per100g").GetProperty("calories").GetInt32().Should().Be(caloriesPer100g);

        var savedProduct = await QueryDatabase(db => db.Products.AsNoTracking()
            .Include(p => p.Categories)
            .SingleAsync(p => p.Id == product.Id));
        savedProduct.CaloriesCost.Should().Be(caloriesPer100g);
        savedProduct.Protein.Should().Be(product.Protein);
        savedProduct.Categories.Select(c => c.Name).Should().BeEquivalentTo(categories);
    }

    public async Task Then_product_and_its_food_logs_are_deleted(Product product)
    {
        _toolResult.IsError.Should().NotBe(true, ToolResultText());

        using var response = JsonDocument.Parse(ToolResultText());
        response.RootElement.GetProperty("deletedId").GetInt32().Should().Be(product.Id);

        (await QueryDatabase(db => db.Products.AnyAsync(p => p.Id == product.Id))).Should().BeFalse();
        (await QueryDatabase(db => db.Notes.AnyAsync(n => n.ProductId == product.Id))).Should().BeFalse();
    }

    public Task Then_categories_are_listed_with_product_counts(params (string Name, int ProductCount)[] categories)
    {
        _toolResult.IsError.Should().NotBe(true, ToolResultText());

        using var response = JsonDocument.Parse(ToolResultText());
        response.RootElement.GetProperty("categories").EnumerateArray()
            .Select(category => (category.GetProperty("name").GetString()!, category.GetProperty("productCount").GetInt32()))
            .Should().Equal(categories);

        return Task.CompletedTask;
    }

    public async Task Then_category_is_saved_as(string name)
    {
        _toolResult.IsError.Should().NotBe(true, ToolResultText());

        using var response = JsonDocument.Parse(ToolResultText());
        var id = response.RootElement.GetProperty("id").GetInt32();
        response.RootElement.GetProperty("name").GetString().Should().Be(name);

        var savedCategory = await QueryDatabase(db => db.Categories.AsNoTracking().SingleAsync(c => c.Id == id));
        savedCategory.Name.Should().Be(name);
    }

    public async Task Then_category_is_deleted_and_its_products_stay(Category category, Product product)
    {
        _toolResult.IsError.Should().NotBe(true, ToolResultText());

        (await QueryDatabase(db => db.Categories.AnyAsync(c => c.Id == category.Id))).Should().BeFalse();

        var savedProduct = await QueryDatabase(db => db.Products.AsNoTracking()
            .Include(p => p.Categories)
            .SingleAsync(p => p.Id == product.Id));
        savedProduct.Categories.Should().BeEmpty();
    }

    public Task Then_weight_logs_are(params (string Date, decimal Weight)[] weightLogs)
    {
        _toolResult.IsError.Should().NotBe(true, ToolResultText());

        using var response = JsonDocument.Parse(ToolResultText());
        response.RootElement.GetProperty("weightLogs").EnumerateArray()
            .Select(weightLog => (weightLog.GetProperty("date").GetString()!, weightLog.GetProperty("weight").GetDecimal()))
            .Should().Equal(weightLogs);

        return Task.CompletedTask;
    }

    public async Task Then_weight_is_logged(string date, decimal weight)
    {
        _toolResult.IsError.Should().NotBe(true, ToolResultText());

        var loggedDate = DateOnly.Parse(date, CultureInfo.InvariantCulture);
        var savedWeightLog = await QueryDatabase(db => db.WeightLogs.AsNoTracking().SingleAsync(wl => wl.Date == loggedDate));
        savedWeightLog.Weight.Should().Be(weight);
    }

    public Task Then_tool_call_is_error(string message)
    {
        _toolResult.IsError.Should().BeTrue();
        ToolResultText().Should().Contain(message);
        return Task.CompletedTask;
    }

    private async Task<McpClient> ConnectMcpClient()
    {
        var transport = new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Endpoint = new Uri(McpResource),
                TransportMode = HttpTransportMode.StreamableHttp,
                AdditionalHeaders = new Dictionary<string, string>
                {
                    ["Authorization"] = $"Bearer {_accessToken}"
                }
            },
            Factory.CreateClient());

        return await McpClient.CreateAsync(transport);
    }

    public async Task When_client_registers(string clientName, string redirectUri, string tokenEndpointAuthMethod)
    {
        _registrationResponse = await OAuthClient.PostAsJsonAsync("/register", new
        {
            client_name = clientName,
            redirect_uris = new[] { redirectUri },
            token_endpoint_auth_method = tokenEndpointAuthMethod,
            grant_types = new[] { "authorization_code", "refresh_token" },
            response_types = new[] { "code" }
        });

        if (_registrationResponse.StatusCode == HttpStatusCode.Created)
        {
            using var registration = await ReadJson(_registrationResponse);
            var root = registration.RootElement;

            _registeredClient = new RegisteredClientCredentials(
                root.GetProperty("client_id").GetString()!,
                root.TryGetProperty("client_secret", out var secret) ? secret.GetString() : null,
                redirectUri,
                tokenEndpointAuthMethod);
        }
    }

    public async Task When_registered_client_requests_authorization()
    {
        var authorizeUrl = QueryHelpers.AddQueryString("/authorize", new Dictionary<string, string?>
        {
            ["response_type"] = "code",
            ["client_id"] = _registeredClient!.ClientId,
            ["redirect_uri"] = _registeredClient.RedirectUri,
            ["code_challenge"] = CodeChallenge,
            ["code_challenge_method"] = "S256",
            ["resource"] = McpResource,
            ["state"] = State
        });

        _consentPageResponse = await OAuthClient.GetAsync(authorizeUrl);
    }

    public Task When_owner_allows_the_connection()
    {
        return AnswerConsent("allow");
    }

    public Task When_owner_does_not_allow_the_connection()
    {
        return AnswerConsent("deny");
    }

    public async Task When_registered_client_exchanges_authorization_code()
    {
        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = AuthorizationResponseParameter("code"),
            ["redirect_uri"] = _registeredClient!.RedirectUri,
            ["code_verifier"] = CodeVerifier,
            ["resource"] = McpResource
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "/token");

        if (_registeredClient.TokenEndpointAuthMethod == "client_secret_basic")
        {
            var credentials = $"{Uri.EscapeDataString(_registeredClient.ClientId)}:{Uri.EscapeDataString(_registeredClient.ClientSecret!)}";
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials)));
        }
        else
        {
            form["client_id"] = _registeredClient.ClientId;
        }

        request.Content = new FormUrlEncodedContent(form);
        var response = await OAuthClient.SendAsync(request);
        _codeExchangeResponses.Add(response);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            using var tokens = await ReadJson(response);
            _accessToken = tokens.RootElement.GetProperty("access_token").GetString();
        }
    }

    public async Task Then_client_is_registered_as(string clientName, string tokenEndpointAuthMethod)
    {
        _registrationResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        using var registration = await ReadJson(_registrationResponse);
        var root = registration.RootElement;
        root.GetProperty("client_name").GetString().Should().Be(clientName);
        root.GetProperty("token_endpoint_auth_method").GetString().Should().Be(tokenEndpointAuthMethod);
        root.GetProperty("redirect_uris").EnumerateArray().Select(uri => uri.GetString())
            .Should().Equal(_registeredClient!.RedirectUri);
        root.TryGetProperty("client_secret", out _).Should().Be(tokenEndpointAuthMethod != "none");
    }

    public async Task Then_registration_is_rejected_with(string error)
    {
        _registrationResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var response = await ReadJson(_registrationResponse);
        response.RootElement.GetProperty("error").GetString().Should().Be(error);
    }

    public async Task Then_owner_is_asked_to_allow(string clientName)
    {
        _consentPageResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        _consentPageResponse.Content.Headers.ContentType?.MediaType.Should().Be("text/html");
        _consentPageResponse.Headers.GetValues("X-Frame-Options").Should().Equal("DENY");

        var page = await _consentPageResponse.Content.ReadAsStringAsync();
        page.Should().Contain($"允許「{clientName}」連接你嘅 gymlun？");
    }

    private async Task AnswerConsent(string decision)
    {
        var page = await _consentPageResponse.Content.ReadAsStringAsync();
        var consentTicket = WebUtility.HtmlDecode(
            Regex.Match(page, "name=\"consent_ticket\" value=\"([^\"]+)\"").Groups[1].Value);

        _authorizeResponse = await OAuthClient.PostAsync("/authorize", new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["consent_ticket"] = consentTicket,
                ["decision"] = decision
            }));
    }

    private sealed record RegisteredClientCredentials(
        string ClientId,
        string? ClientSecret,
        string RedirectUri,
        string TokenEndpointAuthMethod);

    private async Task<TResult> QueryDatabase<TResult>(Func<FoodDiaryContext, Task<TResult>> query)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        return await query(scope.ServiceProvider.GetRequiredService<FoodDiaryContext>());
    }

    private string ToolResultText() => _toolResult.Content.OfType<TextContentBlock>().Single().Text;

    private AccessGrant AccessGrantFor(string email, string scope = "food:read food:write") =>
        new(Client.ClientId!, email, scope, McpResource);

    private static async Task<JsonDocument> ReadJson(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync());

    private string AuthorizationResponseParameter(string name)
    {
        _authorizeResponse.StatusCode.Should().Be(HttpStatusCode.Redirect);

        var location = _authorizeResponse.Headers.Location!;
        location.GetLeftPart(UriPartial.Path).Should().Be(_registeredClient?.RedirectUri ?? Client.RedirectUri);

        return QueryHelpers.ParseQuery(location.Query).GetValueOrDefault(name).ToString();
    }
}
