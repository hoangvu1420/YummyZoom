using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YummyZoom.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Add_FullMenuView_RestaurantReviewSummary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FullMenuViews",
                columns: table => new
                {
                    RestaurantId = table.Column<Guid>(type: "uuid", nullable: false),
                    MenuJson = table.Column<string>(type: "jsonb", nullable: false),
                    LastRebuiltAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FullMenuViews", x => x.RestaurantId);
                });

            migrationBuilder.CreateTable(
                name: "RestaurantReviewSummaries",
                columns: table => new
                {
                    RestaurantId = table.Column<Guid>(type: "uuid", nullable: false),
                    AverageRating = table.Column<double>(type: "double precision", nullable: false, defaultValue: 0.0),
                    TotalReviews = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestaurantReviewSummaries", x => x.RestaurantId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FullMenuViews_LastRebuiltAt",
                table: "FullMenuViews",
                column: "LastRebuiltAt");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantReviewSummaries_AverageRating",
                table: "RestaurantReviewSummaries",
                column: "AverageRating");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FullMenuViews");

            migrationBuilder.DropTable(
                name: "RestaurantReviewSummaries");
        }
    }
}
