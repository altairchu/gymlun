using System.ComponentModel;
using System.Globalization;
using FoodDiary.Application.Notes.Create;
using FoodDiary.Application.Notes.CreateMany;
using FoodDiary.Application.Notes.Delete;
using FoodDiary.Application.Notes.Update;
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
    CreateNotesCommandHandler createNotesCommandHandler,
    UpdateNoteCommandHandler updateNoteCommandHandler,
    DeleteNotesCommandHandler deleteNotesCommandHandler,
    ICaloriesCalculator caloriesCalculator)
{
    internal const int MinQuantity = 1;
    internal const int MaxQuantity = 5000;
    internal const decimal MaxServings = 100;
    internal const int MaxLogFoodsItems = 20;
    internal const int MaxDeleteFoodLogsIds = 50;

    private static readonly string[] TimeFormats = ["H:mm", "H:mm:ss"];

    private const string LogFoodToolDescription =
        """
        Logs one product eaten at one time of day on one date, and returns the food log it created. To log several products eaten together, e.g. "rice, chicken and a glass of milk for lunch", use `log_foods` instead: one call logs them all.

        Find `productId` with `list_products` first; products the person eats often have `isFavorite` set. When the product is not in the catalogue yet, create it with `create_product`, then log it.

        How much was eaten follows the product's `countBy`. For a product counted by `grams`, pass `quantity` in grams, drinks included. For a product counted by `servings`, pass the number of pieces or servings as `servings`, e.g. 10 for "10 dumplings" or 1.5 for "one and a half bowls": it logs `servings` × `defaultQuantity` grams, rounded. Pass `quantity` or `servings`, never both; omit both to log one serving of `defaultQuantity` grams.

        `time` is when it was eaten, `HH:mm` in the person's local time, e.g. 12:30, and the meal follows from it: Breakfast from 04:00, SecondBreakfast from 10:00, Lunch from 12:00, AfternoonSnack from 15:00, Dinner from 18:00 until 04:00. When the person gives only a meal, pass `mealType` without `time` and the meal's usual time is logged: 08:00, 10:30, 12:30, 15:30 or 19:00. Pass both only when the meal the person names differs from the one its time falls in, e.g. a late lunch at 15:30.

        `date` is a calendar date with no time or timezone: use the person's local date.
        """;

    private const string LogFoodsToolDescription =
        """
        Logs several products eaten together, at one time of day on one date, and returns the food logs it created, in the order of `items`. Use it whenever the person names more than one food eaten at the same time, instead of calling `log_food` for each.

        `items` holds 1 to 20 products. In each one, `productId` and the amount work as in `log_food`: `quantity` in grams for a product counted by `grams`, `servings` for one counted by `servings`, never both; omit both to log one serving of `defaultQuantity` grams.

        `date`, `time` and `mealType` work as in `log_food` and apply to every item. Every item is checked before any is logged: when a product is not found or an amount is out of range, nothing is logged.
        """;

    private const string UpdateFoodLogToolDescription =
        """
        Changes one food log by its `id`, as returned by `get_food_logs`, `log_food` or `log_foods`, and returns it as it is now. Pass only what changes: everything else stays as it was.

        The amount works as in `log_food`: `quantity` in grams, or `servings` of the product's `defaultQuantity` grams, never both. `productId` swaps the product eaten and keeps the grams unless a new amount is passed too.

        `time` and `mealType` work as in `log_food`: with `time` the meal follows from it, with only `mealType` the food log moves to that meal's usual time. `date` moves it to another day and keeps its time.
        """;

    private const string DeleteFoodLogsToolDescription =
        """
        Deletes food logs by their `id`, as returned by `get_food_logs`, `log_food` or `log_foods`: pass every food log to delete in `ids`, 1 to 50 of them, in one call. Use it to undo food logs that were logged by mistake.

        When any of `ids` is not found, nothing is deleted.
        """;

    private const string MealTypeParameterDescription =
        "Meal slot: Breakfast, SecondBreakfast, Lunch, AfternoonSnack or Dinner. Omit it to follow from `time`.";

    [McpServerTool(Name = "log_food", ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false)]
    [Description(LogFoodToolDescription)]
    public async Task<LogFoodResponse> LogFood(
        [Description("Calendar date, `yyyy-MM-dd`.")] DateOnly date,
        [Description("Id of the product eaten.")] int productId,
        [Description("Grams eaten, from 1 to 5000. Omit it when passing `servings`.")] int? quantity = null,
        [Description("Pieces or servings eaten, more than 0 and at most 100, e.g. 1.5. Omit it when passing `quantity`.")] decimal? servings = null,
        [Description("Time of day it was eaten, `HH:mm`, in the person's local time.")] string? time = null,
        [Description(MealTypeParameterDescription)] string? mealType = null,
        CancellationToken cancellationToken = default)
    {
        ValidateAmount(quantity, servings);

        var (noteTime, meal) = RequireTimeAndMeal(time, mealType);
        var product = await FindProduct(productId, cancellationToken);
        var grams = ValidateGrams(quantity ?? ServingsToGrams(servings ?? 1, product));
        var displayOrder = await NextDisplayOrder(date, meal, cancellationToken);

        var result = await createNoteCommandHandler.Handle(
            new CreateNoteCommand(date, noteTime, meal, productId, grams, displayOrder),
            cancellationToken);

        if (result is not CreateNoteResult.Success success)
        {
            throw new McpException($"Product {productId} could not be logged.");
        }

        return new LogFoodResponse(date, meal.ToString(), ToFoodLog(success.NoteId, date, noteTime, meal, product, grams));
    }

    [McpServerTool(Name = "log_foods", ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false)]
    [Description(LogFoodsToolDescription)]
    public async Task<LogFoodsResponse> LogFoods(
        [Description("Calendar date, `yyyy-MM-dd`.")] DateOnly date,
        [Description("Products eaten, 1 to 20, each with its amount.")] LogFoodsItem[] items,
        [Description("Time of day they were eaten, `HH:mm`, in the person's local time.")] string? time = null,
        [Description(MealTypeParameterDescription)] string? mealType = null,
        CancellationToken cancellationToken = default)
    {
        if (items.Length is < 1 or > MaxLogFoodsItems)
        {
            throw new McpException($"Requested {items.Length} items; pass 1 to {MaxLogFoodsItems}.");
        }

        for (var index = 0; index < items.Length; index++)
        {
            if (AmountError(items[index].Quantity, items[index].Servings) is { } error)
            {
                throw new McpException($"{ItemName(items[index], index)}: {error}");
            }
        }

        var (noteTime, meal) = RequireTimeAndMeal(time, mealType);

        var productIds = items.Select(item => item.ProductId).Distinct().ToList();
        var products = (await productsRepository.FindByIds(productIds, cancellationToken)).ToDictionary(p => p.Id);
        var missingProductIds = productIds.Where(id => !products.ContainsKey(id)).ToList();

        if (missingProductIds.Count > 0)
        {
            throw new McpException(
                $"Products not found: {IdsArgument.Join(missingProductIds)}. Nothing was logged; look them up with `list_products`.");
        }

        var grams = items
            .Select((item, index) =>
            {
                var itemGrams = item.Quantity ?? ServingsToGrams(item.Servings ?? 1, products[item.ProductId]);

                return GramsError(itemGrams) is { } error
                    ? throw new McpException($"{ItemName(item, index)}: {error}")
                    : itemGrams;
            })
            .ToList();

        var displayOrder = await NextDisplayOrder(date, meal, cancellationToken);

        var result = await createNotesCommandHandler.Handle(
            new CreateNotesCommand(
                date,
                noteTime,
                items.Select((item, index) => new CreateNotesCommand.Item(item.ProductId, grams[index])).ToList(),
                meal,
                displayOrder),
            cancellationToken);

        if (result is not CreateNotesResult.Success success)
        {
            throw new McpException("The products could not be logged.");
        }

        var foodLogs = success.NoteIds
            .Select((noteId, index) => ToFoodLog(noteId, date, noteTime, meal, products[items[index].ProductId], grams[index]))
            .ToList();

        return new LogFoodsResponse(date, meal.ToString(), foodLogs);
    }

    [McpServerTool(Name = "update_food_log", ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = false)]
    [Description(UpdateFoodLogToolDescription)]
    public async Task<UpdateFoodLogResponse> UpdateFoodLog(
        [Description("Id of the food log to change.")] int id,
        [Description("New calendar date, `yyyy-MM-dd`.")] DateOnly? date = null,
        [Description("New time of day it was eaten, `HH:mm`, in the person's local time.")] string? time = null,
        [Description("New meal slot: Breakfast, SecondBreakfast, Lunch, AfternoonSnack or Dinner.")] string? mealType = null,
        [Description("New grams eaten, from 1 to 5000. Omit it when passing `servings`.")] int? quantity = null,
        [Description("New pieces or servings eaten, more than 0 and at most 100. Omit it when passing `quantity`.")] decimal? servings = null,
        [Description("Id of the product eaten instead.")] int? productId = null,
        CancellationToken cancellationToken = default)
    {
        ValidateAmount(quantity, servings);

        var note = await notesRepository.FindById(id, cancellationToken)
                   ?? throw new McpException($"Food log {id} not found. Find it with `get_food_logs`.");

        var (noteTime, meal) = MealType.ResolveTimeAndMeal(ParseTime(time), ParseMealType(mealType))
                               ?? (note.Time, note.MealType);

        var product = await FindProduct(productId ?? note.ProductId, cancellationToken);
        var grams = ValidateGrams(quantity ?? (servings is { } count ? ServingsToGrams(count, product) : note.ProductQuantity));
        var noteDate = date ?? note.Date;

        var displayOrder = noteDate == note.Date && meal == note.MealType
            ? note.DisplayOrder
            : await NextDisplayOrder(noteDate, meal, cancellationToken);

        var result = await updateNoteCommandHandler.Handle(
            new UpdateNoteCommand(id, noteDate, noteTime, meal, product.Id, grams, displayOrder),
            cancellationToken);

        if (result is not UpdateNoteResult.Success)
        {
            throw new McpException($"Food log {id} could not be changed.");
        }

        return new UpdateFoodLogResponse(noteDate, meal.ToString(), ToFoodLog(id, noteDate, noteTime, meal, product, grams));
    }

    [McpServerTool(Name = "delete_food_logs", ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = false)]
    [Description(DeleteFoodLogsToolDescription)]
    public async Task<DeleteFoodLogsResponse> DeleteFoodLogs(
        [Description("Ids of the food logs to delete, 1 to 50.")] int[] ids,
        CancellationToken cancellationToken)
    {
        var distinctIds = IdsArgument.Distinct(ids, MaxDeleteFoodLogsIds);
        var foundNotes = await notesRepository.FindByIds(distinctIds, cancellationToken);
        var missingIds = distinctIds.Except(foundNotes.Select(note => note.Id)).ToList();

        if (missingIds.Count > 0)
        {
            throw new McpException(
                $"Food logs not found: {IdsArgument.Join(missingIds)}. Nothing was deleted; find them with `get_food_logs`.");
        }

        await deleteNotesCommandHandler.Handle(new DeleteNotesCommand(distinctIds), cancellationToken);

        return new DeleteFoodLogsResponse(distinctIds);
    }

    private static void ValidateAmount(int? quantity, decimal? servings)
    {
        if (AmountError(quantity, servings) is { } error)
        {
            throw new McpException(error);
        }
    }

    private static string? AmountError(int? quantity, decimal? servings)
    {
        if (quantity is not null && servings is not null)
        {
            return "Pass either `quantity` or `servings`, not both.";
        }

        if (quantity is < MinQuantity or > MaxQuantity)
        {
            return $"Requested quantity {quantity}; it must be from {MinQuantity} to {MaxQuantity} grams.";
        }

        if (servings is <= 0 or > MaxServings)
        {
            return $"Requested servings {servings}; it must be more than 0 and at most {MaxServings}.";
        }

        return null;
    }

    private static int ValidateGrams(int grams) => GramsError(grams) is { } error
        ? throw new McpException(error)
        : grams;

    private static string? GramsError(int grams) => grams is < MinQuantity or > MaxQuantity
        ? $"That is {grams} g; it must be from {MinQuantity} to {MaxQuantity} grams. Check `servings`."
        : null;

    private static string ItemName(LogFoodsItem item, int index) => $"`items[{index}]` (product {item.ProductId})";

    private static (TimeOnly Time, MealType MealType) RequireTimeAndMeal(string? time, string? mealType) =>
        MealType.ResolveTimeAndMeal(ParseTime(time), ParseMealType(mealType))
        ?? throw new McpException("Pass `time`, `mealType` or both.");

    private async Task<Product> FindProduct(int productId, CancellationToken cancellationToken) =>
        await productsRepository.FindById(productId, cancellationToken)
        ?? throw new McpException($"Product {productId} not found. Look it up with `list_products`.");

    private async Task<int> NextDisplayOrder(DateOnly date, MealType meal, CancellationToken cancellationToken)
    {
        var notesOfDate = await notesRepository.FindByDate(date, cancellationToken);

        return notesOfDate
            .Where(note => note.MealType == meal)
            .Select(note => note.DisplayOrder + 1)
            .DefaultIfEmpty(0)
            .Max();
    }

    private static int ServingsToGrams(decimal servings, Product product) =>
        (int)Math.Round(servings * product.DefaultQuantity, MidpointRounding.AwayFromZero);

    private FoodLog ToFoodLog(int id, DateOnly date, TimeOnly time, MealType meal, Product product, int grams) =>
        FoodLogsToolMapper.ToFoodLog(
            new Note
            {
                Id = id,
                Date = date,
                Time = time,
                MealType = meal,
                ProductId = product.Id,
                ProductQuantity = grams,
                Product = product
            },
            caloriesCalculator);

    private static TimeOnly? ParseTime(string? time)
    {
        if (string.IsNullOrWhiteSpace(time))
        {
            return null;
        }

        return TimeOnly.TryParseExact(time.Trim(), TimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed
            : throw new McpException($"Unknown time '{time}'. Use `HH:mm`, e.g. 12:30.");
    }

    private static MealType? ParseMealType(string? mealType)
    {
        if (string.IsNullOrWhiteSpace(mealType))
        {
            return null;
        }

        if (!Enum.TryParse<MealType>(mealType, ignoreCase: true, out var meal) || !Enum.IsDefined(meal) || int.TryParse(mealType, out _))
        {
            throw new McpException($"Unknown mealType '{mealType}'. Use Breakfast, SecondBreakfast, Lunch, AfternoonSnack or Dinner.");
        }

        return meal;
    }
}
