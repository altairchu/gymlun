using FoodDiary.API.Features.Workouts.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.API.Features.Workouts;

public static class Endpoints
{
    public static void MapWorkoutSets(this IEndpointRouteBuilder app)
    {
        var workoutSets = app.MapGroup("/api/v1/workout-sets")
            .RequireAuthorization(Constants.AuthorizationPolicies.GoogleAllowedEmails);

        workoutSets.MapGet("/", (
            DateOnly? from,
            DateOnly? to,
            [FromServices] GetWorkoutSetsHandler handler,
            CancellationToken cancellationToken) => handler.Handle(from, to, cancellationToken));

        workoutSets.MapGet("/exercises", (
            [FromServices] GetExercisesHandler handler,
            CancellationToken cancellationToken) => handler.Handle(cancellationToken));

        workoutSets.MapPost("/", (
            [FromBody] LogWorkoutSetsRequest request,
            [FromServices] LogWorkoutSetsHandler handler,
            CancellationToken cancellationToken) => handler.Handle(request, cancellationToken));

        workoutSets.MapPut("/{id:int}", (
            [FromRoute] int id,
            [FromBody] UpdateWorkoutSetRequest request,
            [FromServices] UpdateWorkoutSetHandler handler,
            CancellationToken cancellationToken) => handler.Handle(id, request, cancellationToken));

        workoutSets.MapDelete("/{id:int}", (
            [FromRoute] int id,
            [FromServices] DeleteWorkoutSetHandler handler,
            CancellationToken cancellationToken) => handler.Handle(id, cancellationToken));
    }

    public static void AddWorkoutSets(this IServiceCollection services)
    {
        services.AddScoped<ExerciseNames>();
        services.AddScoped<ExerciseHistory>();
        services.AddScoped<GetWorkoutSetsHandler>();
        services.AddScoped<GetExercisesHandler>();
        services.AddScoped<LogWorkoutSetsHandler>();
        services.AddScoped<UpdateWorkoutSetHandler>();
        services.AddScoped<DeleteWorkoutSetHandler>();
    }
}
