using FoodDiary.Domain.Entities;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.Repositories.v2;

namespace FoodDiary.Application.Notes.CreateMany;

public record CreateNotesCommand(DateOnly Date, TimeOnly Time, IReadOnlyList<CreateNotesCommand.Item> Items)
{
    public record Item(int ProductId, int ProductQuantity);
}

public abstract record CreateNotesResult
{
    public record Success(IReadOnlyList<int> NoteIds) : CreateNotesResult;

    public record Failure(Error Error) : CreateNotesResult;

    public static Failure ProductsNotFound(IEnumerable<int> productIds) =>
        new(new Error.ValidationError($"Products with ids {string.Join(", ", productIds)} not found"));
}

public class CreateNotesCommandHandler(IProductsRepository productsRepository, INotesRepository notesRepository)
{
    public async Task<CreateNotesResult> Handle(CreateNotesCommand command, CancellationToken cancellationToken)
    {
        var productIds = command.Items.Select(item => item.ProductId).Distinct().ToList();
        var products = await productsRepository.FindByIds(productIds, cancellationToken);
        var missingProductIds = productIds.Except(products.Select(p => p.Id)).ToList();

        if (missingProductIds.Count > 0)
        {
            return CreateNotesResult.ProductsNotFound(missingProductIds);
        }

        var mealType = MealType.FromTime(command.Time);

        var notes = command.Items
            .Select((item, index) => new Note
            {
                Date = command.Date,
                Time = command.Time,
                MealType = mealType,
                ProductId = item.ProductId,
                ProductQuantity = item.ProductQuantity,
                DisplayOrder = index
            })
            .ToList();

        await notesRepository.AddRange(notes, cancellationToken);
        return new CreateNotesResult.Success(notes.Select(note => note.Id).ToList());
    }
}
