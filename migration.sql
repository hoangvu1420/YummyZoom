CREATE EXTENSION IF NOT EXISTS postgis;
CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE TABLE "AccountTransactions" (
        "Id" uuid NOT NULL,
        "RestaurantAccountId" uuid NOT NULL,
        "Type" character varying(50) NOT NULL,
        "Amount" numeric(18,2) NOT NULL,
        "Currency" character varying(3) NOT NULL,
        "Timestamp" timestamp with time zone NOT NULL,
        "RelatedOrderId" uuid,
        "Notes" character varying(500),
        "Created" timestamp with time zone NOT NULL,
        "CreatedBy" character varying(255),
        CONSTRAINT "PK_AccountTransactions" PRIMARY KEY ("Id")
    );
    COMMENT ON COLUMN "AccountTransactions"."Created" IS 'Timestamp when the entity was created';
    COMMENT ON COLUMN "AccountTransactions"."CreatedBy" IS 'Identifier of who created the entity';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE TABLE "AspNetRoles" (
        "Id" uuid NOT NULL,
        "Name" character varying(256),
        "NormalizedName" character varying(256),
        "ConcurrencyStamp" text,
        CONSTRAINT "PK_AspNetRoles" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE TABLE "AspNetUsers" (
        "Id" uuid NOT NULL,
        "UserName" character varying(256),
        "NormalizedUserName" character varying(256),
        "Email" character varying(256),
        "NormalizedEmail" character varying(256),
        "EmailConfirmed" boolean NOT NULL,
        "PasswordHash" text,
        "SecurityStamp" text,
        "ConcurrencyStamp" text,
        "PhoneNumber" text,
        "PhoneNumberConfirmed" boolean NOT NULL,
        "TwoFactorEnabled" boolean NOT NULL,
        "LockoutEnd" timestamp with time zone,
        "LockoutEnabled" boolean NOT NULL,
        "AccessFailedCount" integer NOT NULL,
        CONSTRAINT "PK_AspNetUsers" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE TABLE "Coupons" (
        "Id" uuid NOT NULL,
        "RestaurantId" uuid NOT NULL,
        "Code" character varying(50) NOT NULL,
        "Description" character varying(500) NOT NULL,
        "Value_Type" text NOT NULL,
        "Value_PercentageValue" numeric(5,2),
        "Value_FixedAmount_Amount" numeric(18,2),
        "Value_FixedAmount_Currency" character varying(3),
        "Value_FreeItemValue" uuid,
        "AppliesTo_Scope" text NOT NULL,
        "AppliesTo_ItemIds" jsonb NOT NULL,
        "AppliesTo_CategoryIds" jsonb NOT NULL,
        "MinOrderAmount_Amount" numeric(18,2),
        "MinOrderAmount_Currency" character varying(3),
        "ValidityStartDate" timestamp with time zone NOT NULL,
        "ValidityEndDate" timestamp with time zone NOT NULL,
        "TotalUsageLimit" integer,
        "CurrentTotalUsageCount" integer NOT NULL DEFAULT 0,
        "IsEnabled" boolean NOT NULL DEFAULT TRUE,
        "UsageLimitPerUser" integer NOT NULL,
        "Created" timestamp with time zone NOT NULL,
        "CreatedBy" character varying(255),
        "LastModified" timestamp with time zone NOT NULL,
        "LastModifiedBy" character varying(255),
        "IsDeleted" boolean NOT NULL DEFAULT FALSE,
        "DeletedOn" timestamp with time zone,
        "DeletedBy" character varying(255),
        CONSTRAINT "PK_Coupons" PRIMARY KEY ("Id")
    );
    COMMENT ON COLUMN "Coupons"."Created" IS 'Timestamp when the entity was created';
    COMMENT ON COLUMN "Coupons"."CreatedBy" IS 'Identifier of who created the entity';
    COMMENT ON COLUMN "Coupons"."LastModified" IS 'Timestamp when the entity was last modified';
    COMMENT ON COLUMN "Coupons"."LastModifiedBy" IS 'Identifier of who last modified the entity';
    COMMENT ON COLUMN "Coupons"."IsDeleted" IS 'Indicates if the entity is soft-deleted';
    COMMENT ON COLUMN "Coupons"."DeletedOn" IS 'Timestamp when the entity was deleted';
    COMMENT ON COLUMN "Coupons"."DeletedBy" IS 'Identifier of who deleted the entity';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE TABLE "CouponUserUsages" (
        "CouponId" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "UsageCount" integer NOT NULL DEFAULT 0,
        CONSTRAINT "PK_CouponUserUsages" PRIMARY KEY ("CouponId", "UserId")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE TABLE "CustomizationGroups" (
        "Id" uuid NOT NULL,
        "RestaurantId" uuid NOT NULL,
        "GroupName" character varying(200) NOT NULL,
        "MinSelections" integer NOT NULL,
        "MaxSelections" integer NOT NULL,
        "Created" timestamp with time zone NOT NULL,
        "CreatedBy" character varying(255),
        "LastModified" timestamp with time zone NOT NULL,
        "LastModifiedBy" character varying(255),
        "IsDeleted" boolean NOT NULL DEFAULT FALSE,
        "DeletedOn" timestamp with time zone,
        "DeletedBy" character varying(255),
        CONSTRAINT "PK_CustomizationGroups" PRIMARY KEY ("Id")
    );
    COMMENT ON COLUMN "CustomizationGroups"."Created" IS 'Timestamp when the entity was created';
    COMMENT ON COLUMN "CustomizationGroups"."CreatedBy" IS 'Identifier of who created the entity';
    COMMENT ON COLUMN "CustomizationGroups"."LastModified" IS 'Timestamp when the entity was last modified';
    COMMENT ON COLUMN "CustomizationGroups"."LastModifiedBy" IS 'Identifier of who last modified the entity';
    COMMENT ON COLUMN "CustomizationGroups"."IsDeleted" IS 'Indicates if the entity is soft-deleted';
    COMMENT ON COLUMN "CustomizationGroups"."DeletedOn" IS 'Timestamp when the entity was deleted';
    COMMENT ON COLUMN "CustomizationGroups"."DeletedBy" IS 'Identifier of who deleted the entity';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE TABLE "Devices" (
        "Id" uuid NOT NULL,
        "DeviceId" text,
        "Platform" text NOT NULL,
        "ModelName" text,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Devices" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE TABLE "DomainUsers" (
        "Id" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Email" character varying(255) NOT NULL,
        "PhoneNumber" character varying(50),
        "IsActive" boolean NOT NULL,
        "Created" timestamp with time zone NOT NULL,
        "CreatedBy" character varying(255),
        "LastModified" timestamp with time zone NOT NULL,
        "LastModifiedBy" character varying(255),
        "IsDeleted" boolean NOT NULL DEFAULT FALSE,
        "DeletedOn" timestamp with time zone,
        "DeletedBy" character varying(255),
        CONSTRAINT "PK_DomainUsers" PRIMARY KEY ("Id")
    );
    COMMENT ON COLUMN "DomainUsers"."Created" IS 'Timestamp when the entity was created';
    COMMENT ON COLUMN "DomainUsers"."CreatedBy" IS 'Identifier of who created the entity';
    COMMENT ON COLUMN "DomainUsers"."LastModified" IS 'Timestamp when the entity was last modified';
    COMMENT ON COLUMN "DomainUsers"."LastModifiedBy" IS 'Identifier of who last modified the entity';
    COMMENT ON COLUMN "DomainUsers"."IsDeleted" IS 'Indicates if the entity is soft-deleted';
    COMMENT ON COLUMN "DomainUsers"."DeletedOn" IS 'Timestamp when the entity was deleted';
    COMMENT ON COLUMN "DomainUsers"."DeletedBy" IS 'Identifier of who deleted the entity';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE TABLE "FullMenuViews" (
        "RestaurantId" uuid NOT NULL,
        "MenuJson" jsonb NOT NULL,
        "LastRebuiltAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_FullMenuViews" PRIMARY KEY ("RestaurantId")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE TABLE "InboxMessages" (
        "EventId" uuid NOT NULL,
        "Handler" character varying(256) NOT NULL,
        "ProcessedOnUtc" timestamp with time zone NOT NULL,
        "Error" text,
        CONSTRAINT "PK_InboxMessages" PRIMARY KEY ("EventId", "Handler")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE TABLE "MenuCategories" (
        "Id" uuid NOT NULL,
        "MenuId" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "DisplayOrder" integer NOT NULL,
        "Created" timestamp with time zone NOT NULL,
        "CreatedBy" character varying(255),
        "LastModified" timestamp with time zone NOT NULL,
        "LastModifiedBy" character varying(255),
        "IsDeleted" boolean NOT NULL DEFAULT FALSE,
        "DeletedOn" timestamp with time zone,
        "DeletedBy" character varying(255),
        CONSTRAINT "PK_MenuCategories" PRIMARY KEY ("Id")
    );
    COMMENT ON COLUMN "MenuCategories"."Created" IS 'Timestamp when the entity was created';
    COMMENT ON COLUMN "MenuCategories"."CreatedBy" IS 'Identifier of who created the entity';
    COMMENT ON COLUMN "MenuCategories"."LastModified" IS 'Timestamp when the entity was last modified';
    COMMENT ON COLUMN "MenuCategories"."LastModifiedBy" IS 'Identifier of who last modified the entity';
    COMMENT ON COLUMN "MenuCategories"."IsDeleted" IS 'Indicates if the entity is soft-deleted';
    COMMENT ON COLUMN "MenuCategories"."DeletedOn" IS 'Timestamp when the entity was deleted';
    COMMENT ON COLUMN "MenuCategories"."DeletedBy" IS 'Identifier of who deleted the entity';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE TABLE "MenuItems" (
        "Id" uuid NOT NULL,
        "RestaurantId" uuid NOT NULL,
        "MenuCategoryId" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Description" character varying(1000) NOT NULL,
        "BasePrice_Amount" numeric(18,2) NOT NULL,
        "BasePrice_Currency" character varying(3) NOT NULL,
        "ImageUrl" character varying(500),
        "IsAvailable" boolean NOT NULL DEFAULT TRUE,
        "Created" timestamp with time zone NOT NULL,
        "CreatedBy" character varying(255),
        "LastModified" timestamp with time zone NOT NULL,
        "LastModifiedBy" character varying(255),
        "IsDeleted" boolean NOT NULL DEFAULT FALSE,
        "DeletedOn" timestamp with time zone,
        "DeletedBy" character varying(255),
        "DietaryTagIds" jsonb NOT NULL,
        "AppliedCustomizations" jsonb NOT NULL,
        CONSTRAINT "PK_MenuItems" PRIMARY KEY ("Id")
    );
    COMMENT ON COLUMN "MenuItems"."Created" IS 'Timestamp when the entity was created';
    COMMENT ON COLUMN "MenuItems"."CreatedBy" IS 'Identifier of who created the entity';
    COMMENT ON COLUMN "MenuItems"."LastModified" IS 'Timestamp when the entity was last modified';
    COMMENT ON COLUMN "MenuItems"."LastModifiedBy" IS 'Identifier of who last modified the entity';
    COMMENT ON COLUMN "MenuItems"."IsDeleted" IS 'Indicates if the entity is soft-deleted';
    COMMENT ON COLUMN "MenuItems"."DeletedOn" IS 'Timestamp when the entity was deleted';
    COMMENT ON COLUMN "MenuItems"."DeletedBy" IS 'Identifier of who deleted the entity';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE TABLE "Menus" (
        "Id" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Description" character varying(500) NOT NULL,
        "IsEnabled" boolean NOT NULL,
        "RestaurantId" uuid NOT NULL,
        "Created" timestamp with time zone NOT NULL,
        "CreatedBy" character varying(255),
        "LastModified" timestamp with time zone NOT NULL,
        "LastModifiedBy" character varying(255),
        "IsDeleted" boolean NOT NULL DEFAULT FALSE,
        "DeletedOn" timestamp with time zone,
        "DeletedBy" character varying(255),
        CONSTRAINT "PK_Menus" PRIMARY KEY ("Id")
    );
    COMMENT ON COLUMN "Menus"."Created" IS 'Timestamp when the entity was created';
    COMMENT ON COLUMN "Menus"."CreatedBy" IS 'Identifier of who created the entity';
    COMMENT ON COLUMN "Menus"."LastModified" IS 'Timestamp when the entity was last modified';
    COMMENT ON COLUMN "Menus"."LastModifiedBy" IS 'Identifier of who last modified the entity';
    COMMENT ON COLUMN "Menus"."IsDeleted" IS 'Indicates if the entity is soft-deleted';
    COMMENT ON COLUMN "Menus"."DeletedOn" IS 'Timestamp when the entity was deleted';
    COMMENT ON COLUMN "Menus"."DeletedBy" IS 'Identifier of who deleted the entity';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE TABLE "Orders" (
        "Id" uuid NOT NULL,
        "Created" timestamp with time zone NOT NULL,
        "CreatedBy" character varying(255),
        "OrderNumber" character varying(50) NOT NULL,
        "Status" character varying(50) NOT NULL,
        "PlacementTimestamp" timestamp with time zone NOT NULL,
        "LastUpdateTimestamp" timestamp with time zone NOT NULL,
        "EstimatedDeliveryTime" timestamp with time zone,
        "ActualDeliveryTime" timestamp with time zone,
        "SpecialInstructions" text,
        "DeliveryAddress_Street" character varying(255) NOT NULL,
        "DeliveryAddress_City" character varying(100) NOT NULL,
        "DeliveryAddress_State" character varying(100) NOT NULL,
        "DeliveryAddress_ZipCode" character varying(20) NOT NULL,
        "DeliveryAddress_Country" character varying(100) NOT NULL,
        "Subtotal_Amount" numeric(18,2) NOT NULL,
        "Subtotal_Currency" character varying(3) NOT NULL,
        "DiscountAmount_Amount" numeric(18,2) NOT NULL,
        "DiscountAmount_Currency" character varying(3) NOT NULL,
        "DeliveryFee_Amount" numeric(18,2) NOT NULL,
        "DeliveryFee_Currency" character varying(3) NOT NULL,
        "TipAmount_Amount" numeric(18,2) NOT NULL,
        "TipAmount_Currency" character varying(3) NOT NULL,
        "TaxAmount_Amount" numeric(18,2) NOT NULL,
        "TaxAmount_Currency" character varying(3) NOT NULL,
        "TotalAmount_Amount" numeric(18,2) NOT NULL,
        "TotalAmount_Currency" character varying(3) NOT NULL,
        "CustomerId" uuid NOT NULL,
        "RestaurantId" uuid NOT NULL,
        "SourceTeamCartId" uuid,
        "AppliedCouponId" uuid,
        CONSTRAINT "PK_Orders" PRIMARY KEY ("Id")
    );
    COMMENT ON COLUMN "Orders"."Created" IS 'Timestamp when the entity was created';
    COMMENT ON COLUMN "Orders"."CreatedBy" IS 'Identifier of who created the entity';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE TABLE "OutboxMessages" (
        "Id" uuid NOT NULL,
        "OccurredOnUtc" timestamp with time zone NOT NULL,
        "Type" character varying(512) NOT NULL,
        "Content" jsonb NOT NULL,
        "CorrelationId" text,
        "CausationId" text,
        "AggregateId" text,
        "AggregateType" text,
        "Attempt" integer NOT NULL,
        "NextAttemptOnUtc" timestamp with time zone,
        "ProcessedOnUtc" timestamp with time zone,
        "Error" text,
        CONSTRAINT "PK_OutboxMessages" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE TABLE "ProcessedWebhookEvents" (
        "Id" text NOT NULL,
        "ProcessedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_ProcessedWebhookEvents" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE TABLE "RestaurantAccounts" (
        "Id" uuid NOT NULL,
        "RestaurantId" uuid NOT NULL,
        "CurrentBalance_Amount" numeric(18,2) NOT NULL,
        "CurrentBalance_Currency" character varying(3) NOT NULL,
        "PayoutMethod_Details" character varying(500),
        "Created" timestamp with time zone NOT NULL,
        "CreatedBy" character varying(255),
        CONSTRAINT "PK_RestaurantAccounts" PRIMARY KEY ("Id")
    );
    COMMENT ON COLUMN "RestaurantAccounts"."Created" IS 'Timestamp when the entity was created';
    COMMENT ON COLUMN "RestaurantAccounts"."CreatedBy" IS 'Identifier of who created the entity';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE TABLE "RestaurantReviewSummaries" (
        "RestaurantId" uuid NOT NULL,
        "AverageRating" double precision NOT NULL DEFAULT 0.0,
        "TotalReviews" integer NOT NULL DEFAULT 0,
        CONSTRAINT "PK_RestaurantReviewSummaries" PRIMARY KEY ("RestaurantId")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE TABLE "Restaurants" (
        "Id" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "LogoUrl" character varying(500),
        "BackgroundImageUrl" character varying(500),
        "Description" character varying(1000) NOT NULL,
        "CuisineType" character varying(200) NOT NULL,
        "Location_Street" character varying(255) NOT NULL,
        "Location_City" character varying(100) NOT NULL,
        "Location_State" character varying(100) NOT NULL,
        "Location_ZipCode" character varying(20) NOT NULL,
        "Location_Country" character varying(100) NOT NULL,
        "Geo_Latitude" double precision,
        "Geo_Longitude" double precision,
        "ContactInfo_PhoneNumber" character varying(50) NOT NULL,
        "ContactInfo_Email" character varying(255) NOT NULL,
        "BusinessHours" character varying(200) NOT NULL,
        "IsVerified" boolean NOT NULL DEFAULT FALSE,
        "IsAcceptingOrders" boolean NOT NULL DEFAULT FALSE,
        "Created" timestamp with time zone NOT NULL,
        "CreatedBy" character varying(255),
        "LastModified" timestamp with time zone NOT NULL,
        "LastModifiedBy" character varying(255),
        "IsDeleted" boolean NOT NULL DEFAULT FALSE,
        "DeletedOn" timestamp with time zone,
        "DeletedBy" character varying(255),
        CONSTRAINT "PK_Restaurants" PRIMARY KEY ("Id")
    );
    COMMENT ON COLUMN "Restaurants"."Created" IS 'Timestamp when the entity was created';
    COMMENT ON COLUMN "Restaurants"."CreatedBy" IS 'Identifier of who created the entity';
    COMMENT ON COLUMN "Restaurants"."LastModified" IS 'Timestamp when the entity was last modified';
    COMMENT ON COLUMN "Restaurants"."LastModifiedBy" IS 'Identifier of who last modified the entity';
    COMMENT ON COLUMN "Restaurants"."IsDeleted" IS 'Indicates if the entity is soft-deleted';
    COMMENT ON COLUMN "Restaurants"."DeletedOn" IS 'Timestamp when the entity was deleted';
    COMMENT ON COLUMN "Restaurants"."DeletedBy" IS 'Identifier of who deleted the entity';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE TABLE "Reviews" (
        "Id" uuid NOT NULL,
        "OrderId" uuid NOT NULL,
        "CustomerId" uuid NOT NULL,
        "RestaurantId" uuid NOT NULL,
        "Rating" integer NOT NULL,
        "Comment" character varying(2000),
        "SubmissionTimestamp" timestamp with time zone NOT NULL,
        "IsModerated" boolean NOT NULL,
        "IsHidden" boolean NOT NULL,
        "Reply" character varying(2000),
        "Created" timestamp with time zone NOT NULL,
        "CreatedBy" character varying(255),
        "LastModified" timestamp with time zone NOT NULL,
        "LastModifiedBy" character varying(255),
        "IsDeleted" boolean NOT NULL DEFAULT FALSE,
        "DeletedOn" timestamp with time zone,
        "DeletedBy" character varying(255),
        CONSTRAINT "PK_Reviews" PRIMARY KEY ("Id")
    );
    COMMENT ON COLUMN "Reviews"."Created" IS 'Timestamp when the entity was created';
    COMMENT ON COLUMN "Reviews"."CreatedBy" IS 'Identifier of who created the entity';
    COMMENT ON COLUMN "Reviews"."LastModified" IS 'Timestamp when the entity was last modified';
    COMMENT ON COLUMN "Reviews"."LastModifiedBy" IS 'Identifier of who last modified the entity';
    COMMENT ON COLUMN "Reviews"."IsDeleted" IS 'Indicates if the entity is soft-deleted';
    COMMENT ON COLUMN "Reviews"."DeletedOn" IS 'Timestamp when the entity was deleted';
    COMMENT ON COLUMN "Reviews"."DeletedBy" IS 'Identifier of who deleted the entity';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE TABLE "RoleAssignments" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "RestaurantId" uuid NOT NULL,
        "Role" text NOT NULL,
        "Created" timestamp with time zone NOT NULL,
        "CreatedBy" character varying(255),
        CONSTRAINT "PK_RoleAssignments" PRIMARY KEY ("Id")
    );
    COMMENT ON COLUMN "RoleAssignments"."Created" IS 'Timestamp when the entity was created';
    COMMENT ON COLUMN "RoleAssignments"."CreatedBy" IS 'Identifier of who created the entity';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE TABLE "SupportTickets" (
        "Id" uuid NOT NULL,
        "TicketNumber" character varying(50) NOT NULL,
        "Subject" character varying(200) NOT NULL,
        "Status" character varying(50) NOT NULL,
        "Priority" character varying(50) NOT NULL,
        "Type" character varying(50) NOT NULL,
        "SubmissionTimestamp" timestamp with time zone NOT NULL,
        "LastUpdateTimestamp" timestamp with time zone NOT NULL,
        "AssignedToAdminId" uuid,
        "Created" timestamp with time zone NOT NULL,
        "CreatedBy" character varying(255),
        CONSTRAINT "PK_SupportTickets" PRIMARY KEY ("Id")
    );
    COMMENT ON COLUMN "SupportTickets"."Created" IS 'Timestamp when the entity was created';
    COMMENT ON COLUMN "SupportTickets"."CreatedBy" IS 'Identifier of who created the entity';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE TABLE "Tags" (
        "Id" uuid NOT NULL,
        "TagName" character varying(100) NOT NULL,
        "TagDescription" character varying(500),
        "TagCategory" character varying(50) NOT NULL,
        "Created" timestamp with time zone NOT NULL,
        "CreatedBy" character varying(255),
        "LastModified" timestamp with time zone NOT NULL,
        "LastModifiedBy" character varying(255),
        "IsDeleted" boolean NOT NULL DEFAULT FALSE,
        "DeletedOn" timestamp with time zone,
        "DeletedBy" character varying(255),
        CONSTRAINT "PK_Tags" PRIMARY KEY ("Id")
    );
    COMMENT ON COLUMN "Tags"."Created" IS 'Timestamp when the entity was created';
    COMMENT ON COLUMN "Tags"."CreatedBy" IS 'Identifier of who created the entity';
    COMMENT ON COLUMN "Tags"."LastModified" IS 'Timestamp when the entity was last modified';
    COMMENT ON COLUMN "Tags"."LastModifiedBy" IS 'Identifier of who last modified the entity';
    COMMENT ON COLUMN "Tags"."IsDeleted" IS 'Indicates if the entity is soft-deleted';
    COMMENT ON COLUMN "Tags"."DeletedOn" IS 'Timestamp when the entity was deleted';
    COMMENT ON COLUMN "Tags"."DeletedBy" IS 'Identifier of who deleted the entity';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE TABLE "TeamCarts" (
        "Id" uuid NOT NULL,
        "Created" timestamp with time zone NOT NULL,
        "CreatedBy" character varying(255),
        "RestaurantId" uuid NOT NULL,
        "HostUserId" uuid NOT NULL,
        "Status" character varying(50) NOT NULL,
        "ShareToken_Value" character varying(50) NOT NULL,
        "ShareToken_ExpiresAt" timestamp with time zone NOT NULL,
        "Deadline" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        "ExpiresAt" timestamp with time zone NOT NULL,
        "TipAmount_Amount" numeric(18,2) NOT NULL,
        "TipAmount_Currency" character varying(3) NOT NULL,
        "AppliedCouponId" uuid,
        CONSTRAINT "PK_TeamCarts" PRIMARY KEY ("Id")
    );
    COMMENT ON COLUMN "TeamCarts"."Created" IS 'Timestamp when the entity was created';
    COMMENT ON COLUMN "TeamCarts"."CreatedBy" IS 'Identifier of who created the entity';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE TABLE "TodoLists" (
        "Id" uuid NOT NULL,
        "Title" character varying(200) NOT NULL,
        "Colour" character varying(10) NOT NULL,
        "Created" timestamp with time zone NOT NULL,
        "CreatedBy" character varying(255),
        "LastModified" timestamp with time zone NOT NULL,
        "LastModifiedBy" character varying(255),
        CONSTRAINT "PK_TodoLists" PRIMARY KEY ("Id")
    );
    COMMENT ON COLUMN "TodoLists"."Created" IS 'Timestamp when the entity was created';
    COMMENT ON COLUMN "TodoLists"."CreatedBy" IS 'Identifier of who created the entity';
    COMMENT ON COLUMN "TodoLists"."LastModified" IS 'Timestamp when the entity was last modified';
    COMMENT ON COLUMN "TodoLists"."LastModifiedBy" IS 'Identifier of who last modified the entity';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE TABLE "UserDeviceSessions" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "DeviceId" uuid NOT NULL,
        "FcmToken" text NOT NULL,
        "IsActive" boolean NOT NULL,
        "LastLoginAt" timestamp with time zone NOT NULL,
        "LoggedOutAt" timestamp with time zone,
        CONSTRAINT "PK_UserDeviceSessions" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE TABLE "AspNetRoleClaims" (
        "Id" integer GENERATED BY DEFAULT AS IDENTITY,
        "RoleId" uuid NOT NULL,
        "ClaimType" text,
        "ClaimValue" text,
        CONSTRAINT "PK_AspNetRoleClaims" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_AspNetRoleClaims_AspNetRoles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE TABLE "AspNetUserClaims" (
        "Id" integer GENERATED BY DEFAULT AS IDENTITY,
        "UserId" uuid NOT NULL,
        "ClaimType" text,
        "ClaimValue" text,
        CONSTRAINT "PK_AspNetUserClaims" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_AspNetUserClaims_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE TABLE "AspNetUserLogins" (
        "LoginProvider" text NOT NULL,
        "ProviderKey" text NOT NULL,
        "ProviderDisplayName" text,
        "UserId" uuid NOT NULL,
        CONSTRAINT "PK_AspNetUserLogins" PRIMARY KEY ("LoginProvider", "ProviderKey"),
        CONSTRAINT "FK_AspNetUserLogins_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE TABLE "AspNetUserRoles" (
        "UserId" uuid NOT NULL,
        "RoleId" uuid NOT NULL,
        CONSTRAINT "PK_AspNetUserRoles" PRIMARY KEY ("UserId", "RoleId"),
        CONSTRAINT "FK_AspNetUserRoles_AspNetRoles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_AspNetUserRoles_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE TABLE "AspNetUserTokens" (
        "UserId" uuid NOT NULL,
        "LoginProvider" text NOT NULL,
        "Name" text NOT NULL,
        "Value" text,
        CONSTRAINT "PK_AspNetUserTokens" PRIMARY KEY ("UserId", "LoginProvider", "Name"),
        CONSTRAINT "FK_AspNetUserTokens_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE TABLE "CustomizationChoices" (
        "ChoiceId" uuid NOT NULL,
        "CustomizationGroupId" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "PriceAdjustment_Amount" numeric(18,2) NOT NULL,
        "PriceAdjustment_Currency" character varying(3) NOT NULL,
        "IsDefault" boolean NOT NULL,
        "DisplayOrder" integer NOT NULL,
        CONSTRAINT "PK_CustomizationChoices" PRIMARY KEY ("CustomizationGroupId", "ChoiceId"),
        CONSTRAINT "FK_CustomizationChoices_CustomizationGroups_CustomizationGroup~" FOREIGN KEY ("CustomizationGroupId") REFERENCES "CustomizationGroups" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE TABLE "UserAddresses" (
        "AddressId" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "Street" character varying(255) NOT NULL,
        "City" character varying(100) NOT NULL,
        "State" character varying(100),
        "ZipCode" character varying(20) NOT NULL,
        "Country" character varying(100) NOT NULL,
        "Label" character varying(100),
        "DeliveryInstructions" character varying(500),
        CONSTRAINT "PK_UserAddresses" PRIMARY KEY ("UserId", "AddressId"),
        CONSTRAINT "FK_UserAddresses_DomainUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "DomainUsers" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE TABLE "UserPaymentMethods" (
        "PaymentMethodId" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "Type" character varying(50) NOT NULL,
        "TokenizedDetails" character varying(500) NOT NULL,
        "IsDefault" boolean NOT NULL,
        "Created" timestamp with time zone NOT NULL,
        "CreatedBy" text,
        "LastModified" timestamp with time zone NOT NULL,
        "LastModifiedBy" text,
        CONSTRAINT "PK_UserPaymentMethods" PRIMARY KEY ("UserId", "PaymentMethodId"),
        CONSTRAINT "FK_UserPaymentMethods_DomainUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "DomainUsers" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE TABLE "OrderItems" (
        "OrderItemId" uuid NOT NULL,
        "OrderId" uuid NOT NULL,
        "Snapshot_MenuCategoryId" uuid NOT NULL,
        "Snapshot_MenuItemId" uuid NOT NULL,
        "Snapshot_ItemName" character varying(200) NOT NULL,
        "BasePrice_Amount" numeric(18,2) NOT NULL,
        "BasePrice_Currency" character varying(3) NOT NULL,
        "Quantity" integer NOT NULL,
        "LineItemTotal_Amount" numeric(18,2) NOT NULL,
        "LineItemTotal_Currency" character varying(3) NOT NULL,
        "SelectedCustomizations" jsonb NOT NULL,
        CONSTRAINT "PK_OrderItems" PRIMARY KEY ("OrderId", "OrderItemId"),
        CONSTRAINT "FK_OrderItems_Orders_OrderId" FOREIGN KEY ("OrderId") REFERENCES "Orders" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE TABLE "PaymentTransactions" (
        "PaymentTransactionId" uuid NOT NULL,
        "OrderId" uuid NOT NULL,
        "PaymentMethodType" character varying(50) NOT NULL,
        "PaymentMethodDisplay" character varying(100),
        "Type" character varying(50) NOT NULL,
        "Transaction_Amount" numeric(18,2) NOT NULL,
        "Transaction_Currency" character varying(3) NOT NULL,
        "Status" character varying(50) NOT NULL,
        "Timestamp" timestamp with time zone NOT NULL,
        "PaymentGatewayReferenceId" character varying(255),
        "PaidByUserId" uuid,
        CONSTRAINT "PK_PaymentTransactions" PRIMARY KEY ("OrderId", "PaymentTransactionId"),
        CONSTRAINT "FK_PaymentTransactions_Orders_OrderId" FOREIGN KEY ("OrderId") REFERENCES "Orders" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE TABLE "SupportTicketContextLinks" (
        "EntityType" character varying(50) NOT NULL,
        "EntityID" uuid NOT NULL,
        "SupportTicketId" uuid NOT NULL,
        CONSTRAINT "PK_SupportTicketContextLinks" PRIMARY KEY ("SupportTicketId", "EntityID", "EntityType"),
        CONSTRAINT "FK_SupportTicketContextLinks_SupportTickets_SupportTicketId" FOREIGN KEY ("SupportTicketId") REFERENCES "SupportTickets" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE TABLE "SupportTicketMessages" (
        "MessageId" uuid NOT NULL,
        "SupportTicketId" uuid NOT NULL,
        "AuthorId" uuid NOT NULL,
        "AuthorType" character varying(50) NOT NULL,
        "MessageText" character varying(5000) NOT NULL,
        "Timestamp" timestamp with time zone NOT NULL,
        "IsInternalNote" boolean NOT NULL,
        CONSTRAINT "PK_SupportTicketMessages" PRIMARY KEY ("SupportTicketId", "MessageId"),
        CONSTRAINT "FK_SupportTicketMessages_SupportTickets_SupportTicketId" FOREIGN KEY ("SupportTicketId") REFERENCES "SupportTickets" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE TABLE "TeamCartItems" (
        "TeamCartItemId" uuid NOT NULL,
        "TeamCartId" uuid NOT NULL,
        "AddedByUserId" uuid NOT NULL,
        "Snapshot_MenuItemId" uuid NOT NULL,
        "Snapshot_MenuCategoryId" uuid NOT NULL,
        "Snapshot_ItemName" character varying(200) NOT NULL,
        "BasePrice_Amount" numeric(18,2) NOT NULL,
        "BasePrice_Currency" character varying(3) NOT NULL,
        "Quantity" integer NOT NULL,
        "LineItemTotal_Amount" numeric(18,2) NOT NULL,
        "LineItemTotal_Currency" character varying(3) NOT NULL,
        "SelectedCustomizations" jsonb NOT NULL,
        CONSTRAINT "PK_TeamCartItems" PRIMARY KEY ("TeamCartId", "TeamCartItemId"),
        CONSTRAINT "FK_TeamCartItems_TeamCarts_TeamCartId" FOREIGN KEY ("TeamCartId") REFERENCES "TeamCarts" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE TABLE "TeamCartMemberPayments" (
        "MemberPaymentId" uuid NOT NULL,
        "TeamCartId" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "Payment_Amount" numeric(18,2) NOT NULL,
        "Payment_Currency" character varying(3) NOT NULL,
        "Method" character varying(50) NOT NULL,
        "Status" character varying(50) NOT NULL,
        "OnlineTransactionId" character varying(255),
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_TeamCartMemberPayments" PRIMARY KEY ("TeamCartId", "MemberPaymentId"),
        CONSTRAINT "FK_TeamCartMemberPayments_TeamCarts_TeamCartId" FOREIGN KEY ("TeamCartId") REFERENCES "TeamCarts" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE TABLE "TeamCartMembers" (
        "TeamCartMemberId" uuid NOT NULL,
        "TeamCartId" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Role" character varying(50) NOT NULL,
        CONSTRAINT "PK_TeamCartMembers" PRIMARY KEY ("TeamCartId", "TeamCartMemberId"),
        CONSTRAINT "FK_TeamCartMembers_TeamCarts_TeamCartId" FOREIGN KEY ("TeamCartId") REFERENCES "TeamCarts" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE TABLE "TodoItems" (
        "TodoItemId" uuid NOT NULL,
        "TodoListId" uuid NOT NULL,
        "Title" character varying(200),
        "Note" character varying(1000),
        "Priority" integer NOT NULL,
        "Reminder" timestamp with time zone,
        "IsDone" boolean NOT NULL,
        "Created" timestamp with time zone NOT NULL,
        "CreatedBy" text,
        "LastModified" timestamp with time zone NOT NULL,
        "LastModifiedBy" text,
        CONSTRAINT "PK_TodoItems" PRIMARY KEY ("TodoListId", "TodoItemId"),
        CONSTRAINT "FK_TodoItems_TodoLists_TodoListId" FOREIGN KEY ("TodoListId") REFERENCES "TodoLists" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_AccountTransaction_Created" ON "AccountTransactions" ("Created");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_AccountTransactions_RestaurantAccountId" ON "AccountTransactions" ("RestaurantAccountId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_AccountTransactions_Timestamp" ON "AccountTransactions" ("Timestamp");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_AspNetRoleClaims_RoleId" ON "AspNetRoleClaims" ("RoleId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE UNIQUE INDEX "RoleNameIndex" ON "AspNetRoles" ("NormalizedName");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_AspNetUserClaims_UserId" ON "AspNetUserClaims" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_AspNetUserLogins_UserId" ON "AspNetUserLogins" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_AspNetUserRoles_RoleId" ON "AspNetUserRoles" ("RoleId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "EmailIndex" ON "AspNetUsers" ("NormalizedEmail");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE UNIQUE INDEX "UserNameIndex" ON "AspNetUsers" ("NormalizedUserName");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_Coupon_Created" ON "Coupons" ("Created");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_Coupon_DeletedOn" ON "Coupons" ("DeletedOn");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_Coupon_IsDeleted" ON "Coupons" ("IsDeleted");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_Coupon_LastModified" ON "Coupons" ("LastModified");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE UNIQUE INDEX "IX_Coupons_Code_RestaurantId" ON "Coupons" ("Code", "RestaurantId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_Coupons_CurrentTotalUsageCount" ON "Coupons" ("CurrentTotalUsageCount");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_CustomizationGroup_Created" ON "CustomizationGroups" ("Created");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_CustomizationGroup_DeletedOn" ON "CustomizationGroups" ("DeletedOn");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_CustomizationGroup_IsDeleted" ON "CustomizationGroups" ("IsDeleted");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_CustomizationGroup_LastModified" ON "CustomizationGroups" ("LastModified");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE UNIQUE INDEX "IX_Devices_DeviceId" ON "Devices" ("DeviceId") WHERE "DeviceId" IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE UNIQUE INDEX "IX_DomainUsers_Email" ON "DomainUsers" ("Email");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_User_Created" ON "DomainUsers" ("Created");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_User_DeletedOn" ON "DomainUsers" ("DeletedOn");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_User_IsDeleted" ON "DomainUsers" ("IsDeleted");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_User_LastModified" ON "DomainUsers" ("LastModified");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_FullMenuViews_LastRebuiltAt" ON "FullMenuViews" ("LastRebuiltAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_InboxMessages_ProcessedOnUtc" ON "InboxMessages" ("ProcessedOnUtc");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_MenuCategories_MenuId" ON "MenuCategories" ("MenuId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_MenuCategories_MenuId_DisplayOrder" ON "MenuCategories" ("MenuId", "DisplayOrder");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_MenuCategory_Created" ON "MenuCategories" ("Created");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_MenuCategory_DeletedOn" ON "MenuCategories" ("DeletedOn");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_MenuCategory_IsDeleted" ON "MenuCategories" ("IsDeleted");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_MenuCategory_LastModified" ON "MenuCategories" ("LastModified");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_MenuItem_Created" ON "MenuItems" ("Created");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_MenuItem_DeletedOn" ON "MenuItems" ("DeletedOn");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_MenuItem_IsDeleted" ON "MenuItems" ("IsDeleted");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_MenuItem_LastModified" ON "MenuItems" ("LastModified");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_MenuItems_Category_Available" ON "MenuItems" ("MenuCategoryId", "IsAvailable");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_MenuItems_IsAvailable" ON "MenuItems" ("IsAvailable");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_MenuItems_MenuCategoryId" ON "MenuItems" ("MenuCategoryId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_MenuItems_Name" ON "MenuItems" ("Name");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_MenuItems_Restaurant_Available" ON "MenuItems" ("RestaurantId", "IsAvailable");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_MenuItems_RestaurantId" ON "MenuItems" ("RestaurantId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_Menu_Created" ON "Menus" ("Created");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_Menu_DeletedOn" ON "Menus" ("DeletedOn");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_Menu_IsDeleted" ON "Menus" ("IsDeleted");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_Menu_LastModified" ON "Menus" ("LastModified");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_Menus_RestaurantId" ON "Menus" ("RestaurantId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_Order_Created" ON "Orders" ("Created");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE UNIQUE INDEX "IX_Orders_OrderNumber" ON "Orders" ("OrderNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_OutboxMessages_NextAttemptOnUtc" ON "OutboxMessages" ("NextAttemptOnUtc");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_OutboxMessages_OccurredOnUtc" ON "OutboxMessages" ("OccurredOnUtc");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_OutboxMessages_ProcessedOnUtc" ON "OutboxMessages" ("ProcessedOnUtc");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_RestaurantAccount_Created" ON "RestaurantAccounts" ("Created");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE UNIQUE INDEX "IX_RestaurantAccounts_RestaurantId" ON "RestaurantAccounts" ("RestaurantId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_RestaurantReviewSummaries_AverageRating" ON "RestaurantReviewSummaries" ("AverageRating");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_Restaurant_Created" ON "Restaurants" ("Created");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_Restaurant_DeletedOn" ON "Restaurants" ("DeletedOn");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_Restaurant_IsDeleted" ON "Restaurants" ("IsDeleted");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_Restaurant_LastModified" ON "Restaurants" ("LastModified");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_Restaurants_CuisineType" ON "Restaurants" ("CuisineType");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_Restaurants_IsAcceptingOrders" ON "Restaurants" ("IsAcceptingOrders");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_Restaurants_IsVerified" ON "Restaurants" ("IsVerified");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_Restaurants_Name" ON "Restaurants" ("Name");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_Restaurants_Verified_AcceptingOrders" ON "Restaurants" ("IsVerified", "IsAcceptingOrders");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_Review_Created" ON "Reviews" ("Created");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_Review_DeletedOn" ON "Reviews" ("DeletedOn");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_Review_IsDeleted" ON "Reviews" ("IsDeleted");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_Review_LastModified" ON "Reviews" ("LastModified");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_Reviews_CustomerId" ON "Reviews" ("CustomerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_Reviews_OrderId" ON "Reviews" ("OrderId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_Reviews_Restaurant_SubmissionTimestamp" ON "Reviews" ("RestaurantId", "SubmissionTimestamp");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_RoleAssignment_Created" ON "RoleAssignments" ("Created");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_RoleAssignments_RestaurantId" ON "RoleAssignments" ("RestaurantId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_RoleAssignments_Role" ON "RoleAssignments" ("Role");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE UNIQUE INDEX "IX_RoleAssignments_User_Restaurant_Role" ON "RoleAssignments" ("UserId", "RestaurantId", "Role");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_RoleAssignments_UserId" ON "RoleAssignments" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_SupportTicket_Created" ON "SupportTickets" ("Created");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_SupportTickets_AssignedToAdminId" ON "SupportTickets" ("AssignedToAdminId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_SupportTickets_LastUpdateTimestamp" ON "SupportTickets" ("LastUpdateTimestamp");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_SupportTickets_Priority" ON "SupportTickets" ("Priority");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_SupportTickets_Status" ON "SupportTickets" ("Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_SupportTickets_SubmissionTimestamp" ON "SupportTickets" ("SubmissionTimestamp");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE UNIQUE INDEX "IX_SupportTickets_TicketNumber" ON "SupportTickets" ("TicketNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_Tag_Created" ON "Tags" ("Created");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_Tag_DeletedOn" ON "Tags" ("DeletedOn");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_Tag_IsDeleted" ON "Tags" ("IsDeleted");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_Tag_LastModified" ON "Tags" ("LastModified");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_Tags_TagCategory" ON "Tags" ("TagCategory");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_Tags_TagName" ON "Tags" ("TagName");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_TeamCart_Created" ON "TeamCarts" ("Created");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_TodoList_Created" ON "TodoLists" ("Created");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_TodoList_LastModified" ON "TodoLists" ("LastModified");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    CREATE INDEX "IX_UserDeviceSessions_DeviceId_IsActive" ON "UserDeviceSessions" ("DeviceId", "IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250818163206_InitialMigration') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20250818163206_InitialMigration', '9.0.6');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250903060356_AddSearchIndexAndFacets') THEN
    CREATE EXTENSION IF NOT EXISTS pg_trgm;
    CREATE EXTENSION IF NOT EXISTS postgis;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250903060356_AddSearchIndexAndFacets') THEN
    CREATE TABLE "SearchIndexItems" (
        "Id" uuid NOT NULL,
        "Type" text NOT NULL,
        "RestaurantId" uuid,
        "Name" text NOT NULL,
        "Description" text,
        "Cuisine" text,
        "Tags" text[],
        "Keywords" text[],
        "IsOpenNow" boolean NOT NULL DEFAULT FALSE,
        "IsAcceptingOrders" boolean NOT NULL DEFAULT FALSE,
        "AvgRating" double precision,
        "ReviewCount" integer NOT NULL DEFAULT 0,
        "PriceBand" smallint,
        "Geo" geography (point, 4326),
        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now()),
        "UpdatedAt" timestamp with time zone NOT NULL DEFAULT (now()),
        "SourceVersion" bigint NOT NULL DEFAULT 0,
        "SoftDeleted" boolean NOT NULL DEFAULT FALSE,
        "TsAll" tsvector NOT NULL,
        "TsName" tsvector NOT NULL,
        "TsDescr" tsvector NOT NULL,
        CONSTRAINT "PK_SearchIndexItems" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250903060356_AddSearchIndexAndFacets') THEN
    CREATE INDEX "SIDX_Geo" ON "SearchIndexItems" USING GIST ("Geo");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250903060356_AddSearchIndexAndFacets') THEN
    CREATE INDEX "SIDX_PriceBand" ON "SearchIndexItems" ("PriceBand");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250903060356_AddSearchIndexAndFacets') THEN
    CREATE INDEX "SIDX_Soft_Deleted" ON "SearchIndexItems" ("SoftDeleted");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250903060356_AddSearchIndexAndFacets') THEN
    CREATE INDEX "SIDX_Tags_Gin" ON "SearchIndexItems" USING GIN ("Tags");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250903060356_AddSearchIndexAndFacets') THEN
    CREATE INDEX "SIDX_Trgm_Cuisine" ON "SearchIndexItems" USING GIN ("Cuisine" gin_trgm_ops);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250903060356_AddSearchIndexAndFacets') THEN
    CREATE INDEX "SIDX_Trgm_Name" ON "SearchIndexItems" USING GIN ("Name" gin_trgm_ops);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250903060356_AddSearchIndexAndFacets') THEN
    CREATE INDEX "SIDX_Tsv_All" ON "SearchIndexItems" USING GIN ("TsAll");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250903060356_AddSearchIndexAndFacets') THEN
    CREATE INDEX "SIDX_Tsv_Descr" ON "SearchIndexItems" USING GIN ("TsDescr");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250903060356_AddSearchIndexAndFacets') THEN
    CREATE INDEX "SIDX_Tsv_Name" ON "SearchIndexItems" USING GIN ("TsName");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250903060356_AddSearchIndexAndFacets') THEN
    CREATE INDEX "SIDX_Type_Open" ON "SearchIndexItems" ("Type", "IsOpenNow", "IsAcceptingOrders");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250903060356_AddSearchIndexAndFacets') THEN
    CREATE INDEX "SIDX_Updated_At" ON "SearchIndexItems" ("UpdatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250903060356_AddSearchIndexAndFacets') THEN
    CREATE EXTENSION IF NOT EXISTS postgis;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250903060356_AddSearchIndexAndFacets') THEN

    INSERT INTO spatial_ref_sys (srid, auth_name, auth_srid, proj4text, srtext)
    SELECT 4326, 'EPSG', 4326,
           '+proj=longlat +datum=WGS84 +no_defs',
           'GEOGCS["WGS 84",DATUM["WGS_1984",SPHEROID["WGS 84",6378137,298.257223563,AUTHORITY["EPSG","7030"]],AUTHORITY["EPSG","6326"]],PRIMEM["Greenwich",0,AUTHORITY["EPSG","8901"]],UNIT["degree",0.0174532925199433,AUTHORITY["EPSG","9122"]],AUTHORITY["EPSG","4326"]]'
    WHERE NOT EXISTS (SELECT 1 FROM spatial_ref_sys WHERE srid = 4326);
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250903060356_AddSearchIndexAndFacets') THEN
    CREATE OR REPLACE FUNCTION searchindexitems_tsv_update() RETURNS trigger AS $$
    BEGIN
      NEW."TsName" := to_tsvector('simple', coalesce(NEW."Name",''));
      NEW."TsDescr" := to_tsvector('simple', coalesce(NEW."Description",''));
      NEW."TsAll" :=
          setweight(to_tsvector('simple', coalesce(NEW."Name",'')), 'A') ||
          setweight(to_tsvector('simple', coalesce(NEW."Cuisine",'')), 'B') ||
          setweight(to_tsvector('simple', coalesce(array_to_string(NEW."Tags", ' '),'')), 'B') ||
          setweight(to_tsvector('simple', coalesce(NEW."Description",'')), 'C') ||
          setweight(to_tsvector('simple', coalesce(array_to_string(NEW."Keywords", ' '),'')), 'C');
      RETURN NEW;
    END
    $$ LANGUAGE plpgsql;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250903060356_AddSearchIndexAndFacets') THEN
    DO $$
    BEGIN
      IF NOT EXISTS (
        SELECT 1 FROM pg_trigger WHERE tgname = 'searchindexitems_tsvector_trg'
      ) THEN
        CREATE TRIGGER searchindexitems_tsvector_trg
        BEFORE INSERT OR UPDATE OF "Name", "Description", "Cuisine", "Tags", "Keywords"
        ON "SearchIndexItems"
        FOR EACH ROW
        EXECUTE FUNCTION searchindexitems_tsv_update();
      END IF;
    END
    $$;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250903060356_AddSearchIndexAndFacets') THEN
    UPDATE "SearchIndexItems" SET "Name" = "Name";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250903060356_AddSearchIndexAndFacets') THEN
    CREATE INDEX IF NOT EXISTS "SIDX_Lower_Cuisine" ON "SearchIndexItems" (LOWER("Cuisine"));
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250903060356_AddSearchIndexAndFacets') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20250903060356_AddSearchIndexAndFacets', '9.0.6');
    END IF;
END $EF$;
COMMIT;

