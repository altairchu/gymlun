using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace FoodDiary.Migrator;

public class MigrationRunner
{
    public static async Task<int> RunMigrations(string[] args)
    {
        var serviceProvider = BuildServiceProvider();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<MigrationRunner>();
    
        try
        {
            var factory = new FoodDiaryContextFactory(loggerFactory);
            if (args is ["--claim-legacy-owner", var owner])
            {
                if (!long.TryParse(owner, NumberStyles.None, CultureInfo.InvariantCulture, out var ownerId) || ownerId <= 0)
                {
                    logger.LogError("--claim-legacy-owner requires a positive AtlasApple account ID.");
                    return -1;
                }

                await using var legacyContext = factory.CreateDbContext([]);
                var rowsMoved = await LegacyDiaryOwnership.ClaimAsync(legacyContext, ownerId);
                logger.LogInformation("Transferred {RowCount} legacy diary rows to the selected AtlasApple account", rowsMoved);
                return 0;
            }

            if (args.Any(arg => arg.StartsWith("--", StringComparison.Ordinal)))
            {
                logger.LogError("Use --claim-legacy-owner <positive AtlasApple account ID> to transfer the legacy diary.");
                return -1;
            }

            await using var context = factory.CreateDbContext(args);
            await context.Database.MigrateAsync();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error while applying migrations");
            return -1;
        }
        
        return 0;
    }
    
    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        
        services.AddSerilog(logger => logger
            .ReadFrom.Configuration(MigratorConfiguration.Configuration));

        return services.BuildServiceProvider();
    }
}
