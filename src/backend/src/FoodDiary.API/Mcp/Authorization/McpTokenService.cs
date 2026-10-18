using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace FoodDiary.API.Mcp.Authorization;

public sealed record AccessGrant(string ClientId, string Email, string Scope, string Resource);

public sealed record AuthorizationCodeGrant(AccessGrant Access, string RedirectUri, string CodeChallenge);

public sealed record ConsentRequest(AccessGrant Access, string RedirectUri, string CodeChallenge, string? State);

public sealed record McpTokens(string AccessToken, string RefreshToken);

public sealed class McpTokenService(
    IDataProtectionProvider dataProtectionProvider,
    IMemoryCache memoryCache,
    TimeProvider timeProvider,
    IOptions<McpOptions> options)
{
    private static readonly TimeSpan AuthorizationCodeLifetime = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan ConsentTicketLifetime = TimeSpan.FromMinutes(10);

    private readonly IDataProtector _authorizationCodeProtector = dataProtectionProvider.CreateProtector("FoodDiary.Mcp.AuthorizationCode");
    private readonly IDataProtector _accessTokenProtector = dataProtectionProvider.CreateProtector("FoodDiary.Mcp.AccessToken");
    private readonly IDataProtector _refreshTokenProtector = dataProtectionProvider.CreateProtector("FoodDiary.Mcp.RefreshToken");
    private readonly IDataProtector _consentTicketProtector = dataProtectionProvider.CreateProtector("FoodDiary.Mcp.ConsentTicket");
    private readonly McpOptions _options = options.Value;
    private readonly Lock _authorizationCodeRedemptionLock = new();

    public string IssueAuthorizationCode(AuthorizationCodeGrant grant)
    {
        var codeId = Guid.NewGuid().ToString("N");
        var payload = new AuthorizationCodePayload(codeId, grant, timeProvider.GetUtcNow() + AuthorizationCodeLifetime);

        lock (_authorizationCodeRedemptionLock)
        {
            memoryCache.Set(UnredeemedCodeKey(codeId), true, AuthorizationCodeLifetime);
        }

        return Protect(_authorizationCodeProtector, payload);
    }

    public AuthorizationCodeGrant? RedeemAuthorizationCode(string code)
    {
        var payload = Unprotect<AuthorizationCodePayload>(_authorizationCodeProtector, code);

        if (payload is null || IsExpired(payload.ExpiresAt))
        {
            return null;
        }

        lock (_authorizationCodeRedemptionLock)
        {
            if (!memoryCache.TryGetValue(UnredeemedCodeKey(payload.CodeId), out _))
            {
                return null;
            }

            memoryCache.Remove(UnredeemedCodeKey(payload.CodeId));
        }

        return payload.Grant;
    }

    public string IssueConsentTicket(ConsentRequest consent) =>
        Protect(_consentTicketProtector, new ConsentTicketPayload(consent, timeProvider.GetUtcNow() + ConsentTicketLifetime));

    public ConsentRequest? RedeemConsentTicket(string consentTicket)
    {
        var payload = Unprotect<ConsentTicketPayload>(_consentTicketProtector, consentTicket);
        return payload is null || IsExpired(payload.ExpiresAt) ? null : payload.Consent;
    }

    public McpTokens IssueTokens(AccessGrant grant) => new(
        IssueAccessToken(grant),
        Protect(_refreshTokenProtector, new TokenPayload(grant, timeProvider.GetUtcNow() + _options.RefreshTokenLifetime)));

    public string IssueAccessToken(AccessGrant grant) =>
        Protect(_accessTokenProtector, new TokenPayload(grant, timeProvider.GetUtcNow() + _options.AccessTokenLifetime));

    public AccessGrant? ValidateAccessToken(string accessToken)
    {
        var payload = Unprotect<TokenPayload>(_accessTokenProtector, accessToken);

        if (payload is null || IsExpired(payload.ExpiresAt) || payload.Grant.Resource != _options.McpResource)
        {
            return null;
        }

        return payload.Grant;
    }

    public AccessGrant? ValidateRefreshToken(string refreshToken)
    {
        var payload = Unprotect<TokenPayload>(_refreshTokenProtector, refreshToken);
        return payload is null || IsExpired(payload.ExpiresAt) ? null : payload.Grant;
    }

    private bool IsExpired(DateTimeOffset expiresAt) => timeProvider.GetUtcNow() >= expiresAt;

    private static string UnredeemedCodeKey(string codeId) => $"mcp:unredeemed-authorization-code:{codeId}";

    private static string Protect<TPayload>(IDataProtector protector, TPayload payload) =>
        protector.Protect(JsonSerializer.Serialize(payload));

    private static TPayload? Unprotect<TPayload>(IDataProtector protector, string protectedPayload)
    {
        try
        {
            return JsonSerializer.Deserialize<TPayload>(protector.Unprotect(protectedPayload));
        }
        catch (Exception exception) when (exception is CryptographicException or FormatException)
        {
            return default;
        }
    }

    private sealed record AuthorizationCodePayload(string CodeId, AuthorizationCodeGrant Grant, DateTimeOffset ExpiresAt);

    private sealed record TokenPayload(AccessGrant Grant, DateTimeOffset ExpiresAt);

    private sealed record ConsentTicketPayload(ConsentRequest Consent, DateTimeOffset ExpiresAt);
}
