using System.Text.Json;
using JetBrains.Annotations;

namespace FoodDiary.API.Mcp.Authorization;

/// <summary>
/// OAuth 2.0 Dynamic Client Registration (RFC 7591), so an MCP client connects with nothing but the server URL.
/// </summary>
internal sealed class RegistrationRequestHandler(McpClientRegistry registry)
{
    private const int MaxRedirectUris = 5;
    private const int MaxRedirectUriLength = 2000;
    private const int MaxClientNameLength = 100;

    private static readonly string[] GrantTypes = ["authorization_code", "refresh_token"];
    private static readonly string[] ResponseTypes = ["code"];

    private static readonly HashSet<string> UnsafeRedirectSchemes =
        new(["javascript", "data", "file", "vbscript", "about", "blob", "ftp", "ws", "wss"], StringComparer.OrdinalIgnoreCase);

    [PublicAPI]
    private sealed record RegistrationRequest(
        string[]? RedirectUris,
        string? ClientName,
        string? TokenEndpointAuthMethod,
        string[]? GrantTypes,
        string[]? ResponseTypes);

    [PublicAPI]
    private sealed record RegistrationResponse(
        string ClientId,
        long ClientIdIssuedAt,
        string? ClientSecret,
        long? ClientSecretExpiresAt,
        string? ClientName,
        IReadOnlyList<string> RedirectUris,
        string[] GrantTypes,
        string[] ResponseTypes,
        string TokenEndpointAuthMethod);

    [PublicAPI]
    // ReSharper disable once MemberHidesStaticFromOuterClass
    private sealed record ErrorResponse(string Error, string ErrorDescription);

    public async Task<IResult> Handle(HttpContext context)
    {
        context.Response.Headers.CacheControl = "no-store";
        context.Response.Headers.Pragma = "no-cache";

        RegistrationRequest? request;

        try
        {
            request = await context.Request.ReadFromJsonAsync<RegistrationRequest>(
                AuthorizationServerEndpoints.SerializerOptions,
                context.RequestAborted);
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException)
        {
            return InvalidClientMetadata("Expected a JSON client registration request");
        }

        if (request is null)
        {
            return InvalidClientMetadata("Expected a JSON client registration request");
        }

        if (FindRedirectUrisFailure(request.RedirectUris) is { } redirectUrisFailure)
        {
            return Error("invalid_redirect_uri", redirectUrisFailure);
        }

        var clientName = string.IsNullOrWhiteSpace(request.ClientName) ? null : request.ClientName.Trim();

        if (clientName?.Length > MaxClientNameLength)
        {
            return InvalidClientMetadata($"client_name must be at most {MaxClientNameLength} characters");
        }

        var authMethod = request.TokenEndpointAuthMethod ?? TokenEndpointAuthMethods.ClientSecretBasic;

        if (!TokenEndpointAuthMethods.Supported.Contains(authMethod))
        {
            return InvalidClientMetadata(
                $"Supported token_endpoint_auth_method values are {string.Join(", ", TokenEndpointAuthMethods.Supported)}");
        }

        if (request.GrantTypes is { } grantTypes &&
            (!grantTypes.Contains("authorization_code") || grantTypes.Except(GrantTypes).Any()))
        {
            return InvalidClientMetadata("grant_types must include authorization_code and may add refresh_token");
        }

        if (request.ResponseTypes is { } responseTypes && responseTypes.Except(ResponseTypes).Any())
        {
            return InvalidClientMetadata("The only supported response_types value is code");
        }

        var credentials = registry.Register(
            new ClientMetadata(clientName, request.RedirectUris!.Distinct().ToArray(), authMethod));

        var response = new RegistrationResponse(
            credentials.Client.ClientId,
            credentials.IssuedAt.ToUnixTimeSeconds(),
            credentials.ClientSecret,
            ClientSecretExpiresAt: credentials.ClientSecret is null ? null : 0,
            credentials.Client.ClientName,
            credentials.Client.RedirectUris,
            GrantTypes,
            ResponseTypes,
            credentials.Client.TokenEndpointAuthMethod);

        return Results.Json(response, AuthorizationServerEndpoints.SerializerOptions, statusCode: StatusCodes.Status201Created);
    }

    private static string? FindRedirectUrisFailure(string[]? redirectUris)
    {
        if (redirectUris is not { Length: > 0 })
        {
            return "redirect_uris requires at least one URI";
        }

        if (redirectUris.Length > MaxRedirectUris)
        {
            return $"redirect_uris allows at most {MaxRedirectUris} URIs";
        }

        return redirectUris.Select(FindRedirectUriFailure).FirstOrDefault(failure => failure is not null);
    }

    private static string? FindRedirectUriFailure(string? redirectUri)
    {
        if (string.IsNullOrWhiteSpace(redirectUri) || redirectUri.Length > MaxRedirectUriLength)
        {
            return $"Each redirect URI must be 1 to {MaxRedirectUriLength} characters";
        }

        // On Unix, "/callback" parses as an absolute file:///callback URI
        if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out var uri) || uri.IsFile)
        {
            return $"{redirectUri} is not an absolute URI";
        }

        if (uri.Fragment.Length > 0)
        {
            return $"{redirectUri} must not have a fragment";
        }

        if (uri.Scheme == Uri.UriSchemeHttp)
        {
            return uri.IsLoopback ? null : $"{redirectUri} must use https; http is allowed only for localhost";
        }

        return UnsafeRedirectSchemes.Contains(uri.Scheme) ? $"{redirectUri} uses an unsupported scheme" : null;
    }

    private static IResult InvalidClientMetadata(string description) => Error("invalid_client_metadata", description);

    private static IResult Error(string error, string description) =>
        Results.Json(
            new ErrorResponse(error, description),
            AuthorizationServerEndpoints.SerializerOptions,
            statusCode: StatusCodes.Status400BadRequest);
}
