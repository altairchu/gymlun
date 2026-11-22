using FoodDiary.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure;

public partial class FoodDiaryContext
{
    public DbSet<GoalSettings> GoalSettings => Set<GoalSettings>();
    public DbSet<BodyMeasurement> BodyMeasurements => Set<BodyMeasurement>();
    public DbSet<McpConnection> McpConnections => Set<McpConnection>();
}
