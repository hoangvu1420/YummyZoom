using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YummyZoom.Infrastructure.Persistence.EfCore.Migrations
{
    /// <inheritdoc />
    public partial class AddMenuItemSalesSummaries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MenuItemSalesSummaries",
                columns: table => new
                {
                    RestaurantId = table.Column<Guid>(type: "uuid", nullable: false),
                    MenuItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    LifetimeQuantity = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    Rolling7DayQuantity = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    Rolling30DayQuantity = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    LastSoldAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastUpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SourceVersion = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuItemSalesSummaries", x => new { x.RestaurantId, x.MenuItemId });
                });

            migrationBuilder.CreateIndex(
                name: "IX_MenuItemSalesSummaries_LastUpdatedAt",
                table: "MenuItemSalesSummaries",
                column: "LastUpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItemSalesSummaries_MenuItemId",
                table: "MenuItemSalesSummaries",
                column: "MenuItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MenuItemSalesSummaries");
        }
    }
}
