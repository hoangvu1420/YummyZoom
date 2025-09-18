using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YummyZoom.Infrastructure.Persistence.EfCore.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueActiveReviewIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE UNIQUE INDEX IF NOT EXISTS \"UX_Reviews_Restaurant_Customer_Active\" ON \"Reviews\"(\"RestaurantId\", \"CustomerId\") WHERE \"IsDeleted\" = FALSE;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"UX_Reviews_Restaurant_Customer_Active\";");
        }
    }
}
