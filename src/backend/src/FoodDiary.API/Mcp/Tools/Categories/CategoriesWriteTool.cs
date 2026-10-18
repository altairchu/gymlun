using System.ComponentModel;
using FoodDiary.API.Mcp.Tools.Products;
using FoodDiary.Application.Categories.Create;
using FoodDiary.Application.Categories.Delete;
using FoodDiary.Application.Categories.Get;
using FoodDiary.Application.Categories.Update;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace FoodDiary.API.Mcp.Tools.Categories;

[McpServerToolType]
[Authorize(Policy = McpExtensions.McpWritePolicy)]
public class CategoriesWriteTool(
    GetCategoriesQueryHandler getCategoriesQueryHandler,
    CreateCategoryCommandHandler createCategoryCommandHandler,
    UpdateCategoryCommandHandler updateCategoryCommandHandler,
    DeleteCategoryCommandHandler deleteCategoryCommandHandler)
{
    private const string CreateCategoryToolDescription =
        """
        Adds an empty category to the personal catalogue and returns it. Names are unique ignoring case, so call `list_categories` first. `create_product` and `update_product` also create categories by name, so this is only needed for a category with no products yet.
        """;

    private const string RenameCategoryToolDescription =
        """
        Renames one category by its `id`, as returned by `list_categories`, and returns it. Its products stay in it.
        """;

    private const string DeleteCategoryToolDescription =
        """
        Deletes one category by its `id`, as returned by `list_categories`. Its products stay in the catalogue, just without this category.
        """;

    [McpServerTool(Name = "create_category", ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false)]
    [Description(CreateCategoryToolDescription)]
    public async Task<CategoryToolResponse> CreateCategory(
        [Description("Category name, 1 to 64 characters, in the person's language.")] string name,
        CancellationToken cancellationToken)
    {
        name = ValidateName(name);
        await EnsureNameIsFree(name, exceptId: null, cancellationToken);

        var result = await createCategoryCommandHandler.Handle(new CreateCategoryCommand(name), cancellationToken);

        return result is CreateCategoryResult.Success success
            ? new CategoryToolResponse(success.Category.Id, success.Category.Name)
            : throw new McpException($"Category '{name}' could not be created.");
    }

    [McpServerTool(Name = "rename_category", ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = false)]
    [Description(RenameCategoryToolDescription)]
    public async Task<CategoryToolResponse> RenameCategory(
        [Description("Id of the category to rename.")] int id,
        [Description("New name, 1 to 64 characters.")] string name,
        CancellationToken cancellationToken)
    {
        name = ValidateName(name);
        await EnsureNameIsFree(name, exceptId: id, cancellationToken);

        var result = await updateCategoryCommandHandler.Handle(new UpdateCategoryCommand(id, name), cancellationToken);

        return result switch
        {
            UpdateCategoryResult.Success => new CategoryToolResponse(id, name),
            UpdateCategoryResult.NotFound => throw new McpException($"Category {id} not found. Look it up with `list_categories`."),
            _ => throw new McpException($"Category {id} could not be renamed.")
        };
    }

    [McpServerTool(Name = "delete_category", ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = false)]
    [Description(DeleteCategoryToolDescription)]
    public async Task<DeleteCategoryToolResponse> DeleteCategory(
        [Description("Id of the category to delete.")] int id,
        CancellationToken cancellationToken)
    {
        var result = await deleteCategoryCommandHandler.Handle(new DeleteCategoryCommand(id), cancellationToken);

        return result switch
        {
            DeleteCategoryResult.Success => new DeleteCategoryToolResponse(id),
            DeleteCategoryResult.NotFound => throw new McpException($"Category {id} not found."),
            _ => throw new McpException($"Category {id} could not be deleted.")
        };
    }

    private static string ValidateName(string name)
    {
        name = name.Trim();

        return name.Length is < 1 or > ProductsWriteTool.MaxCategoryNameLength
            ? throw new McpException($"`name` must be 1 to {ProductsWriteTool.MaxCategoryNameLength} characters.")
            : name;
    }

    private async Task EnsureNameIsFree(string name, int? exceptId, CancellationToken cancellationToken)
    {
        var categories = (await getCategoriesQueryHandler.Handle(new GetCategoriesQuery(), cancellationToken)).Categories;
        var sameName = categories.FirstOrDefault(c => c.Id != exceptId && string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));

        if (sameName is not null)
        {
            throw new McpException($"Category '{sameName.Name}' already exists with id {sameName.Id}.");
        }
    }
}
