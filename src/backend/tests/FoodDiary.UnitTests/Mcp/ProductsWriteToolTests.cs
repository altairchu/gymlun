using FoodDiary.API.Mcp.Tools.Products;
using FoodDiary.Application.Categories.Create;
using FoodDiary.Application.Categories.Get;
using FoodDiary.Application.Products.Create;
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
    private readonly Mock<ICategoriesRepository> _categoriesRepository = new();
    private readonly Mock<IFoodDiaryUnitOfWork> _unitOfWork = new();
    private readonly List<Category> _categories = [];
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
    }

    private Task<CreateProductToolResponse> CreateProduct(
        string[]? categories = null,
        string? countBy = null,
        int servingGrams = 25,
        decimal calories = 55)
    {
        var tool = new ProductsWriteTool(
            new GetCategoriesQueryHandler(_categoryRepository.Object),
            new CreateCategoryCommandHandler(_categoryRepository.Object),
            new CreateProductCommandHandler(_productsRepository.Object, _unitOfWork.Object));

        return tool.CreateProduct(
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
}
