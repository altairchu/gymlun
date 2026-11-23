using System.Net.Http.Headers;
using System.Globalization;
using System.Security.Claims;
using System.Text.Encodings.Web;
using FoodDiary.API.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace FoodDiary.API.Mcp.Authorization;

internal sealed class McpAccessTokenHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> schemeOptions,
    ILoggerFactory logger,
    UrlEncoder encoder,
    McpTokenService tokenService,
    IOptions<McpOptions> mcpOptions,
    McpGrantValidator validator) : AuthenticationHandler<AuthenticationSchemeOptions>(schemeOptions, logger, encoder)
{
    public const string SchemeName = "Bearer";
    public const string ContextAccessClaim = "gym_mcp_access_id";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!AuthenticationHeaderValue.TryParse(Request.Headers.Authorization, out var authorization) ||
            !authorization.Scheme.Equals(SchemeName, StringComparison.OrdinalIgnoreCase) ||
            authorization.Parameter is null)
        {
            return AuthenticateResult.NoResult();
        }

        var grant = tokenService.ValidateAccessToken(authorization.Parameter);

        if (grant is null || !await validator.CanUse(grant, Context.RequestAborted))
        {
            return AuthenticateResult.Fail("Access token is invalid, expired or revoked");
        }

        List<Claim> claims =
        [
            new(Constants.ClaimTypes.Email, grant.Email),
            ..grant.Scope
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(scope => new Claim(McpScopes.ClaimType, scope))
        ];

        if (validator.UsesAtlas)
        {
            if (!grant.AccessId.HasValue) return AuthenticateResult.Fail("Access token has no context identifier");
            claims.Add(new Claim(AtlasAppleAuthentication.OwnerClaim, grant.OwnerId!.Value.ToString(CultureInfo.InvariantCulture)));
            claims.Add(new Claim(AtlasAppleAuthentication.AuthEpochClaim, grant.AtlasAuthEpoch!.Value.ToString(CultureInfo.InvariantCulture)));
            claims.Add(new Claim(ContextAccessClaim, grant.AccessId.Value.ToString("N")));
        }

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.Headers.Append(
            HeaderNames.WWWAuthenticate,
            $"Bearer resource_metadata=\"{mcpOptions.Value.ResourceMetadataUrl}\", scope=\"{string.Join(' ', McpScopes.Supported)}\"");

        return Task.CompletedTask;
    }
}
