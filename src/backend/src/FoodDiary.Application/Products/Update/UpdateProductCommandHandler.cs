using FoodDiary.Domain.Abstractions.v2;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.Repositories;

namespace FoodDiary.Application.Products.Update;

public record UpdateProductCommand(
    int Id,
    string Name,
    int CaloriesCost,
    int DefaultQuantity,
    QuantityUnit QuantityUnit,
    IReadOnlyCollection<int> CategoryIds,
    decimal? Protein,
    decimal? Fats,
    decimal? Carbs,
    decimal? Sugar,
    decimal? Salt,
    bool IsFavorite = false);

public abstract record UpdateProductResult
{
    public record Success : UpdateProductResult;

    public record NotFound : UpdateProductResult;

    public record NameAlreadyExists : UpdateProductResult;

    public record CategoriesNotFound(IReadOnlyCollection<int> CategoryIds) : UpdateProductResult;
}

public class UpdateProductCommandHandler(IProductRepository productRepository, IFoodDiaryUnitOfWork unitOfWork)
{
    public async Task<UpdateProductResult> Handle(UpdateProductCommand command, CancellationToken cancellationToken)
    {
        var productQuery = productRepository.LoadCategories(
            productRepository.GetQuery().Where(p => p.Id == command.Id));
        var product = (await productRepository.GetByQueryAsync(productQuery, cancellationToken)).FirstOrDefault();

        if (product is null)
        {
            return new UpdateProductResult.NotFound();
        }

        var query = productRepository.GetQueryWithoutTracking().Where(p => p.Name == command.Name);
        var productsWithSameName = await productRepository.GetByQueryAsync(query, cancellationToken);
        var nameChanged = product.Name != command.Name;

        if (nameChanged && productsWithSameName.Count > 0)
        {
            return new UpdateProductResult.NameAlreadyExists();
        }

        var categories = await unitOfWork.Categories.FindByIds(command.CategoryIds, cancellationToken);
        var missingCategoryIds = command.CategoryIds.Except(categories.Select(c => c.Id)).ToList();

        if (missingCategoryIds.Count > 0)
        {
            return new UpdateProductResult.CategoriesNotFound(missingCategoryIds);
        }

        product.Name = command.Name;
        product.CaloriesCost = command.CaloriesCost;
        product.DefaultQuantity = command.DefaultQuantity;
        product.QuantityUnit = command.QuantityUnit;
        product.Protein = command.Protein;
        product.Fats = command.Fats;
        product.Carbs = command.Carbs;
        product.Sugar = command.Sugar;
        product.Salt = command.Salt;
        product.IsFavorite = command.IsFavorite;

        product.Categories.Clear();

        foreach (var category in categories)
        {
            product.Categories.Add(category);
        }

        productRepository.Update(product);
        await productRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
        return new UpdateProductResult.Success();
    }
}
