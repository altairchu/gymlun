using System.Net;
using System.Net.Http.Json;
using FoodDiary.API;
using FoodDiary.API.Features.Gym;
using FoodDiary.API.Features.Products.Contracts;
using FoodDiary.ComponentTests.Infrastructure;
using FoodDiary.ComponentTests.Infrastructure.Auth;
using FoodDiary.Configuration;
using FoodDiary.Domain.Abstractions;
using FoodDiary.Domain.Entities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace FoodDiary.ComponentTests.Scenarios.Ownership;

[Collection(nameof(InfrastructureCollection))]
public class GymFeatureApiTests(InfrastructureFixture infrastructure) : IAsyncLifetime
{
    public Task InitializeAsync() => infrastructure.Database.Clear();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Goals_body_and_estimates_use_only_the_current_owners_data()
    {
        using var firstFactory = CreateFactory(7101);
        using var secondFactory = CreateFactory(7102);
        using var first = firstFactory.CreateClient();
        using var second = secondFactory.CreateClient();

        var goals = new GymContextInput(Goal: "maintain", TargetKcal: 2300, ProteinPerKg: 1.5m, AgeYears: 39,
            Sex: "m", ActivityFactor: 1.5m);
        using var saveGoals = await first.PutAsJsonAsync("/api/v1/gym-context", goals);
        saveGoals.EnsureSuccessStatusCode();
        Assert.Equal(1, (await saveGoals.Content.ReadFromJsonAsync<GymContext>())!.Version);
        using var staleGoals = await first.PutAsJsonAsync("/api/v1/gym-context", goals);
        Assert.Equal(HttpStatusCode.Conflict, staleGoals.StatusCode);

        var input = new BodyMeasurementInput(new DateOnly(2026, 1, 11), Source: "Synthetic scale",
            Metrics: new() { ["weightKg"] = 80, ["bodyFatPercent"] = 20 },
            Profile: new BodyProfile(HeightCm: 180),
            Segments: new() { ["leftArm"] = new BodySegment(FatMassKg: 0) });
        using var create = await first.PostAsJsonAsync("/api/v1/body-composition", input);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var report = (await create.Content.ReadFromJsonAsync<BodyMeasurementResult>())!;
        Assert.Equal(24.7m, report.DerivedMetrics["bmi"]);
        Assert.Equal(16m, report.DerivedMetrics["fatMassKg"]);
        Assert.Equal(64m, report.DerivedMetrics["fatFreeMassKg"]);
        Assert.Equal(0m, report.Segments["leftArm"].FatMassKg);
        Assert.False(report.Metrics.ContainsKey("fatMassKg"));

        var path = $"/api/v1/body-composition/{report.Id}";
        using var crossGet = await second.GetAsync(path);
        using var crossUpdate = await second.PutAsJsonAsync(path, input with { Version = report.Version });
        using var crossDelete = await second.DeleteAsync(path);
        Assert.Equal(HttpStatusCode.NotFound, crossGet.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, crossUpdate.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, crossDelete.StatusCode);
        Assert.Empty((await second.GetFromJsonAsync<BodyMeasurementResult[]>("/api/v1/body-composition"))!);
        Assert.Empty((await first.GetFromJsonAsync<BodyMeasurementResult[]>("/api/v1/body-composition?from=2026-01-12"))!);
        Assert.Single((await first.GetFromJsonAsync<BodyMeasurementResult[]>("/api/v1/body-composition?from=2026-01-11&to=2026-01-11"))!);

        var firstSettings = (await first.GetFromJsonAsync<GymContext>("/api/v1/gym-context"))!;
        var secondSettings = (await second.GetFromJsonAsync<GymContext>("/api/v1/gym-context"))!;
        Assert.Equal(2300, firstSettings.TargetKcal);
        Assert.Equal(80m, firstSettings.LatestWeight!.WeightKg);
        Assert.Equal("bodyComposition", firstSettings.LatestWeight.Source);
        Assert.Equal(120m, firstSettings.DailyNeeds.TargetProteinG);
        Assert.Equal(0, secondSettings.Version);
        Assert.Null(secondSettings.LatestWeight);
        Assert.Null(secondSettings.TargetKcal);

        using var update = await first.PutAsJsonAsync(path, input with { Version = report.Version, Source = "Edited" });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        using var staleUpdate = await first.PutAsJsonAsync(path, input with { Version = report.Version });
        Assert.Equal(HttpStatusCode.Conflict, staleUpdate.StatusCode);
        using var delete = await first.DeleteAsync(path);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }

    [Fact]
    public async Task Product_photos_cannot_be_cached_in_Atlas_mode()
    {
        using var firstFactory = CreateFactory(7101, atlasPhotoCache: true);
        using var secondFactory = CreateFactory(7102, atlasPhotoCache: true);
        var product = new Product
        {
            Name = "Synthetic food", CaloriesCost = 200,
            Protein = null, Fats = null, Carbs = null, Sugar = null, Salt = null,
            Photo = new ProductPhoto { Content = [1, 2, 3] },
            PhotoUpdatedAt = DateTimeOffset.UnixEpoch.AddDays(15000)
        };
        await firstFactory.SeedDataAsync([product]);
        using var first = firstFactory.CreateClient();
        using var second = secondFactory.CreateClient();
        var visible = await first.GetFromJsonAsync<GetProductByIdResponse>($"/api/v1/products/{product.Id}");
        Assert.NotNull(visible!.PhotoUrl);

        using var photo = await first.GetAsync(visible.PhotoUrl);
        Assert.Equal(HttpStatusCode.OK, photo.StatusCode);
        Assert.True(photo.Headers.CacheControl?.NoStore);
        using var other = await second.GetAsync(visible.PhotoUrl);
        Assert.Equal(HttpStatusCode.NotFound, other.StatusCode);
        Assert.True(other.Headers.CacheControl?.NoStore);
    }

    private WebApplicationFactory<Startup> CreateFactory(long owner, bool atlasPhotoCache = false)
    {
        var factory = infrastructure.Services.GetRequiredService<FoodDiaryWebApplicationFactory>();
        var auth = factory.Services.GetRequiredService<IOptions<AuthOptions>>().Value;
        return factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.AddFakeAuthForTests(auth, options =>
            {
                options.UserEmail = auth.AllowedEmails.First();
                options.ShouldAuthenticate = true;
            });
            services.RemoveAll<IFoodDiaryOwner>();
            services.AddScoped<IFoodDiaryOwner>(_ => new TestOwner(owner));
            if (atlasPhotoCache)
            {
                services.AddSingleton(Options.Create(new AtlasAppleAuthOptions { Enabled = true }));
            }
        }));
    }

    private sealed record TestOwner(long Id) : IFoodDiaryOwner;
}
