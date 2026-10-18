using FoodDiary.API.Mcp.Tools.Products;
using FoodDiary.Application.Categories.Create;
using FoodDiary.Application.Categories.Get;
using FoodDiary.Application.Products.Create;
using FoodDiary.Application.Products.Delete;
using FoodDiary.Application.Products.Update;
using FoodDiary.Domain.Abstractions;
using FoodDiary.Domain.Abstractions.v2;
using FoodDiary.Domain.Entities;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.Repositories;
using FoodDiary.Domain.Repositories.v2;
using ModelContextProtocol;
using Moq;

namespace FoodDiary.UnitTests.Mcp;

public class ProductsWriteToolTests
{
    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly Mock<IProductsRepository> _productsRepository = new();
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<ICategoriesRepository> _categoriesRepository = new();
    private readonly Mock<IFoodDiaryUnitOfWork> _unitOfWork = new();
    private readonly List<Category> _categories = [];
    private readonly List<Product> _products = [];
    private Product? _createdProduct;

    public ProductsWriteToolTests()
    {
        _categoryRepository.Setup(r => r.GetQueryWithoutTracking()).Returns(() => _categories.AsQueryable());
        _categoryRepository.Setup(r => r.LoadProducts(It.IsAny<IQueryable<Category>>())).Returns((IQueryable<Category> q) => q);
        _categoryRepository
            .Setup(r => r.GetByQueryAsync(It.IsAny<IQueryable<Category>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IQueryable<Category> q, CancellationToken _) => q.ToList());
        _categoryRepository
            .Setup(r => r.Add(It.IsAny<Category>()))
            .Returns((Category category) =>
            {
                category.Id = _categories.Count + 1;
                _categories.Add(category);
                return category;
            });
        _categoryRepository.Setup(r => r.UnitOfWork).Returns(Mock.Of<IUnitOfWork>());

        _unitOfWork.Setup(u => u.Categories).Returns(_categoriesRepository.Object);
        _categoriesRepository
            .Setup(r => r.FindByIds(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<int> ids, CancellationToken _) =>
                _categories.Where(c => ids.Contains(c.Id)).ToArray());

        _productsRepository
            .Setup(r => r.FindByExactName(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);
        _productsRepository
            .Setup(r => r.Create(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Callback((Product product, CancellationToken _) => _createdProduct = product)
            .Returns(Task.CompletedTask);

        _productRepository.Setup(r => r.GetQuery()).Returns(() => _products.AsQueryable());
        _productRepository.Setup(r => r.GetQueryWithoutTracking()).Returns(() => _products.AsQueryable());
        _productRepository.Setup(r => r.LoadCategories(It.IsAny<IQueryable<Product>>())).Returns((IQueryable<Product> q) => q);
        _productRepository
            .Setup(r => r.GetByQueryAsync(It.IsAny<IQueryable<Product>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IQueryable<Product> q, CancellationToken _) => q.ToList());
        _productRepository.Setup(r => r.UnitOfWork).Returns(Mock.Of<IUnitOfWork>());
    }

    private ProductsWriteTool CreateTool() => new(
        new GetCategoriesQueryHandler(_categoryRepository.Object),
        new CreateCategoryCommandHandler(_categoryRepository.Object),
        new CreateProductCommandHandler(_productsRepository.Object, _unitOfWork.Object),
        _productRepository.Object,
        new UpdateProductCommandHandler(_productRepository.Object, _unitOfWork.Object),
        new DeleteProductCommandHandler(_productRepository.Object));

    private Task<CreateProductToolResponse> CreateProduct(
        string[]? categories = null,
        string? countBy = null,
        int servingGrams = 25,
        decimal calories = 55)
    {
        return CreateTool().CreateProduct(
            "豬肉韭菜餃",
            servingGrams,
            calories,
            categories: categories,
            countBy: countBy,
            cancellationToken: CancellationToken.None);
    }

    [Fact]
    public async Task Categories_AreReusedIgnoringCaseOrCreated()
    {
        _categories.Add(new Category { Id = 1, Name = "Dumplings" });

        var response = await CreateProduct(categories: ["dumplings", " 肉類 ", "DUMPLINGS"]);

        response.Product.Categories.Select(c => c.Name).Should().Equal("Dumplings", "肉類");
        _categories.Select(c => c.Name).Should().Equal("Dumplings", "肉類");
        _createdProduct!.Categories.Select(c => c.Id).Should().BeEquivalentTo([1, 2]);
    }

    [Fact]
    public async Task NoCategories_CreatesProductWithoutCategories()
    {
        var response = await CreateProduct();

        response.Product.Categories.Should().BeEmpty();
        response.Product.CountBy.Should().Be("grams");
        _createdProduct!.QuantityUnit.Should().Be(QuantityUnit.Grams);
    }

    [Fact]
    public async Task CountByServings_IsStoredAsServingsUnit()
    {
        var response = await CreateProduct(countBy: "Servings");

        response.Product.CountBy.Should().Be("servings");
        response.Product.DefaultQuantity.Should().Be(25);
        _createdProduct!.QuantityUnit.Should().Be(QuantityUnit.Servings);
    }

    [Fact]
    public async Task ServingOfOneGram_IsAllowed()
    {
        var response = await CreateProduct(servingGrams: 1, calories: 4, countBy: "servings");

        response.Product.DefaultQuantity.Should().Be(1);
    }

    [Theory]
    [InlineData("pieces", "Unknown countBy 'pieces'. Use grams or servings.")]
    public async Task UnknownCountBy_IsToolError(string countBy, string message)
    {
        var act = () => CreateProduct(countBy: countBy);

        await act.Should().ThrowExactlyAsync<McpException>().WithMessage(message);
    }

    [Fact]
    public async Task MoreThan10Categories_IsToolError()
    {
        var act = () => CreateProduct(categories: Enumerable.Range(1, 11).Select(i => $"Category {i}").ToArray());

        await act.Should().ThrowExactlyAsync<McpException>().WithMessage("Requested 11 categories; the maximum is 10.");
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("This category name is far too long to be accepted by the catalogue!")]
    public async Task CategoryNameOutsideLengthRange_IsToolError(string category)
    {
        var act = () => CreateProduct(categories: [category]);

        await act.Should().ThrowExactlyAsync<McpException>().WithMessage("Each of `categories` must be 1 to 64 characters.");
    }

    private Product GivenChickenBreast()
    {
        var meat = new Category { Id = 1, Name = "Meat" };
        _categories.Add(meat);

        var chicken = new Product
        {
            Id = 30,
            Name = "雞胸肉",
            CaloriesCost = 120,
            DefaultQuantity = 150,
            Protein = 23m,
            Fats = 2m,
            Carbs = null,
            Sugar = null,
            Salt = null,
            Categories = [meat]
        };
        _products.Add(chicken);

        return chicken;
    }

    [Fact]
    public async Task Update_ChangesOnlyWhatIsPassed()
    {
        var chicken = GivenChickenBreast();

        var response = await CreateTool().UpdateProduct(chicken.Id, caloriesPer100g: 165, isFavorite: true,
            cancellationToken: CancellationToken.None);

        response.Product.Should().BeEquivalentTo(new
        {
            Name = "雞胸肉",
            DefaultQuantity = 150,
            CountBy = "grams",
            IsFavorite = true,
            Per100g = new { Calories = 165, Protein = 23m, Fats = 2m, Carbs = (decimal?)null }
        });
        response.Product.Categories.Select(c => c.Name).Should().Equal("Meat");
        chicken.Should().BeEquivalentTo(new { CaloriesCost = 165, Protein = 23m, IsFavorite = true });
    }

    [Fact]
    public async Task Update_Categories_ReplaceAll_ReusingNamesIgnoringCase()
    {
        var chicken = GivenChickenBreast();

        var response = await CreateTool().UpdateProduct(chicken.Id, categories: ["meat", "Frozen"],
            cancellationToken: CancellationToken.None);

        response.Product.Categories.Select(c => c.Name).Should().Equal("Frozen", "Meat");
        chicken.Categories.Select(c => c.Id).Should().BeEquivalentTo([1, 2]);
    }

    [Fact]
    public async Task Update_EmptyCategories_RemovesThemAll()
    {
        var chicken = GivenChickenBreast();

        var response = await CreateTool().UpdateProduct(chicken.Id, categories: [], cancellationToken: CancellationToken.None);

        response.Product.Categories.Should().BeEmpty();
        chicken.Categories.Should().BeEmpty();
    }

    [Fact]
    public async Task Update_NutrientAbove100GramsPer100g_IsToolError()
    {
        var chicken = GivenChickenBreast();

        var act = () => CreateTool().UpdateProduct(chicken.Id, proteinPer100g: 230, cancellationToken: CancellationToken.None);

        await act.Should().ThrowExactlyAsync<McpException>().WithMessage("`proteinPer100g` must be from 0 to 100 grams per 100 g.");
        chicken.Protein.Should().Be(23m);
    }

    [Fact]
    public async Task Update_UnknownProduct_IsToolError()
    {
        var act = () => CreateTool().UpdateProduct(404, name: "Tofu", cancellationToken: CancellationToken.None);

        await act.Should().ThrowExactlyAsync<McpException>().WithMessage("Product 404 not found. Look it up with `list_products`.");
    }
}
