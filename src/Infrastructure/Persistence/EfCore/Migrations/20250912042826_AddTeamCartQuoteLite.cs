using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YummyZoom.Infrastructure.Persistence.EfCore.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamCartQuoteLite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "GrandTotal_Amount",
                table: "TeamCarts",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "GrandTotal_Currency",
                table: "TeamCarts",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MemberTotals",
                table: "TeamCarts",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "QuoteVersion",
                table: "TeamCarts",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GrandTotal_Amount",
                table: "TeamCarts");

            migrationBuilder.DropColumn(
                name: "GrandTotal_Currency",
                table: "TeamCarts");

            migrationBuilder.DropColumn(
                name: "MemberTotals",
                table: "TeamCarts");

            migrationBuilder.DropColumn(
                name: "QuoteVersion",
                table: "TeamCarts");
        }
    }
}
