using System.Net;
using FoodDiary.ComponentTests.Dsl;
using FoodDiary.ComponentTests.Infrastructure;
using FoodDiary.Domain.Enums;

namespace FoodDiary.ComponentTests.Scenarios.Notes;

public class NotesApiTests(InfrastructureFixture infrastructure) : BaseTest<NotesApiContext>(infrastructure)
{    
    [Scenario]
    public Task I_can_retrieve_notes_list()
    {
        var breakfast = Create.Note().WithDate("2024-01-04").WithMealType(MealType.Breakfast);
        var lunch = Create.Note().WithDate("2024-01-04").WithMealType(MealType.Lunch);
        var notes = new
        {
            Chicken = breakfast.WithProduct("Chicken", 150).WithDisplayOrder(0).Please(),
            Rice = breakfast.WithProduct("Rice", 100).WithDisplayOrder(1).Please(),
            Cheese = lunch.WithProduct("Cheese", 350).WithDisplayOrder(0).Please(),
            Bread = lunch.WithProduct("Bread", 250).WithDisplayOrder(1).Please(),
        };
        
        return CtxRunner.RunScenarioAsync(
            c => c.Given_authenticated_user(),
            c => c.Given_notes(notes.Rice, notes.Bread, notes.Chicken, notes.Cheese),
            c => c.When_user_retrieves_notes_list_for_date("2024-01-04"),
            c => c.Then_notes_list_contains_items(notes.Chicken, notes.Rice, notes.Cheese, notes.Bread));
    }
    
    [Scenario]
    public Task I_can_retrieve_notes_list_ordered_by_time()
    {
        var notes = new
        {
            Soup = Create.Note().WithDate("2024-01-04").WithTime("19:30").WithProduct("Soup", 300).WithDisplayOrder(0).Please(),
            Coffee = Create.Note().WithDate("2024-01-04").WithTime("07:15").WithProduct("Coffee", 200).WithDisplayOrder(5).Please(),
            Noodles = Create.Note().WithDate("2024-01-04").WithTime("12:10").WithProduct("Noodles", 250).WithDisplayOrder(1).Please(),
            Tea = Create.Note().WithDate("2024-01-04").WithTime("12:10").WithProduct("Tea", 150).WithDisplayOrder(0).Please(),
            LateLunch = Create.Note().WithDate("2024-01-04").WithTime("15:30").WithMealType(MealType.Lunch)
                .WithProduct("Rice", 200).WithDisplayOrder(0).Please()
        };

        return CtxRunner.RunScenarioAsync(
            c => c.Given_authenticated_user(),
            c => c.Given_notes(notes.Soup, notes.Coffee, notes.Noodles, notes.Tea, notes.LateLunch),
            c => c.When_user_retrieves_notes_list_for_date("2024-01-04"),
            c => c.Then_notes_list_contains_items(notes.Coffee, notes.Tea, notes.Noodles, notes.LateLunch, notes.Soup));
    }

    [Scenario]
    public Task I_can_retrieve_notes_history_list()
    {
        var notes = new
        {
            Chicken = Create.Note().WithDate("2024-01-04").WithProduct("Chicken", 150).Please(),
            Rice = Create.Note().WithDate("2024-01-04").WithProduct("Rice", 100).Please(),
            Broccoli = Create.Note().WithDate("2024-01-05").WithProduct("Broccoli", 50).Please()
        };

        var history = new
        {
            Jan04 = Create.NoteHistoryItem("2024-01-04", 250),
            Jan05 = Create.NoteHistoryItem("2024-01-05", 50)
        };
        
        return CtxRunner.RunScenarioAsync(
            c => c.Given_authenticated_user(),
            c => c.Given_notes(notes.Chicken, notes.Broccoli, notes.Rice),
            c => c.When_user_retrieves_notes_history("2024-01-01", "2024-01-31"),
            c => c.Then_notes_history_contains_items(history.Jan04, history.Jan05));
    }

    [Scenario]
    public Task I_can_create_note()
    {
        var product = Create.Product("Chicken").Please();
        var note = Create.Note()
            .WithDate("2024-01-04")
            .WithProduct(product, 150)
            .Please();
        
        return CtxRunner.RunScenarioAsync(
            c => c.Given_authenticated_user(),
            c => c.Given_product(product),
            c => c.When_user_creates_note(note),
            c => c.Then_note_is_successfully_created(),
            c => c.When_user_retrieves_notes_list_for_date("2024-01-04"),
            c => c.Then_notes_list_contains_items(note));
    }
    
    [Scenario]
    public Task I_can_create_note_by_time_only_and_get_meal_from_it()
    {
        var product = Create.Product("Cake").Please();
        var note = Create.Note()
            .WithDate("2024-01-04")
            .WithTime("15:45")
            .WithProduct(product, 80)
            .WithDisplayOrder(0)
            .Please();

        return CtxRunner.RunScenarioAsync(
            c => c.Given_authenticated_user(),
            c => c.Given_product(product),
            c => c.When_user_creates_note_with_time_only(note),
            c => c.Then_note_is_successfully_created(),
            c => c.When_user_retrieves_notes_list_for_date("2024-01-04"),
            c => c.Then_notes_list_contains_items(note));
    }

    [Scenario]
    public Task I_can_create_note_by_meal_only_and_get_its_usual_time()
    {
        var product = Create.Product("Soup").Please();
        var note = Create.Note()
            .WithDate("2024-01-04")
            .WithMealType(MealType.Lunch)
            .WithProduct(product, 300)
            .Please();

        return CtxRunner.RunScenarioAsync(
            c => c.Given_authenticated_user(),
            c => c.Given_product(product),
            c => c.When_user_creates_note_with_meal_type_only(note),
            c => c.Then_note_is_successfully_created(),
            c => c.When_user_retrieves_notes_list_json_for_date("2024-01-04"),
            c => c.Then_note_json_has_time_and_meal_type("12:30:00", (int)MealType.Lunch));
    }

    [Scenario]
    public Task I_cannot_create_note_without_time_and_meal()
    {
        var product = Create.Product("Soup").Please();
        var note = Create.Note().WithDate("2024-01-04").WithProduct(product, 300).Please();

        return CtxRunner.RunScenarioAsync(
            c => c.Given_authenticated_user(),
            c => c.Given_product(product),
            c => c.When_user_creates_note_without_time_and_meal_type(note),
            c => c.Then_note_is_not_created_because_time_and_meal_type_are_missing(),
            c => c.When_user_retrieves_notes_list_for_date("2024-01-04"),
            c => c.Then_notes_list_contains_no_items());
    }

    [Scenario]
    [InlineData("18:05:00", "18:05:00")]
    [InlineData("18:05", "18:05:00")]
    [InlineData("18:05:30.25", "18:05:30")]
    public Task Note_time_is_written_as_hours_minutes_and_seconds(string sentTime, string returnedTime)
    {
        var product = Create.Product("Toast").Please();

        return CtxRunner.RunScenarioAsync(
            c => c.Given_authenticated_user(),
            c => c.Given_product(product),
            c => c.When_user_creates_note_from_json(
                $$"""{ "date": "2024-01-04", "time": "{{sentTime}}", "productId": {{product.Id}}, "productQuantity": 60 }"""),
            c => c.Then_note_is_successfully_created(),
            c => c.When_user_retrieves_notes_list_json_for_date("2024-01-04"),
            c => c.Then_note_json_has_time_and_meal_type(returnedTime, (int)MealType.Dinner));
    }

    [Scenario]
    public Task I_can_log_products_eaten_together()
    {
        var rice = Create.Product("Rice").Please();
        var dumplings = Create.Product("餃子").CountedInServingsOf(25).Please();
        var lunch = Create.Note().WithDate("2024-01-04").WithTime("12:45");
        var notes = new
        {
            Rice = lunch.WithProduct(rice, 200).WithDisplayOrder(0).Please(),
            Dumplings = lunch.WithProduct(dumplings, 250).WithDisplayOrder(1).Please()
        };

        return CtxRunner.RunScenarioAsync(
            c => c.Given_authenticated_user(),
            c => c.Given_products(rice, dumplings),
            c => c.When_user_logs_products_eaten_together("2024-01-04", "12:45:00", notes.Rice, notes.Dumplings),
            c => c.Then_products_eaten_together_are_logged(2),
            c => c.When_user_retrieves_notes_list_for_date("2024-01-04"),
            c => c.Then_notes_list_contains_items(notes.Rice, notes.Dumplings));
    }

    [Scenario]
    public Task I_cannot_log_products_eaten_together_when_one_is_unknown()
    {
        var rice = Create.Product("Rice").Please();
        var unknown = Create.Product("Unknown").Please();
        var lunch = Create.Note().WithDate("2024-01-04").WithTime("12:45");

        return CtxRunner.RunScenarioAsync(
            c => c.Given_authenticated_user(),
            c => c.Given_product(rice),
            c => c.When_user_logs_products_eaten_together(
                "2024-01-04",
                "12:45:00",
                lunch.WithProduct(rice, 200).Please(),
                lunch.WithProduct(unknown, 100).Please()),
            c => c.Then_products_eaten_together_are_not_logged(),
            c => c.When_user_retrieves_notes_list_for_date("2024-01-04"),
            c => c.Then_notes_list_contains_no_items());
    }

    [Scenario]
    public Task I_can_update_note()
    {
        var newProduct = Create.Product("Beef").Please();
        
        var originalNote = Create.Note()
            .WithDate("2024-01-04")
            .WithProduct("Chicken", 150)
            .Please();
        
        var updatedNote = Create.Note()
            .From(originalNote)
            .WithProduct(newProduct, 200)
            .Please();
        
        return CtxRunner.RunScenarioAsync(
            c => c.Given_authenticated_user(),
            c => c.Given_notes(originalNote),
            c => c.Given_product(newProduct),
            c => c.When_user_updates_product_with_quantity_for_note(originalNote, newProduct, 200),
            c => c.Then_note_is_successfully_updated(),
            c => c.When_user_retrieves_notes_list_for_date("2024-01-04"),
            c => c.Then_notes_list_contains_items(updatedNote));
    }

    [Scenario]
    public Task I_can_delete_single_note()
    {
        var note = Create.Note().WithDate("2024-01-04").Please();
        
        return CtxRunner.RunScenarioAsync(
            c => c.Given_authenticated_user(),
            c => c.Given_notes(note),
            c => c.When_user_deletes_note(note),
            c => c.Then_note_is_successfully_deleted(),
            c => c.When_user_retrieves_notes_list_for_date("2024-01-04"),
            c => c.Then_notes_list_contains_no_items());
    }

    [Scenario]
    public Task I_can_delete_multiple_notes()
    {
        var chicken = Create.Note().WithDate("2024-01-04").WithProduct("Chicken", 150).WithDisplayOrder(0).Please();
        var rice = Create.Note().WithDate("2024-01-04").WithProduct("Rice", 100).WithDisplayOrder(1).Please();
        var broccoli = Create.Note().WithDate("2024-01-04").WithProduct("Broccoli", 50).WithDisplayOrder(2).Please();

        var remainingBroccoli = Create.Note().From(broccoli).WithDisplayOrder(0).Please();

        return CtxRunner.RunScenarioAsync(
            c => c.Given_authenticated_user(),
            c => c.Given_notes(chicken, rice, broccoli),
            c => c.When_user_deletes_notes(chicken, rice),
            c => c.Then_multiple_notes_are_successfully_deleted(),
            c => c.When_user_retrieves_notes_list_for_date("2024-01-04"),
            c => c.Then_notes_list_contains_items(remainingBroccoli));
    }

    [Scenario]
    public Task I_can_recognize_notes_by_photo()
    {
        var givenFood = Create.RecognizeNoteModelResponse()
            .WithProduct("Orange")
            .WithCalories(50)
            .WithQuantity(400)
            .WithBrandName("Green Village")
            .WithNutritionComponents(protein: 0.12m, fats: 0.234m, carbs: 10, sugar: 2, salt: 0.367m)
            .Please();
        
        var recognizedProduct = Create.Product()
            .WithName("Orange (Green Village)")
            .WithCalories(50)
            .WithNutritionComponents(protein: 0.12m, fats: 0.23m, carbs: 10, sugar: 2, salt: 0.37m)
            .Please();
        
        const int recognizedQuantity = 400;
        
        return CtxRunner.RunScenarioAsync(
            c => c.Given_OpenAI_api_is_ready(),
            c => c.Given_OpenAI_api_can_recognize_food(givenFood),
            c => c.Given_authenticated_user(),
            c => c.When_user_uploads_file_for_note_recognition("recognizeNoteSamplePhoto.png"),
            c => c.Then_note_is_successfully_recognized_as(recognizedProduct, recognizedQuantity));
    }
    
    [Scenario]
    public Task I_cannot_recognize_notes_when_OpenAI_model_response_is_invalid()
    {
        return CtxRunner.RunScenarioAsync(
            c => c.Given_OpenAI_api_is_ready(),
            c => c.Given_OpenAI_completion_response_is_not_recognized_notes_json(),
            c => c.Given_authenticated_user(),
            c => c.When_user_uploads_file_for_note_recognition("recognizeNoteSamplePhoto.png"),
            c => c.Then_recognize_note_response_returns_error(HttpStatusCode.InternalServerError));
    }
    
    [Scenario]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.PaymentRequired)]
    [InlineData(HttpStatusCode.Forbidden)]
    public Task I_cannot_recognize_notes_when_OpenAI_request_fails(HttpStatusCode statusCode)
    {
        return CtxRunner.RunScenarioAsync(
            c => c.Given_OpenAI_api_is_ready(),
            c => c.Given_OpenAI_request_failed_with_error(statusCode),
            c => c.Given_authenticated_user(),
            c => c.When_user_uploads_file_for_note_recognition("recognizeNoteSamplePhoto.png"),
            c => c.Then_recognize_note_response_returns_error(HttpStatusCode.InternalServerError));
    }
}