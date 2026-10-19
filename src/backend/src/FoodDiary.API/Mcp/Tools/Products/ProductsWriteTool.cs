using System.ComponentModel;
using FoodDiary.Application.Categories.Create;
using FoodDiary.Application.Categories.Get;
using FoodDiary.Application.Products.Create;
using FoodDiary.Application.Products.Delete;
using FoodDiary.Application.Products.Update;
using FoodDiary.Domain.Entities;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace FoodDiary.API.Mcp.Tools.Products;

[McpServerToolType]
[Authorize(Policy = McpExtensions.McpWritePolicy)]
public class ProductsWriteTool(
    GetCategoriesQueryHandler getCategoriesQueryHandler,
    CreateCategoryCommandHandler createCategoryCommandHandler,
    CreateProductCommandHandler createProductCommandHandler,
    IProductRepository productRepository,
    UpdateProductCommandHandler updateProductCommandHandler,
    DeleteProductsCommandHandler deleteProductsCommandHandler)
{
    internal const int MinServingGrams = 1;
    internal const int MaxServingGrams = 1000;
    internal const int MaxCaloriesPer100g = 1000;
    internal const int MaxCategories = 10;
    internal const int MaxCategoryNameLength = 64;
    internal const int MaxNutrientGramsPer100g = 100;
    internal const int MaxDeleteProductsIds = 50;

    private const string CreateProductToolDescription =
        """
        Adds a product or dish to the personal catalogue and returns it. Call `list_products` first: names are unique, so reuse an existing product instead of adding a near-duplicate.

        Nutrition values are for one serving of `servingGrams` grams, the way the person usually eats it, e.g. a plate of 10 dumplings of 250 g. For a food label given per 100 g, pass `servingGrams` = 100. They are stored per 100 g, and `servingGrams` becomes the product's `defaultQuantity`. `calories` is kilocalories; `protein`, `fats`, `carbs`, `sugar` and `salt` are grams (salt is sodium chloride: multiply sodium by 2.5). When only an estimate is possible, estimate; omit a value only when it cannot be estimated at all, since `null` means unknown.

        `countBy` is how the person counts the product, `grams` by default for weighed foods such as rice or meat. Pass `servings` for foods counted in pieces or servings, such as dumplings, eggs or a bowl of soup: then `servingGrams` and the nutrition values are for ONE piece or serving, e.g. one dumpling of 25 g, so `log_food` can log "10 dumplings" as 10 `servings`.

        `categories` group products, e.g. ["Dumplings", "Frozen"], in any language; omit them when none fits. An existing category with the same name, ignoring case, is reused, otherwise one is created.

        `isFavorite` defaults to true: a product added through a conversation is usually one the person eats often.
        """;

    private const string UpdateProductToolDescription =
        """
        Changes one product by its `id`, as returned by `list_products`, and returns it as it is now. Pass only what changes: everything else stays as it was.

        Unlike `create_product`, nutrition values here are per 100 g, as `list_products` shows them under `per100g`: `caloriesPer100g` in kilocalories, the rest in grams (salt is sodium chloride). `defaultQuantity` is the usual portion in grams, or the grams in one piece or serving when `countBy` is `servings`; changing it keeps the values per 100 g.

        `categories` replaces all of the product's categories: pass the whole list, or an empty list to remove them all. As in `create_product`, an existing category with the same name, ignoring case, is reused, otherwise one is created.

        Food logs of the product keep their grams, so their nutrition follows the new values.
        """;

    private const string DeleteProductsToolDescription =
        """
        Deletes products by their `id`, as returned by `list_products`: pass every product to delete in `ids`, 1 to 50 of them, in one call. Every food log of these products is deleted with them, so make sure the person means it; to fix a wrong value, use `update_product` instead.

        When any of `ids` is not found, nothing is deleted.
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

    [McpServerTool(Name = "update_product", ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = false)]
    [Description(UpdateProductToolDescription)]
    public async Task<UpdateProductToolResponse> UpdateProduct(
        [Description("Id of the product to change.")] int id,
        [Description("New name, 1 to 100 characters.")] string? name = null,
        [Description("New usual portion, or grams in one piece or serving, from 1 to 1000.")] int? defaultQuantity = null,
        [Description("`grams` or `servings`.")] string? countBy = null,
        [Description("Kilocalories in 100 g, from 1 to 1000.")] int? caloriesPer100g = null,
        [Description("Grams of protein in 100 g.")] decimal? proteinPer100g = null,
        [Description("Grams of fats in 100 g.")] decimal? fatsPer100g = null,
        [Description("Grams of carbs in 100 g.")] decimal? carbsPer100g = null,
        [Description("Grams of sugar in 100 g.")] decimal? sugarPer100g = null,
        [Description("Grams of salt in 100 g.")] decimal? saltPer100g = null,
        [Description("Whether the person eats it often.")] bool? isFavorite = null,
        [Description("All of the product's category names, at most 10, each 1 to 64 characters.")] string[]? categories = null,
        CancellationToken cancellationToken = default)
    {
        name = name?.Trim();

        if (name?.Length is < 1 or > 100)
        {
            throw new McpException("`name` must be 1 to 100 characters.");
        }

        if (defaultQuantity is < MinServingGrams or > MaxServingGrams)
        {
            throw new McpException($"Requested defaultQuantity {defaultQuantity}; it must be from {MinServingGrams} to {MaxServingGrams}.");
        }

        if (caloriesPer100g is < 1 or > MaxCaloriesPer100g)
        {
            throw new McpException($"Requested caloriesPer100g {caloriesPer100g}; it must be from 1 to {MaxCaloriesPer100g}.");
        }

        decimal? Nutrient(decimal? per100g, decimal? current, string parameter) => per100g switch
        {
            null => current,
            < 0 or > MaxNutrientGramsPer100g => throw new McpException(
                $"`{parameter}` must be from 0 to {MaxNutrientGramsPer100g} grams per 100 g."),
            { } value => value
        };

        var quantityUnit = string.IsNullOrWhiteSpace(countBy)
            ? (QuantityUnit?)null
            : CountBy.ToQuantityUnit(countBy)
              ?? throw new McpException($"Unknown countBy '{countBy}'. Use {CountBy.Grams} or {CountBy.Servings}.");

        var categoryNames = categories is null ? null : ToCategoryNames(categories);

        var productQuery = productRepository.LoadCategories(productRepository.GetQueryWithoutTracking().Where(p => p.Id == id));
        var product = (await productRepository.GetByQueryAsync(productQuery, cancellationToken)).FirstOrDefault()
                      ?? throw new McpException($"Product {id} not found. Look it up with `list_products`.");

        var productCategories = categoryNames is null
            ? product.Categories.ToList()
            : await FindOrCreateCategories(categoryNames, cancellationToken);

        var updatedProduct = new Product
        {
            Id = id,
            Name = name ?? product.Name,
            CaloriesCost = caloriesPer100g ?? product.CaloriesCost,
            DefaultQuantity = defaultQuantity ?? product.DefaultQuantity,
            QuantityUnit = quantityUnit ?? product.QuantityUnit,
            IsFavorite = isFavorite ?? product.IsFavorite,
            Protein = Nutrient(proteinPer100g, product.Protein, nameof(proteinPer100g)),
            Fats = Nutrient(fatsPer100g, product.Fats, nameof(fatsPer100g)),
            Carbs = Nutrient(carbsPer100g, product.Carbs, nameof(carbsPer100g)),
            Sugar = Nutrient(sugarPer100g, product.Sugar, nameof(sugarPer100g)),
            Salt = Nutrient(saltPer100g, product.Salt, nameof(saltPer100g)),
            PhotoUpdatedAt = product.PhotoUpdatedAt,
            Categories = productCategories
        };

        var result = await updateProductCommandHandler.Handle(
            new UpdateProductCommand(
                id,
                updatedProduct.Name,
                updatedProduct.CaloriesCost,
                updatedProduct.DefaultQuantity,
                updatedProduct.QuantityUnit,
                productCategories.Select(c => c.Id).ToList(),
                updatedProduct.Protein,
                updatedProduct.Fats,
                updatedProduct.Carbs,
                updatedProduct.Sugar,
                updatedProduct.Salt,
                updatedProduct.IsFavorite),
            cancellationToken);

        return result switch
        {
            UpdateProductResult.Success => updatedProduct.ToUpdateProductToolResponse(),
            UpdateProductResult.NameAlreadyExists => throw new McpException($"Another product is already named '{updatedProduct.Name}'. Find it with `list_products`."),
            UpdateProductResult.NotFound => throw new McpException($"Product {id} not found. Look it up with `list_products`."),
            _ => throw new McpException($"Product {id} could not be changed.")
        };
    }

    [McpServerTool(Name = "delete_products", ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = false)]
    [Description(DeleteProductsToolDescription)]
    public async Task<DeleteProductsToolResponse> DeleteProducts(
        [Description("Ids of the products to delete, 1 to 50.")] int[] ids,
        CancellationToken cancellationToken)
    {
        var distinctIds = IdsArgument.Distinct(ids, MaxDeleteProductsIds);
        var productsQuery = productRepository.GetQueryWithoutTracking().Where(p => distinctIds.Contains(p.Id));
        var foundProducts = await productRepository.GetByQueryAsync(productsQuery, cancellationToken);
        var missingIds = distinctIds.Except(foundProducts.Select(p => p.Id)).ToList();

        if (missingIds.Count > 0)
        {
            throw new McpException(
                $"Products not found: {IdsArgument.Join(missingIds)}. Nothing was deleted; look them up with `list_products`.");
        }

        await deleteProductsCommandHandler.Handle(new DeleteProductsCommand(distinctIds), cancellationToken);

        return new DeleteProductsToolResponse(distinctIds);
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
