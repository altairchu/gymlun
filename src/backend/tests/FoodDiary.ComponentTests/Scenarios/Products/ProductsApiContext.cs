using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FoodDiary.API.Dtos;
using FoodDiary.API.Features.Products.Contracts;
using FoodDiary.API.Features.Products.Extensions;
using FoodDiary.API.Mapping;
using FoodDiary.API.Requests;
using FoodDiary.Application.Products.SuggestNutrition;
using FoodDiary.ComponentTests.Dsl;
using FoodDiary.ComponentTests.Infrastructure;
using FoodDiary.ComponentTests.Infrastructure.DateAndTime;
using FoodDiary.ComponentTests.Infrastructure.ExternalServices;
using FoodDiary.Contracts.Products;
using FoodDiary.Domain.Entities;
using JetBrains.Annotations;

namespace FoodDiary.ComponentTests.Scenarios.Products;

[UsedImplicitly]
public class ProductsApiContext(
    FoodDiaryWebApplicationFactory factory,
    ExternalServicesFixture externalServices) : BaseContext(factory)
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private OpenAIApi OpenAiApi => externalServices.OpenAiApi;
    private HttpResponseMessage _suggestNutritionResponse = null!;

    private ProductsSearchResultDto? _productsResponse;
    private SearchProductsResponse.Product[]? _productsForAutocompleteResponse;
    private HttpResponseMessage _getProductByIdResponse = null!;
    private HttpResponseMessage _createProductHttpResponse = null!;
    private CreateProductResponse? _createProductResponse;
    private HttpResponseMessage _deleteCategoryResponse = null!;
    private HttpResponseMessage _updateProductResponse = null!;
    private HttpResponseMessage _deleteProductResponse = null!;
    private HttpResponseMessage _deleteMultipleProductsResponse = null!;

    private readonly ProductsCatalog _products = new();
    
    public Task Given_products(params Product[] products)
    {
        return Factory.SeedDataAsync(products);
    }
    
    public async Task Given_product(string product)
    {
        await Factory.SeedDataAsync([_products.TryGetOrAdd(product, out _)]);
    }

    private async Task Given_product_logged_on(string productName, DateOnly loggedOn)
    {
        var product = _products.TryGetOrAdd(productName, out var isNewProduct);

        if (isNewProduct)
        {
            await Factory.SeedDataAsync([product]);
        }
        
        await Factory.SeedDataAsync([
            Create.Note()
                .WithDate(loggedOn)
                .WithExistingProduct(product)
                .Please()
        ]);
    }

    public Task Given_product_logged_yesterday(string product) =>
        Given_product_logged_on(product, FakeDateTimeProvider.Yesterday());

    public Task Given_product_logged_today(string product) =>
        Given_product_logged_on(product, FakeDateTimeProvider.Today());
    
    public Task Given_categories(params Domain.Entities.Category[] categories)
    {
        return Factory.SeedDataAsync(categories);
    }

    public Task Given_OpenAI_api_is_ready()
    {
        return OpenAiApi.Start();
    }

    public Task Given_OpenAI_api_can_suggest_nutrition(SuggestedNutrition nutrition)
    {
        var content = JsonSerializer.Serialize(nutrition, SerializerOptions);
        return OpenAiApi.SetupCompletionSuccess(content);
    }

    public async Task When_user_retrieves_products_list()
    {
        _productsResponse = await ApiClient.GetFromJsonAsync<ProductsSearchResultDto>("/api/v1/products");
    }

    public async Task When_user_searches_products_by_name(string name)
    {
        _productsResponse = await ApiClient
            .GetFromJsonAsync<ProductsSearchResultDto>($"/api/v1/products?productSearchName={name}");
    }

    public async Task When_user_filters_products_by_category(Domain.Entities.Category category)
    {
        _productsResponse = await ApiClient
            .GetFromJsonAsync<ProductsSearchResultDto>($"/api/v1/products?categoryId={category.Id}");
    }
    
    public async Task When_user_searches_products_for_autocomplete()
    {
        _productsForAutocompleteResponse = await ApiClient
            .GetFromJsonAsync<SearchProductsResponse.Product[]>("api/v1/products/autocomplete");
    }

    public async Task When_user_creates_product(Product product)
    {
        var request = Create.ProductCreateEditRequest()
            .From(product)
            .Please();
        
        await PostProduct(request);
    }

    public async Task When_user_creates_product_with_categories(Product product, params int[] categoryIds)
    {
        var request = Create.ProductCreateEditRequest()
            .From(product)
            .WithCategoryIds(categoryIds)
            .Please();

        await PostProduct(request);
    }

    public Task When_old_client_creates_product_with_single_category(Product product, Domain.Entities.Category category)
    {
        return PostProduct(new
        {
            name = product.Name,
            caloriesCost = product.CaloriesCost,
            defaultQuantity = product.DefaultQuantity,
            categoryId = category.Id,
            protein = product.Protein,
            fats = product.Fats,
            carbs = product.Carbs,
            sugar = product.Sugar,
            salt = product.Salt
        });
    }

    private async Task PostProduct<TRequest>(TRequest request)
    {
        _createProductHttpResponse = await ApiClient.PostAsJsonAsync("/api/v1/products", request);

        _createProductResponse = _createProductHttpResponse.IsSuccessStatusCode
            ? await _createProductHttpResponse.Content.ReadFromJsonAsync<CreateProductResponse>()
            : null;
    }

    public async Task When_user_renames_product(Product product, string newName)
    {
        var request = Create.ProductCreateEditRequest()
            .From(product)
            .WithName(newName)
            .Please();
        
        _updateProductResponse = await ApiClient.PutAsJsonAsync($"/api/v1/products/{product.Id}", request);
    }

    public async Task When_user_changes_product_categories(Product product, params Domain.Entities.Category[] categories)
    {
        var request = Create.ProductCreateEditRequest()
            .From(product)
            .WithCategoryIds(categories.Select(c => c.Id).ToArray())
            .Please();

        _updateProductResponse = await ApiClient.PutAsJsonAsync($"/api/v1/products/{product.Id}", request);
    }

    public async Task When_user_deletes_category(Domain.Entities.Category category)
    {
        _deleteCategoryResponse = await ApiClient.DeleteAsync($"/api/v1/categories/{category.Id}");
    }

    public async Task When_user_deletes_product(Product product)
    {
        _deleteProductResponse = await ApiClient.DeleteAsync($"/api/v1/products/{product.Id}");
    }
    
    public async Task When_user_deletes_products(params Product[] products)
    {
        var productIds = products.Select(p => p.Id);

        var request = new HttpRequestMessage(HttpMethod.Delete, "api/v1/products/batch")
        {
            Content = new StringContent(JsonSerializer.Serialize(productIds), Encoding.Unicode, "application/json")
        };
        
        _deleteMultipleProductsResponse = await ApiClient.SendAsync(request);
    }
    
    public async Task When_user_retrieves_product_by_id(int id)
    {
        _getProductByIdResponse = await ApiClient.GetAsync($"/api/v1/products/{id}");
    }
    
    public Task When_user_retrieves_created_product_by_id()
        => When_user_retrieves_product_by_id(_createProductResponse?.Id ?? 0);

    public async Task When_user_requests_nutrition_suggestion(string name)
    {
        _suggestNutritionResponse = await ApiClient.PostAsJsonAsync(
            "/api/v1/products/nutrition/suggestions",
            new { name });
    }

    public Task Then_products_list_contains_items(params Product[] items)
    {
        var expectedProductsList = items.Select(p => p.ToProductItemDto());
        
        _productsResponse?.ProductItems.Should()
            .BeEquivalentTo(expectedProductsList, options => options
                .Excluding(p => p.Id))
            .And.BeInAscendingOrder(p => p.Name);
        
        return Task.CompletedTask;
    }

    public Task Then_products_list_is_empty()
    {
        _productsResponse?.ProductItems.Should().BeEmpty();
        return Task.CompletedTask;
    }
    
    public Task Then_products_list_for_autocomplete_contains_items(params string[] items)
    {
        _productsForAutocompleteResponse?.Select(p => p.Name)
            .Should().ContainInOrder(items)
            .And.HaveSameCount(items);
        
        return Task.CompletedTask;
    }

    public Task Then_products_list_for_autocomplete_contains(Product product)
    {
        var item = _productsForAutocompleteResponse?.Single(p => p.Name == product.Name);

        item.Should().NotBeNull();
        item!.QuantityUnit.Should().Be(product.QuantityUnit);
        item.DefaultQuantity.Should().Be(product.DefaultQuantity);
        item.CategoryIds.Should().Equal(product.Categories.Select(c => c.Id).Order());
        return Task.CompletedTask;
    }

    public Task Then_product_is_successfully_created()
    {
        _createProductHttpResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        _createProductResponse.Should().NotBeNull();
        _createProductResponse?.Id.Should().BePositive();
        return Task.CompletedTask;
    }
    
    public async Task Then_product_is_not_created_because_categories_are_not_found()
    {
        _createProductHttpResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errors = await _createProductHttpResponse.Content.ReadFromJsonAsync<Dictionary<string, string[]>>();
        errors.Should().ContainKey(nameof(ProductCreateEditRequest.CategoryIds));
    }

    public Task Then_category_is_successfully_deleted()
    {
        _deleteCategoryResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        return Task.CompletedTask;
    }

    public Task Then_product_is_successfully_updated()
    {
        _updateProductResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        return Task.CompletedTask;
    }
    
    public Task Then_product_is_successfully_deleted()
    {
        _deleteProductResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        return Task.CompletedTask;
    }
    
    public Task Then_multiple_products_are_successfully_deleted()
    {
        _deleteMultipleProductsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        return Task.CompletedTask;
    }

    public async Task Then_product_is_successfully_retrieved(Product product)
    {
        _getProductByIdResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var response = await _getProductByIdResponse.Content.ReadFromJsonAsync<GetProductByIdResponse>();
        
        response.Should()
            .BeEquivalentTo(product.ToGetProductByIdResponse(), options => options
                .Excluding(p => p.Id)
                .WithStrictOrdering());
    }

    public async Task Then_nutrition_suggestion_is(
        int? calories,
        decimal? protein,
        decimal? fats,
        decimal? carbs,
        decimal? sugar,
        decimal? salt)
    {
        _suggestNutritionResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await _suggestNutritionResponse.Content
            .ReadFromJsonAsync<SuggestProductNutritionResponse>();

        response.Should().NotBeNull();
        response!.Calories.Should().Be(calories);
        response.Protein.Should().Be(protein);
        response.Fats.Should().Be(fats);
        response.Carbs.Should().Be(carbs);
        response.Sugar.Should().Be(sugar);
        response.Salt.Should().Be(salt);
    }
}
