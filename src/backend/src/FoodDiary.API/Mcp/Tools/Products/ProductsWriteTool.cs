using System.ComponentModel;
using FoodDiary.Application.Categories.Create;
using FoodDiary.Application.Categories.Get;
using FoodDiary.Application.Products.Create;
using FoodDiary.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace FoodDiary.API.Mcp.Tools.Products;

[McpServerToolType]
[Authorize(Policy = McpExtensions.McpWritePolicy)]
public class ProductsWriteTool(
    GetCategoriesQueryHandler getCategoriesQueryHandler,
    CreateCategoryCommandHandler createCategoryCommandHandler,
    CreateProductCommandHandler createProductCommandHandler)
{
    internal const int MinServingGrams = 10;
    internal const int MaxServingGrams = 1000;
    internal const int MaxCaloriesPer100g = 1000;

    private const string CreateProductToolDescription =
        """
        Adds a product or dish to the personal catalogue and returns it. Call `list_products` first: names are unique, so reuse an existing product instead of adding a near-duplicate.

        Nutrition values are for one serving of `servingGrams` grams, the way the person usually eats it, e.g. a plate of 10 dumplings of 250 g. For a food label given per 100 g, pass `servingGrams` = 100. They are stored per 100 g, and `servingGrams` becomes the product's `defaultQuantity`. `calories` is kilocalories; `protein`, `fats`, `carbs`, `sugar` and `salt` are grams (salt is sodium chloride: multiply sodium by 2.5). When only an estimate is possible, estimate; omit a value only when it cannot be estimated at all, since `null` means unknown.

        `category` groups products, e.g. "Dumplings" or "Drinks". An existing category with that name is reused, otherwise one is created.

        `isFavorite` defaults to true: a product added through a conversation is usually one the person eats often.
        """;

    [McpServerTool(Name = "create_product", ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false)]
    [Description(CreateProductToolDescription)]
    public async Task<CreateProductToolResponse> CreateProduct(
        [Description("Product name, 1 to 100 characters, in the person's language.")] string name,
        [Description("Category name.")] string category,
        [Description("Grams in one serving, from 10 to 1000.")] int servingGrams,
        [Description("Kilocalories in one serving.")] decimal calories,
        [Description("Grams of protein in one serving.")] decimal? protein = null,
        [Description("Grams of fats in one serving.")] decimal? fats = null,
        [Description("Grams of carbs in one serving.")] decimal? carbs = null,
        [Description("Grams of sugar in one serving.")] decimal? sugar = null,
        [Description("Grams of salt in one serving.")] decimal? salt = null,
        [Description("Whether the person eats it often.")] bool isFavorite = true,
        CancellationToken cancellationToken = default)
    {
        name = name.Trim();
        category = category.Trim();

        if (name.Length is < 1 or > 100)
        {
            throw new McpException("`name` must be 1 to 100 characters.");
        }

        if (category.Length is < 1 or > 64)
        {
            throw new McpException("`category` must be 1 to 64 characters.");
        }

        if (servingGrams is < MinServingGrams or > MaxServingGrams)
        {
            throw new McpException($"Requested servingGrams {servingGrams}; it must be from {MinServingGrams} to {MaxServingGrams}.");
        }

        decimal? Per100g(decimal? perServing, string parameter)
        {
            if (perServing < 0)
            {
                throw new McpException($"`{parameter}` cannot be negative.");
            }

            return perServing is { } value
                ? Math.Round(value * 100 / servingGrams, 2, MidpointRounding.AwayFromZero)
                : null;
        }

        var caloriesPer100g = (int)Math.Round(Per100g(calories, nameof(calories))!.Value, MidpointRounding.AwayFromZero);

        if (caloriesPer100g is < 1 or > MaxCaloriesPer100g)
        {
            throw new McpException($"That is {caloriesPer100g} kcal per 100 g; it must be from 1 to {MaxCaloriesPer100g}. Check `calories` and `servingGrams`.");
        }

        var productCategory = await FindOrCreateCategory(category, cancellationToken);

        var result = await createProductCommandHandler.Handle(
            new CreateProductCommand(
                name,
                caloriesPer100g,
                servingGrams,
                productCategory.Id,
                Per100g(protein, nameof(protein)),
                Per100g(fats, nameof(fats)),
                Per100g(carbs, nameof(carbs)),
                Per100g(sugar, nameof(sugar)),
                Per100g(salt, nameof(salt)),
                isFavorite),
            cancellationToken);

        if (result is not CreateProductResult.Success success)
        {
            throw new McpException($"A product named '{name}' already exists. Find it with `list_products`.");
        }

        success.Product.Category = productCategory;
        return success.Product.ToCreateProductToolResponse();
    }

    private async Task<Category> FindOrCreateCategory(string name, CancellationToken cancellationToken)
    {
        var categories = await getCategoriesQueryHandler.Handle(new GetCategoriesQuery(), cancellationToken);
        var existing = categories.Categories.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            return existing;
        }

        var created = await createCategoryCommandHandler.Handle(new CreateCategoryCommand(name), cancellationToken);

        return created is CreateCategoryResult.Success success
            ? success.Category
            : throw new McpException($"Category '{name}' could not be created.");
    }
}
