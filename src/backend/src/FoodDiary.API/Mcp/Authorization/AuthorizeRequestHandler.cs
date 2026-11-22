using System.Diagnostics;
using System.Security.Claims;
using FoodDiary.API.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace FoodDiary.API.Mcp.Authorization;

internal sealed class AuthorizeRequestHandler(
    AuthorizeRequestValidator validator,
    IAuthorizationService authorizationService,
    McpTokenService tokenService,
    IOptions<McpOptions> options,
    IConfiguration configuration,
    McpConnectionStore connections,
    McpClientRegistry clients)
{
    private bool UsesAtlas => configuration.GetValue<bool>("AtlasApple:Enabled");
    private string BrowserScheme => UsesAtlas
        ? Constants.AuthenticationSchemes.Cookie
        : Constants.AuthenticationSchemes.OAuthGoogle;

    public async Task<IResult> Handle(AuthorizeRequest request, HttpContext context) =>
        validator.Validate(request) switch
        {
            AuthorizeRequestValidation.Refused refused =>
                Results.Text(refused.Reason, statusCode: StatusCodes.Status400BadRequest),

            AuthorizeRequestValidation.ErrorRedirect error =>
                RedirectToClient(error.RedirectUri, error.State,
                    ("error", error.Error),
                    ("error_description", error.Description)),

            AuthorizeRequestValidation.Accepted accepted =>
                await AuthorizeSignedInUser(accepted, context),

            _ => throw new UnreachableException()
        };

    public async Task<IResult> HandleConsent(HttpContext context)
    {
        if (!context.Request.HasFormContentType)
        {
            return Results.Text("Expected a consent form", statusCode: StatusCodes.Status400BadRequest);
        }

        var form = await context.Request.ReadFormAsync(context.RequestAborted);
        var consent = tokenService.RedeemConsentTicket(form["consent_ticket"].ToString());
        var signedIn = await context.AuthenticateAsync(BrowserScheme);

        if (consent is null || !signedIn.Succeeded || !await IsAllowed(signedIn.Principal) ||
            !MatchesConsent(signedIn.Principal, consent.Access))
        {
            return Results.Text(
                "This approval has expired. Connect again from your AI app.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (form["decision"].ToString() != "allow")
        {
            return RedirectToClient(consent.RedirectUri, consent.State,
                ("error", "access_denied"),
                ("error_description", "The owner did not allow this connection"));
        }

        if (UsesAtlas)
        {
            var client = clients.Find(consent.Access.ClientId);
            if (client is null)
            {
                return Results.BadRequest("The client registration is no longer available.");
            }

            var access = await connections.Create(consent.Access, client.ClientName, context.RequestAborted);
            consent = consent with { Access = access };
        }

        return RedirectWithAuthorizationCode(consent);
    }

    private async Task<IResult> AuthorizeSignedInUser(AuthorizeRequestValidation.Accepted request, HttpContext context)
    {
        var authentication = await context.AuthenticateAsync(BrowserScheme);

        if (!authentication.Succeeded)
        {
            var returnToThisRequest = new AuthenticationProperties
            {
                RedirectUri = context.Request.GetEncodedPathAndQuery()
            };

            return Results.Challenge(returnToThisRequest, [UsesAtlas
                ? Constants.AuthenticationSchemes.AtlasApple
                : Constants.AuthenticationSchemes.OAuthGoogle]);
        }

        if (!await IsAllowed(authentication.Principal))
        {
            return RedirectToClient(request.RedirectUri, request.State,
                ("error", "access_denied"),
                ("error_description", "This account is not allowed to use Food Diary"));
        }

        var principal = authentication.Principal;
        long? owner = UsesAtlas ? AtlasAppleAuthentication.GetOwnerId(principal) : null;
        long? epoch = UsesAtlas && AtlasAppleAuthentication.TryGetClaim(
            principal, AtlasAppleAuthentication.AuthEpochClaim, out var parsedEpoch) ? parsedEpoch : null;
        if (UsesAtlas && (owner is null or <= 0 || epoch is null))
        {
            return Results.Forbid();
        }

        var consent = new ConsentRequest(
            new AccessGrant(
                request.Client.ClientId,
                principal.FindFirstValue(Constants.ClaimTypes.Email) ?? string.Empty,
                UsesAtlas ? string.Join(' ', McpScopes.Supported) : request.Scope,
                request.Resource,
                owner,
                epoch),
            request.RedirectUri,
            request.CodeChallenge,
            request.State);

        if (!UsesAtlas && !request.Client.IsDynamic)
        {
            return RedirectWithAuthorizationCode(consent);
        }

        context.Response.Headers.CacheControl = "no-store";
        context.Response.Headers.ContentSecurityPolicy = "default-src 'none'; style-src 'unsafe-inline'; frame-ancestors 'none'";
        context.Response.Headers.XFrameOptions = "DENY";

        return Results.Content(
            ConsentPage.Render(request.Client, consent, tokenService.IssueConsentTicket(consent)),
            "text/html; charset=utf-8");
    }

    private bool MatchesConsent(ClaimsPrincipal principal, AccessGrant access) => UsesAtlas
        ? access.OwnerId is > 0 && access.OwnerId == AtlasAppleAuthentication.GetOwnerId(principal) &&
          AtlasAppleAuthentication.TryGetClaim(principal, AtlasAppleAuthentication.AuthEpochClaim, out var epoch) &&
          access.AtlasAuthEpoch == epoch
        : access.OwnerId is null && access.Email == principal.FindFirstValue(Constants.ClaimTypes.Email);

    private async Task<bool> IsAllowed(ClaimsPrincipal principal)
    {
        var emailAllowlistCheck = await authorizationService.AuthorizeAsync(
            principal,
            Constants.AuthorizationPolicies.GoogleAllowedEmails);

        return emailAllowlistCheck.Succeeded;
    }

    private IResult RedirectWithAuthorizationCode(ConsentRequest consent)
    {
        var code = tokenService.IssueAuthorizationCode(
            new AuthorizationCodeGrant(consent.Access, consent.RedirectUri, consent.CodeChallenge));

        return RedirectToClient(consent.RedirectUri, consent.State, ("code", code));
    }

    private IResult RedirectToClient(string redirectUri, string? state, params (string Name, string Value)[] parameters)
    {
        var query = parameters
            .Select(parameter => KeyValuePair.Create(parameter.Name, (string?)parameter.Value))
            .Append(KeyValuePair.Create("state", state))
            .Append(KeyValuePair.Create("iss", options.Value.BaseUrl));

        return Results.Redirect(QueryHelpers.AddQueryString(redirectUri, query));
    }
}
