using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YummyZoom.Infrastructure.Persistence.EfCore.Migrations
{
    /// <inheritdoc />
    public partial class ExtendRestaurantReviewSummary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastReviewAtUtc",
                table: "RestaurantReviewSummaries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Ratings1",
                table: "RestaurantReviewSummaries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Ratings2",
                table: "RestaurantReviewSummaries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Ratings3",
                table: "RestaurantReviewSummaries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Ratings4",
                table: "RestaurantReviewSummaries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Ratings5",
                table: "RestaurantReviewSummaries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalWithText",
                table: "RestaurantReviewSummaries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "RestaurantReviewSummaries",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastReviewAtUtc",
                table: "RestaurantReviewSummaries");

            migrationBuilder.DropColumn(
                name: "Ratings1",
                table: "RestaurantReviewSummaries");

            migrationBuilder.DropColumn(
                name: "Ratings2",
                table: "RestaurantReviewSummaries");

            migrationBuilder.DropColumn(
                name: "Ratings3",
                table: "RestaurantReviewSummaries");

            migrationBuilder.DropColumn(
                name: "Ratings4",
                table: "RestaurantReviewSummaries");

            migrationBuilder.DropColumn(
                name: "Ratings5",
                table: "RestaurantReviewSummaries");

            migrationBuilder.DropColumn(
                name: "TotalWithText",
                table: "RestaurantReviewSummaries");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "RestaurantReviewSummaries");
        }
    }
}
