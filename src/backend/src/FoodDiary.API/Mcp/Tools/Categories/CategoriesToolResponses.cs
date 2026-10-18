using JetBrains.Annotations;

namespace FoodDiary.API.Mcp.Tools.Categories;

[PublicAPI]
public record ListCategoriesToolResponse(IReadOnlyList<ListCategoriesToolResponse.Category> Categories)
{
    public record Category(int Id, string Name, int ProductCount);
}

[PublicAPI]
public record CategoryToolResponse(int Id, string Name);

[PublicAPI]
public record DeleteCategoryToolResponse(int DeletedId);
