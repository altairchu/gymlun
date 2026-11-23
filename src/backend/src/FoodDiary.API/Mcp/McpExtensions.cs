using FoodDiary.API.Authentication;
using FoodDiary.API.Mcp.Authorization;
using FoodDiary.API.Mcp.Tools.Gym;
using FoodDiary.API.Mcp.Tools.Products;
using FoodDiary.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.AspNetCore.Authentication;
using ModelContextProtocol.Authentication;
using ModelContextProtocol.Protocol;

namespace FoodDiary.API.Mcp;

public static class McpExtensions
{
    private const string McpAuthorizationPolicy = "Mcp";
    public const string McpWritePolicy = "McpWrite";

    private static readonly PathString[] McpAndOAuthPaths = ["/mcp", "/authorize", "/token", "/register", "/.well-known"];

    public static void AddFoodDiaryMcp(this IServiceCollection services, IConfiguration configuration)
    {
        var mcpSection = configuration.GetSection(McpOptions.SectionName);

        services.AddOptions<McpOptions>()
            .Bind(mcpSection)
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<McpOptions>, McpOptionsValidator>();

        services.AddMemoryCache();
        services.AddSingleton<McpClientRegistry>();
        services.AddSingleton<AuthorizeRequestValidator>();
        services.AddSingleton<McpTokenService>();
        services.AddScoped<AuthorizeRequestHandler>();
        services.AddScoped<TokenRequestHandler>();
        services.AddScoped<RegistrationRequestHandler>();
        services.AddScoped<McpGrantValidator>();

        if (mcpSection.Get<McpOptions>() is { Enabled: true })
        {
            services.AddMcpResourceServer(configuration.GetValue<bool>("AtlasApple:Enabled"));
        }
    }

    public static void MapMcpResourceServer(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapMcp("/mcp").RequireAuthorization(McpAuthorizationPolicy);
    }

    public static void UseNotFoundForMcpAndOAuthPaths(this IApplicationBuilder app)
    {
        app.MapWhen(
            context => McpAndOAuthPaths.Any(path => context.Request.Path.StartsWithSegments(path)),
            branch => branch.Run(context =>
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return Task.CompletedTask;
            }));
    }

    private static void AddMcpResourceServer(this IServiceCollection services, bool usesAtlas)
    {
        services.AddScoped<PhotoDownloader>();
        PhotoDownloader.AddHttpClient(services);

        var server = services.AddMcpServer(options =>
            {
                if (usesAtlas)
                {
                    options.ServerInstructions =
                        "Call get_gym_context before other GymLun tools at the start of a conversation. " +
                        "Use the user's goal, current eating approach and latest weight for requests. " +
                        "Distinguish daily estimates from chosen targets and leave unknown measurements blank.";
                }
            })
            .WithHttpTransport(options => options.SessionMode = HttpServerSessionMode.Stateless)
            .AddAuthorizationFilters()
            .WithToolsFromAssembly()
            .WithPromptsFromAssembly();

        if (usesAtlas)
        {
            server.WithTools<GymTools>()
                .WithMessageFilters(filters => filters.AddIncomingFilter(next => async (context, token) =>
                {
                    var access = context.User?.FindFirst(McpAccessTokenHandler.ContextAccessClaim)?.Value;
                    if (context.JsonRpcMessage is JsonRpcRequest { Method: "initialize" or "server/discover" } &&
                        Guid.TryParseExact(access, "N", out var accessId))
                    {
                        context.Services?.GetService<IMemoryCache>()?.Remove(ContextKey(accessId));
                    }

                    await next(context, token);
                }))
                .WithRequestFilters(filters => filters.AddCallToolFilter(next => async (context, token) =>
                {
                    var cache = context.Services?.GetService<IMemoryCache>();
                    var access = context.User?.FindFirst(McpAccessTokenHandler.ContextAccessClaim)?.Value;
                    if (cache is null || !Guid.TryParseExact(access, "N", out var accessId))
                    {
                        return ToolError("Reconnect GymLun with the current AtlasApple account.");
                    }

                    if (context.Params?.Name == GymTools.ContextTool)
                    {
                        var result = await next(context, token);
                        if (result.IsError != true)
                        {
                            var lifetime = context.Services?.GetService<IOptions<McpOptions>>()?.Value.AccessTokenLifetime ??
                                TimeSpan.FromHours(1);
                            cache.Set(ContextKey(accessId), true, lifetime);
                        }

                        return result;
                    }

                    return cache.TryGetValue(ContextKey(accessId), out _)
                        ? await next(context, token)
                        : ToolError("Call get_gym_context first to read the current goal, eating approach and latest weight.");
                }));
        }

        services.AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, McpAccessTokenHandler>(McpAccessTokenHandler.SchemeName, configureOptions: null)
            .AddMcp();

        services.AddOptions<McpAuthenticationOptions>(McpAuthenticationDefaults.AuthenticationScheme)
            .Configure<IOptions<McpOptions>>((options, mcpOptions) =>
            {
                options.ForwardAuthenticate = McpAccessTokenHandler.SchemeName;
                options.ForwardChallenge = McpAccessTokenHandler.SchemeName;
                options.ResourceMetadataUri = new Uri(mcpOptions.Value.ResourceMetadataUrl);
                options.ResourceMetadata = new ProtectedResourceMetadata
                {
                    Resource = mcpOptions.Value.McpResource,
                    AuthorizationServers = [mcpOptions.Value.BaseUrl!],
                    ScopesSupported = McpScopes.Supported
                };
            });

        services.AddOptions<AuthorizationOptions>()
            .Configure<IOptions<AuthOptions>>((options, authOptions) =>
            {
                options.AddPolicy(McpAuthorizationPolicy, policy =>
                {
                    policy.AddAuthenticationSchemes(McpAuthenticationDefaults.AuthenticationScheme)
                        .RequireAuthenticatedUser();
                    if (usesAtlas)
                    {
                        policy.RequireClaim(AtlasAppleAuthentication.OwnerClaim)
                            .RequireClaim(McpScopes.ClaimType, McpScopes.FoodRead)
                            .RequireClaim(McpScopes.ClaimType, McpScopes.FoodWrite);
                    }
                    else
                    {
                        policy.RequireClaim(Constants.ClaimTypes.Email, authOptions.Value.AllowedEmails);
                    }
                });

                options.AddPolicy(McpWritePolicy, policy => policy
                    .AddAuthenticationSchemes(McpAuthenticationDefaults.AuthenticationScheme)
                    .RequireAuthenticatedUser()
                    .RequireClaim(McpScopes.ClaimType, McpScopes.FoodWrite));
            });
    }

    private static (string, Guid) ContextKey(Guid access) => ("mcp:loaded-context", access);

    private static CallToolResult ToolError(string message) => new()
    {
        IsError = true,
        Content = [new TextContentBlock { Text = message }]
    };
}
