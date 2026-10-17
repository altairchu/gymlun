using FoodDiary.API.Requests;
using FoodDiary.Domain.Entities;
using FoodDiary.Domain.Enums;

namespace FoodDiary.ComponentTests.Dsl;

public class ProductCreateEditRequestBuilder
{
    private string _name = string.Empty;
    private int _caloriesCost;
    private int _defaultQuantity;
    private QuantityUnit _quantityUnit;
    private IReadOnlyList<int>? _categoryIds;
    private decimal? _protein;
    private decimal? _fats;
    private decimal? _carbs;
    private decimal? _sugar;
    private decimal? _salt;
    private bool _isFavorite;

    public ProductCreateEditRequest Please() => new()
    {
        Name = _name,
        CaloriesCost = _caloriesCost,
        DefaultQuantity = _defaultQuantity,
        QuantityUnit = _quantityUnit,
        CategoryIds = _categoryIds,
        Protein = _protein,
        Fats = _fats,
        Carbs = _carbs,
        Sugar = _sugar,
        Salt = _salt,
        IsFavorite = _isFavorite
    };

    public ProductCreateEditRequestBuilder From(Product product)
    {
        _name = product.Name;
        _caloriesCost = product.CaloriesCost;
        _defaultQuantity = product.DefaultQuantity;
        _quantityUnit = product.QuantityUnit;
        _categoryIds = product.Categories.Select(c => c.Id).ToList();
        _protein = product.Protein;
        _fats = product.Fats;
        _carbs = product.Carbs;
        _sugar = product.Sugar;
        _salt = product.Salt;
        _isFavorite = product.IsFavorite;
        return this;
    }

    public ProductCreateEditRequestBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public ProductCreateEditRequestBuilder WithCategoryIds(params int[] categoryIds)
    {
        _categoryIds = categoryIds;
        return this;
    }
}
