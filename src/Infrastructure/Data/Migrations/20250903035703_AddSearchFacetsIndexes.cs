using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YummyZoom.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSearchFacetsIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "SIDX_PriceBand",
                table: "SearchIndexItems",
                column: "PriceBand");

            // Expression index to speed case-insensitive grouping and equality on Cuisine
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"SIDX_Lower_Cuisine\" ON \"SearchIndexItems\" (LOWER(\"Cuisine\"));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "SIDX_PriceBand",
                table: "SearchIndexItems");

            migrationBuilder.Sql("DROP INDEX IF EXISTS \"SIDX_Lower_Cuisine\";");
        }
    }
}
