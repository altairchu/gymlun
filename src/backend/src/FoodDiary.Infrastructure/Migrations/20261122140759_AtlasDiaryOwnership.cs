using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AtlasDiaryOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notes_Products_ProductId",
                table: "Notes");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductCategories_Categories_CategoryId",
                table: "ProductCategories");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductCategories_Products_ProductId",
                table: "ProductCategories");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductPhotos_Products_ProductId",
                table: "ProductPhotos");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutSets_Date",
                table: "WorkoutSets");

            migrationBuilder.DropPrimaryKey(
                name: "PK_WeightLogs",
                table: "WeightLogs");

            migrationBuilder.DropIndex(
                name: "IX_Products_Name",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_ProductCategories_CategoryId",
                table: "ProductCategories");

            migrationBuilder.DropIndex(
                name: "IX_Notes_Date",
                table: "Notes");

            migrationBuilder.DropIndex(
                name: "IX_Notes_ProductId",
                table: "Notes");

            migrationBuilder.DropIndex(
                name: "IX_Categories_Name",
                table: "Categories");

            migrationBuilder.AddColumn<long>(
                name: "OwnerId",
                table: "WorkoutSets",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "OwnerId",
                table: "WeightLogs",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "OwnerId",
                table: "Products",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "OwnerId",
                table: "ProductPhotos",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "OwnerId",
                table: "ProductCategories",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "OwnerId",
                table: "Notes",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "OwnerId",
                table: "Categories",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddPrimaryKey(
                name: "PK_WeightLogs",
                table: "WeightLogs",
                columns: new[] { "OwnerId", "Date" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Products_OwnerId_Id",
                table: "Products",
                columns: new[] { "OwnerId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Categories_OwnerId_Id",
                table: "Categories",
                columns: new[] { "OwnerId", "Id" });

            migrationBuilder.CreateTable(
                name: "BodyMeasurements",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OwnerId = table.Column<long>(type: "bigint", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Source = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    WeightKg = table.Column<decimal>(type: "numeric", nullable: true),
                    Data = table.Column<string>(type: "jsonb", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BodyMeasurements", x => x.Id);
                    table.CheckConstraint("CK_BodyMeasurements_OwnerId", "\"OwnerId\" >= 0");
                });

            migrationBuilder.CreateTable(
                name: "GoalSettings",
                columns: table => new
                {
                    OwnerId = table.Column<long>(type: "bigint", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Goal = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    EatingApproach = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    TargetWeightKg = table.Column<decimal>(type: "numeric", nullable: true),
                    TargetKcal = table.Column<int>(type: "integer", nullable: true),
                    ProteinPerKg = table.Column<decimal>(type: "numeric", nullable: true),
                    TargetCarbsG = table.Column<decimal>(type: "numeric", nullable: true),
                    TargetFatG = table.Column<decimal>(type: "numeric", nullable: true),
                    AgeYears = table.Column<int>(type: "integer", nullable: true),
                    HeightCm = table.Column<decimal>(type: "numeric", nullable: true),
                    Sex = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: true),
                    ActivityFactor = table.Column<decimal>(type: "numeric", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoalSettings", x => x.OwnerId);
                    table.CheckConstraint("CK_GoalSettings_OwnerId", "\"OwnerId\" >= 0");
                });

            migrationBuilder.CreateTable(
                name: "McpConnections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<long>(type: "bigint", nullable: false),
                    ClientId = table.Column<string>(type: "text", nullable: false),
                    ClientName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AuthorizedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RefreshVersion = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_McpConnections", x => x.Id);
                    table.CheckConstraint("CK_McpConnections_OwnerId", "\"OwnerId\" >= 0");
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSets_OwnerId_Date",
                table: "WorkoutSets",
                columns: new[] { "OwnerId", "Date" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_WorkoutSets_OwnerId",
                table: "WorkoutSets",
                sql: "\"OwnerId\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_WeightLogs_OwnerId",
                table: "WeightLogs",
                sql: "\"OwnerId\" >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_Products_OwnerId_Name",
                table: "Products",
                columns: new[] { "OwnerId", "Name" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Products_OwnerId",
                table: "Products",
                sql: "\"OwnerId\" >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPhotos_OwnerId_ProductId",
                table: "ProductPhotos",
                columns: new[] { "OwnerId", "ProductId" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_ProductPhotos_OwnerId",
                table: "ProductPhotos",
                sql: "\"OwnerId\" >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_ProductCategories_OwnerId_CategoryId",
                table: "ProductCategories",
                columns: new[] { "OwnerId", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductCategories_OwnerId_ProductId",
                table: "ProductCategories",
                columns: new[] { "OwnerId", "ProductId" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_ProductCategories_OwnerId",
                table: "ProductCategories",
                sql: "\"OwnerId\" >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_Notes_OwnerId_Date",
                table: "Notes",
                columns: new[] { "OwnerId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_Notes_OwnerId_ProductId",
                table: "Notes",
                columns: new[] { "OwnerId", "ProductId" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Notes_OwnerId",
                table: "Notes",
                sql: "\"OwnerId\" >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_OwnerId_Name",
                table: "Categories",
                columns: new[] { "OwnerId", "Name" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Categories_OwnerId",
                table: "Categories",
                sql: "\"OwnerId\" >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_BodyMeasurements_OwnerId_Date_Id",
                table: "BodyMeasurements",
                columns: new[] { "OwnerId", "Date", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_McpConnections_OwnerId_RevokedAt",
                table: "McpConnections",
                columns: new[] { "OwnerId", "RevokedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_Notes_Products_Owner",
                table: "Notes",
                columns: new[] { "OwnerId", "ProductId" },
                principalTable: "Products",
                principalColumns: new[] { "OwnerId", "Id" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductCategories_Categories_Owner",
                table: "ProductCategories",
                columns: new[] { "OwnerId", "CategoryId" },
                principalTable: "Categories",
                principalColumns: new[] { "OwnerId", "Id" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductCategories_Products_Owner",
                table: "ProductCategories",
                columns: new[] { "OwnerId", "ProductId" },
                principalTable: "Products",
                principalColumns: new[] { "OwnerId", "Id" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductPhotos_Products_Owner",
                table: "ProductPhotos",
                columns: new[] { "OwnerId", "ProductId" },
                principalTable: "Products",
                principalColumns: new[] { "OwnerId", "Id" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.Sql(
                """
                ALTER TABLE "Notes" ALTER CONSTRAINT "FK_Notes_Products_Owner" DEFERRABLE INITIALLY IMMEDIATE;
                ALTER TABLE "ProductCategories" ALTER CONSTRAINT "FK_ProductCategories_Categories_Owner" DEFERRABLE INITIALLY IMMEDIATE;
                ALTER TABLE "ProductCategories" ALTER CONSTRAINT "FK_ProductCategories_Products_Owner" DEFERRABLE INITIALLY IMMEDIATE;
                ALTER TABLE "ProductPhotos" ALTER CONSTRAINT "FK_ProductPhotos_Products_Owner" DEFERRABLE INITIALLY IMMEDIATE;

                ALTER TABLE "WorkoutSets" ALTER COLUMN "OwnerId" DROP DEFAULT;
                ALTER TABLE "WeightLogs" ALTER COLUMN "OwnerId" DROP DEFAULT;
                ALTER TABLE "Products" ALTER COLUMN "OwnerId" DROP DEFAULT;
                ALTER TABLE "ProductPhotos" ALTER COLUMN "OwnerId" DROP DEFAULT;
                ALTER TABLE "ProductCategories" ALTER COLUMN "OwnerId" DROP DEFAULT;
                ALTER TABLE "Notes" ALTER COLUMN "OwnerId" DROP DEFAULT;
                ALTER TABLE "Categories" ALTER COLUMN "OwnerId" DROP DEFAULT;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                LOCK TABLE "Categories", "Products", "ProductCategories", "Notes", "ProductPhotos", "WeightLogs", "WorkoutSets",
                           "BodyMeasurements", "GoalSettings", "McpConnections" IN ACCESS EXCLUSIVE MODE;
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM "Categories" WHERE "OwnerId" <> 0)
                        OR EXISTS (SELECT 1 FROM "Products" WHERE "OwnerId" <> 0)
                        OR EXISTS (SELECT 1 FROM "ProductCategories" WHERE "OwnerId" <> 0)
                        OR EXISTS (SELECT 1 FROM "Notes" WHERE "OwnerId" <> 0)
                        OR EXISTS (SELECT 1 FROM "ProductPhotos" WHERE "OwnerId" <> 0)
                        OR EXISTS (SELECT 1 FROM "WeightLogs" WHERE "OwnerId" <> 0)
                        OR EXISTS (SELECT 1 FROM "WorkoutSets" WHERE "OwnerId" <> 0)
                        OR EXISTS (SELECT 1 FROM "BodyMeasurements")
                        OR EXISTS (SELECT 1 FROM "GoalSettings")
                        OR EXISTS (SELECT 1 FROM "McpConnections") THEN
                        RAISE EXCEPTION 'Cannot remove diary ownership while AtlasApple records exist';
                    END IF;
                END $$;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_Notes_Products_Owner",
                table: "Notes");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductCategories_Categories_Owner",
                table: "ProductCategories");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductCategories_Products_Owner",
                table: "ProductCategories");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductPhotos_Products_Owner",
                table: "ProductPhotos");

            migrationBuilder.DropTable(
                name: "BodyMeasurements");

            migrationBuilder.DropTable(
                name: "GoalSettings");

            migrationBuilder.DropTable(
                name: "McpConnections");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutSets_OwnerId_Date",
                table: "WorkoutSets");

            migrationBuilder.DropCheckConstraint(
                name: "CK_WorkoutSets_OwnerId",
                table: "WorkoutSets");

            migrationBuilder.DropPrimaryKey(
                name: "PK_WeightLogs",
                table: "WeightLogs");

            migrationBuilder.DropCheckConstraint(
                name: "CK_WeightLogs_OwnerId",
                table: "WeightLogs");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Products_OwnerId_Id",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_OwnerId_Name",
                table: "Products");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Products_OwnerId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_ProductPhotos_OwnerId_ProductId",
                table: "ProductPhotos");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ProductPhotos_OwnerId",
                table: "ProductPhotos");

            migrationBuilder.DropIndex(
                name: "IX_ProductCategories_OwnerId_CategoryId",
                table: "ProductCategories");

            migrationBuilder.DropIndex(
                name: "IX_ProductCategories_OwnerId_ProductId",
                table: "ProductCategories");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ProductCategories_OwnerId",
                table: "ProductCategories");

            migrationBuilder.DropIndex(
                name: "IX_Notes_OwnerId_Date",
                table: "Notes");

            migrationBuilder.DropIndex(
                name: "IX_Notes_OwnerId_ProductId",
                table: "Notes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Notes_OwnerId",
                table: "Notes");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Categories_OwnerId_Id",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Categories_OwnerId_Name",
                table: "Categories");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Categories_OwnerId",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "WorkoutSets");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "WeightLogs");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "ProductPhotos");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "ProductCategories");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Notes");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Categories");

            migrationBuilder.AddPrimaryKey(
                name: "PK_WeightLogs",
                table: "WeightLogs",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSets_Date",
                table: "WorkoutSets",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Name",
                table: "Products",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductCategories_CategoryId",
                table: "ProductCategories",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Notes_Date",
                table: "Notes",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_Notes_ProductId",
                table: "Notes",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                table: "Categories",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Notes_Products_ProductId",
                table: "Notes",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductCategories_Categories_CategoryId",
                table: "ProductCategories",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductCategories_Products_ProductId",
                table: "ProductCategories",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductPhotos_Products_ProductId",
                table: "ProductPhotos",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
