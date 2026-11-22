using System.Globalization;
using FoodDiary.API.Authentication;
using FoodDiary.Application.Auth.GetStatus;
using FoodDiary.Configuration;
using FoodDiary.Contracts.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FoodDiary.API.Controllers.v1;

[ApiController]
[Route("api/v1/auth")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class AuthController(IOptions<AtlasAppleAuthOptions> atlasOptions) : ControllerBase
{
    [HttpGet("login")]
    public IActionResult Login([FromQuery] string? returnUrl)
    {
        var redirectUrl = Url.Action(nameof(LoginCallback), "Auth", new { returnUrl = SafeReturnUrl(returnUrl) });
        var atlas = atlasOptions.Value.Enabled;
        var properties = new AuthenticationProperties
        {
            RedirectUri = redirectUrl,
            AllowRefresh = !atlas,
        };

        return Challenge(properties, atlas ? Constants.AuthenticationSchemes.AtlasApple : Constants.AuthenticationSchemes.OAuthGoogle);
    }

    [HttpGet("login-callback")]
    public IActionResult LoginCallback(string? returnUrl = null)
    {
        return Redirect($"/#/post-login?returnUrl={Uri.EscapeDataString(SafeReturnUrl(returnUrl))}");
    }

    [HttpGet("logout")]
    [Authorize]
    public IActionResult Logout() => SignOut(Constants.AuthenticationSchemes.Cookie);

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(
        [FromServices] GetAuthStatusQueryHandler handler,
        CancellationToken cancellationToken)
    {
        if (atlasOptions.Value.Enabled)
        {
            var owner = AtlasAppleAuthentication.GetOwnerId(User);
            return Ok(new GetAuthStatusResponse
            {
                IsAuthenticated = owner > 0,
                Provider = "atlasApple",
                AccountId = owner > 0 ? owner.ToString(CultureInfo.InvariantCulture) : null,
            });
        }

        var authResult = await HttpContext.AuthenticateAsync(Constants.AuthenticationSchemes.OAuthGoogle);
        var result = await handler.Handle(new GetAuthStatusQuery(authResult), cancellationToken);
        return Ok(new GetAuthStatusResponse { IsAuthenticated = result is GetAuthStatusResult.Authenticated });
    }

    private string SafeReturnUrl(string? returnUrl) => Url.IsLocalUrl(returnUrl) ? returnUrl! : "/";
}
