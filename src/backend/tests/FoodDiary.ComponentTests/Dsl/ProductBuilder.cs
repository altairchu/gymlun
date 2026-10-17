using FoodDiary.Domain.Entities;
using FoodDiary.Domain.Enums;

namespace FoodDiary.ComponentTests.Dsl;

public class ProductBuilder(string? name)
{
    private int _id = Random.Shared.Next();
    private string _name = string.IsNullOrWhiteSpace(name) ? $"TestProduct-{Guid.NewGuid()}" : name;
    private int _defaultQuantity = 100;
    private QuantityUnit _quantityUnit = QuantityUnit.Grams;
    private List<Category> _categories = [Create.Category().Please()];
    private int _calories = 100;
    private decimal? _protein = 10;
    private decimal? _fats = 5;
    private decimal? _carbs = 3;
    private decimal? _sugar = 8;
    private decimal? _salt = 2;
    private bool _isFavorite;

    public Product Please() => new()
    {
        Id = _id,
        Name = _name,
        CaloriesCost = _calories,
        DefaultQuantity = _defaultQuantity,
        QuantityUnit = _quantityUnit,
        Categories = _categories.ToList(),
        Protein = _protein,
        Fats = _fats,
        Carbs = _carbs,
        Sugar = _sugar,
        Salt = _salt,
        IsFavorite = _isFavorite
    };

    public ProductBuilder From(Product product)
    {
        _id = product.Id;
        _name = product.Name;
        _calories = product.CaloriesCost;
        _defaultQuantity = product.DefaultQuantity;
        _quantityUnit = product.QuantityUnit;
        _categories = product.Categories.ToList();
        _protein = product.Protein;
        _fats = product.Fats;
        _carbs = product.Carbs;
        _sugar = product.Sugar;
        _salt = product.Salt;
        _isFavorite = product.IsFavorite;
        return this;
    }

    public ProductBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public ProductBuilder WithCalories(int calories)
    {
        _calories = calories;
        return this;
    }

    public ProductBuilder WithCategories(params Category[] categories)
    {
        _categories = categories.ToList();
        return this;
    }

    public ProductBuilder WithoutCategories() => WithCategories();

    public ProductBuilder CountedInServingsOf(int grams)
    {
        _quantityUnit = QuantityUnit.Servings;
        _defaultQuantity = grams;
        return this;
    }

    public ProductBuilder WithDefaultQuantity(int grams)
    {
        _defaultQuantity = grams;
        return this;
    }

    public ProductBuilder WithNutritionComponents(
        decimal? protein,
        decimal? fats,
        decimal? carbs,
        decimal? sugar,
        decimal? salt)
    {
        _protein = protein;
        _fats = fats;
        _carbs = carbs;
        _sugar = sugar;
        _salt = salt;
        return this;
    }
}
