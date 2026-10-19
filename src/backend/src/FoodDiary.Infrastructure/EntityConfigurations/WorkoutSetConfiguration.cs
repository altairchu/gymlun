using FoodDiary.Domain.Workouts;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.EntityConfigurations;

[UsedImplicitly]
public class WorkoutSetConfiguration : IEntityTypeConfiguration<WorkoutSet>
{
    public void Configure(EntityTypeBuilder<WorkoutSet> builder)
    {
        builder.ToTable("WorkoutSets");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Exercise)
            .HasMaxLength(WorkoutSet.MaxExerciseLength);

        builder.Property(s => s.WeightKg)
            .HasPrecision(6, 2);

        builder.HasIndex(s => s.Date);
    }
}
