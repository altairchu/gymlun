using FoodDiary.API.Features.ProductPhotos.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.API.Features.ProductPhotos;

public static class Endpoints
{
    public static void MapProductPhotos(this IEndpointRouteBuilder app)
    {
        var photo = app.MapGroup("/api/v1/products/{productId:int}/photo")
            .RequireAuthorization(Constants.AuthorizationPolicies.GoogleAllowedEmails);

        photo.MapGet("/", async (
            int productId,
            string? v,
            HttpResponse response,
            [FromServices] GetProductPhotoHandler handler,
            CancellationToken cancellationToken) =>
        {
            var photoFile = await handler.Handle(productId, cancellationToken);

            if (photoFile is null)
            {
                return Results.NotFound();
            }

            // Photo URLs carry the version they were handed out with, so a matching URL never changes content
            response.Headers.CacheControl = v == photoFile.Version
                ? "private, max-age=31536000, immutable"
                : "private, no-cache";

            return Results.Bytes(photoFile.Content, "image/jpeg");
        });

        photo.MapPut("/", async (
            int productId,
            IFormFile photo,
            [FromServices] SetProductPhotoHandler handler,
            CancellationToken cancellationToken) =>
        {
            if (photo.Length > ProductPhotoProcessor.MaxUploadBytes)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Failed to set product photo",
                    detail: $"Photo must be no larger than {ProductPhotoProcessor.MaxUploadBytes / 1024 / 1024} MB");
            }

            await using var stream = photo.OpenReadStream();
            var result = await handler.Handle(productId, stream, cancellationToken);

            return result switch
            {
                SetProductPhotoResult.Success success => Results.Ok(new SetProductPhotoResponse(success.PhotoUrl)),
                SetProductPhotoResult.ProductNotFound => Results.NotFound(),
                SetProductPhotoResult.NotAnImage => Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Failed to set product photo",
                    detail: "File is not a supported image"),
                _ => throw new InvalidOperationException($"Unexpected result: {result}")
            };
        })
        // Cookie-authenticated PUT is out of reach of cross-site forms
        .DisableAntiforgery();

        photo.MapDelete("/", async (
            int productId,
            [FromServices] DeleteProductPhotoHandler handler,
            CancellationToken cancellationToken) => await handler.Handle(productId, cancellationToken)
            ? Results.NoContent()
            : Results.NotFound());
    }

    public static void AddProductPhotos(this IServiceCollection services)
    {
        services.AddSingleton<ProductPhotoProcessor>();
        services.AddScoped<GetProductPhotoHandler>();
        services.AddScoped<SetProductPhotoHandler>();
        services.AddScoped<DeleteProductPhotoHandler>();
    }
}
