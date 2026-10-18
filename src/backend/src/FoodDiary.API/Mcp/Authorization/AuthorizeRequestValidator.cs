using Microsoft.Extensions.Options;

namespace FoodDiary.API.Mcp.Authorization;

public abstract record AuthorizeRequestValidation
{
    public sealed record Accepted(
        RegisteredClient Client,
        string RedirectUri,
        string CodeChallenge,
        string Scope,
        string Resource,
        string? State) : AuthorizeRequestValidation;

    /// <summary>
    /// The client or its redirect_uri cannot be trusted, so the error must not be sent to that redirect_uri
    /// (RFC 6749 §4.1.2.1).
    /// </summary>
    public sealed record Refused(string Reason) : AuthorizeRequestValidation;

    public sealed record ErrorRedirect(
        string RedirectUri,
        string Error,
        string Description,
        string? State) : AuthorizeRequestValidation;
}

public sealed class AuthorizeRequestValidator(McpClientRegistry clientRegistry, IOptions<McpOptions> options)
{
    public AuthorizeRequestValidation Validate(AuthorizeRequest request)
    {
        var client = clientRegistry.Find(request.ClientId);

        if (client is null)
        {
            return new AuthorizeRequestValidation.Refused("client_id is not registered");
        }

        if (request.RedirectUri is not { } redirectUri || !client.RedirectUris.Contains(redirectUri))
        {
            return new AuthorizeRequestValidation.Refused("redirect_uri is not one registered for this client_id");
        }

        AuthorizeRequestValidation.ErrorRedirect Error(string error, string description) =>
            new(redirectUri, error, description, request.State);

        if (request.ResponseType != "code")
        {
            return Error("unsupported_response_type", "response_type must be code");
        }

        if (string.IsNullOrEmpty(request.CodeChallenge) || request.CodeChallengeMethod != "S256")
        {
            return Error("invalid_request", "PKCE is required with code_challenge_method S256");
        }

        var requestedScopes = request.Scope?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? [];

        if (requestedScopes.Any(scope => !McpScopes.Supported.Contains(scope)))
        {
            return Error("invalid_scope", $"Supported scopes are {string.Join(' ', McpScopes.Supported)}");
        }

        // A client that asks for no scope gets full access: the app has a single owner who approves the connection
        var grantedScopes = requestedScopes.Length == 0 ? McpScopes.Supported : requestedScopes.Distinct();

        var mcpResource = options.Value.McpResource;

        if (request.Resource is not null && request.Resource != mcpResource)
        {
            return Error("invalid_target", $"The only supported resource is {mcpResource}");
        }

        return new AuthorizeRequestValidation.Accepted(
            client,
            redirectUri,
            request.CodeChallenge,
            string.Join(' ', grantedScopes),
            mcpResource,
            request.State);
    }
}
