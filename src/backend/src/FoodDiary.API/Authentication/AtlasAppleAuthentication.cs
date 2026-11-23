using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using FoodDiary.Configuration;
using FoodDiary.Domain.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.Extensions.Options;

namespace FoodDiary.API.Authentication;

public static class AtlasAppleAuthentication
{
    public const string OwnerClaim = "gym_owner_id";
    public const string AuthEpochClaim = "atlas_auth_epoch";
    private const string ExpiresAtField = "atlas_expires_at";
    public const string EndpointPrefix = "/api/gym-lun/sso";
    public const string CallbackPath = "/signin-atlasapple";

    public static void ValidateConfiguration(AtlasAppleAuthOptions options)
    {
        if (!options.Enabled)
        {
            return;
        }

        if (!Uri.TryCreate(options.Authority, UriKind.Absolute, out var authority) ||
            authority.Scheme != Uri.UriSchemeHttps || authority.AbsolutePath != "/" ||
            authority.UserInfo.Length != 0 || authority.Query.Length != 0 || authority.Fragment.Length != 0)
        {
            throw new InvalidOperationException("AtlasApple:Authority must be an HTTPS origin.");
        }

        if (string.IsNullOrWhiteSpace(options.ClientId) || options.ClientId.Length > 128)
        {
            throw new InvalidOperationException("AtlasApple:ClientId must identify the registered GymLun client.");
        }

        if (options.ClientSecret.Length < 32 || options.ClientSecret.Any(character => !char.IsAscii(character)))
        {
            throw new InvalidOperationException("AtlasApple:ClientSecret must be an ASCII secret of at least 32 characters.");
        }
    }

    public static void AddAtlasApple(this AuthenticationBuilder builder, AtlasAppleAuthOptions settings)
    {
        builder.AddOAuth(Constants.AuthenticationSchemes.AtlasApple, options =>
        {
            options.SignInScheme = Constants.AuthenticationSchemes.Cookie;
            options.ClientId = settings.ClientId;
            options.ClientSecret = settings.ClientSecret;
            options.AuthorizationEndpoint = Endpoint(settings, "authorize");
            options.TokenEndpoint = Endpoint(settings, "token");
            options.CallbackPath = CallbackPath;
            options.UsePkce = true;
            options.SaveTokens = false;
            options.Scope.Add("gymlun.identity");
            options.BackchannelTimeout = TimeSpan.FromSeconds(8);
            options.BackchannelHttpHandler = CreateBackchannelHandler();
            options.Events.OnCreatingTicket = CreateTicketAsync;
            options.Events.OnTicketReceived = context =>
            {
                // Remove food photos cached by the old single-account site before opening this diary.
                context.Response.Headers["Clear-Site-Data"] = "\"cache\"";
                context.Response.Headers.CacheControl = "no-store";
                return Task.CompletedTask;
            };
            options.Events.OnRemoteFailure = context =>
            {
                context.HandleResponse();
                context.Response.Headers.CacheControl = "no-store";
                context.Response.Redirect("/#/login?atlasLoginFailed=true");
                return Task.CompletedTask;
            };
        });
    }

    public static HttpClientHandler CreateBackchannelHandler() => new()
    {
        AllowAutoRedirect = false,
        UseCookies = false,
    };

    public static string Endpoint(AtlasAppleAuthOptions options, string name) =>
        new Uri(new Uri(options.Authority), $"{EndpointPrefix}/{name}").AbsoluteUri;

    public static long GetOwnerId(ClaimsPrincipal? principal) =>
        principal?.Identity?.IsAuthenticated == true &&
        TryGetClaim(principal, OwnerClaim, out var id) && id > 0
            ? id
            : -1;

    public static bool TryGetClaim(ClaimsPrincipal principal, string name, out long value) =>
        long.TryParse(principal.FindFirstValue(name), NumberStyles.None, CultureInfo.InvariantCulture, out value);

    private static Task CreateTicketAsync(OAuthCreatingTicketContext context)
    {
        var claims = context.Identity;
        var response = context.TokenResponse.Response?.RootElement;
        var now = (context.Options.TimeProvider ?? TimeProvider.System).GetUtcNow();
        if (claims is null || response is not { ValueKind: JsonValueKind.Object } payload ||
            !TryGetNumber(payload, OwnerClaim, out var ownerId) || ownerId <= 0 ||
            !TryGetNumber(payload, AuthEpochClaim, out var authEpoch) || authEpoch < 0 ||
            !TryGetNumber(payload, ExpiresAtField, out var expiresAt) || expiresAt <= now.ToUnixTimeSeconds())
        {
            throw new AuthenticationFailureException("AtlasApple returned an invalid identity.");
        }

        var owner = ownerId.ToString(CultureInfo.InvariantCulture);
        claims.AddClaim(new Claim(OwnerClaim, owner, ClaimValueTypes.Integer64));
        claims.AddClaim(new Claim(ClaimTypes.NameIdentifier, owner));
        claims.AddClaim(new Claim(AuthEpochClaim, authEpoch.ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer64));
        context.Properties.AllowRefresh = false;
        context.Properties.ExpiresUtc = DateTimeOffset.FromUnixTimeSeconds(
            Math.Min(expiresAt, (now + Constants.AuthenticationParameters.CookieLifetime).ToUnixTimeSeconds()));
        return Task.CompletedTask;
    }

    private static bool TryGetNumber(JsonElement payload, string name, out long value)
    {
        value = default;
        return payload.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.Number &&
            property.TryGetInt64(out value);
    }
}

public sealed class AuthenticatedDiaryOwner(
    IHttpContextAccessor httpContext,
    IOptions<AtlasAppleAuthOptions> settings) : IFoodDiaryOwner
{
    public long Id => settings.Value.Enabled
        ? AtlasAppleAuthentication.GetOwnerId(httpContext.HttpContext?.User)
        : 0;
}
