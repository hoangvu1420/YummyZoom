using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YummyZoom.Infrastructure.Persistence.EfCore.Migrations
{
    /// <inheritdoc />
    public partial class UpdateReviewConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the old customer-restaurant unique constraint
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"UX_Reviews_Restaurant_Customer_Active\";");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_OrderId",
                table: "Reviews");

            migrationBuilder.CreateIndex(
                name: "UX_Reviews_OrderId_Unique",
                table: "Reviews",
                column: "OrderId",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Reviews_OrderId_Unique",
                table: "Reviews");

            // Recreate the old customer-restaurant unique constraint
            migrationBuilder.Sql("CREATE UNIQUE INDEX IF NOT EXISTS \"UX_Reviews_Restaurant_Customer_Active\" ON \"Reviews\"(\"RestaurantId\", \"CustomerId\") WHERE \"IsDeleted\" = FALSE;");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_OrderId",
                table: "Reviews",
                column: "OrderId");
        }
    }
}
