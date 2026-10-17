using System.ComponentModel;
using FoodDiary.Application.Notes.Create;
using FoodDiary.Application.Notes.Delete;
using FoodDiary.Domain.Entities;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.Repositories.v2;
using FoodDiary.Domain.Utils;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace FoodDiary.API.Mcp.Tools.FoodLogs;

[McpServerToolType]
[Authorize(Policy = McpExtensions.McpWritePolicy)]
public class FoodLogsWriteTool(
    IProductsRepository productsRepository,
    INotesRepository notesRepository,
    CreateNoteCommandHandler createNoteCommandHandler,
    DeleteNoteCommandHandler deleteNoteCommandHandler,
    ICaloriesCalculator caloriesCalculator)
{
    internal const int MinQuantity = 1;
    internal const int MaxQuantity = 5000;

    private const string LogFoodToolDescription =
        """
        Logs one product eaten in one meal on one date, and returns the food log it created.

        Find `productId` with `list_products` first; products the person eats often have `isFavorite` set. When the product is not in the catalogue yet, create it with `create_product`, then log it.

        `quantity` is grams, drinks included. Omit it to log the product's `defaultQuantity`, its usual portion. When the person counts pieces or servings ("2 bowls", "10 dumplings"), convert them to grams from `defaultQuantity` or from what the person said.

        `date` is a calendar date with no time or timezone: use the person's local date. Meals are fixed slots: Breakfast, SecondBreakfast, Lunch, AfternoonSnack, Dinner.
        """;

    private const string DeleteFoodLogToolDescription =
        """
        Deletes one food log by its `id`, as returned by `get_food_logs` or `log_food`. Use it to undo a food log that was logged by mistake.
        """;

    [McpServerTool(Name = "log_food", ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false)]
    [Description(LogFoodToolDescription)]
    public async Task<LogFoodResponse> LogFood(
        [Description("Calendar date, `yyyy-MM-dd`.")] DateOnly date,
        [Description("Meal slot: Breakfast, SecondBreakfast, Lunch, AfternoonSnack or Dinner.")] string mealType,
        [Description("Id of the product eaten.")] int productId,
        [Description("Grams eaten, from 1 to 5000. Omit to log the product's defaultQuantity.")] int? quantity = null,
        CancellationToken cancellationToken = default)
    {
        if (quantity is < MinQuantity or > MaxQuantity)
        {
            throw new McpException($"Requested quantity {quantity}; it must be from {MinQuantity} to {MaxQuantity} grams.");
        }

        if (!Enum.TryParse<MealType>(mealType, ignoreCase: true, out var meal) || !Enum.IsDefined(meal) || int.TryParse(mealType, out _))
        {
            throw new McpException($"Unknown mealType '{mealType}'. Use Breakfast, SecondBreakfast, Lunch, AfternoonSnack or Dinner.");
        }

        var product = await productsRepository.FindById(productId, cancellationToken)
                      ?? throw new McpException($"Product {productId} not found. Look it up with `list_products`.");

        var notesOfDate = await notesRepository.FindByDate(date, cancellationToken);
        var displayOrder = notesOfDate
            .Where(note => note.MealType == meal)
            .Select(note => note.DisplayOrder + 1)
            .DefaultIfEmpty(0)
            .Max();

        var result = await createNoteCommandHandler.Handle(
            new CreateNoteCommand(date, meal, productId, quantity ?? product.DefaultQuantity, displayOrder),
            cancellationToken);

        if (result is not CreateNoteResult.Success success)
        {
            throw new McpException($"Product {productId} could not be logged.");
        }

        var note = new Note
        {
            Id = success.NoteId,
            Date = date,
            MealType = meal,
            ProductId = productId,
            ProductQuantity = quantity ?? product.DefaultQuantity,
            Product = product
        };

        return new LogFoodResponse(date, meal.ToString(), FoodLogsToolMapper.ToFoodLog(note, caloriesCalculator));
    }

    [McpServerTool(Name = "delete_food_log", ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = false)]
    [Description(DeleteFoodLogToolDescription)]
    public async Task<DeleteFoodLogResponse> DeleteFoodLog(
        [Description("Id of the food log to delete.")] int id,
        CancellationToken cancellationToken)
    {
        var result = await deleteNoteCommandHandler.Handle(new DeleteNoteCommand(id), cancellationToken);

        return result switch
        {
            DeleteNoteResult.Success => new DeleteFoodLogResponse(id),
            DeleteNoteResult.NotFound => throw new McpException($"Food log {id} not found."),
            _ => throw new McpException($"Food log {id} could not be deleted.")
        };
    }
}
