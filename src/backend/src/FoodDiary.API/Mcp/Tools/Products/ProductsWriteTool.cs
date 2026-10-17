using System.ComponentModel;
using FoodDiary.Application.Categories.Create;
using FoodDiary.Application.Categories.Get;
using FoodDiary.Application.Products.Create;
using FoodDiary.Domain.Entities;
using FoodDiary.Domain.Enums;
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
    internal const int MinServingGrams = 1;
    internal const int MaxServingGrams = 1000;
    internal const int MaxCaloriesPer100g = 1000;
    internal const int MaxCategories = 10;
    internal const int MaxCategoryNameLength = 64;

    private const string CreateProductToolDescription =
        """
        Adds a product or dish to the personal catalogue and returns it. Call `list_products` first: names are unique, so reuse an existing product instead of adding a near-duplicate.

        Nutrition values are for one serving of `servingGrams` grams, the way the person usually eats it, e.g. a plate of 10 dumplings of 250 g. For a food label given per 100 g, pass `servingGrams` = 100. They are stored per 100 g, and `servingGrams` becomes the product's `defaultQuantity`. `calories` is kilocalories; `protein`, `fats`, `carbs`, `sugar` and `salt` are grams (salt is sodium chloride: multiply sodium by 2.5). When only an estimate is possible, estimate; omit a value only when it cannot be estimated at all, since `null` means unknown.

        `countBy` is how the person counts the product, `grams` by default for weighed foods such as rice or meat. Pass `servings` for foods counted in pieces or servings, such as dumplings, eggs or a bowl of soup: then `servingGrams` and the nutrition values are for ONE piece or serving, e.g. one dumpling of 25 g, so `log_food` can log "10 dumplings" as 10 `servings`.

        `categories` group products, e.g. ["Dumplings", "Frozen"], in any language; omit them when none fits. An existing category with the same name, ignoring case, is reused, otherwise one is created.

        `isFavorite` defaults to true: a product added through a conversation is usually one the person eats often.
        """;

    [McpServerTool(Name = "create_product", ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false)]
    [Description(CreateProductToolDescription)]
    public async Task<CreateProductToolResponse> CreateProduct(
        [Description("Product name, 1 to 100 characters, in the person's language.")] string name,
        [Description("Grams in one serving, or in one piece or serving when `countBy` is `servings`, from 1 to 1000.")] int servingGrams,
        [Description("Kilocalories in one serving.")] decimal calories,
        [Description("Grams of protein in one serving.")] decimal? protein = null,
        [Description("Grams of fats in one serving.")] decimal? fats = null,
        [Description("Grams of carbs in one serving.")] decimal? carbs = null,
        [Description("Grams of sugar in one serving.")] decimal? sugar = null,
        [Description("Grams of salt in one serving.")] decimal? salt = null,
        [Description("Whether the person eats it often.")] bool isFavorite = true,
        [Description("Category names, at most 10, each 1 to 64 characters.")] string[]? categories = null,
        [Description("`grams` or `servings`. Defaults to `grams`.")] string? countBy = null,
        CancellationToken cancellationToken = default)
    {
        name = name.Trim();

        if (name.Length is < 1 or > 100)
        {
            throw new McpException("`name` must be 1 to 100 characters.");
        }

        var categoryNames = ToCategoryNames(categories ?? []);

        var quantityUnit = string.IsNullOrWhiteSpace(countBy)
            ? QuantityUnit.Grams
            : CountBy.ToQuantityUnit(countBy)
              ?? throw new McpException($"Unknown countBy '{countBy}'. Use {CountBy.Grams} or {CountBy.Servings}.");

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

        var productCategories = await FindOrCreateCategories(categoryNames, cancellationToken);

        var result = await createProductCommandHandler.Handle(
            new CreateProductCommand(
                name,
                caloriesPer100g,
                servingGrams,
                quantityUnit,
                productCategories.Select(c => c.Id).ToList(),
                Per100g(protein, nameof(protein)),
                Per100g(fats, nameof(fats)),
                Per100g(carbs, nameof(carbs)),
                Per100g(sugar, nameof(sugar)),
                Per100g(salt, nameof(salt)),
                isFavorite),
            cancellationToken);

        return result switch
        {
            CreateProductResult.Success success => success.Product.ToCreateProductToolResponse(),
            CreateProductResult.ProductAlreadyExists => throw new McpException($"A product named '{name}' already exists. Find it with `list_products`."),
            _ => throw new McpException($"Product '{name}' could not be created.")
        };
    }

    private static List<string> ToCategoryNames(IEnumerable<string> categories)
    {
        var names = categories
            .Select(category => category.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (names.Count > MaxCategories)
        {
            throw new McpException($"Requested {names.Count} categories; the maximum is {MaxCategories}.");
        }

        if (names.Any(category => category.Length is < 1 or > MaxCategoryNameLength))
        {
            throw new McpException($"Each of `categories` must be 1 to {MaxCategoryNameLength} characters.");
        }

        return names;
    }

    private async Task<List<Category>> FindOrCreateCategories(IReadOnlyCollection<string> names, CancellationToken cancellationToken)
    {
        if (names.Count == 0)
        {
            return [];
        }

        var existingCategories = (await getCategoriesQueryHandler.Handle(new GetCategoriesQuery(), cancellationToken)).Categories;
        var categories = new List<Category>();

        foreach (var name in names)
        {
            var existing = existingCategories.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
            categories.Add(existing ?? await CreateCategory(name, cancellationToken));
        }

        return categories;
    }

    private async Task<Category> CreateCategory(string name, CancellationToken cancellationToken)
    {
        var created = await createCategoryCommandHandler.Handle(new CreateCategoryCommand(name), cancellationToken);

        return created is CreateCategoryResult.Success success
            ? success.Category
            : throw new McpException($"Category '{name}' could not be created.");
    }
}
