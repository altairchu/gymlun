using FoodDiary.ComponentTests.Infrastructure.DateAndTime;
using FoodDiary.Domain.Entities;
using FoodDiary.Domain.Enums;

namespace FoodDiary.ComponentTests.Dsl;

public class NoteBuilder
{
    private int? _id;
    private DateOnly _date = FakeDateTimeProvider.Today();
    private TimeOnly? _time;
    private MealType _mealType = MealType.Breakfast;
    private Product? _product = Create.Product().Please();
    private int _productId;
    private int _productQuantity = 100;
    private int _displayOrder = 1;
    
    public Note Please() => new()
    {
        Id = _id ?? Random.Shared.Next(),
        Date = _date,
        Time = _time ?? _mealType.DefaultTime,
        MealType = _mealType,
        Product = _product,
        ProductId = _productId,
        ProductQuantity = _productQuantity,
        DisplayOrder = _displayOrder
    };
    
    public NoteBuilder WithDate(string date)
    {
        _date = DateOnly.Parse(date);
        return this;
    }
    
    public NoteBuilder WithDate(DateOnly date)
    {
        _date = date;
        return this;
    }

    /// <summary>
    /// Sets the time of day, and the meal it falls in
    /// </summary>
    public NoteBuilder WithTime(string time)
    {
        _time = TimeOnly.Parse(time);
        _mealType = MealType.FromTime(_time.Value);
        return this;
    }

    public NoteBuilder WithProduct(Product product, int quantity)
    {
        _product = product;
        _productQuantity = quantity;
        return this;
    }
    
    public NoteBuilder WithExistingProduct(Product product)
    {
        _product = null;
        _productId = product.Id;
        return this;
    }

    public NoteBuilder WithProduct(string productName, int quantity)
    {
        _product = Create.Product(productName).Please();
        _productQuantity = quantity;
        return this;
    }
    
    public NoteBuilder WithMealType(MealType mealType)
    {
        _mealType = mealType;
        return this;
    }
    
    public NoteBuilder WithDisplayOrder(int displayOrder)
    {
        _displayOrder = displayOrder;
        return this;
    }

    public NoteBuilder From(Note note)
    {
        _id = note.Id;
        _date = note.Date;
        _time = note.Time;
        _mealType = note.MealType;
        _product = note.Product;
        _productId = note.ProductId;
        _productQuantity = note.ProductQuantity;
        _displayOrder = note.DisplayOrder;
        return this;
    }
}
