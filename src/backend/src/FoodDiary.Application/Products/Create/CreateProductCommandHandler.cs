using FoodDiary.Domain.Abstractions.v2;
using FoodDiary.Domain.Entities;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.Repositories.v2;

namespace FoodDiary.Application.Products.Create;

public record CreateProductCommand(
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

public abstract record CreateProductResult
{
    public record Success(Product Product) : CreateProductResult;

    public record ProductAlreadyExists : CreateProductResult;

    public record CategoriesNotFound(IReadOnlyCollection<int> CategoryIds) : CreateProductResult;
}

public class CreateProductCommandHandler(IProductsRepository repository, IFoodDiaryUnitOfWork unitOfWork)
{
    public async Task<CreateProductResult> Handle(CreateProductCommand command, CancellationToken cancellationToken)
    {
        var productWithTheSameName = await repository.FindByExactName(command.Name, cancellationToken);

        if (productWithTheSameName is not null)
        {
            return new CreateProductResult.ProductAlreadyExists();
        }

        var categories = await unitOfWork.Categories.FindByIds(command.CategoryIds, cancellationToken);
        var missingCategoryIds = command.CategoryIds.Except(categories.Select(c => c.Id)).ToList();

        if (missingCategoryIds.Count > 0)
        {
            return new CreateProductResult.CategoriesNotFound(missingCategoryIds);
        }

        var product = new Product
        {
            Name = command.Name,
            CaloriesCost = command.CaloriesCost,
            DefaultQuantity = command.DefaultQuantity,
            QuantityUnit = command.QuantityUnit,
            Categories = categories.ToList(),
            Protein = command.Protein,
            Fats = command.Fats,
            Carbs = command.Carbs,
            Sugar = command.Sugar,
            Salt = command.Salt,
            IsFavorite = command.IsFavorite
        };

        await repository.Create(product, cancellationToken);
        return new CreateProductResult.Success(product);
    }
}
