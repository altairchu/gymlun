using System.Globalization;

namespace FoodDiary.API.Authentication;

internal static class AtlasAppleAccountContext
{
    private const string ExpectedAccountHeader = "X-Gym-Expected-Account";

    public static async Task ValidateAsync(HttpContext context, RequestDelegate next)
    {
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            await next(context);
            return;
        }

        context.Response.OnStarting(() =>
        {
            context.Response.Headers.CacheControl = "private, no-store";
            return Task.CompletedTask;
        });

        if (context.Request.Path.StartsWithSegments("/api/v1/auth"))
        {
            await next(context);
            return;
        }

        var owner = AtlasAppleAuthentication.GetOwnerId(context.User);
        if (owner > 0 && context.Request.Headers.TryGetValue(ExpectedAccountHeader, out var expected) &&
            (expected.Count != 1 || !string.Equals(expected[0], owner.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)))
        {
            context.Response.Headers["X-Gym-Account-Changed"] = "true";
            await Results.Conflict(new
            {
                code = "account_context_changed",
                error = "Refresh to use the current AtlasApple account.",
            }).ExecuteAsync(context);
            return;
        }

        await next(context);
    }
}
