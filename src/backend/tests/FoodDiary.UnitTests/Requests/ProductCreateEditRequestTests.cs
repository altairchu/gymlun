using System.ComponentModel.DataAnnotations;
using FoodDiary.API.Requests;
using FoodDiary.Domain.Enums;

namespace FoodDiary.UnitTests.Requests;

public class ProductCreateEditRequestTests
{
    private static ProductCreateEditRequest Request(IReadOnlyList<int>? categoryIds = null, int? categoryId = null) => new()
    {
        Name = "Rice",
        CaloriesCost = 130,
        DefaultQuantity = 200,
        CategoryIds = categoryIds,
        CategoryId = categoryId
    };

    private static List<ValidationResult> Validate(object request)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void CategoryIds_TakePrecedenceOverLegacyCategoryId()
    {
        Request(categoryIds: [3, 4], categoryId: 1).GetCategoryIds().Should().Equal(3, 4);
    }

    [Fact]
    public void LegacyCategoryId_IsUsedWhenCategoryIdsAreAbsent()
    {
        Request(categoryId: 5).GetCategoryIds().Should().Equal(5);
    }

    [Fact]
    public void NoCategories_IsValidAndEmpty()
    {
        var request = Request();

        Validate(request).Should().BeEmpty();
        request.GetCategoryIds().Should().BeEmpty();
        request.QuantityUnit.Should().Be(QuantityUnit.Grams);
    }

    [Fact]
    public void TenDistinctCategories_AreValid()
    {
        Validate(Request(categoryIds: Enumerable.Range(1, 10).ToList())).Should().BeEmpty();
    }

    [Theory]
    [InlineData(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 })]
    [InlineData(new[] { 1, 2, 1 })]
    [InlineData(new[] { 0 })]
    public void InvalidCategoryIds_AreRejected(int[] categoryIds)
    {
        Validate(Request(categoryIds: categoryIds))
            .Should().ContainSingle()
            .Which.MemberNames.Should().Equal(nameof(ProductCreateEditRequest.CategoryIds));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(1000, true)]
    [InlineData(1001, false)]
    public void DefaultQuantity_IsFrom1To1000(int defaultQuantity, bool isValid)
    {
        var request = new ProductCreateEditRequest { Name = "Egg", CaloriesCost = 150, DefaultQuantity = defaultQuantity };

        Validate(request).Should().HaveCount(isValid ? 0 : 1);
    }

    [Fact]
    public void UnknownQuantityUnit_IsRejected()
    {
        var request = new ProductCreateEditRequest
        {
            Name = "Egg",
            CaloriesCost = 150,
            DefaultQuantity = 50,
            QuantityUnit = (QuantityUnit)3
        };

        Validate(request).Should().ContainSingle();
    }

    [Theory]
    [InlineData("飯", true)]
    [InlineData("肉類", true)]
    [InlineData("", false)]
    [InlineData(" ", false)]
    public void CategoryName_IsFrom1To64Characters(string name, bool isValid)
    {
        Validate(new CategoryCreateEditRequest { Name = name }).Should().HaveCount(isValid ? 0 : 1);
    }

    [Fact]
    public void CategoryNameOf65Characters_IsRejected()
    {
        Validate(new CategoryCreateEditRequest { Name = new string('a', 65) }).Should().ContainSingle();
        Validate(new CategoryCreateEditRequest { Name = new string('a', 64) }).Should().BeEmpty();
    }
}
