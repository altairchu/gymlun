using FoodDiary.ComponentTests.Dsl;
using FoodDiary.ComponentTests.Infrastructure;

namespace FoodDiary.ComponentTests.Scenarios.ProductPhotos;

public class ProductPhotosApiTests(InfrastructureFixture infrastructure)
    : BaseTest<ProductPhotosApiContext>(infrastructure)
{
    [Scenario]
    public Task I_can_set_product_photo_scaled_down_to_jpeg()
    {
        var apple = Create.Product("Apple").Please();

        return CtxRunner.RunScenarioAsync(
            c => c.Given_authenticated_user(),
            c => c.Given_products(apple),
            c => c.When_user_sets_png_photo(apple, 1200, 800),
            c => c.Then_photo_is_set(),
            c => c.When_user_retrieves_product_by_id(apple),
            c => c.Then_product_shows_the_photo_url_that_was_set(),
            c => c.When_user_downloads_the_photo_url_that_was_set(),
            c => c.Then_photo_is_cacheable_jpeg_of_size(640, 427));
    }

    [Scenario]
    public Task I_can_replace_product_photo()
    {
        var apple = Create.Product("Apple").WithPhoto(TestImages.Jpeg(100, 100)).Please();

        return CtxRunner.RunScenarioAsync(
            c => c.Given_authenticated_user(),
            c => c.Given_products(apple),
            c => c.When_user_sets_png_photo(apple, 300, 200),
            c => c.Then_photo_is_set(),
            c => c.When_user_downloads_the_photo_url_that_was_set(),
            c => c.Then_photo_is_cacheable_jpeg_of_size(300, 200));
    }

    [Scenario]
    public Task I_can_remove_product_photo()
    {
        var apple = Create.Product("Apple").WithPhoto(TestImages.Jpeg(100, 100)).Please();

        return CtxRunner.RunScenarioAsync(
            c => c.Given_authenticated_user(),
            c => c.Given_products(apple),
            c => c.When_user_removes_photo(apple),
            c => c.Then_photo_is_removed(),
            c => c.When_user_retrieves_product_by_id(apple),
            c => c.Then_product_has_no_photo(),
            c => c.When_user_downloads_photo(apple),
            c => c.Then_photo_is_not_found());
    }

    [Scenario]
    public Task I_cannot_set_product_photo_from_a_file_that_is_not_an_image()
    {
        var apple = Create.Product("Apple").Please();

        return CtxRunner.RunScenarioAsync(
            c => c.Given_authenticated_user(),
            c => c.Given_products(apple),
            c => c.When_user_sets_text_file_as_photo(apple),
            c => c.Then_photo_is_rejected());
    }
}
