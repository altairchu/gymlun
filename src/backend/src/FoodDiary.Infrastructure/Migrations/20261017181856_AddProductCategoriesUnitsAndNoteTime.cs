using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductCategoriesUnitsAndNoteTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductCategories",
                columns: table => new
                {
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    CategoryId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductCategories", x => new { x.ProductId, x.CategoryId });
                    table.ForeignKey(
                        name: "FK_ProductCategories_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductCategories_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductCategories_CategoryId",
                table: "ProductCategories",
                column: "CategoryId");

            migrationBuilder.Sql(
                """
                INSERT INTO "ProductCategories" ("ProductId", "CategoryId")
                SELECT "Id", "CategoryId" FROM "Products";
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_Products_Categories_CategoryId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_CategoryId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Products");

            migrationBuilder.AddColumn<int>(
                name: "QuantityUnit",
                table: "Products",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "Time",
                table: "Notes",
                type: "time without time zone",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));

            migrationBuilder.Sql(
                """
                UPDATE "Notes"
                SET "Time" = CASE "MealType"
                    WHEN 1 THEN TIME '08:00'
                    WHEN 2 THEN TIME '10:30'
                    WHEN 3 THEN TIME '12:30'
                    WHEN 4 THEN TIME '15:30'
                    ELSE TIME '19:00'
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Time",
                table: "Notes");

            migrationBuilder.DropColumn(
                name: "QuantityUnit",
                table: "Products");

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "Products",
                type: "integer",
                nullable: true);

            // The old schema needs exactly one category per product: keep the lowest linked one,
            // and put products that have none into a fallback category
            migrationBuilder.Sql(
                """
                UPDATE "Products" AS p
                SET "CategoryId" = (
                    SELECT MIN(pc."CategoryId") FROM "ProductCategories" AS pc WHERE pc."ProductId" = p."Id");

                INSERT INTO "Categories" ("Name")
                SELECT 'Uncategorized'
                WHERE EXISTS (SELECT 1 FROM "Products" WHERE "CategoryId" IS NULL)
                  AND NOT EXISTS (SELECT 1 FROM "Categories" WHERE "Name" = 'Uncategorized');

                UPDATE "Products"
                SET "CategoryId" = (SELECT "Id" FROM "Categories" WHERE "Name" = 'Uncategorized')
                WHERE "CategoryId" IS NULL;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "CategoryId",
                table: "Products",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_CategoryId",
                table: "Products",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Categories_CategoryId",
                table: "Products",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.DropTable(
                name: "ProductCategories");
        }
    }
}
