using System.Text.Json;
using System.Text.Json.Nodes;
using FoodDiary.API.Mcp.Tools.FoodLogs;
using FoodDiary.Application.Notes.Create;
using FoodDiary.Application.Notes.Delete;
using FoodDiary.Application.Notes.Update;
using FoodDiary.Domain.Entities;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.Repositories;
using FoodDiary.Domain.Repositories.v2;
using FoodDiary.Domain.Utils;
using FoodDiary.Infrastructure.Utils;
using ModelContextProtocol;
using Moq;

namespace FoodDiary.UnitTests.Mcp;

public class FoodLogsWriteToolTests
{
    private static readonly DateOnly September1 = new(2026, 9, 1);
    private static readonly DateOnly September2 = new(2026, 9, 2);

    private readonly Mock<IProductsRepository> _productsRepository = new();
    private readonly Mock<INotesRepository> _notesRepository = new();
    private readonly List<Note> _addedNotes = [];
    private Note? _updatedNote;

    private static readonly Product Dumpling = new()
    {
        Id = 7,
        Name = "餃子",
        CaloriesCost = 220,
        DefaultQuantity = 25,
        QuantityUnit = QuantityUnit.Servings,
        Protein = 9m,
        Fats = null,
        Carbs = null,
        Sugar = null,
        Salt = null
    };

    public FoodLogsWriteToolTests()
    {
        _productsRepository
            .Setup(r => r.FindById(Dumpling.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Dumpling);
        _notesRepository
            .Setup(r => r.FindByDate(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => _addedNotes);
        _notesRepository
            .Setup(r => r.Add(It.IsAny<Note>(), It.IsAny<CancellationToken>()))
            .Callback((Note note, CancellationToken _) =>
            {
                note.Id = _addedNotes.Count + 1;
                _addedNotes.Add(note);
            })
            .Returns(Task.CompletedTask);
        _notesRepository
            .Setup(r => r.Update(It.IsAny<Note>(), It.IsAny<CancellationToken>()))
            .Callback((Note note, CancellationToken _) => _updatedNote = note)
            .Returns(Task.CompletedTask);
    }

    private FoodLogsWriteTool CreateTool() => new(
        _productsRepository.Object,
        _notesRepository.Object,
        new CreateNoteCommandHandler(_productsRepository.Object, _notesRepository.Object),
        new UpdateNoteCommandHandler(_productsRepository.Object, _notesRepository.Object),
        new DeleteNoteCommandHandler(Mock.Of<INoteRepository>(), Mock.Of<INotesOrderCalculator>()),
        new CaloriesCalculator());

    private async Task<JsonNode> LogFood(
        int? quantity = null,
        decimal? servings = null,
        string? time = null,
        string? mealType = null)
    {
        var response = await CreateTool().LogFood(September1, Dumpling.Id, quantity, servings, time, mealType, CancellationToken.None);

        return JsonSerializer.SerializeToNode(response, McpJsonUtilities.DefaultOptions)!;
    }

    [Fact]
    public async Task Servings_AreLoggedAsGramsOfDefaultQuantity_RoundedHalfAwayFromZero()
    {
        var json = await LogFood(servings: 2.5m, time: "12:45");

        json["foodLog"]!["quantity"]!.GetValue<int>().Should().Be(63);
        _addedNotes.Single().ProductQuantity.Should().Be(63);
    }

    [Fact]
    public async Task NeitherQuantityNorServings_LogsOneServing()
    {
        var json = await LogFood(mealType: "Dinner");

        json["foodLog"]!["quantity"]!.GetValue<int>().Should().Be(25);
    }

    [Theory]
    [InlineData("12:45", "12:45")]
    [InlineData("7:05", "07:05")]
    [InlineData("21:10:30", "21:10")]
    public async Task Time_IsLoggedAndReturnedAsHoursAndMinutes(string time, string expected)
    {
        var json = await LogFood(time: time);

        json["foodLog"]!["time"]!.GetValue<string>().Should().Be(expected);
        _addedNotes.Single().Time.Should().Be(TimeOnly.Parse(time));
    }

    [Fact]
    public async Task TimeWithoutMealType_DerivesMeal()
    {
        var json = await LogFood(time: "16:00");

        json["mealType"]!.GetValue<string>().Should().Be("AfternoonSnack");
        _addedNotes.Single().MealType.Should().Be(MealType.AfternoonSnack);
    }

    [Fact]
    public async Task MealTypeWithoutTime_LogsMealDefaultTime()
    {
        var json = await LogFood(mealType: "lunch");

        json["mealType"]!.GetValue<string>().Should().Be("Lunch");
        json["foodLog"]!["time"]!.GetValue<string>().Should().Be("12:30");
    }

    [Fact]
    public async Task TimeAndMealType_AreBothKeptAsGiven()
    {
        await LogFood(time: "15:30", mealType: "Lunch");

        _addedNotes.Single().Should().BeEquivalentTo(new { Time = new TimeOnly(15, 30), MealType = MealType.Lunch });
    }

    [Theory]
    [InlineData(100, 1.0, null, null, "Pass either `quantity` or `servings`, not both.")]
    [InlineData(null, 0.0, "12:00", null, "Requested servings 0")]
    [InlineData(null, 100.5, "12:00", null, "Requested servings 100.5")]
    [InlineData(null, null, null, null, "Pass `time`, `mealType` or both.")]
    [InlineData(null, null, "noon", null, "Unknown time 'noon'. Use `HH:mm`, e.g. 12:30.")]
    [InlineData(null, null, "25:00", null, "Unknown time '25:00'")]
    [InlineData(null, null, null, "Supper", "Unknown mealType 'Supper'")]
    [InlineData(0, null, "12:00", null, "Requested quantity 0; it must be from 1 to 5000 grams.")]
    public async Task InvalidArguments_AreToolErrors(int? quantity, double? servings, string? time, string? mealType, string message)
    {
        var act = () => LogFood(quantity, (decimal?)servings, time, mealType);

        await act.Should().ThrowExactlyAsync<McpException>().WithMessage($"{message}*");
        _addedNotes.Should().BeEmpty();
    }

    [Fact]
    public async Task ServingsAboveMaxGrams_IsToolError()
    {
        var heavy = new Product
        {
            Id = 8,
            Name = "Whole cake",
            DefaultQuantity = 1000,
            Protein = null,
            Fats = null,
            Carbs = null,
            Sugar = null,
            Salt = null
        };
        _productsRepository.Setup(r => r.FindById(heavy.Id, It.IsAny<CancellationToken>())).ReturnsAsync(heavy);

        var act = () => CreateTool().LogFood(September1, heavy.Id, servings: 6, time: "19:00", cancellationToken: CancellationToken.None);

        await act.Should().ThrowExactlyAsync<McpException>()
            .WithMessage("That is 6000 g; it must be from 1 to 5000 grams. Check `servings`.");
    }

    private Note GivenLoggedLunch()
    {
        var lunch = new Note
        {
            Id = 41,
            Date = September1,
            Time = new TimeOnly(12, 30),
            MealType = MealType.Lunch,
            ProductId = Dumpling.Id,
            ProductQuantity = 50,
            DisplayOrder = 2,
            Product = Dumpling
        };
        _notesRepository.Setup(r => r.FindById(lunch.Id, It.IsAny<CancellationToken>())).ReturnsAsync(lunch);

        return lunch;
    }

    private async Task<JsonNode> UpdateFoodLog(
        int id,
        DateOnly? date = null,
        string? time = null,
        string? mealType = null,
        int? quantity = null,
        decimal? servings = null)
    {
        var response = await CreateTool().UpdateFoodLog(id, date, time, mealType, quantity, servings, cancellationToken: CancellationToken.None);

        return JsonSerializer.SerializeToNode(response, McpJsonUtilities.DefaultOptions)!;
    }

    [Fact]
    public async Task UpdatingOnlyTime_MovesToTheMealOfThatTime_AndKeepsTheRest()
    {
        var lunch = GivenLoggedLunch();

        var json = await UpdateFoodLog(lunch.Id, time: "15:45");

        json["mealType"]!.GetValue<string>().Should().Be("AfternoonSnack");
        json["foodLog"]!["time"]!.GetValue<string>().Should().Be("15:45");
        json["foodLog"]!["quantity"]!.GetValue<int>().Should().Be(50);
        _updatedNote.Should().BeEquivalentTo(new
        {
            Date = September1,
            Time = new TimeOnly(15, 45),
            MealType = MealType.AfternoonSnack,
            ProductQuantity = 50
        });
    }

    [Fact]
    public async Task UpdatingServings_LogsServingsOfDefaultQuantity_AndKeepsTimeAndOrder()
    {
        var lunch = GivenLoggedLunch();

        var json = await UpdateFoodLog(lunch.Id, servings: 4);

        json["foodLog"]!["quantity"]!.GetValue<int>().Should().Be(100);
        _updatedNote.Should().BeEquivalentTo(new { Time = new TimeOnly(12, 30), MealType = MealType.Lunch, DisplayOrder = 2 });
    }

    [Fact]
    public async Task UpdatingDate_KeepsTime_AndPutsItLastInThatMeal()
    {
        var lunch = GivenLoggedLunch();
        _notesRepository
            .Setup(r => r.FindByDate(September2, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Note { Id = 50, Date = September2, MealType = MealType.Lunch, DisplayOrder = 3 }]);

        var json = await UpdateFoodLog(lunch.Id, date: September2);

        json["date"]!.GetValue<string>().Should().Be("2026-09-02");
        _updatedNote.Should().BeEquivalentTo(new { Date = September2, Time = new TimeOnly(12, 30), DisplayOrder = 4 });
    }

    [Fact]
    public async Task UpdatingUnknownFoodLog_IsToolError()
    {
        var act = () => UpdateFoodLog(404, quantity: 100);

        await act.Should().ThrowExactlyAsync<McpException>().WithMessage("Food log 404 not found. Find it with `get_food_logs`.");
        _updatedNote.Should().BeNull();
    }
}
