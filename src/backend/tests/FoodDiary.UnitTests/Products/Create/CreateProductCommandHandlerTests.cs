using FoodDiary.Application.Products.Create;
using FoodDiary.Domain.Abstractions.v2;
using FoodDiary.Domain.Entities;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.Repositories.v2;
using Moq;

namespace FoodDiary.UnitTests.Products.Create;

public class CreateProductCommandHandlerTests
{
    private readonly Mock<IProductsRepository> _repository = new();
    private readonly Mock<ICategoriesRepository> _categoriesRepository = new();
    private readonly Mock<IFoodDiaryUnitOfWork> _unitOfWork = new();
    private readonly List<Category> _categories = [];

    public CreateProductCommandHandlerTests()
    {
        _unitOfWork.Setup(u => u.Categories).Returns(_categoriesRepository.Object);
        _categoriesRepository
            .Setup(r => r.FindByIds(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<int> ids, CancellationToken _) =>
                _categories.Where(c => ids.Contains(c.Id)).ToArray());
    }

    private CreateProductCommandHandler Handler => new(_repository.Object, _unitOfWork.Object);

    private static CreateProductCommand Command(
        string name = "Chicken",
        QuantityUnit quantityUnit = QuantityUnit.Grams,
        params int[] categoryIds) =>
        new(name, 100, 100, quantityUnit, categoryIds, null, null, null, null, null);

    private void GivenNoProductWithSameName() =>
        _repository.Setup(r => r.FindByExactName(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product)null!);

    [Fact]
    public async Task Handle_ProductWithSameNameExists_ReturnsProductAlreadyExists()
    {
        _repository.Setup(r => r.FindByExactName("Chicken", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Product { Name = "Chicken", Protein = null, Fats = null, Carbs = null, Sugar = null, Salt = null });

        var result = await Handler.Handle(Command(), CancellationToken.None);

        result.Should().BeOfType<CreateProductResult.ProductAlreadyExists>();
        _repository.Verify(r => r.Create(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NewName_CreatesAndReturnsSuccess()
    {
        GivenNoProductWithSameName();

        var result = await Handler.Handle(Command(), CancellationToken.None);

        result.Should().BeOfType<CreateProductResult.Success>()
            .Which.Product.Name.Should().Be("Chicken");
        _repository.Verify(r => r.Create(It.Is<Product>(p => p.Name == "Chicken"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ManyCategoriesAndServings_CreatesProductLinkedToEveryCategory()
    {
        GivenNoProductWithSameName();
        _categories.AddRange([new Category { Id = 1, Name = "餃子" }, new Category { Id = 2, Name = "Frozen" }]);

        var result = await Handler.Handle(Command("Pork dumplings", QuantityUnit.Servings, 1, 2), CancellationToken.None);

        var product = result.Should().BeOfType<CreateProductResult.Success>().Which.Product;
        product.QuantityUnit.Should().Be(QuantityUnit.Servings);
        product.Categories.Select(c => c.Name).Should().BeEquivalentTo("餃子", "Frozen");
    }

    [Fact]
    public async Task Handle_NoCategories_CreatesProductWithoutCategories()
    {
        GivenNoProductWithSameName();

        var result = await Handler.Handle(Command(), CancellationToken.None);

        result.Should().BeOfType<CreateProductResult.Success>()
            .Which.Product.Categories.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_UnknownCategory_ReturnsCategoriesNotFoundWithoutCreating()
    {
        GivenNoProductWithSameName();
        _categories.Add(new Category { Id = 1, Name = "Dairy" });

        var result = await Handler.Handle(Command("Milk", QuantityUnit.Grams, 1, 99), CancellationToken.None);

        result.Should().BeOfType<CreateProductResult.CategoriesNotFound>()
            .Which.CategoryIds.Should().Equal(99);
        _repository.Verify(r => r.Create(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
