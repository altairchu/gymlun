using JetBrains.Annotations;

namespace FoodDiary.API.Features.ProductPhotos.Contracts;

[PublicAPI]
public record SetProductPhotoResponse(string PhotoUrl);
