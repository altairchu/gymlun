using FoodDiary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.EntityConfigurations;

internal sealed class GoalSettingsConfiguration : IEntityTypeConfiguration<GoalSettings>
{
    public void Configure(EntityTypeBuilder<GoalSettings> builder)
    {
        builder.HasKey(settings => settings.OwnerId);
        builder.Property(settings => settings.Version).IsConcurrencyToken();
        builder.Property(settings => settings.Goal).HasMaxLength(16);
        builder.Property(settings => settings.EatingApproach).HasMaxLength(1000);
        builder.Property(settings => settings.Sex).HasMaxLength(1);
    }
}

internal sealed class BodyMeasurementConfiguration : IEntityTypeConfiguration<BodyMeasurement>
{
    public void Configure(EntityTypeBuilder<BodyMeasurement> builder)
    {
        builder.HasIndex(measurement => new { measurement.OwnerId, measurement.Date, measurement.Id });
        builder.Property(measurement => measurement.Version).IsConcurrencyToken();
        builder.Property(measurement => measurement.Source).HasMaxLength(200);
        builder.Property(measurement => measurement.Data).HasColumnType("jsonb");
    }
}

internal sealed class McpConnectionConfiguration : IEntityTypeConfiguration<McpConnection>
{
    public void Configure(EntityTypeBuilder<McpConnection> builder)
    {
        builder.HasIndex(connection => new { connection.OwnerId, connection.RevokedAt });
        builder.Property(connection => connection.ClientName).HasMaxLength(200);
    }
}
