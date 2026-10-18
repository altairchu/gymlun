using System.ComponentModel;
using FoodDiary.Application.Categories.Get;
using ModelContextProtocol.Server;

namespace FoodDiary.API.Mcp.Tools.Categories;

[McpServerToolType]
public class CategoriesTool(GetCategoriesQueryHandler getCategoriesQueryHandler)
{
    private const string ListCategoriesToolDescription =
        """
        Every category of the personal catalogue, ordered by name, with the number of products in it. A product can be in several categories, or in none.

        Pass a category's `id` to `list_products` as `categoryId` to list its products.
        """;

    [McpServerTool(Name = "list_categories", ReadOnly = true, OpenWorld = false)]
    [Description(ListCategoriesToolDescription)]
    public async Task<ListCategoriesToolResponse> ListCategories(CancellationToken cancellationToken)
    {
        var result = await getCategoriesQueryHandler.Handle(new GetCategoriesQuery(), cancellationToken);

        return new ListCategoriesToolResponse(
        [
            ..result.Categories.Select(category => new ListCategoriesToolResponse.Category(
                category.Id,
                category.Name,
                category.Products.Count))
        ]);
    }
}
