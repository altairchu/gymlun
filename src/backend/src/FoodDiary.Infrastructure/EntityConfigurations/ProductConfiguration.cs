using FoodDiary.Domain.Entities;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.EntityConfigurations;

[UsedImplicitly]
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.HasKey(p => p.Id);

        builder.HasMany(p => p.Categories)
            .WithMany(c => c.Products)
            .UsingEntity<ProductCategory>(
                category => category.HasOne<Category>()
                    .WithMany()
                    .HasForeignKey(link => new { link.OwnerId, link.CategoryId })
                    .HasPrincipalKey(c => new { c.OwnerId, c.Id })
                    .HasConstraintName("FK_ProductCategories_Categories_Owner")
                    .OnDelete(DeleteBehavior.Cascade),
                product => product.HasOne<Product>()
                    .WithMany()
                    .HasForeignKey(link => new { link.OwnerId, link.ProductId })
                    .HasPrincipalKey(p => new { p.OwnerId, p.Id })
                    .HasConstraintName("FK_ProductCategories_Products_Owner")
                    .OnDelete(DeleteBehavior.Cascade),
                join =>
                {
                    join.ToTable("ProductCategories");
                    join.HasKey(link => new { link.ProductId, link.CategoryId });
                });

        builder.HasIndex(p => new { p.OwnerId, p.Name }).IsUnique();

        builder.Property(p => p.DefaultQuantity)
            .HasDefaultValue(100);

        builder.Property(p => p.IsFavorite)
            .HasDefaultValue(false);
    }
}
