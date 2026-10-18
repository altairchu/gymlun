using FoodDiary.API.Features.ProductPhotos;
using FoodDiary.API.Features.Products.Contracts;
using FoodDiary.Domain.Entities;

namespace FoodDiary.API.Features.Products.Extensions;

public static class ProductsMappingExtensions
{
    public static GetProductByIdResponse ToGetProductByIdResponse(this Product p)
    {
        return new GetProductByIdResponse
        {
            Id = p.Id,
            Name = p.Name,
            DefaultQuantity = p.DefaultQuantity,
            QuantityUnit = p.QuantityUnit,
            CaloriesCost = p.CaloriesCost,
            Protein = p.Protein,
            Fats = p.Fats,
            Carbs = p.Carbs,
            Sugar = p.Sugar,
            Salt = p.Salt,
            IsFavorite = p.IsFavorite,
            PhotoUrl = p.PhotoUrl(),
            Categories = p.Categories.ToResponseCategories()
        };
    }

    public static IReadOnlyList<Contracts.Category> ToResponseCategories(this IEnumerable<Domain.Entities.Category> categories)
    {
        return categories
            .OrderBy(c => c.Name)
            .Select(c => new Contracts.Category { Id = c.Id, Name = c.Name })
            .ToList();
    }
    
    public static SearchProductsResponse.Product[] ToResponse(this SearchProductsResult result)
    {
        return result.Products
            .Select(p => p.ToResponseProduct())
            .ToArray();
    }

    private static SearchProductsResponse.Product ToResponseProduct(this Product product)
    {
        return new SearchProductsResponse.Product(
            Id: product.Id,
            Name: product.Name,
            DefaultQuantity: product.DefaultQuantity,
            QuantityUnit: product.QuantityUnit,
            CategoryIds: product.Categories.Select(c => c.Id).Order().ToList(),
            Calories: product.CaloriesCost,
            Protein: product.Protein,
            Fats: product.Fats,
            Carbs: product.Carbs,
            Sugar: product.Sugar,
            Salt: product.Salt,
            IsFavorite: product.IsFavorite,
            PhotoUrl: product.PhotoUrl());
    }
}
