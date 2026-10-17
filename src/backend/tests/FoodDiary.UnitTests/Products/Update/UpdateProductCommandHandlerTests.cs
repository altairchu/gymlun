using FoodDiary.Application.Products.Update;
using FoodDiary.Domain.Abstractions;
using FoodDiary.Domain.Abstractions.v2;
using FoodDiary.Domain.Entities;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.Repositories;
using FoodDiary.Domain.Repositories.v2;
using Moq;

namespace FoodDiary.UnitTests.Products.Update;

public class UpdateProductCommandHandlerTests
{
    private static readonly Category Dairy = new() { Id = 1, Name = "Dairy" };
    private static readonly Category Breakfast = new() { Id = 2, Name = "早餐" };
    private static readonly Category Drinks = new() { Id = 3, Name = "Drinks" };

    private readonly Mock<IProductRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICategoriesRepository> _categoriesRepository = new();
    private readonly Mock<IFoodDiaryUnitOfWork> _foodDiaryUnitOfWork = new();
    private readonly List<Product> _products = [];
    private readonly List<Product> _productsWithName = [];

    public UpdateProductCommandHandlerTests()
    {
        _repository.Setup(r => r.UnitOfWork).Returns(_unitOfWork.Object);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _repository.Setup(r => r.GetQuery()).Returns(() => _products.AsQueryable());
        _repository.Setup(r => r.GetQueryWithoutTracking()).Returns(() => _productsWithName.AsQueryable());
        _repository
            .Setup(r => r.LoadCategories(It.IsAny<IQueryable<Product>>()))
            .Returns((IQueryable<Product> query) => query);
        _repository
            .Setup(r => r.GetByQueryAsync(It.IsAny<IQueryable<Product>>(), It.IsAny<CancellationToken>()))
            .Returns((IQueryable<Product> q, CancellationToken _) => Task.FromResult(q.ToList()));

        _foodDiaryUnitOfWork.Setup(u => u.Categories).Returns(_categoriesRepository.Object);
        Category[] categories = [Dairy, Breakfast, Drinks];
        _categoriesRepository
            .Setup(r => r.FindByIds(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<int> ids, CancellationToken _) =>
                categories.Where(c => ids.Contains(c.Id)).ToArray());
    }

    private UpdateProductCommandHandler Handler => new(_repository.Object, _foodDiaryUnitOfWork.Object);

    private static Product NewProduct(int id, string name, params Category[] categories) =>
        new()
        {
            Id = id,
            Name = name,
            Protein = null,
            Fats = null,
            Carbs = null,
            Sugar = null,
            Salt = null,
            Categories = categories.ToList()
        };

    private static UpdateProductCommand Command(int id, string name, params int[] categoryIds) =>
        new(id, name, 100, 100, QuantityUnit.Grams, categoryIds, null, null, null, null, null);

    private void GivenExistingProduct(Product product) => _products.Add(product);

    private void GivenProductsWithName(params Product[] products) => _productsWithName.AddRange(products);

    [Fact]
    public async Task Handle_ProductNotFound_ReturnsNotFound()
    {
        var result = await Handler.Handle(Command(1, "Beef"), CancellationToken.None);

        result.Should().BeOfType<UpdateProductResult.NotFound>();
        _repository.Verify(r => r.Update(It.IsAny<Product>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NameChangedToExistingName_ReturnsNameAlreadyExists()
    {
        GivenExistingProduct(NewProduct(1, "Chicken"));
        GivenProductsWithName(NewProduct(2, "Beef"));

        var result = await Handler.Handle(Command(1, "Beef"), CancellationToken.None);

        result.Should().BeOfType<UpdateProductResult.NameAlreadyExists>();
        _repository.Verify(r => r.Update(It.IsAny<Product>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NameUnchanged_AllowedEvenWhenSameNameRowExists()
    {
        var existing = NewProduct(1, "Chicken");
        GivenExistingProduct(existing);
        GivenProductsWithName(NewProduct(1, "Chicken"));

        var result = await Handler.Handle(Command(1, "Chicken"), CancellationToken.None);

        result.Should().BeOfType<UpdateProductResult.Success>();
        _repository.Verify(r => r.Update(existing), Times.Once);
    }

    [Fact]
    public async Task Handle_NameChangedToFreeName_AssignsFieldsAndReturnsSuccess()
    {
        var existing = NewProduct(1, "Chicken", Dairy);
        GivenExistingProduct(existing);

        var result = await Handler.Handle(
            new UpdateProductCommand(1, "Boiled chicken", 200, 150, QuantityUnit.Servings, [2, 3], 1.1m, 2.2m, 3.3m, 4.4m, 5.5m),
            CancellationToken.None);

        result.Should().BeOfType<UpdateProductResult.Success>();
        existing.Name.Should().Be("Boiled chicken");
        existing.CaloriesCost.Should().Be(200);
        existing.DefaultQuantity.Should().Be(150);
        existing.QuantityUnit.Should().Be(QuantityUnit.Servings);
        existing.Categories.Should().BeEquivalentTo([Breakfast, Drinks]);
        existing.Protein.Should().Be(1.1m);
        existing.Fats.Should().Be(2.2m);
        existing.Carbs.Should().Be(3.3m);
        existing.Sugar.Should().Be(4.4m);
        existing.Salt.Should().Be(5.5m);
        _repository.Verify(r => r.Update(existing), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NoCategories_RemovesEveryCategory()
    {
        var existing = NewProduct(1, "Chicken", Dairy, Drinks);
        GivenExistingProduct(existing);

        var result = await Handler.Handle(Command(1, "Chicken"), CancellationToken.None);

        result.Should().BeOfType<UpdateProductResult.Success>();
        existing.Categories.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_UnknownCategory_ReturnsCategoriesNotFoundWithoutSaving()
    {
        var existing = NewProduct(1, "Chicken", Dairy);
        GivenExistingProduct(existing);

        var result = await Handler.Handle(Command(1, "Chicken", 1, 42), CancellationToken.None);

        result.Should().BeOfType<UpdateProductResult.CategoriesNotFound>()
            .Which.CategoryIds.Should().Equal(42);
        existing.Categories.Should().Equal(Dairy);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
