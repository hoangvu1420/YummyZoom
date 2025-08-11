using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YummyZoom.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIndexInCoupon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "TotalUsageLimit",
                table: "Coupons",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateIndex(
                name: "IX_Coupons_CurrentTotalUsageCount",
                table: "Coupons",
                column: "CurrentTotalUsageCount");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Coupons_CurrentTotalUsageCount",
                table: "Coupons");

            migrationBuilder.AlterColumn<int>(
                name: "TotalUsageLimit",
                table: "Coupons",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
