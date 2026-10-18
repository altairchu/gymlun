using System.ComponentModel.DataAnnotations;
using FoodDiary.API.Features.WeightTracking.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.API.Features.WeightTracking;

public static class Endpoints
{
    public static void MapWeightLogs(this IEndpointRouteBuilder app)
    {
        var weightLogs = app.MapGroup("/api/weight-logs")
            .RequireAuthorization(Constants.AuthorizationPolicies.GoogleAllowedEmails);

        weightLogs.MapGet("/", (
            [Required] DateOnly? from,
            [Required] DateOnly? to,
            [FromServices] GetWeightLogsHandler handler,
            CancellationToken cancellationToken) => handler.Handle(
            new GetWeightLogsRequest(from.GetValueOrDefault(), to.GetValueOrDefault()),
            cancellationToken));

        weightLogs.MapPost("/", (
            [FromBody] WeightLogBody request,
            [FromServices] AddWeightLogHandler handler,
            CancellationToken cancellationToken) => handler.Handle(request, cancellationToken));

        weightLogs.MapPut("/{date}", (
            [FromRoute] DateOnly date,
            [FromBody] UpdateWeightLogBody request,
            [FromServices] UpdateWeightLogHandler handler,
            CancellationToken cancellationToken) => handler.Handle(date, request, cancellationToken));

        weightLogs.MapDelete("/{date}", (
            [FromRoute] DateOnly date,
            [FromServices] DeleteWeightLogHandler handler,
            CancellationToken cancellationToken) => handler.Handle(date, cancellationToken));
    }

    public static void AddWeightLogs(this IServiceCollection services)
    {
        services.AddScoped<GetWeightLogsHandler>();
        services.AddScoped<AddWeightLogHandler>();
        services.AddScoped<UpdateWeightLogHandler>();
        services.AddScoped<DeleteWeightLogHandler>();
    }
}