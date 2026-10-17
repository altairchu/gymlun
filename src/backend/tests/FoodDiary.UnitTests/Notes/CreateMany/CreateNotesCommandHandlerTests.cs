using FoodDiary.Application.Notes.CreateMany;
using FoodDiary.Domain.Entities;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.Repositories.v2;
using Moq;

namespace FoodDiary.UnitTests.Notes.CreateMany;

public class CreateNotesCommandHandlerTests
{
    private static readonly DateOnly Date = new(2026, 10, 17);

    private readonly Mock<IProductsRepository> _productsRepository = new();
    private readonly Mock<INotesRepository> _notesRepository = new();
    private readonly List<Product> _products = [];
    private IReadOnlyCollection<Note> _addedNotes = [];

    public CreateNotesCommandHandlerTests()
    {
        _productsRepository
            .Setup(r => r.FindByIds(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<int> ids, CancellationToken _) =>
                _products.Where(p => ids.Contains(p.Id)).ToArray());
        _notesRepository
            .Setup(r => r.AddRange(It.IsAny<IReadOnlyCollection<Note>>(), It.IsAny<CancellationToken>()))
            .Callback((IReadOnlyCollection<Note> notes, CancellationToken _) =>
            {
                var id = 100;
                foreach (var note in notes)
                {
                    note.Id = ++id;
                }

                _addedNotes = notes;
            })
            .Returns(Task.CompletedTask);
    }

    private CreateNotesCommandHandler Handler => new(_productsRepository.Object, _notesRepository.Object);

    private void GivenProducts(params int[] ids) =>
        _products.AddRange(ids.Select(id => new Product
        {
            Id = id,
            Name = $"Product {id}",
            Protein = null,
            Fats = null,
            Carbs = null,
            Sugar = null,
            Salt = null
        }));

    [Fact]
    public async Task Handle_KnownProducts_AddsEveryNoteAtOnceWithMealFromTimeAndOrderFromIndex()
    {
        GivenProducts(1, 2);
        var command = new CreateNotesCommand(Date, new TimeOnly(12, 45), [new(2, 250), new(1, 150), new(2, 50)]);

        var result = await Handler.Handle(command, CancellationToken.None);

        result.Should().BeOfType<CreateNotesResult.Success>()
            .Which.NoteIds.Should().Equal(101, 102, 103);
        _addedNotes.Select(n => (n.ProductId, n.ProductQuantity, n.DisplayOrder)).Should().Equal(
            (2, 250, 0),
            (1, 150, 1),
            (2, 50, 2));
        _addedNotes.Should().AllSatisfy(note =>
        {
            note.Date.Should().Be(Date);
            note.Time.Should().Be(new TimeOnly(12, 45));
            note.MealType.Should().Be(MealType.Lunch);
        });
        _notesRepository.Verify(
            r => r.AddRange(It.IsAny<IReadOnlyCollection<Note>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_UnknownProduct_ReturnsValidationErrorAndAddsNothing()
    {
        GivenProducts(1);
        var command = new CreateNotesCommand(Date, new TimeOnly(8, 0), [new(1, 100), new(7, 100)]);

        var result = await Handler.Handle(command, CancellationToken.None);

        result.Should().BeOfType<CreateNotesResult.Failure>()
            .Which.Error.Description.Should().Contain("7");
        _notesRepository.Verify(
            r => r.AddRange(It.IsAny<IReadOnlyCollection<Note>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
