using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YummyZoom.Infrastructure.Persistence.EfCore.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminMetricsReadModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdminDailyPerformanceSeries",
                columns: table => new
                {
                    BucketDate = table.Column<DateTime>(type: "date", nullable: false),
                    TotalOrders = table.Column<int>(type: "integer", nullable: false),
                    DeliveredOrders = table.Column<int>(type: "integer", nullable: false),
                    GrossMerchandiseVolume = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalRefunds = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    NewCustomers = table.Column<int>(type: "integer", nullable: false),
                    NewRestaurants = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminDailyPerformanceSeries", x => x.BucketDate);
                });

            migrationBuilder.CreateTable(
                name: "AdminPlatformMetricsSnapshots",
                columns: table => new
                {
                    SnapshotId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TotalOrders = table.Column<long>(type: "bigint", nullable: false),
                    ActiveOrders = table.Column<long>(type: "bigint", nullable: false),
                    DeliveredOrders = table.Column<long>(type: "bigint", nullable: false),
                    GrossMerchandiseVolume = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalRefunds = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ActiveRestaurants = table.Column<int>(type: "integer", nullable: false),
                    ActiveCustomers = table.Column<int>(type: "integer", nullable: false),
                    OpenSupportTickets = table.Column<int>(type: "integer", nullable: false),
                    TotalReviews = table.Column<int>(type: "integer", nullable: false),
                    LastOrderAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminPlatformMetricsSnapshots", x => x.SnapshotId);
                });

            migrationBuilder.CreateTable(
                name: "AdminRestaurantHealthSummaries",
                columns: table => new
                {
                    RestaurantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RestaurantName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    IsAcceptingOrders = table.Column<bool>(type: "boolean", nullable: false),
                    OrdersLast7Days = table.Column<int>(type: "integer", nullable: false),
                    OrdersLast30Days = table.Column<int>(type: "integer", nullable: false),
                    RevenueLast30Days = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    AverageRating = table.Column<double>(type: "double precision", nullable: false),
                    TotalReviews = table.Column<int>(type: "integer", nullable: false),
                    CouponRedemptionsLast30Days = table.Column<int>(type: "integer", nullable: false),
                    OutstandingBalance = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    LastOrderAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminRestaurantHealthSummaries", x => x.RestaurantId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdminDailyPerformanceSeries_UpdatedAtUtc",
                table: "AdminDailyPerformanceSeries",
                column: "UpdatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AdminPlatformMetricsSnapshots_UpdatedAtUtc",
                table: "AdminPlatformMetricsSnapshots",
                column: "UpdatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AdminRestaurantHealthSummaries_UpdatedAtUtc",
                table: "AdminRestaurantHealthSummaries",
                column: "UpdatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AdminRestaurantHealthSummaries_VerifiedAccepting",
                table: "AdminRestaurantHealthSummaries",
                columns: new[] { "IsVerified", "IsAcceptingOrders" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminDailyPerformanceSeries");

            migrationBuilder.DropTable(
                name: "AdminPlatformMetricsSnapshots");

            migrationBuilder.DropTable(
                name: "AdminRestaurantHealthSummaries");
        }
    }
}
