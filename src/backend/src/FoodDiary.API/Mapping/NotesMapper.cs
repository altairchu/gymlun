using FoodDiary.Application.Notes.Create;
using FoodDiary.Application.Notes.CreateMany;
using FoodDiary.Application.Notes.Get;
using FoodDiary.Application.Notes.GetHistory;
using FoodDiary.Application.Notes.Update;
using FoodDiary.Contracts.Notes;
using FoodDiary.Domain.Entities;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.Utils;

namespace FoodDiary.API.Mapping;

public static class NotesMapper
{
    public static GetNotesResponse.Note ToGetNotesResponse(this Note note) => new(
        Id: note.Id,
        Date: note.Date,
        Time: note.Time,
        MealType: note.MealType,
        DisplayOrder: note.DisplayOrder,
        ProductQuantity: note.ProductQuantity,
        Product: note.Product!.ToGetNotesResponse());

    private static GetNotesResponse.Product ToGetNotesResponse(this Product product) => new(
        Id: product.Id,
        Name: product.Name,
        DefaultQuantity: product.DefaultQuantity,
        QuantityUnit: product.QuantityUnit,
        Calories: product.CaloriesCost,
        Protein: product.Protein,
        Fats: product.Fats,
        Carbs: product.Carbs,
        Sugar: product.Sugar,
        Salt: product.Salt);

    public static GetNotesQuery ToGetNotesQuery(this GetNotesRequest request) => new(
        request.Date.GetValueOrDefault()
    );

    public static GetNotesHistoryQuery ToGetNotesHistoryQuery(this GetNotesHistoryRequest request) => new(
        request.From.GetValueOrDefault(),
        request.To.GetValueOrDefault()
    );

    public static GetNotesResponse ToGetNotesResponse(this GetNotesQueryResult result) => new(
        Notes: result.Notes
            .Select(n => n.ToGetNotesResponse())
            .ToList());

    public static GetNotesHistoryResponse ToGetNotesHistoryResponse(
        this GetNotesHistoryQueryResult result,
        ICaloriesCalculator calculator) => new(
        result.Notes
            .GroupBy(n => n.Date)
            .Select(g => new NotesHistoryItem(
                Date: g.Key,
                CaloriesCount: calculator.Calculate(g.ToArray())))
            .ToList()
    );

    public static CreateNoteCommand ToCreateNoteCommand(this NoteRequestBody body)
    {
        var (time, mealType) = body.ResolveTimeAndMeal();

        return new CreateNoteCommand(
            body.Date.GetValueOrDefault(),
            time,
            mealType,
            body.ProductId,
            body.ProductQuantity,
            body.DisplayOrder);
    }

    public static UpdateNoteCommand ToUpdateNoteCommand(this NoteRequestBody body, int id)
    {
        var (time, mealType) = body.ResolveTimeAndMeal();

        return new UpdateNoteCommand(
            id,
            body.Date.GetValueOrDefault(),
            time,
            mealType,
            body.ProductId,
            body.ProductQuantity,
            body.DisplayOrder);
    }

    public static CreateNotesCommand ToCreateNotesCommand(this CreateNotesRequest request) => new(
        request.Date.GetValueOrDefault(),
        request.Time.GetValueOrDefault().ToWholeSeconds(),
        request.Items
            .Select(item => new CreateNotesCommand.Item(item.ProductId, item.ProductQuantity))
            .ToList());

    public static CreateNotesResponse ToCreateNotesResponse(this CreateNotesResult.Success result) => new(result.NoteIds);

    private static (TimeOnly Time, MealType MealType) ResolveTimeAndMeal(this NoteRequestBody body) =>
        MealType.ResolveTimeAndMeal(body.Time?.ToWholeSeconds(), body.MealType)
        ?? throw new InvalidOperationException("Note request body was validated to carry a time or a meal type");

    // System.Text.Json reads fractional seconds and writes them back, which would break the `HH:mm:ss` contract
    private static TimeOnly ToWholeSeconds(this TimeOnly time) => new(time.Hour, time.Minute, time.Second);
}
