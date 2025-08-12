using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace YummyZoom.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RestaurantAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RelatedOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Timestamp when the entity was created"),
                    CreatedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Identifier of who created the entity")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountTransactions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Coupons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RestaurantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Value_Type = table.Column<string>(type: "text", nullable: false),
                    Value_PercentageValue = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    Value_FixedAmount_Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    Value_FixedAmount_Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    Value_FreeItemValue = table.Column<Guid>(type: "uuid", nullable: true),
                    AppliesTo_Scope = table.Column<string>(type: "text", nullable: false),
                    AppliesTo_ItemIds = table.Column<string>(type: "jsonb", nullable: false),
                    AppliesTo_CategoryIds = table.Column<string>(type: "jsonb", nullable: false),
                    MinOrderAmount_Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    MinOrderAmount_Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    ValidityStartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ValidityEndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TotalUsageLimit = table.Column<int>(type: "integer", nullable: true),
                    CurrentTotalUsageCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    UsageLimitPerUser = table.Column<int>(type: "integer", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Timestamp when the entity was created"),
                    CreatedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Identifier of who created the entity"),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Timestamp when the entity was last modified"),
                    LastModifiedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Identifier of who last modified the entity"),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Indicates if the entity is soft-deleted"),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Timestamp when the entity was deleted"),
                    DeletedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Identifier of who deleted the entity")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Coupons", x => x.Id);
                });

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

            migrationBuilder.CreateTable(
                name: "CustomizationGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RestaurantId = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MinSelections = table.Column<int>(type: "integer", nullable: false),
                    MaxSelections = table.Column<int>(type: "integer", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Timestamp when the entity was created"),
                    CreatedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Identifier of who created the entity"),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Timestamp when the entity was last modified"),
                    LastModifiedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Identifier of who last modified the entity"),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Indicates if the entity is soft-deleted"),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Timestamp when the entity was deleted"),
                    DeletedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Identifier of who deleted the entity")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomizationGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Devices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<string>(type: "text", nullable: true),
                    Platform = table.Column<string>(type: "text", nullable: false),
                    ModelName = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DomainUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Timestamp when the entity was created"),
                    CreatedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Identifier of who created the entity"),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Timestamp when the entity was last modified"),
                    LastModifiedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Identifier of who last modified the entity"),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Indicates if the entity is soft-deleted"),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Timestamp when the entity was deleted"),
                    DeletedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Identifier of who deleted the entity")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DomainUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MenuCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MenuId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Timestamp when the entity was created"),
                    CreatedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Identifier of who created the entity"),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Timestamp when the entity was last modified"),
                    LastModifiedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Identifier of who last modified the entity"),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Indicates if the entity is soft-deleted"),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Timestamp when the entity was deleted"),
                    DeletedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Identifier of who deleted the entity")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MenuItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RestaurantId = table.Column<Guid>(type: "uuid", nullable: false),
                    MenuCategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    BasePrice_Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    BasePrice_Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsAvailable = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Timestamp when the entity was created"),
                    CreatedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Identifier of who created the entity"),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Timestamp when the entity was last modified"),
                    LastModifiedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Identifier of who last modified the entity"),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Indicates if the entity is soft-deleted"),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Timestamp when the entity was deleted"),
                    DeletedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Identifier of who deleted the entity"),
                    DietaryTagIds = table.Column<string>(type: "jsonb", nullable: false),
                    AppliedCustomizations = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Menus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    RestaurantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Timestamp when the entity was created"),
                    CreatedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Identifier of who created the entity"),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Timestamp when the entity was last modified"),
                    LastModifiedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Identifier of who last modified the entity"),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Indicates if the entity is soft-deleted"),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Timestamp when the entity was deleted"),
                    DeletedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Identifier of who deleted the entity")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Menus", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Timestamp when the entity was created"),
                    CreatedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Identifier of who created the entity"),
                    OrderNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PlacementTimestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdateTimestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EstimatedDeliveryTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ActualDeliveryTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SpecialInstructions = table.Column<string>(type: "text", nullable: true),
                    DeliveryAddress_Street = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    DeliveryAddress_City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DeliveryAddress_State = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DeliveryAddress_ZipCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DeliveryAddress_Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Subtotal_Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Subtotal_Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    DiscountAmount_Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DiscountAmount_Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    DeliveryFee_Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DeliveryFee_Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    TipAmount_Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TipAmount_Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    TaxAmount_Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TaxAmount_Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    TotalAmount_Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalAmount_Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    RestaurantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceTeamCartId = table.Column<Guid>(type: "uuid", nullable: true),
                    AppliedCouponId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProcessedWebhookEvents",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedWebhookEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RestaurantAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RestaurantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentBalance_Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CurrentBalance_Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    PayoutMethod_Details = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Timestamp when the entity was created"),
                    CreatedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Identifier of who created the entity")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestaurantAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Restaurants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    LogoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CuisineType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Location_Street = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Location_City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Location_State = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Location_ZipCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Location_Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ContactInfo_PhoneNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ContactInfo_Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    BusinessHours = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsAcceptingOrders = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Timestamp when the entity was created"),
                    CreatedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Identifier of who created the entity"),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Timestamp when the entity was last modified"),
                    LastModifiedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Identifier of who last modified the entity"),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Indicates if the entity is soft-deleted"),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Timestamp when the entity was deleted"),
                    DeletedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Identifier of who deleted the entity")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Restaurants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Reviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    RestaurantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SubmissionTimestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsModerated = table.Column<bool>(type: "boolean", nullable: false),
                    IsHidden = table.Column<bool>(type: "boolean", nullable: false),
                    Reply = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Timestamp when the entity was created"),
                    CreatedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Identifier of who created the entity"),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Timestamp when the entity was last modified"),
                    LastModifiedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Identifier of who last modified the entity"),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Indicates if the entity is soft-deleted"),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Timestamp when the entity was deleted"),
                    DeletedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Identifier of who deleted the entity")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reviews", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoleAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RestaurantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Timestamp when the entity was created"),
                    CreatedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Identifier of who created the entity")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleAssignments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SupportTickets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Priority = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SubmissionTimestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastUpdateTimestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AssignedToAdminId = table.Column<Guid>(type: "uuid", nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Timestamp when the entity was created"),
                    CreatedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Identifier of who created the entity")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportTickets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TagName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TagDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TagCategory = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Timestamp when the entity was created"),
                    CreatedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Identifier of who created the entity"),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Timestamp when the entity was last modified"),
                    LastModifiedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Identifier of who last modified the entity"),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Indicates if the entity is soft-deleted"),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Timestamp when the entity was deleted"),
                    DeletedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Identifier of who deleted the entity")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TeamCarts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Timestamp when the entity was created"),
                    CreatedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Identifier of who created the entity"),
                    RestaurantId = table.Column<Guid>(type: "uuid", nullable: false),
                    HostUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ShareToken_Value = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ShareToken_ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Deadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TipAmount_Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TipAmount_Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    AppliedCouponId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamCarts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TodoLists",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Colour = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Timestamp when the entity was created"),
                    CreatedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Identifier of who created the entity"),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Timestamp when the entity was last modified"),
                    LastModifiedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Identifier of who last modified the entity")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TodoLists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserDeviceSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    FcmToken = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LoggedOutAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDeviceSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomizationChoices",
                columns: table => new
                {
                    ChoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomizationGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PriceAdjustment_Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PriceAdjustment_Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomizationChoices", x => new { x.CustomizationGroupId, x.ChoiceId });
                    table.ForeignKey(
                        name: "FK_CustomizationChoices_CustomizationGroups_CustomizationGroup~",
                        column: x => x.CustomizationGroupId,
                        principalTable: "CustomizationGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserAddresses",
                columns: table => new
                {
                    AddressId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Street = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    State = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ZipCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DeliveryInstructions = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAddresses", x => new { x.UserId, x.AddressId });
                    table.ForeignKey(
                        name: "FK_UserAddresses_DomainUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "DomainUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserPaymentMethods",
                columns: table => new
                {
                    PaymentMethodId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TokenizedDetails = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPaymentMethods", x => new { x.UserId, x.PaymentMethodId });
                    table.ForeignKey(
                        name: "FK_UserPaymentMethods_DomainUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "DomainUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderItems",
                columns: table => new
                {
                    OrderItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Snapshot_MenuCategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Snapshot_MenuItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Snapshot_ItemName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    BasePrice_Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    BasePrice_Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    LineItemTotal_Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    LineItemTotal_Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    SelectedCustomizations = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItems", x => new { x.OrderId, x.OrderItemId });
                    table.ForeignKey(
                        name: "FK_OrderItems_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaymentTransactions",
                columns: table => new
                {
                    PaymentTransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentMethodType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PaymentMethodDisplay = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Transaction_Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Transaction_Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PaymentGatewayReferenceId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    PaidByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentTransactions", x => new { x.OrderId, x.PaymentTransactionId });
                    table.ForeignKey(
                        name: "FK_PaymentTransactions_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupportTicketContextLinks",
                columns: table => new
                {
                    EntityType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EntityID = table.Column<Guid>(type: "uuid", nullable: false),
                    SupportTicketId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportTicketContextLinks", x => new { x.SupportTicketId, x.EntityID, x.EntityType });
                    table.ForeignKey(
                        name: "FK_SupportTicketContextLinks_SupportTickets_SupportTicketId",
                        column: x => x.SupportTicketId,
                        principalTable: "SupportTickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupportTicketMessages",
                columns: table => new
                {
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupportTicketId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MessageText = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsInternalNote = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportTicketMessages", x => new { x.SupportTicketId, x.MessageId });
                    table.ForeignKey(
                        name: "FK_SupportTicketMessages_SupportTickets_SupportTicketId",
                        column: x => x.SupportTicketId,
                        principalTable: "SupportTickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeamCartItems",
                columns: table => new
                {
                    TeamCartItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamCartId = table.Column<Guid>(type: "uuid", nullable: false),
                    AddedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Snapshot_MenuItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Snapshot_MenuCategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Snapshot_ItemName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    BasePrice_Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    BasePrice_Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    LineItemTotal_Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    LineItemTotal_Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    SelectedCustomizations = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamCartItems", x => new { x.TeamCartId, x.TeamCartItemId });
                    table.ForeignKey(
                        name: "FK_TeamCartItems_TeamCarts_TeamCartId",
                        column: x => x.TeamCartId,
                        principalTable: "TeamCarts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeamCartMemberPayments",
                columns: table => new
                {
                    MemberPaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamCartId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Payment_Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Payment_Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Method = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OnlineTransactionId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamCartMemberPayments", x => new { x.TeamCartId, x.MemberPaymentId });
                    table.ForeignKey(
                        name: "FK_TeamCartMemberPayments_TeamCarts_TeamCartId",
                        column: x => x.TeamCartId,
                        principalTable: "TeamCarts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeamCartMembers",
                columns: table => new
                {
                    TeamCartMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamCartId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamCartMembers", x => new { x.TeamCartId, x.TeamCartMemberId });
                    table.ForeignKey(
                        name: "FK_TeamCartMembers_TeamCarts_TeamCartId",
                        column: x => x.TeamCartId,
                        principalTable: "TeamCarts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TodoItems",
                columns: table => new
                {
                    TodoItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    TodoListId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    Reminder = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDone = table.Column<bool>(type: "boolean", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TodoItems", x => new { x.TodoListId, x.TodoItemId });
                    table.ForeignKey(
                        name: "FK_TodoItems_TodoLists_TodoListId",
                        column: x => x.TodoListId,
                        principalTable: "TodoLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountTransaction_Created",
                table: "AccountTransactions",
                column: "Created");

            migrationBuilder.CreateIndex(
                name: "IX_AccountTransactions_RestaurantAccountId",
                table: "AccountTransactions",
                column: "RestaurantAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountTransactions_Timestamp",
                table: "AccountTransactions",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Coupon_Created",
                table: "Coupons",
                column: "Created");

            migrationBuilder.CreateIndex(
                name: "IX_Coupon_DeletedOn",
                table: "Coupons",
                column: "DeletedOn");

            migrationBuilder.CreateIndex(
                name: "IX_Coupon_IsDeleted",
                table: "Coupons",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Coupon_LastModified",
                table: "Coupons",
                column: "LastModified");

            migrationBuilder.CreateIndex(
                name: "IX_Coupons_Code_RestaurantId",
                table: "Coupons",
                columns: new[] { "Code", "RestaurantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Coupons_CurrentTotalUsageCount",
                table: "Coupons",
                column: "CurrentTotalUsageCount");

            migrationBuilder.CreateIndex(
                name: "IX_CustomizationGroup_Created",
                table: "CustomizationGroups",
                column: "Created");

            migrationBuilder.CreateIndex(
                name: "IX_CustomizationGroup_DeletedOn",
                table: "CustomizationGroups",
                column: "DeletedOn");

            migrationBuilder.CreateIndex(
                name: "IX_CustomizationGroup_IsDeleted",
                table: "CustomizationGroups",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_CustomizationGroup_LastModified",
                table: "CustomizationGroups",
                column: "LastModified");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_DeviceId",
                table: "Devices",
                column: "DeviceId",
                unique: true,
                filter: "\"DeviceId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DomainUsers_Email",
                table: "DomainUsers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_User_Created",
                table: "DomainUsers",
                column: "Created");

            migrationBuilder.CreateIndex(
                name: "IX_User_DeletedOn",
                table: "DomainUsers",
                column: "DeletedOn");

            migrationBuilder.CreateIndex(
                name: "IX_User_IsDeleted",
                table: "DomainUsers",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_User_LastModified",
                table: "DomainUsers",
                column: "LastModified");

            migrationBuilder.CreateIndex(
                name: "IX_MenuCategories_MenuId",
                table: "MenuCategories",
                column: "MenuId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuCategories_MenuId_DisplayOrder",
                table: "MenuCategories",
                columns: new[] { "MenuId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_MenuCategory_Created",
                table: "MenuCategories",
                column: "Created");

            migrationBuilder.CreateIndex(
                name: "IX_MenuCategory_DeletedOn",
                table: "MenuCategories",
                column: "DeletedOn");

            migrationBuilder.CreateIndex(
                name: "IX_MenuCategory_IsDeleted",
                table: "MenuCategories",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_MenuCategory_LastModified",
                table: "MenuCategories",
                column: "LastModified");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItem_Created",
                table: "MenuItems",
                column: "Created");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItem_DeletedOn",
                table: "MenuItems",
                column: "DeletedOn");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItem_IsDeleted",
                table: "MenuItems",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItem_LastModified",
                table: "MenuItems",
                column: "LastModified");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_Category_Available",
                table: "MenuItems",
                columns: new[] { "MenuCategoryId", "IsAvailable" });

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_IsAvailable",
                table: "MenuItems",
                column: "IsAvailable");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_MenuCategoryId",
                table: "MenuItems",
                column: "MenuCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_Name",
                table: "MenuItems",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_Restaurant_Available",
                table: "MenuItems",
                columns: new[] { "RestaurantId", "IsAvailable" });

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_RestaurantId",
                table: "MenuItems",
                column: "RestaurantId");

            migrationBuilder.CreateIndex(
                name: "IX_Menu_Created",
                table: "Menus",
                column: "Created");

            migrationBuilder.CreateIndex(
                name: "IX_Menu_DeletedOn",
                table: "Menus",
                column: "DeletedOn");

            migrationBuilder.CreateIndex(
                name: "IX_Menu_IsDeleted",
                table: "Menus",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Menu_LastModified",
                table: "Menus",
                column: "LastModified");

            migrationBuilder.CreateIndex(
                name: "IX_Menus_RestaurantId",
                table: "Menus",
                column: "RestaurantId");

            migrationBuilder.CreateIndex(
                name: "IX_Order_Created",
                table: "Orders",
                column: "Created");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderNumber",
                table: "Orders",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantAccount_Created",
                table: "RestaurantAccounts",
                column: "Created");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantAccounts_RestaurantId",
                table: "RestaurantAccounts",
                column: "RestaurantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Restaurant_Created",
                table: "Restaurants",
                column: "Created");

            migrationBuilder.CreateIndex(
                name: "IX_Restaurant_DeletedOn",
                table: "Restaurants",
                column: "DeletedOn");

            migrationBuilder.CreateIndex(
                name: "IX_Restaurant_IsDeleted",
                table: "Restaurants",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Restaurant_LastModified",
                table: "Restaurants",
                column: "LastModified");

            migrationBuilder.CreateIndex(
                name: "IX_Restaurants_CuisineType",
                table: "Restaurants",
                column: "CuisineType");

            migrationBuilder.CreateIndex(
                name: "IX_Restaurants_IsAcceptingOrders",
                table: "Restaurants",
                column: "IsAcceptingOrders");

            migrationBuilder.CreateIndex(
                name: "IX_Restaurants_IsVerified",
                table: "Restaurants",
                column: "IsVerified");

            migrationBuilder.CreateIndex(
                name: "IX_Restaurants_Name",
                table: "Restaurants",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Restaurants_Verified_AcceptingOrders",
                table: "Restaurants",
                columns: new[] { "IsVerified", "IsAcceptingOrders" });

            migrationBuilder.CreateIndex(
                name: "IX_Review_Created",
                table: "Reviews",
                column: "Created");

            migrationBuilder.CreateIndex(
                name: "IX_Review_DeletedOn",
                table: "Reviews",
                column: "DeletedOn");

            migrationBuilder.CreateIndex(
                name: "IX_Review_IsDeleted",
                table: "Reviews",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Review_LastModified",
                table: "Reviews",
                column: "LastModified");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_CustomerId",
                table: "Reviews",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_OrderId",
                table: "Reviews",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_Restaurant_SubmissionTimestamp",
                table: "Reviews",
                columns: new[] { "RestaurantId", "SubmissionTimestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_RoleAssignment_Created",
                table: "RoleAssignments",
                column: "Created");

            migrationBuilder.CreateIndex(
                name: "IX_RoleAssignments_RestaurantId",
                table: "RoleAssignments",
                column: "RestaurantId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleAssignments_Role",
                table: "RoleAssignments",
                column: "Role");

            migrationBuilder.CreateIndex(
                name: "IX_RoleAssignments_User_Restaurant_Role",
                table: "RoleAssignments",
                columns: new[] { "UserId", "RestaurantId", "Role" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoleAssignments_UserId",
                table: "RoleAssignments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTicket_Created",
                table: "SupportTickets",
                column: "Created");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_AssignedToAdminId",
                table: "SupportTickets",
                column: "AssignedToAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_LastUpdateTimestamp",
                table: "SupportTickets",
                column: "LastUpdateTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_Priority",
                table: "SupportTickets",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_Status",
                table: "SupportTickets",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_SubmissionTimestamp",
                table: "SupportTickets",
                column: "SubmissionTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_TicketNumber",
                table: "SupportTickets",
                column: "TicketNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tag_Created",
                table: "Tags",
                column: "Created");

            migrationBuilder.CreateIndex(
                name: "IX_Tag_DeletedOn",
                table: "Tags",
                column: "DeletedOn");

            migrationBuilder.CreateIndex(
                name: "IX_Tag_IsDeleted",
                table: "Tags",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Tag_LastModified",
                table: "Tags",
                column: "LastModified");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_TagCategory",
                table: "Tags",
                column: "TagCategory");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_TagName",
                table: "Tags",
                column: "TagName");

            migrationBuilder.CreateIndex(
                name: "IX_TeamCart_Created",
                table: "TeamCarts",
                column: "Created");

            migrationBuilder.CreateIndex(
                name: "IX_TodoList_Created",
                table: "TodoLists",
                column: "Created");

            migrationBuilder.CreateIndex(
                name: "IX_TodoList_LastModified",
                table: "TodoLists",
                column: "LastModified");

            migrationBuilder.CreateIndex(
                name: "IX_UserDeviceSessions_DeviceId_IsActive",
                table: "UserDeviceSessions",
                columns: new[] { "DeviceId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountTransactions");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "Coupons");

            migrationBuilder.DropTable(
                name: "CouponUserUsages");

            migrationBuilder.DropTable(
                name: "CustomizationChoices");

            migrationBuilder.DropTable(
                name: "Devices");

            migrationBuilder.DropTable(
                name: "MenuCategories");

            migrationBuilder.DropTable(
                name: "MenuItems");

            migrationBuilder.DropTable(
                name: "Menus");

            migrationBuilder.DropTable(
                name: "OrderItems");

            migrationBuilder.DropTable(
                name: "PaymentTransactions");

            migrationBuilder.DropTable(
                name: "ProcessedWebhookEvents");

            migrationBuilder.DropTable(
                name: "RestaurantAccounts");

            migrationBuilder.DropTable(
                name: "Restaurants");

            migrationBuilder.DropTable(
                name: "Reviews");

            migrationBuilder.DropTable(
                name: "RoleAssignments");

            migrationBuilder.DropTable(
                name: "SupportTicketContextLinks");

            migrationBuilder.DropTable(
                name: "SupportTicketMessages");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropTable(
                name: "TeamCartItems");

            migrationBuilder.DropTable(
                name: "TeamCartMemberPayments");

            migrationBuilder.DropTable(
                name: "TeamCartMembers");

            migrationBuilder.DropTable(
                name: "TodoItems");

            migrationBuilder.DropTable(
                name: "UserAddresses");

            migrationBuilder.DropTable(
                name: "UserDeviceSessions");

            migrationBuilder.DropTable(
                name: "UserPaymentMethods");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "CustomizationGroups");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "SupportTickets");

            migrationBuilder.DropTable(
                name: "TeamCarts");

            migrationBuilder.DropTable(
                name: "TodoLists");

            migrationBuilder.DropTable(
                name: "DomainUsers");
        }
    }
}
