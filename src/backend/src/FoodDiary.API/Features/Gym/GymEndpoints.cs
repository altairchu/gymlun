using FoodDiary.API.Mcp;
using FoodDiary.API.Mcp.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FoodDiary.API.Features.Gym;

internal static class GymEndpoints
{
    public static void AddGymFeatures(this IServiceCollection services)
    {
        services.AddScoped<GymContextService>();
        services.AddScoped<BodyMeasurementService>();
        services.AddScoped<McpConnectionStore>();
    }

    public static void MapGymFeatures(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api/v1")
            .RequireAuthorization(Constants.AuthorizationPolicies.GoogleAllowedEmails)
            .AddEndpointFilter(async (context, next) =>
            {
                context.HttpContext.Response.Headers.CacheControl = "no-store";
                try
                {
                    return await next(context);
                }
                catch (GymDataException error)
                {
                    return Results.Problem(detail: error.Message, statusCode: error.Status);
                }
            });

        api.MapGet("/gym-context", ([FromServices] GymContextService service, CancellationToken token) =>
            service.Get(token));
        api.MapPut("/gym-context", ([FromBody] GymContextInput request, [FromServices] GymContextService service,
            CancellationToken token) => service.Set(request, token));
        api.MapGet("/body-composition", (DateOnly? from, DateOnly? to, [FromServices] BodyMeasurementService service,
            CancellationToken token) => service.List(from, to, token));
        api.MapPost("/body-composition", async ([FromBody] BodyMeasurementInput request,
            [FromServices] BodyMeasurementService service, CancellationToken token) =>
        {
            var created = await service.Create(request, token);
            return Results.Created($"/api/v1/body-composition/{created.Id}", created);
        });
        api.MapGet("/body-composition/{id:long}", (long id, [FromServices] BodyMeasurementService service,
            CancellationToken token) => service.Get(id, token));
        api.MapPut("/body-composition/{id:long}", (long id, [FromBody] BodyMeasurementInput request,
            [FromServices] BodyMeasurementService service, CancellationToken token) => service.Update(id, request, token));
        api.MapDelete("/body-composition/{id:long}", async (long id, [FromServices] BodyMeasurementService service,
            CancellationToken token) =>
        {
            await service.Delete(id, token);
            return Results.NoContent();
        });

        api.MapGet("/mcp-connections", async ([FromServices] McpConnectionStore store,
            [FromServices] IOptions<McpOptions> options, CancellationToken token) =>
            new
            {
                ServerUrl = options.Value.Enabled ? options.Value.McpResource : null,
                Connections = await store.List(token)
            });
        api.MapDelete("/mcp-connections/{id:guid}", async (Guid id, [FromServices] McpConnectionStore store,
            CancellationToken token) => await store.Revoke(id, token) ? Results.NoContent() : Results.NotFound());
    }
}
