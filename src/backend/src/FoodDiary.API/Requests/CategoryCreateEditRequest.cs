using System.ComponentModel.DataAnnotations;

namespace FoodDiary.API.Requests;

public class CategoryCreateEditRequest
{
    [Required(ErrorMessage = "Category name is required")]
    [StringLength(64, MinimumLength = 1, ErrorMessage = "Category name must be between 1 and 64 characters")]
    public required string Name { get; set; }
}