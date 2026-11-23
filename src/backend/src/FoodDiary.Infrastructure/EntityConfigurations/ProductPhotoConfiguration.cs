using FoodDiary.Domain.Entities;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.EntityConfigurations;

[UsedImplicitly]
public class ProductPhotoConfiguration : IEntityTypeConfiguration<ProductPhoto>
{
    public void Configure(EntityTypeBuilder<ProductPhoto> builder)
    {
        builder.ToTable("ProductPhotos");

        builder.HasKey(p => p.ProductId);

        builder.HasOne<Product>()
            .WithOne(p => p.Photo)
            .HasForeignKey<ProductPhoto>(p => new { p.OwnerId, p.ProductId })
            .HasPrincipalKey<Product>(p => new { p.OwnerId, p.Id })
            .HasConstraintName("FK_ProductPhotos_Products_Owner")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
