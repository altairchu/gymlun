using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FoodDiary.API.Mapping;
using FoodDiary.Application.Notes.Recognize;
using FoodDiary.ComponentTests.Dsl;
using FoodDiary.ComponentTests.Infrastructure;
using FoodDiary.ComponentTests.Infrastructure.ExternalServices;
using FoodDiary.Contracts.Notes;
using FoodDiary.Domain.Entities;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.ComponentTests.Scenarios.Notes;

[UsedImplicitly]
public class NotesApiContext(
    FoodDiaryWebApplicationFactory factory,
    ExternalServicesFixture externalServices) : BaseContext(factory)
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private OpenAIApi OpenAiApi => externalServices.OpenAiApi;
    private GetNotesResponse? _getNotesResponse;
    private GetNotesHistoryResponse? _getNotesHistoryResponse;
    private HttpResponseMessage _createNoteResponse = null!;
    private HttpResponseMessage _updateNoteResponse = null!;
    private HttpResponseMessage _deleteNoteResponse = null!;
    private HttpResponseMessage _deleteMultipleNotesResponse = null!;
    private HttpResponseMessage _recognizeNoteResponse = null!;
    private HttpResponseMessage _createNotesResponse = null!;
    private JsonDocument? _rawNotesResponse;

    public Task Given_notes(params Note[] notes)
    {
        return Factory.SeedDataAsync(notes);
    }

    public Task Given_product(Product product)
    {
        return Factory.SeedDataAsync([product]);
    }

    public Task Given_products(params Product[] products)
    {
        return Factory.SeedDataAsync(products);
    }

    public Task Given_OpenAI_api_is_ready()
    {
        return OpenAiApi.Start();
    }
    
    public Task Given_OpenAI_api_can_recognize_food(RecognizeNoteModelResponse food)
    {
        var content = JsonSerializer.Serialize(food, SerializerOptions);
        return OpenAiApi.SetupCompletionSuccess(content);
    }
    
    public Task Given_OpenAI_completion_response_is_not_recognized_notes_json()
    {
        return OpenAiApi.SetupCompletionSuccess("Sorry, I didn't understand that.");
    }
    
    public Task Given_OpenAI_request_failed_with_error(HttpStatusCode error)
    {
        return OpenAiApi.SetupCompletionFailure(error);
    }

    public async Task When_user_retrieves_notes_list_for_date(string date)
    {
        _getNotesResponse = await ApiClient.GetFromJsonAsync<GetNotesResponse>($"/api/v1/notes?date={date}");
    }
    
    public async Task When_user_retrieves_notes_list_json_for_date(string date)
    {
        var json = await ApiClient.GetStringAsync($"/api/v1/notes?date={date}");
        _rawNotesResponse = JsonDocument.Parse(json);
    }

    public async Task When_user_retrieves_notes_history(string from, string to)
    {
        _getNotesHistoryResponse = await ApiClient
            .GetFromJsonAsync<GetNotesHistoryResponse>($"/api/v1/notes/history?from={from}&to={to}");
    }

    public async Task When_user_creates_note(Note note)
    {
        var body = Create.NoteRequestBody()
            .From(note)
            .Please();
        
        _createNoteResponse = await ApiClient.PostAsJsonAsync("/api/v1/notes", body);
    }

    public async Task When_user_creates_note_with_time_only(Note note)
    {
        var body = Create.NoteRequestBody()
            .From(note)
            .WithoutMealType()
            .Please();

        _createNoteResponse = await ApiClient.PostAsJsonAsync("/api/v1/notes", body);
    }

    public async Task When_user_creates_note_with_meal_type_only(Note note)
    {
        var body = Create.NoteRequestBody()
            .From(note)
            .WithoutTime()
            .Please();

        _createNoteResponse = await ApiClient.PostAsJsonAsync("/api/v1/notes", body);
    }

    public async Task When_user_creates_note_without_time_and_meal_type(Note note)
    {
        var body = Create.NoteRequestBody()
            .From(note)
            .WithoutTime()
            .WithoutMealType()
            .Please();

        _createNoteResponse = await ApiClient.PostAsJsonAsync("/api/v1/notes", body);
    }

    public async Task When_user_creates_note_from_json(string json)
    {
        _createNoteResponse = await ApiClient.PostAsync(
            "/api/v1/notes",
            new StringContent(json, Encoding.UTF8, "application/json"));
    }

    public async Task When_user_logs_products_eaten_together(string date, string time, params Note[] notes)
    {
        var request = new CreateNotesRequest
        {
            Date = DateOnly.Parse(date),
            Time = TimeOnly.Parse(time),
            Items = notes
                .Select(note => new CreateNotesRequestItem
                {
                    ProductId = note.Product!.Id,
                    ProductQuantity = note.ProductQuantity
                })
                .ToList()
        };

        _createNotesResponse = await ApiClient.PostAsJsonAsync("/api/v1/notes/batch", request);
    }
    
    public async Task When_user_updates_product_with_quantity_for_note(Note note, Product product, int quantity)
    {
        var body = Create.NoteRequestBody()
            .From(note)
            .WithProduct(product)
            .WithProductQuantity(quantity)
            .Please();
        
        _updateNoteResponse = await ApiClient.PutAsJsonAsync($"/api/v1/notes/{note.Id}", body);
    }

    public async Task When_user_deletes_note(Note note)
    {
        _deleteNoteResponse = await ApiClient.DeleteAsync($"/api/v1/notes/{note.Id}");
    }

    public async Task When_user_deletes_notes(params Note[] notes)
    {
        var noteIds = notes.Select(n => n.Id);

        var request = new HttpRequestMessage(HttpMethod.Delete, "api/v1/notes/batch")
        {
            Content = new StringContent(JsonSerializer.Serialize(noteIds), Encoding.Unicode, "application/json")
        };

        _deleteMultipleNotesResponse = await ApiClient.SendAsync(request);
    }

    public async Task When_user_uploads_file_for_note_recognition(string file)
    {
        var filePath = Path.Combine("Scenarios", "Notes", file);
        await using var stream = File.OpenRead(filePath);
        using var content = new MultipartFormDataContent();
        content.Add(new StreamContent(stream) { Headers = { { "Content-Type", "image/png" } } }, "files", file);
        
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri("/api/v1/notes/recognitions", UriKind.Relative),
            Content = content
        };
        
        _recognizeNoteResponse = await ApiClient.SendAsync(request);
    }

    public Task Then_notes_list_contains_items(params Note[] items)
    {
        var expectedNotesList = items.Select(n => n.ToGetNotesResponse());

        _getNotesResponse.Should().NotBeNull();
        _getNotesResponse!.Notes.Should()
            .BeEquivalentTo(expectedNotesList, options => options
                .Excluding(note => note.Id)
                .WithStrictOrdering())
            .And.AllSatisfy(note => { note.Product.Calories.Should().BePositive(); });
        
        return Task.CompletedTask;
    }
    
    public Task Then_notes_list_contains_no_items()
    {
        _getNotesResponse?.Notes.Should().BeEmpty();
        return Task.CompletedTask;
    }
    
    public Task Then_notes_history_contains_items(params NotesHistoryItem[] items)
    {
        _getNotesHistoryResponse?.NotesHistory.Should().BeEquivalentTo(items);
        return Task.CompletedTask;
    }

    public Task Then_note_is_successfully_created()
    {
        _createNoteResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        return Task.CompletedTask;
    }
    
    public async Task Then_note_is_not_created_because_time_and_meal_type_are_missing()
    {
        _createNoteResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await _createNoteResponse.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem!.Errors.Should().ContainKey(nameof(NoteRequestBody.Time));
    }

    public Task Then_note_json_has_time_and_meal_type(string time, int mealType)
    {
        var note = _rawNotesResponse!.RootElement.GetProperty("notes").EnumerateArray().Single();
        note.GetProperty("time").GetString().Should().Be(time);
        note.GetProperty("mealType").GetInt32().Should().Be(mealType);
        return Task.CompletedTask;
    }

    public async Task Then_products_eaten_together_are_logged(int count)
    {
        _createNotesResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var response = await _createNotesResponse.Content.ReadFromJsonAsync<CreateNotesResponse>();
        response!.Ids.Should().HaveCount(count).And.OnlyHaveUniqueItems().And.AllSatisfy(id => id.Should().BePositive());
    }

    public Task Then_products_eaten_together_are_not_logged()
    {
        _createNotesResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        return Task.CompletedTask;
    }

    public Task Then_note_is_successfully_updated()
    {
        _updateNoteResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        return Task.CompletedTask;
    }
    
    public Task Then_note_is_successfully_deleted()
    {
        _deleteNoteResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        return Task.CompletedTask;
    }

    public Task Then_multiple_notes_are_successfully_deleted()
    {
        _deleteMultipleNotesResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        return Task.CompletedTask;
    }

    public async Task Then_note_is_successfully_recognized_as(Product product, int quantity)
    {
        var response = await _recognizeNoteResponse.Content.ReadFromJsonAsync<RecognizeNoteResponse>();
        var recognizedNote = response?.Notes[0];

        recognizedNote.Should().NotBeNull();
        recognizedNote!.Quantity.Should().Be(quantity);
        recognizedNote.Product.Name.Should().Be(product.Name);
        recognizedNote.Product.CaloriesCost.Should().Be(product.CaloriesCost);
        recognizedNote.Product.Protein.Should().Be(product.Protein);
        recognizedNote.Product.Fats.Should().Be(product.Fats);
        recognizedNote.Product.Carbs.Should().Be(product.Carbs);
        recognizedNote.Product.Sugar.Should().Be(product.Sugar);
        recognizedNote.Product.Salt.Should().Be(product.Salt);
    }

    public async Task Then_recognize_note_response_returns_error(HttpStatusCode statusCode)
    {
        _recognizeNoteResponse.StatusCode.Should().Be(statusCode);
        var response = await _recognizeNoteResponse.Content.ReadFromJsonAsync<ProblemDetails>();
        response!.Title.Should().NotBeNullOrWhiteSpace();
        response.Detail.Should().NotBeNullOrWhiteSpace();
    }
}