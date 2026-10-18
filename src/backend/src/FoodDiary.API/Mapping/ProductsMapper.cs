using FoodDiary.API.Dtos;
using FoodDiary.API.Features.ProductPhotos;
using FoodDiary.API.Features.Products.Extensions;
using FoodDiary.Contracts.Products;
using FoodDiary.Domain.Entities;

namespace FoodDiary.API.Mapping;

public static class ProductsMapper
{
    public static ProductItemDto ToProductItemDto(this Product product) => new()
    {
        Id = product.Id,
        Name = product.Name,
        CaloriesCost = product.CaloriesCost,
        DefaultQuantity = product.DefaultQuantity,
        QuantityUnit = product.QuantityUnit,
        Categories = product.Categories.ToResponseCategories(),
        Protein = product.Protein,
        Fats = product.Fats,
        Carbs = product.Carbs,
        Sugar = product.Sugar,
        Salt = product.Salt,
        IsFavorite = product.IsFavorite,
        PhotoUrl = product.PhotoUrl()
    };

    public static CreateProductResponse ToCreateProductResponse(this Product product) => new(product.Id);
}
