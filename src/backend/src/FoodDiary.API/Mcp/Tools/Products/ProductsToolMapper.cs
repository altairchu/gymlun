using FoodDiary.Application.Products.Get;
using FoodDiary.Domain.Entities;
using FoodDiary.Domain.Enums;

namespace FoodDiary.API.Mcp.Tools.Products;

public static class ProductsToolMapper
{
    extension(GetProductsQueryResult result)
    {
        public ListProductsToolResponse ToListProductsToolResponse(int pageNumber, int pageSize)
        {
            return new ListProductsToolResponse(
                Products: [..result.Products.Select(ToResponseProduct)],
                PageNumber: pageNumber,
                PageSize: pageSize,
                TotalCount: result.TotalProductsCount);
        }
    }

    extension(Product product)
    {
        public CreateProductToolResponse ToCreateProductToolResponse() => new(ToResponseProduct(product));

        public UpdateProductToolResponse ToUpdateProductToolResponse() => new(ToResponseProduct(product));
    }

    extension(QuantityUnit quantityUnit)
    {
        public string ToCountBy() => quantityUnit switch
        {
            QuantityUnit.Servings => CountBy.Servings,
            _ => CountBy.Grams
        };
    }

    private static ListProductsToolResponse.Product ToResponseProduct(Product product) => new(
        product.Id,
        product.Name,
        [..product.Categories.OrderBy(c => c.Name).Select(c => new ListProductsToolResponse.ProductCategory(c.Name))],
        product.DefaultQuantity,
        product.QuantityUnit.ToCountBy(),
        product.IsFavorite,
        new ListProductsToolResponse.ProductNutrition(
            product.CaloriesCost,
            product.Protein,
            product.Fats,
            product.Carbs,
            product.Sugar,
            product.Salt));
}
