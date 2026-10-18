using System.Diagnostics;
using System.Security.Claims;
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
    IOptions<McpOptions> options)
{
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
        var email = await FindAllowedSignedInEmail(context);

        if (consent is null || email is null || consent.Access.Email != email)
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

        return RedirectWithAuthorizationCode(consent);
    }

    private async Task<IResult> AuthorizeSignedInUser(AuthorizeRequestValidation.Accepted request, HttpContext context)
    {
        var authentication = await context.AuthenticateAsync(Constants.AuthenticationSchemes.OAuthGoogle);

        if (!authentication.Succeeded)
        {
            var returnToThisRequest = new AuthenticationProperties
            {
                RedirectUri = context.Request.GetEncodedPathAndQuery()
            };

            return Results.Challenge(returnToThisRequest, [Constants.AuthenticationSchemes.OAuthGoogle]);
        }

        if (!await IsAllowed(authentication.Principal))
        {
            return RedirectToClient(request.RedirectUri, request.State,
                ("error", "access_denied"),
                ("error_description", "This account is not allowed to use Food Diary"));
        }

        var consent = new ConsentRequest(
            new AccessGrant(
                request.Client.ClientId,
                authentication.Principal.FindFirstValue(Constants.ClaimTypes.Email)!,
                request.Scope,
                request.Resource),
            request.RedirectUri,
            request.CodeChallenge,
            request.State);

        if (!request.Client.IsDynamic)
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

    private async Task<string?> FindAllowedSignedInEmail(HttpContext context)
    {
        var authentication = await context.AuthenticateAsync(Constants.AuthenticationSchemes.OAuthGoogle);

        return authentication.Succeeded && await IsAllowed(authentication.Principal)
            ? authentication.Principal.FindFirstValue(Constants.ClaimTypes.Email)
            : null;
    }

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
