using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace FoodDiary.API.Mcp.Authorization;

public static class TokenEndpointAuthMethods
{
    public const string None = "none";
    public const string ClientSecretPost = "client_secret_post";
    public const string ClientSecretBasic = "client_secret_basic";

    public static readonly string[] Supported = [ClientSecretPost, ClientSecretBasic, None];
}

// A dynamic client registered itself rather than being configured by the owner, so the owner approves it on a
// consent page before it gets an authorization code
public sealed record RegisteredClient(
    string ClientId,
    string? ClientName,
    IReadOnlyList<string> RedirectUris,
    string TokenEndpointAuthMethod,
    bool IsDynamic);

public sealed record ClientMetadata(string? ClientName, IReadOnlyList<string> RedirectUris, string TokenEndpointAuthMethod);

public sealed record DynamicClientCredentials(RegisteredClient Client, string? ClientSecret, DateTimeOffset IssuedAt);

/// <summary>
/// Dynamically registered clients are stateless: the client_id is the protected registration itself and the
/// client_secret is the protected client_id, so registration writes nothing and survives restarts with the
/// persisted data protection keys.
/// </summary>
public sealed class McpClientRegistry(
    IDataProtectionProvider dataProtectionProvider,
    TimeProvider timeProvider,
    IOptions<McpOptions> options)
{
    private const string DynamicClientIdPrefix = "dcr.";

    private readonly IDataProtector _clientIdProtector = dataProtectionProvider.CreateProtector("FoodDiary.Mcp.ClientId");
    private readonly IDataProtector _clientSecretProtector = dataProtectionProvider.CreateProtector("FoodDiary.Mcp.ClientSecret");

    public DynamicClientCredentials Register(ClientMetadata metadata)
    {
        var issuedAt = timeProvider.GetUtcNow();
        var registration = new DynamicRegistration(
            metadata.ClientName,
            metadata.RedirectUris,
            metadata.TokenEndpointAuthMethod,
            issuedAt.ToUnixTimeSeconds());

        var clientId = DynamicClientIdPrefix + _clientIdProtector.Protect(JsonSerializer.Serialize(registration));
        var client = ToClient(clientId, registration);
        var clientSecret = metadata.TokenEndpointAuthMethod == TokenEndpointAuthMethods.None
            ? null
            : _clientSecretProtector.Protect(clientId);

        return new DynamicClientCredentials(client, clientSecret, issuedAt);
    }

    public RegisteredClient? Find(string? clientId)
    {
        if (string.IsNullOrEmpty(clientId))
        {
            return null;
        }

        if (!clientId.StartsWith(DynamicClientIdPrefix, StringComparison.Ordinal))
        {
            return options.Value.FindClient(clientId) is { } configured
                ? new RegisteredClient(
                    configured.ClientId!,
                    ClientName: null,
                    [configured.RedirectUri!],
                    TokenEndpointAuthMethods.ClientSecretPost,
                    IsDynamic: false)
                : null;
        }

        var registration = Unprotect<DynamicRegistration>(_clientIdProtector, clientId[DynamicClientIdPrefix.Length..]);
        return registration is null ? null : ToClient(clientId, registration);
    }

    public bool Authenticate(RegisteredClient client, string? presentedSecret)
    {
        if (!client.IsDynamic)
        {
            return presentedSecret is not null &&
                   options.Value.FindClient(client.ClientId)?.ClientSecret is { } configuredSecret &&
                   FixedTimeEquals(presentedSecret, configuredSecret);
        }

        if (client.TokenEndpointAuthMethod == TokenEndpointAuthMethods.None)
        {
            return true;
        }

        return presentedSecret is not null &&
               Unprotect(_clientSecretProtector, presentedSecret) is { } secretClientId &&
               FixedTimeEquals(secretClientId, client.ClientId);
    }

    private static RegisteredClient ToClient(string clientId, DynamicRegistration registration) => new(
        clientId,
        registration.ClientName,
        registration.RedirectUris,
        registration.TokenEndpointAuthMethod,
        IsDynamic: true);

    private static bool FixedTimeEquals(string left, string right) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(left), Encoding.UTF8.GetBytes(right));

    private static string? Unprotect(IDataProtector protector, string protectedValue)
    {
        try
        {
            return protector.Unprotect(protectedValue);
        }
        catch (Exception exception) when (exception is CryptographicException or FormatException)
        {
            return null;
        }
    }

    private static TPayload? Unprotect<TPayload>(IDataProtector protector, string protectedPayload) =>
        Unprotect(protector, protectedPayload) is { } json ? JsonSerializer.Deserialize<TPayload>(json) : default;

    private sealed record DynamicRegistration(
        string? ClientName,
        IReadOnlyList<string> RedirectUris,
        string TokenEndpointAuthMethod,
        long IssuedAt);
}
