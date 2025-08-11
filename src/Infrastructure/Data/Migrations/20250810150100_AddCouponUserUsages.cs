using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YummyZoom.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCouponUserUsages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CouponUserUsages",
                columns: table => new
                {
                    CouponId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsageCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CouponUserUsages", x => new { x.CouponId, x.UserId });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CouponUserUsages");
        }
    }
}
