--
-- PostgreSQL database dump
--

-- Dumped from database version 16.4 (Debian 16.4-1.pgdg110+2)
-- Dumped by pg_dump version 16.4 (Debian 16.4-1.pgdg110+2)

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- Name: tiger; Type: SCHEMA; Schema: -; Owner: -
--

CREATE SCHEMA tiger;


--
-- Name: tiger_data; Type: SCHEMA; Schema: -; Owner: -
--

CREATE SCHEMA tiger_data;


--
-- Name: topology; Type: SCHEMA; Schema: -; Owner: -
--

CREATE SCHEMA topology;


--
-- Name: SCHEMA topology; Type: COMMENT; Schema: -; Owner: -
--

COMMENT ON SCHEMA topology IS 'PostGIS Topology schema';


--
-- Name: fuzzystrmatch; Type: EXTENSION; Schema: -; Owner: -
--

CREATE EXTENSION IF NOT EXISTS fuzzystrmatch WITH SCHEMA public;


--
-- Name: EXTENSION fuzzystrmatch; Type: COMMENT; Schema: -; Owner: -
--

COMMENT ON EXTENSION fuzzystrmatch IS 'determine similarities and distance between strings';


--
-- Name: pg_trgm; Type: EXTENSION; Schema: -; Owner: -
--

CREATE EXTENSION IF NOT EXISTS pg_trgm WITH SCHEMA public;


--
-- Name: EXTENSION pg_trgm; Type: COMMENT; Schema: -; Owner: -
--

COMMENT ON EXTENSION pg_trgm IS 'text similarity measurement and index searching based on trigrams';


--
-- Name: postgis; Type: EXTENSION; Schema: -; Owner: -
--

CREATE EXTENSION IF NOT EXISTS postgis WITH SCHEMA public;


--
-- Name: EXTENSION postgis; Type: COMMENT; Schema: -; Owner: -
--

COMMENT ON EXTENSION postgis IS 'PostGIS geometry and geography spatial types and functions';


--
-- Name: postgis_tiger_geocoder; Type: EXTENSION; Schema: -; Owner: -
--

CREATE EXTENSION IF NOT EXISTS postgis_tiger_geocoder WITH SCHEMA tiger;


--
-- Name: EXTENSION postgis_tiger_geocoder; Type: COMMENT; Schema: -; Owner: -
--

COMMENT ON EXTENSION postgis_tiger_geocoder IS 'PostGIS tiger geocoder and reverse geocoder';


--
-- Name: postgis_topology; Type: EXTENSION; Schema: -; Owner: -
--

CREATE EXTENSION IF NOT EXISTS postgis_topology WITH SCHEMA topology;


--
-- Name: EXTENSION postgis_topology; Type: COMMENT; Schema: -; Owner: -
--

COMMENT ON EXTENSION postgis_topology IS 'PostGIS topology spatial types and functions';


--
-- Name: unaccent; Type: EXTENSION; Schema: -; Owner: -
--

CREATE EXTENSION IF NOT EXISTS unaccent WITH SCHEMA public;


--
-- Name: EXTENSION unaccent; Type: COMMENT; Schema: -; Owner: -
--

COMMENT ON EXTENSION unaccent IS 'text search dictionary that removes accents';


--
-- Name: searchindexitems_tsv_update(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.searchindexitems_tsv_update() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
  NEW."TsName" := to_tsvector('simple', unaccent(coalesce(NEW."Name",'')));
  NEW."TsDescr" := to_tsvector('simple', unaccent(coalesce(NEW."Description",'')));
  NEW."TsAll" :=
      setweight(to_tsvector('simple', unaccent(coalesce(NEW."Name",''))), 'A') ||
      setweight(to_tsvector('simple', unaccent(coalesce(NEW."Cuisine",''))), 'B') ||
      setweight(to_tsvector('simple', unaccent(coalesce(array_to_string(NEW."Tags", ' '),''))), 'B') ||
      setweight(to_tsvector('simple', unaccent(coalesce(NEW."Description",''))), 'C') ||
      setweight(to_tsvector('simple', unaccent(coalesce(array_to_string(NEW."Keywords", ' '),''))), 'C');
  RETURN NEW;
END
$$;


SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: AccountTransactions; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."AccountTransactions" (
    "Id" uuid NOT NULL,
    "RestaurantAccountId" uuid NOT NULL,
    "Type" character varying(50) NOT NULL,
    "Amount" numeric(18,2) NOT NULL,
    "Currency" character varying(3) NOT NULL,
    "Timestamp" timestamp with time zone NOT NULL,
    "RelatedOrderId" uuid,
    "Notes" character varying(500),
    "Created" timestamp with time zone NOT NULL,
    "CreatedBy" character varying(255)
);


--
-- Name: COLUMN "AccountTransactions"."Created"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."AccountTransactions"."Created" IS 'Timestamp when the entity was created';


--
-- Name: COLUMN "AccountTransactions"."CreatedBy"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."AccountTransactions"."CreatedBy" IS 'Identifier of who created the entity';


--
-- Name: AdminDailyPerformanceSeries; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."AdminDailyPerformanceSeries" (
    "BucketDate" date NOT NULL,
    "TotalOrders" integer NOT NULL,
    "DeliveredOrders" integer NOT NULL,
    "GrossMerchandiseVolume" numeric(18,2) NOT NULL,
    "TotalRefunds" numeric(18,2) NOT NULL,
    "NewCustomers" integer NOT NULL,
    "NewRestaurants" integer NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NOT NULL
);


--
-- Name: AdminPlatformMetricsSnapshots; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."AdminPlatformMetricsSnapshots" (
    "SnapshotId" character varying(32) NOT NULL,
    "TotalOrders" bigint NOT NULL,
    "ActiveOrders" bigint NOT NULL,
    "DeliveredOrders" bigint NOT NULL,
    "GrossMerchandiseVolume" numeric(18,2) NOT NULL,
    "TotalRefunds" numeric(18,2) NOT NULL,
    "ActiveRestaurants" integer NOT NULL,
    "ActiveCustomers" integer NOT NULL,
    "OpenSupportTickets" integer NOT NULL,
    "TotalReviews" integer NOT NULL,
    "LastOrderAtUtc" timestamp with time zone,
    "UpdatedAtUtc" timestamp with time zone NOT NULL
);


--
-- Name: AdminRestaurantHealthSummaries; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."AdminRestaurantHealthSummaries" (
    "RestaurantId" uuid NOT NULL,
    "RestaurantName" character varying(255) NOT NULL,
    "IsVerified" boolean NOT NULL,
    "IsAcceptingOrders" boolean NOT NULL,
    "OrdersLast7Days" integer NOT NULL,
    "OrdersLast30Days" integer NOT NULL,
    "RevenueLast30Days" numeric(18,2) NOT NULL,
    "AverageRating" double precision NOT NULL,
    "TotalReviews" integer NOT NULL,
    "CouponRedemptionsLast30Days" integer NOT NULL,
    "OutstandingBalance" numeric(18,2) NOT NULL,
    "LastOrderAtUtc" timestamp with time zone,
    "UpdatedAtUtc" timestamp with time zone NOT NULL
);


--
-- Name: AspNetRoleClaims; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."AspNetRoleClaims" (
    "Id" integer NOT NULL,
    "RoleId" uuid NOT NULL,
    "ClaimType" text,
    "ClaimValue" text
);


--
-- Name: AspNetRoleClaims_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public."AspNetRoleClaims" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."AspNetRoleClaims_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: AspNetRoles; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."AspNetRoles" (
    "Id" uuid NOT NULL,
    "Name" character varying(256),
    "NormalizedName" character varying(256),
    "ConcurrencyStamp" text
);


--
-- Name: AspNetUserClaims; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."AspNetUserClaims" (
    "Id" integer NOT NULL,
    "UserId" uuid NOT NULL,
    "ClaimType" text,
    "ClaimValue" text
);


--
-- Name: AspNetUserClaims_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public."AspNetUserClaims" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."AspNetUserClaims_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: AspNetUserLogins; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."AspNetUserLogins" (
    "LoginProvider" text NOT NULL,
    "ProviderKey" text NOT NULL,
    "ProviderDisplayName" text,
    "UserId" uuid NOT NULL
);


--
-- Name: AspNetUserRoles; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."AspNetUserRoles" (
    "UserId" uuid NOT NULL,
    "RoleId" uuid NOT NULL
);


--
-- Name: AspNetUserTokens; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."AspNetUserTokens" (
    "UserId" uuid NOT NULL,
    "LoginProvider" text NOT NULL,
    "Name" text NOT NULL,
    "Value" text
);


--
-- Name: AspNetUsers; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."AspNetUsers" (
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
    "AccessFailedCount" integer NOT NULL
);


--
-- Name: CouponUserUsages; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."CouponUserUsages" (
    "CouponId" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "UsageCount" integer DEFAULT 0 NOT NULL
);


--
-- Name: Coupons; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Coupons" (
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
    "CurrentTotalUsageCount" integer DEFAULT 0 NOT NULL,
    "IsEnabled" boolean DEFAULT true NOT NULL,
    "UsageLimitPerUser" integer,
    "Created" timestamp with time zone NOT NULL,
    "CreatedBy" character varying(255),
    "LastModified" timestamp with time zone NOT NULL,
    "LastModifiedBy" character varying(255),
    "IsDeleted" boolean DEFAULT false NOT NULL,
    "DeletedOn" timestamp with time zone,
    "DeletedBy" character varying(255)
);


--
-- Name: COLUMN "Coupons"."Created"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."Coupons"."Created" IS 'Timestamp when the entity was created';


--
-- Name: COLUMN "Coupons"."CreatedBy"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."Coupons"."CreatedBy" IS 'Identifier of who created the entity';


--
-- Name: COLUMN "Coupons"."LastModified"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."Coupons"."LastModified" IS 'Timestamp when the entity was last modified';


--
-- Name: COLUMN "Coupons"."LastModifiedBy"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."Coupons"."LastModifiedBy" IS 'Identifier of who last modified the entity';


--
-- Name: COLUMN "Coupons"."IsDeleted"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."Coupons"."IsDeleted" IS 'Indicates if the entity is soft-deleted';


--
-- Name: COLUMN "Coupons"."DeletedOn"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."Coupons"."DeletedOn" IS 'Timestamp when the entity was deleted';


--
-- Name: COLUMN "Coupons"."DeletedBy"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."Coupons"."DeletedBy" IS 'Identifier of who deleted the entity';


--
-- Name: CustomizationChoices; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."CustomizationChoices" (
    "ChoiceId" uuid NOT NULL,
    "CustomizationGroupId" uuid NOT NULL,
    "Name" character varying(200) NOT NULL,
    "PriceAdjustment_Amount" numeric(18,2) NOT NULL,
    "PriceAdjustment_Currency" character varying(3) NOT NULL,
    "IsDefault" boolean NOT NULL,
    "DisplayOrder" integer NOT NULL
);


--
-- Name: CustomizationGroups; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."CustomizationGroups" (
    "Id" uuid NOT NULL,
    "RestaurantId" uuid NOT NULL,
    "GroupName" character varying(200) NOT NULL,
    "MinSelections" integer NOT NULL,
    "MaxSelections" integer NOT NULL,
    "Created" timestamp with time zone NOT NULL,
    "CreatedBy" character varying(255),
    "LastModified" timestamp with time zone NOT NULL,
    "LastModifiedBy" character varying(255),
    "IsDeleted" boolean DEFAULT false NOT NULL,
    "DeletedOn" timestamp with time zone,
    "DeletedBy" character varying(255)
);


--
-- Name: COLUMN "CustomizationGroups"."Created"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."CustomizationGroups"."Created" IS 'Timestamp when the entity was created';


--
-- Name: COLUMN "CustomizationGroups"."CreatedBy"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."CustomizationGroups"."CreatedBy" IS 'Identifier of who created the entity';


--
-- Name: COLUMN "CustomizationGroups"."LastModified"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."CustomizationGroups"."LastModified" IS 'Timestamp when the entity was last modified';


--
-- Name: COLUMN "CustomizationGroups"."LastModifiedBy"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."CustomizationGroups"."LastModifiedBy" IS 'Identifier of who last modified the entity';


--
-- Name: COLUMN "CustomizationGroups"."IsDeleted"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."CustomizationGroups"."IsDeleted" IS 'Indicates if the entity is soft-deleted';


--
-- Name: COLUMN "CustomizationGroups"."DeletedOn"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."CustomizationGroups"."DeletedOn" IS 'Timestamp when the entity was deleted';


--
-- Name: COLUMN "CustomizationGroups"."DeletedBy"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."CustomizationGroups"."DeletedBy" IS 'Identifier of who deleted the entity';


--
-- Name: Devices; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Devices" (
    "Id" uuid NOT NULL,
    "DeviceId" text,
    "Platform" text NOT NULL,
    "ModelName" text,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL
);


--
-- Name: DomainUsers; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."DomainUsers" (
    "Id" uuid NOT NULL,
    "Name" character varying(200) NOT NULL,
    "Email" character varying(255) NOT NULL,
    "PhoneNumber" character varying(50),
    "IsActive" boolean NOT NULL,
    "Created" timestamp with time zone NOT NULL,
    "CreatedBy" character varying(255),
    "LastModified" timestamp with time zone NOT NULL,
    "LastModifiedBy" character varying(255),
    "IsDeleted" boolean DEFAULT false NOT NULL,
    "DeletedOn" timestamp with time zone,
    "DeletedBy" character varying(255)
);


--
-- Name: COLUMN "DomainUsers"."Created"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."DomainUsers"."Created" IS 'Timestamp when the entity was created';


--
-- Name: COLUMN "DomainUsers"."CreatedBy"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."DomainUsers"."CreatedBy" IS 'Identifier of who created the entity';


--
-- Name: COLUMN "DomainUsers"."LastModified"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."DomainUsers"."LastModified" IS 'Timestamp when the entity was last modified';


--
-- Name: COLUMN "DomainUsers"."LastModifiedBy"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."DomainUsers"."LastModifiedBy" IS 'Identifier of who last modified the entity';


--
-- Name: COLUMN "DomainUsers"."IsDeleted"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."DomainUsers"."IsDeleted" IS 'Indicates if the entity is soft-deleted';


--
-- Name: COLUMN "DomainUsers"."DeletedOn"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."DomainUsers"."DeletedOn" IS 'Timestamp when the entity was deleted';


--
-- Name: COLUMN "DomainUsers"."DeletedBy"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."DomainUsers"."DeletedBy" IS 'Identifier of who deleted the entity';


--
-- Name: FullMenuViews; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."FullMenuViews" (
    "RestaurantId" uuid NOT NULL,
    "MenuJson" jsonb NOT NULL,
    "LastRebuiltAt" timestamp with time zone NOT NULL
);


--
-- Name: InboxMessages; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."InboxMessages" (
    "EventId" uuid NOT NULL,
    "Handler" character varying(256) NOT NULL,
    "ProcessedOnUtc" timestamp with time zone NOT NULL,
    "Error" text
);


--
-- Name: MenuCategories; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."MenuCategories" (
    "Id" uuid NOT NULL,
    "MenuId" uuid NOT NULL,
    "Name" character varying(200) NOT NULL,
    "DisplayOrder" integer NOT NULL,
    "Created" timestamp with time zone NOT NULL,
    "CreatedBy" character varying(255),
    "LastModified" timestamp with time zone NOT NULL,
    "LastModifiedBy" character varying(255),
    "IsDeleted" boolean DEFAULT false NOT NULL,
    "DeletedOn" timestamp with time zone,
    "DeletedBy" character varying(255)
);


--
-- Name: COLUMN "MenuCategories"."Created"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."MenuCategories"."Created" IS 'Timestamp when the entity was created';


--
-- Name: COLUMN "MenuCategories"."CreatedBy"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."MenuCategories"."CreatedBy" IS 'Identifier of who created the entity';


--
-- Name: COLUMN "MenuCategories"."LastModified"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."MenuCategories"."LastModified" IS 'Timestamp when the entity was last modified';


--
-- Name: COLUMN "MenuCategories"."LastModifiedBy"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."MenuCategories"."LastModifiedBy" IS 'Identifier of who last modified the entity';


--
-- Name: COLUMN "MenuCategories"."IsDeleted"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."MenuCategories"."IsDeleted" IS 'Indicates if the entity is soft-deleted';


--
-- Name: COLUMN "MenuCategories"."DeletedOn"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."MenuCategories"."DeletedOn" IS 'Timestamp when the entity was deleted';


--
-- Name: COLUMN "MenuCategories"."DeletedBy"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."MenuCategories"."DeletedBy" IS 'Identifier of who deleted the entity';


--
-- Name: MenuItemSalesSummaries; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."MenuItemSalesSummaries" (
    "RestaurantId" uuid NOT NULL,
    "MenuItemId" uuid NOT NULL,
    "LifetimeQuantity" bigint DEFAULT 0 NOT NULL,
    "Rolling7DayQuantity" bigint DEFAULT 0 NOT NULL,
    "Rolling30DayQuantity" bigint DEFAULT 0 NOT NULL,
    "LastSoldAt" timestamp with time zone,
    "LastUpdatedAt" timestamp with time zone NOT NULL,
    "SourceVersion" bigint DEFAULT 0 NOT NULL
);


--
-- Name: MenuItems; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."MenuItems" (
    "Id" uuid NOT NULL,
    "RestaurantId" uuid NOT NULL,
    "MenuCategoryId" uuid NOT NULL,
    "Name" character varying(200) NOT NULL,
    "Description" character varying(1000) NOT NULL,
    "BasePrice_Amount" numeric(18,2) NOT NULL,
    "BasePrice_Currency" character varying(3) NOT NULL,
    "ImageUrl" character varying(500),
    "IsAvailable" boolean DEFAULT true NOT NULL,
    "Created" timestamp with time zone NOT NULL,
    "CreatedBy" character varying(255),
    "LastModified" timestamp with time zone NOT NULL,
    "LastModifiedBy" character varying(255),
    "IsDeleted" boolean DEFAULT false NOT NULL,
    "DeletedOn" timestamp with time zone,
    "DeletedBy" character varying(255),
    "DietaryTagIds" jsonb NOT NULL,
    "AppliedCustomizations" jsonb NOT NULL
);


--
-- Name: COLUMN "MenuItems"."Created"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."MenuItems"."Created" IS 'Timestamp when the entity was created';


--
-- Name: COLUMN "MenuItems"."CreatedBy"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."MenuItems"."CreatedBy" IS 'Identifier of who created the entity';


--
-- Name: COLUMN "MenuItems"."LastModified"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."MenuItems"."LastModified" IS 'Timestamp when the entity was last modified';


--
-- Name: COLUMN "MenuItems"."LastModifiedBy"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."MenuItems"."LastModifiedBy" IS 'Identifier of who last modified the entity';


--
-- Name: COLUMN "MenuItems"."IsDeleted"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."MenuItems"."IsDeleted" IS 'Indicates if the entity is soft-deleted';


--
-- Name: COLUMN "MenuItems"."DeletedOn"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."MenuItems"."DeletedOn" IS 'Timestamp when the entity was deleted';


--
-- Name: COLUMN "MenuItems"."DeletedBy"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."MenuItems"."DeletedBy" IS 'Identifier of who deleted the entity';


--
-- Name: Menus; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Menus" (
    "Id" uuid NOT NULL,
    "Name" character varying(200) NOT NULL,
    "Description" character varying(500) NOT NULL,
    "IsEnabled" boolean NOT NULL,
    "RestaurantId" uuid NOT NULL,
    "Created" timestamp with time zone NOT NULL,
    "CreatedBy" character varying(255),
    "LastModified" timestamp with time zone NOT NULL,
    "LastModifiedBy" character varying(255),
    "IsDeleted" boolean DEFAULT false NOT NULL,
    "DeletedOn" timestamp with time zone,
    "DeletedBy" character varying(255)
);


--
-- Name: COLUMN "Menus"."Created"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."Menus"."Created" IS 'Timestamp when the entity was created';


--
-- Name: COLUMN "Menus"."CreatedBy"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."Menus"."CreatedBy" IS 'Identifier of who created the entity';


--
-- Name: COLUMN "Menus"."LastModified"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."Menus"."LastModified" IS 'Timestamp when the entity was last modified';


--
-- Name: COLUMN "Menus"."LastModifiedBy"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."Menus"."LastModifiedBy" IS 'Identifier of who last modified the entity';


--
-- Name: COLUMN "Menus"."IsDeleted"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."Menus"."IsDeleted" IS 'Indicates if the entity is soft-deleted';


--
-- Name: COLUMN "Menus"."DeletedOn"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."Menus"."DeletedOn" IS 'Timestamp when the entity was deleted';


--
-- Name: COLUMN "Menus"."DeletedBy"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."Menus"."DeletedBy" IS 'Identifier of who deleted the entity';


--
-- Name: OrderItems; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."OrderItems" (
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
    "SelectedCustomizations" jsonb NOT NULL
);


--
-- Name: Orders; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Orders" (
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
    "Version" bigint DEFAULT 0 NOT NULL
);


--
-- Name: COLUMN "Orders"."Created"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."Orders"."Created" IS 'Timestamp when the entity was created';


--
-- Name: COLUMN "Orders"."CreatedBy"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."Orders"."CreatedBy" IS 'Identifier of who created the entity';


--
-- Name: OutboxMessages; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."OutboxMessages" (
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
    "Error" text
);


--
-- Name: PaymentTransactions; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."PaymentTransactions" (
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
    "PaidByUserId" uuid
);


--
-- Name: ProcessedWebhookEvents; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."ProcessedWebhookEvents" (
    "Id" text NOT NULL,
    "ProcessedAt" timestamp with time zone NOT NULL
);


--
-- Name: RestaurantAccounts; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."RestaurantAccounts" (
    "Id" uuid NOT NULL,
    "RestaurantId" uuid NOT NULL,
    "CurrentBalance_Amount" numeric(18,2) NOT NULL,
    "CurrentBalance_Currency" character varying(3) NOT NULL,
    "PayoutMethod_Details" character varying(500),
    "Created" timestamp with time zone NOT NULL,
    "CreatedBy" character varying(255)
);


--
-- Name: COLUMN "RestaurantAccounts"."Created"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."RestaurantAccounts"."Created" IS 'Timestamp when the entity was created';


--
-- Name: COLUMN "RestaurantAccounts"."CreatedBy"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."RestaurantAccounts"."CreatedBy" IS 'Identifier of who created the entity';


--
-- Name: RestaurantRegistrations; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."RestaurantRegistrations" (
    "Id" uuid NOT NULL,
    "SubmitterUserId" uuid NOT NULL,
    "Name" character varying(100) NOT NULL,
    "Description" character varying(500) NOT NULL,
    "CuisineType" character varying(50) NOT NULL,
    "Street" character varying(200) NOT NULL,
    "City" character varying(100) NOT NULL,
    "State" character varying(100) NOT NULL,
    "ZipCode" character varying(20) NOT NULL,
    "Country" character varying(100) NOT NULL,
    "PhoneNumber" character varying(30) NOT NULL,
    "Email" character varying(320) NOT NULL,
    "BusinessHours" character varying(200) NOT NULL,
    "LogoUrl" character varying(2048),
    "Latitude" double precision,
    "Longitude" double precision,
    "Status" integer NOT NULL,
    "SubmittedAtUtc" timestamp with time zone NOT NULL,
    "ReviewedAtUtc" timestamp with time zone,
    "ReviewedByUserId" uuid,
    "ReviewNote" character varying(500),
    "Created" timestamp with time zone NOT NULL,
    "CreatedBy" text
);


--
-- Name: RestaurantReviewSummaries; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."RestaurantReviewSummaries" (
    "RestaurantId" uuid NOT NULL,
    "AverageRating" double precision DEFAULT 0.0 NOT NULL,
    "TotalReviews" integer DEFAULT 0 NOT NULL,
    "LastReviewAtUtc" timestamp with time zone,
    "Ratings1" integer DEFAULT 0 NOT NULL,
    "Ratings2" integer DEFAULT 0 NOT NULL,
    "Ratings3" integer DEFAULT 0 NOT NULL,
    "Ratings4" integer DEFAULT 0 NOT NULL,
    "Ratings5" integer DEFAULT 0 NOT NULL,
    "TotalWithText" integer DEFAULT 0 NOT NULL,
    "UpdatedAtUtc" timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: Restaurants; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Restaurants" (
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
    "IsVerified" boolean DEFAULT false NOT NULL,
    "IsAcceptingOrders" boolean DEFAULT false NOT NULL,
    "Created" timestamp with time zone NOT NULL,
    "CreatedBy" character varying(255),
    "LastModified" timestamp with time zone NOT NULL,
    "LastModifiedBy" character varying(255),
    "IsDeleted" boolean DEFAULT false NOT NULL,
    "DeletedOn" timestamp with time zone,
    "DeletedBy" character varying(255)
);


--
-- Name: COLUMN "Restaurants"."Created"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."Restaurants"."Created" IS 'Timestamp when the entity was created';


--
-- Name: COLUMN "Restaurants"."CreatedBy"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."Restaurants"."CreatedBy" IS 'Identifier of who created the entity';


--
-- Name: COLUMN "Restaurants"."LastModified"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."Restaurants"."LastModified" IS 'Timestamp when the entity was last modified';


--
-- Name: COLUMN "Restaurants"."LastModifiedBy"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."Restaurants"."LastModifiedBy" IS 'Identifier of who last modified the entity';


--
-- Name: COLUMN "Restaurants"."IsDeleted"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."Restaurants"."IsDeleted" IS 'Indicates if the entity is soft-deleted';


--
-- Name: COLUMN "Restaurants"."DeletedOn"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."Restaurants"."DeletedOn" IS 'Timestamp when the entity was deleted';


--
-- Name: COLUMN "Restaurants"."DeletedBy"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."Restaurants"."DeletedBy" IS 'Identifier of who deleted the entity';


--
-- Name: Reviews; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Reviews" (
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
    "IsDeleted" boolean DEFAULT false NOT NULL,
    "DeletedOn" timestamp with time zone,
    "DeletedBy" character varying(255)
);


--
-- Name: COLUMN "Reviews"."Created"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."Reviews"."Created" IS 'Timestamp when the entity was created';


--
-- Name: COLUMN "Reviews"."CreatedBy"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."Reviews"."CreatedBy" IS 'Identifier of who created the entity';


--
-- Name: COLUMN "Reviews"."LastModified"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."Reviews"."LastModified" IS 'Timestamp when the entity was last modified';


--
-- Name: COLUMN "Reviews"."LastModifiedBy"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."Reviews"."LastModifiedBy" IS 'Identifier of who last modified the entity';


--
-- Name: COLUMN "Reviews"."IsDeleted"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."Reviews"."IsDeleted" IS 'Indicates if the entity is soft-deleted';


--
-- Name: COLUMN "Reviews"."DeletedOn"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."Reviews"."DeletedOn" IS 'Timestamp when the entity was deleted';


--
-- Name: COLUMN "Reviews"."DeletedBy"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."Reviews"."DeletedBy" IS 'Identifier of who deleted the entity';


--
-- Name: RoleAssignments; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."RoleAssignments" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "RestaurantId" uuid NOT NULL,
    "Role" text NOT NULL,
    "Created" timestamp with time zone NOT NULL,
    "CreatedBy" character varying(255)
);


--
-- Name: COLUMN "RoleAssignments"."Created"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."RoleAssignments"."Created" IS 'Timestamp when the entity was created';


--
-- Name: COLUMN "RoleAssignments"."CreatedBy"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."RoleAssignments"."CreatedBy" IS 'Identifier of who created the entity';


--
-- Name: SearchIndexItems; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."SearchIndexItems" (
    "Id" uuid NOT NULL,
    "Type" text NOT NULL,
    "RestaurantId" uuid,
    "Name" text NOT NULL,
    "Description" text,
    "Cuisine" text,
    "Tags" text[],
    "Keywords" text[],
    "IsOpenNow" boolean DEFAULT false NOT NULL,
    "IsAcceptingOrders" boolean DEFAULT false NOT NULL,
    "AvgRating" double precision,
    "ReviewCount" integer DEFAULT 0 NOT NULL,
    "PriceBand" smallint,
    "Geo" public.geography(Point,4326),
    "CreatedAt" timestamp with time zone DEFAULT now() NOT NULL,
    "UpdatedAt" timestamp with time zone DEFAULT now() NOT NULL,
    "SourceVersion" bigint DEFAULT 0 NOT NULL,
    "SoftDeleted" boolean DEFAULT false NOT NULL,
    "TsAll" tsvector NOT NULL,
    "TsName" tsvector NOT NULL,
    "TsDescr" tsvector NOT NULL
);


--
-- Name: SupportTicketContextLinks; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."SupportTicketContextLinks" (
    "EntityType" character varying(50) NOT NULL,
    "EntityID" uuid NOT NULL,
    "SupportTicketId" uuid NOT NULL
);


--
-- Name: SupportTicketMessages; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."SupportTicketMessages" (
    "MessageId" uuid NOT NULL,
    "SupportTicketId" uuid NOT NULL,
    "AuthorId" uuid NOT NULL,
    "AuthorType" character varying(50) NOT NULL,
    "MessageText" character varying(5000) NOT NULL,
    "Timestamp" timestamp with time zone NOT NULL,
    "IsInternalNote" boolean NOT NULL
);


--
-- Name: SupportTickets; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."SupportTickets" (
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
    "CreatedBy" character varying(255)
);


--
-- Name: COLUMN "SupportTickets"."Created"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."SupportTickets"."Created" IS 'Timestamp when the entity was created';


--
-- Name: COLUMN "SupportTickets"."CreatedBy"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."SupportTickets"."CreatedBy" IS 'Identifier of who created the entity';


--
-- Name: Tags; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Tags" (
    "Id" uuid NOT NULL,
    "TagName" character varying(100) NOT NULL,
    "TagDescription" character varying(500),
    "TagCategory" character varying(50) NOT NULL,
    "Created" timestamp with time zone NOT NULL,
    "CreatedBy" character varying(255),
    "LastModified" timestamp with time zone NOT NULL,
    "LastModifiedBy" character varying(255),
    "IsDeleted" boolean DEFAULT false NOT NULL,
    "DeletedOn" timestamp with time zone,
    "DeletedBy" character varying(255)
);


--
-- Name: COLUMN "Tags"."Created"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."Tags"."Created" IS 'Timestamp when the entity was created';


--
-- Name: COLUMN "Tags"."CreatedBy"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."Tags"."CreatedBy" IS 'Identifier of who created the entity';


--
-- Name: COLUMN "Tags"."LastModified"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."Tags"."LastModified" IS 'Timestamp when the entity was last modified';


--
-- Name: COLUMN "Tags"."LastModifiedBy"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."Tags"."LastModifiedBy" IS 'Identifier of who last modified the entity';


--
-- Name: COLUMN "Tags"."IsDeleted"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."Tags"."IsDeleted" IS 'Indicates if the entity is soft-deleted';


--
-- Name: COLUMN "Tags"."DeletedOn"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."Tags"."DeletedOn" IS 'Timestamp when the entity was deleted';


--
-- Name: COLUMN "Tags"."DeletedBy"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."Tags"."DeletedBy" IS 'Identifier of who deleted the entity';


--
-- Name: TeamCartItems; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."TeamCartItems" (
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
    "SelectedCustomizations" jsonb NOT NULL
);


--
-- Name: TeamCartMemberPayments; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."TeamCartMemberPayments" (
    "MemberPaymentId" uuid NOT NULL,
    "TeamCartId" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Payment_Amount" numeric(18,2) NOT NULL,
    "Payment_Currency" character varying(3) NOT NULL,
    "Method" character varying(50) NOT NULL,
    "Status" character varying(50) NOT NULL,
    "OnlineTransactionId" character varying(255),
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL
);


--
-- Name: TeamCartMembers; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."TeamCartMembers" (
    "TeamCartMemberId" uuid NOT NULL,
    "TeamCartId" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Name" character varying(200) NOT NULL,
    "Role" character varying(50) NOT NULL
);


--
-- Name: TeamCarts; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."TeamCarts" (
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
    "GrandTotal_Amount" numeric(18,2) DEFAULT 0.0 NOT NULL,
    "GrandTotal_Currency" character varying(3) DEFAULT ''::character varying NOT NULL,
    "MemberTotals" jsonb DEFAULT '{}'::jsonb NOT NULL,
    "QuoteVersion" bigint DEFAULT 0 NOT NULL
);


--
-- Name: COLUMN "TeamCarts"."Created"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."TeamCarts"."Created" IS 'Timestamp when the entity was created';


--
-- Name: COLUMN "TeamCarts"."CreatedBy"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."TeamCarts"."CreatedBy" IS 'Identifier of who created the entity';


--
-- Name: TodoItems; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."TodoItems" (
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
    "LastModifiedBy" text
);


--
-- Name: TodoLists; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."TodoLists" (
    "Id" uuid NOT NULL,
    "Title" character varying(200) NOT NULL,
    "Colour" character varying(10) NOT NULL,
    "Created" timestamp with time zone NOT NULL,
    "CreatedBy" character varying(255),
    "LastModified" timestamp with time zone NOT NULL,
    "LastModifiedBy" character varying(255)
);


--
-- Name: COLUMN "TodoLists"."Created"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."TodoLists"."Created" IS 'Timestamp when the entity was created';


--
-- Name: COLUMN "TodoLists"."CreatedBy"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."TodoLists"."CreatedBy" IS 'Identifier of who created the entity';


--
-- Name: COLUMN "TodoLists"."LastModified"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."TodoLists"."LastModified" IS 'Timestamp when the entity was last modified';


--
-- Name: COLUMN "TodoLists"."LastModifiedBy"; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public."TodoLists"."LastModifiedBy" IS 'Identifier of who last modified the entity';


--
-- Name: UserAddresses; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."UserAddresses" (
    "AddressId" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Street" character varying(255) NOT NULL,
    "City" character varying(100) NOT NULL,
    "State" character varying(100),
    "ZipCode" character varying(20) NOT NULL,
    "Country" character varying(100) NOT NULL,
    "Label" character varying(100),
    "DeliveryInstructions" character varying(500)
);


--
-- Name: UserDeviceSessions; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."UserDeviceSessions" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "DeviceId" uuid NOT NULL,
    "FcmToken" text NOT NULL,
    "IsActive" boolean NOT NULL,
    "LastLoginAt" timestamp with time zone NOT NULL,
    "LoggedOutAt" timestamp with time zone
);


--
-- Name: UserPaymentMethods; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."UserPaymentMethods" (
    "PaymentMethodId" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Type" character varying(50) NOT NULL,
    "TokenizedDetails" character varying(500) NOT NULL,
    "IsDefault" boolean NOT NULL,
    "Created" timestamp with time zone NOT NULL,
    "CreatedBy" text,
    "LastModified" timestamp with time zone NOT NULL,
    "LastModifiedBy" text
);


--
-- Name: __EFMigrationsHistory; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL
);


--
-- Name: active_coupons_view; Type: MATERIALIZED VIEW; Schema: public; Owner: -
--

CREATE MATERIALIZED VIEW public.active_coupons_view AS
 SELECT "Id" AS coupon_id,
    "RestaurantId" AS restaurant_id,
    "Code" AS code,
    "Description" AS description,
    "Value_Type" AS value_type,
    "Value_PercentageValue" AS percentage_value,
    "Value_FixedAmount_Amount" AS fixed_amount_value,
    "Value_FixedAmount_Currency" AS fixed_amount_currency,
    "Value_FreeItemValue" AS free_item_id,
    "AppliesTo_Scope" AS applies_to_scope,
    "AppliesTo_ItemIds" AS applies_to_item_ids,
    "AppliesTo_CategoryIds" AS applies_to_category_ids,
    "MinOrderAmount_Amount" AS min_order_amount,
    "MinOrderAmount_Currency" AS min_order_currency,
    "ValidityStartDate" AS validity_start_date,
    "ValidityEndDate" AS validity_end_date,
    "IsEnabled" AS is_enabled,
    "TotalUsageLimit" AS total_usage_limit,
    "UsageLimitPerUser" AS usage_limit_per_user,
    "CurrentTotalUsageCount" AS current_total_usage_count,
    now() AS last_refreshed_at
   FROM public."Coupons" c
  WHERE (("IsEnabled" = true) AND ("IsDeleted" = false) AND ("ValidityEndDate" >= now()))
  WITH NO DATA;


--
-- Name: AccountTransactions PK_AccountTransactions; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."AccountTransactions"
    ADD CONSTRAINT "PK_AccountTransactions" PRIMARY KEY ("Id");


--
-- Name: AdminDailyPerformanceSeries PK_AdminDailyPerformanceSeries; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."AdminDailyPerformanceSeries"
    ADD CONSTRAINT "PK_AdminDailyPerformanceSeries" PRIMARY KEY ("BucketDate");


--
-- Name: AdminPlatformMetricsSnapshots PK_AdminPlatformMetricsSnapshots; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."AdminPlatformMetricsSnapshots"
    ADD CONSTRAINT "PK_AdminPlatformMetricsSnapshots" PRIMARY KEY ("SnapshotId");


--
-- Name: AdminRestaurantHealthSummaries PK_AdminRestaurantHealthSummaries; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."AdminRestaurantHealthSummaries"
    ADD CONSTRAINT "PK_AdminRestaurantHealthSummaries" PRIMARY KEY ("RestaurantId");


--
-- Name: AspNetRoleClaims PK_AspNetRoleClaims; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."AspNetRoleClaims"
    ADD CONSTRAINT "PK_AspNetRoleClaims" PRIMARY KEY ("Id");


--
-- Name: AspNetRoles PK_AspNetRoles; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."AspNetRoles"
    ADD CONSTRAINT "PK_AspNetRoles" PRIMARY KEY ("Id");


--
-- Name: AspNetUserClaims PK_AspNetUserClaims; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."AspNetUserClaims"
    ADD CONSTRAINT "PK_AspNetUserClaims" PRIMARY KEY ("Id");


--
-- Name: AspNetUserLogins PK_AspNetUserLogins; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."AspNetUserLogins"
    ADD CONSTRAINT "PK_AspNetUserLogins" PRIMARY KEY ("LoginProvider", "ProviderKey");


--
-- Name: AspNetUserRoles PK_AspNetUserRoles; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."AspNetUserRoles"
    ADD CONSTRAINT "PK_AspNetUserRoles" PRIMARY KEY ("UserId", "RoleId");


--
-- Name: AspNetUserTokens PK_AspNetUserTokens; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."AspNetUserTokens"
    ADD CONSTRAINT "PK_AspNetUserTokens" PRIMARY KEY ("UserId", "LoginProvider", "Name");


--
-- Name: AspNetUsers PK_AspNetUsers; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."AspNetUsers"
    ADD CONSTRAINT "PK_AspNetUsers" PRIMARY KEY ("Id");


--
-- Name: CouponUserUsages PK_CouponUserUsages; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."CouponUserUsages"
    ADD CONSTRAINT "PK_CouponUserUsages" PRIMARY KEY ("CouponId", "UserId");


--
-- Name: Coupons PK_Coupons; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Coupons"
    ADD CONSTRAINT "PK_Coupons" PRIMARY KEY ("Id");


--
-- Name: CustomizationChoices PK_CustomizationChoices; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."CustomizationChoices"
    ADD CONSTRAINT "PK_CustomizationChoices" PRIMARY KEY ("CustomizationGroupId", "ChoiceId");


--
-- Name: CustomizationGroups PK_CustomizationGroups; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."CustomizationGroups"
    ADD CONSTRAINT "PK_CustomizationGroups" PRIMARY KEY ("Id");


--
-- Name: Devices PK_Devices; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Devices"
    ADD CONSTRAINT "PK_Devices" PRIMARY KEY ("Id");


--
-- Name: DomainUsers PK_DomainUsers; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."DomainUsers"
    ADD CONSTRAINT "PK_DomainUsers" PRIMARY KEY ("Id");


--
-- Name: FullMenuViews PK_FullMenuViews; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."FullMenuViews"
    ADD CONSTRAINT "PK_FullMenuViews" PRIMARY KEY ("RestaurantId");


--
-- Name: InboxMessages PK_InboxMessages; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."InboxMessages"
    ADD CONSTRAINT "PK_InboxMessages" PRIMARY KEY ("EventId", "Handler");


--
-- Name: MenuCategories PK_MenuCategories; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."MenuCategories"
    ADD CONSTRAINT "PK_MenuCategories" PRIMARY KEY ("Id");


--
-- Name: MenuItemSalesSummaries PK_MenuItemSalesSummaries; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."MenuItemSalesSummaries"
    ADD CONSTRAINT "PK_MenuItemSalesSummaries" PRIMARY KEY ("RestaurantId", "MenuItemId");


--
-- Name: MenuItems PK_MenuItems; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."MenuItems"
    ADD CONSTRAINT "PK_MenuItems" PRIMARY KEY ("Id");


--
-- Name: Menus PK_Menus; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Menus"
    ADD CONSTRAINT "PK_Menus" PRIMARY KEY ("Id");


--
-- Name: OrderItems PK_OrderItems; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."OrderItems"
    ADD CONSTRAINT "PK_OrderItems" PRIMARY KEY ("OrderId", "OrderItemId");


--
-- Name: Orders PK_Orders; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Orders"
    ADD CONSTRAINT "PK_Orders" PRIMARY KEY ("Id");


--
-- Name: OutboxMessages PK_OutboxMessages; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."OutboxMessages"
    ADD CONSTRAINT "PK_OutboxMessages" PRIMARY KEY ("Id");


--
-- Name: PaymentTransactions PK_PaymentTransactions; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."PaymentTransactions"
    ADD CONSTRAINT "PK_PaymentTransactions" PRIMARY KEY ("OrderId", "PaymentTransactionId");


--
-- Name: ProcessedWebhookEvents PK_ProcessedWebhookEvents; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."ProcessedWebhookEvents"
    ADD CONSTRAINT "PK_ProcessedWebhookEvents" PRIMARY KEY ("Id");


--
-- Name: RestaurantAccounts PK_RestaurantAccounts; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."RestaurantAccounts"
    ADD CONSTRAINT "PK_RestaurantAccounts" PRIMARY KEY ("Id");


--
-- Name: RestaurantRegistrations PK_RestaurantRegistrations; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."RestaurantRegistrations"
    ADD CONSTRAINT "PK_RestaurantRegistrations" PRIMARY KEY ("Id");


--
-- Name: RestaurantReviewSummaries PK_RestaurantReviewSummaries; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."RestaurantReviewSummaries"
    ADD CONSTRAINT "PK_RestaurantReviewSummaries" PRIMARY KEY ("RestaurantId");


--
-- Name: Restaurants PK_Restaurants; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Restaurants"
    ADD CONSTRAINT "PK_Restaurants" PRIMARY KEY ("Id");


--
-- Name: Reviews PK_Reviews; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Reviews"
    ADD CONSTRAINT "PK_Reviews" PRIMARY KEY ("Id");


--
-- Name: RoleAssignments PK_RoleAssignments; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."RoleAssignments"
    ADD CONSTRAINT "PK_RoleAssignments" PRIMARY KEY ("Id");


--
-- Name: SearchIndexItems PK_SearchIndexItems; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."SearchIndexItems"
    ADD CONSTRAINT "PK_SearchIndexItems" PRIMARY KEY ("Id");


--
-- Name: SupportTicketContextLinks PK_SupportTicketContextLinks; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."SupportTicketContextLinks"
    ADD CONSTRAINT "PK_SupportTicketContextLinks" PRIMARY KEY ("SupportTicketId", "EntityID", "EntityType");


--
-- Name: SupportTicketMessages PK_SupportTicketMessages; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."SupportTicketMessages"
    ADD CONSTRAINT "PK_SupportTicketMessages" PRIMARY KEY ("SupportTicketId", "MessageId");


--
-- Name: SupportTickets PK_SupportTickets; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."SupportTickets"
    ADD CONSTRAINT "PK_SupportTickets" PRIMARY KEY ("Id");


--
-- Name: Tags PK_Tags; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Tags"
    ADD CONSTRAINT "PK_Tags" PRIMARY KEY ("Id");


--
-- Name: TeamCartItems PK_TeamCartItems; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."TeamCartItems"
    ADD CONSTRAINT "PK_TeamCartItems" PRIMARY KEY ("TeamCartId", "TeamCartItemId");


--
-- Name: TeamCartMemberPayments PK_TeamCartMemberPayments; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."TeamCartMemberPayments"
    ADD CONSTRAINT "PK_TeamCartMemberPayments" PRIMARY KEY ("TeamCartId", "MemberPaymentId");


--
-- Name: TeamCartMembers PK_TeamCartMembers; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."TeamCartMembers"
    ADD CONSTRAINT "PK_TeamCartMembers" PRIMARY KEY ("TeamCartId", "TeamCartMemberId");


--
-- Name: TeamCarts PK_TeamCarts; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."TeamCarts"
    ADD CONSTRAINT "PK_TeamCarts" PRIMARY KEY ("Id");


--
-- Name: TodoItems PK_TodoItems; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."TodoItems"
    ADD CONSTRAINT "PK_TodoItems" PRIMARY KEY ("TodoListId", "TodoItemId");


--
-- Name: TodoLists PK_TodoLists; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."TodoLists"
    ADD CONSTRAINT "PK_TodoLists" PRIMARY KEY ("Id");


--
-- Name: UserAddresses PK_UserAddresses; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."UserAddresses"
    ADD CONSTRAINT "PK_UserAddresses" PRIMARY KEY ("UserId", "AddressId");


--
-- Name: UserDeviceSessions PK_UserDeviceSessions; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."UserDeviceSessions"
    ADD CONSTRAINT "PK_UserDeviceSessions" PRIMARY KEY ("Id");


--
-- Name: UserPaymentMethods PK_UserPaymentMethods; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."UserPaymentMethods"
    ADD CONSTRAINT "PK_UserPaymentMethods" PRIMARY KEY ("UserId", "PaymentMethodId");


--
-- Name: __EFMigrationsHistory PK___EFMigrationsHistory; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."__EFMigrationsHistory"
    ADD CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId");


--
-- Name: EmailIndex; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "EmailIndex" ON public."AspNetUsers" USING btree ("NormalizedEmail");


--
-- Name: IX_AccountTransaction_Created; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_AccountTransaction_Created" ON public."AccountTransactions" USING btree ("Created");


--
-- Name: IX_AccountTransactions_RestaurantAccountId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_AccountTransactions_RestaurantAccountId" ON public."AccountTransactions" USING btree ("RestaurantAccountId");


--
-- Name: IX_AccountTransactions_Timestamp; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_AccountTransactions_Timestamp" ON public."AccountTransactions" USING btree ("Timestamp");


--
-- Name: IX_AdminDailyPerformanceSeries_UpdatedAtUtc; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_AdminDailyPerformanceSeries_UpdatedAtUtc" ON public."AdminDailyPerformanceSeries" USING btree ("UpdatedAtUtc");


--
-- Name: IX_AdminPlatformMetricsSnapshots_UpdatedAtUtc; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_AdminPlatformMetricsSnapshots_UpdatedAtUtc" ON public."AdminPlatformMetricsSnapshots" USING btree ("UpdatedAtUtc");


--
-- Name: IX_AdminRestaurantHealthSummaries_UpdatedAtUtc; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_AdminRestaurantHealthSummaries_UpdatedAtUtc" ON public."AdminRestaurantHealthSummaries" USING btree ("UpdatedAtUtc");


--
-- Name: IX_AdminRestaurantHealthSummaries_VerifiedAccepting; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_AdminRestaurantHealthSummaries_VerifiedAccepting" ON public."AdminRestaurantHealthSummaries" USING btree ("IsVerified", "IsAcceptingOrders");


--
-- Name: IX_AspNetRoleClaims_RoleId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_AspNetRoleClaims_RoleId" ON public."AspNetRoleClaims" USING btree ("RoleId");


--
-- Name: IX_AspNetUserClaims_UserId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_AspNetUserClaims_UserId" ON public."AspNetUserClaims" USING btree ("UserId");


--
-- Name: IX_AspNetUserLogins_UserId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_AspNetUserLogins_UserId" ON public."AspNetUserLogins" USING btree ("UserId");


--
-- Name: IX_AspNetUserRoles_RoleId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_AspNetUserRoles_RoleId" ON public."AspNetUserRoles" USING btree ("RoleId");


--
-- Name: IX_AspNetUsers_PhoneNumber; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_AspNetUsers_PhoneNumber" ON public."AspNetUsers" USING btree ("PhoneNumber") WHERE ("PhoneNumber" IS NOT NULL);


--
-- Name: IX_Coupon_Created; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Coupon_Created" ON public."Coupons" USING btree ("Created");


--
-- Name: IX_Coupon_DeletedOn; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Coupon_DeletedOn" ON public."Coupons" USING btree ("DeletedOn");


--
-- Name: IX_Coupon_IsDeleted; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Coupon_IsDeleted" ON public."Coupons" USING btree ("IsDeleted");


--
-- Name: IX_Coupon_LastModified; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Coupon_LastModified" ON public."Coupons" USING btree ("LastModified");


--
-- Name: IX_Coupons_Code_RestaurantId; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_Coupons_Code_RestaurantId" ON public."Coupons" USING btree ("Code", "RestaurantId");


--
-- Name: IX_Coupons_CurrentTotalUsageCount; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Coupons_CurrentTotalUsageCount" ON public."Coupons" USING btree ("CurrentTotalUsageCount");


--
-- Name: IX_CustomizationGroup_Created; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_CustomizationGroup_Created" ON public."CustomizationGroups" USING btree ("Created");


--
-- Name: IX_CustomizationGroup_DeletedOn; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_CustomizationGroup_DeletedOn" ON public."CustomizationGroups" USING btree ("DeletedOn");


--
-- Name: IX_CustomizationGroup_IsDeleted; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_CustomizationGroup_IsDeleted" ON public."CustomizationGroups" USING btree ("IsDeleted");


--
-- Name: IX_CustomizationGroup_LastModified; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_CustomizationGroup_LastModified" ON public."CustomizationGroups" USING btree ("LastModified");


--
-- Name: IX_Devices_DeviceId; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_Devices_DeviceId" ON public."Devices" USING btree ("DeviceId") WHERE ("DeviceId" IS NOT NULL);


--
-- Name: IX_DomainUsers_Email; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_DomainUsers_Email" ON public."DomainUsers" USING btree ("Email");


--
-- Name: IX_FullMenuViews_LastRebuiltAt; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_FullMenuViews_LastRebuiltAt" ON public."FullMenuViews" USING btree ("LastRebuiltAt");


--
-- Name: IX_InboxMessages_ProcessedOnUtc; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_InboxMessages_ProcessedOnUtc" ON public."InboxMessages" USING btree ("ProcessedOnUtc");


--
-- Name: IX_MenuCategories_MenuId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_MenuCategories_MenuId" ON public."MenuCategories" USING btree ("MenuId");


--
-- Name: IX_MenuCategories_MenuId_DisplayOrder; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_MenuCategories_MenuId_DisplayOrder" ON public."MenuCategories" USING btree ("MenuId", "DisplayOrder");


--
-- Name: IX_MenuCategory_Created; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_MenuCategory_Created" ON public."MenuCategories" USING btree ("Created");


--
-- Name: IX_MenuCategory_DeletedOn; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_MenuCategory_DeletedOn" ON public."MenuCategories" USING btree ("DeletedOn");


--
-- Name: IX_MenuCategory_IsDeleted; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_MenuCategory_IsDeleted" ON public."MenuCategories" USING btree ("IsDeleted");


--
-- Name: IX_MenuCategory_LastModified; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_MenuCategory_LastModified" ON public."MenuCategories" USING btree ("LastModified");


--
-- Name: IX_MenuItemSalesSummaries_LastUpdatedAt; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_MenuItemSalesSummaries_LastUpdatedAt" ON public."MenuItemSalesSummaries" USING btree ("LastUpdatedAt");


--
-- Name: IX_MenuItemSalesSummaries_MenuItemId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_MenuItemSalesSummaries_MenuItemId" ON public."MenuItemSalesSummaries" USING btree ("MenuItemId");


--
-- Name: IX_MenuItem_Created; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_MenuItem_Created" ON public."MenuItems" USING btree ("Created");


--
-- Name: IX_MenuItem_DeletedOn; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_MenuItem_DeletedOn" ON public."MenuItems" USING btree ("DeletedOn");


--
-- Name: IX_MenuItem_IsDeleted; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_MenuItem_IsDeleted" ON public."MenuItems" USING btree ("IsDeleted");


--
-- Name: IX_MenuItem_LastModified; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_MenuItem_LastModified" ON public."MenuItems" USING btree ("LastModified");


--
-- Name: IX_MenuItems_Category_Available; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_MenuItems_Category_Available" ON public."MenuItems" USING btree ("MenuCategoryId", "IsAvailable");


--
-- Name: IX_MenuItems_IsAvailable; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_MenuItems_IsAvailable" ON public."MenuItems" USING btree ("IsAvailable");


--
-- Name: IX_MenuItems_MenuCategoryId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_MenuItems_MenuCategoryId" ON public."MenuItems" USING btree ("MenuCategoryId");


--
-- Name: IX_MenuItems_Name; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_MenuItems_Name" ON public."MenuItems" USING btree ("Name");


--
-- Name: IX_MenuItems_RestaurantId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_MenuItems_RestaurantId" ON public."MenuItems" USING btree ("RestaurantId");


--
-- Name: IX_MenuItems_Restaurant_Available; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_MenuItems_Restaurant_Available" ON public."MenuItems" USING btree ("RestaurantId", "IsAvailable");


--
-- Name: IX_Menu_Created; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Menu_Created" ON public."Menus" USING btree ("Created");


--
-- Name: IX_Menu_DeletedOn; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Menu_DeletedOn" ON public."Menus" USING btree ("DeletedOn");


--
-- Name: IX_Menu_IsDeleted; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Menu_IsDeleted" ON public."Menus" USING btree ("IsDeleted");


--
-- Name: IX_Menu_LastModified; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Menu_LastModified" ON public."Menus" USING btree ("LastModified");


--
-- Name: IX_Menus_RestaurantId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Menus_RestaurantId" ON public."Menus" USING btree ("RestaurantId");


--
-- Name: IX_Order_Created; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Order_Created" ON public."Orders" USING btree ("Created");


--
-- Name: IX_Orders_OrderNumber; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_Orders_OrderNumber" ON public."Orders" USING btree ("OrderNumber");


--
-- Name: IX_OutboxMessages_NextAttemptOnUtc; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_OutboxMessages_NextAttemptOnUtc" ON public."OutboxMessages" USING btree ("NextAttemptOnUtc");


--
-- Name: IX_OutboxMessages_OccurredOnUtc; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_OutboxMessages_OccurredOnUtc" ON public."OutboxMessages" USING btree ("OccurredOnUtc");


--
-- Name: IX_OutboxMessages_ProcessedOnUtc; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_OutboxMessages_ProcessedOnUtc" ON public."OutboxMessages" USING btree ("ProcessedOnUtc");


--
-- Name: IX_RestaurantAccount_Created; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_RestaurantAccount_Created" ON public."RestaurantAccounts" USING btree ("Created");


--
-- Name: IX_RestaurantAccounts_RestaurantId; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_RestaurantAccounts_RestaurantId" ON public."RestaurantAccounts" USING btree ("RestaurantId");


--
-- Name: IX_RestaurantRegistrations_Status; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_RestaurantRegistrations_Status" ON public."RestaurantRegistrations" USING btree ("Status");


--
-- Name: IX_RestaurantRegistrations_SubmittedAtUtc; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_RestaurantRegistrations_SubmittedAtUtc" ON public."RestaurantRegistrations" USING btree ("SubmittedAtUtc");


--
-- Name: IX_RestaurantRegistrations_Submitter_Name_City; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_RestaurantRegistrations_Submitter_Name_City" ON public."RestaurantRegistrations" USING btree ("SubmitterUserId", "Name", "City");


--
-- Name: IX_RestaurantReviewSummaries_AverageRating; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_RestaurantReviewSummaries_AverageRating" ON public."RestaurantReviewSummaries" USING btree ("AverageRating");


--
-- Name: IX_Restaurant_Created; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Restaurant_Created" ON public."Restaurants" USING btree ("Created");


--
-- Name: IX_Restaurant_DeletedOn; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Restaurant_DeletedOn" ON public."Restaurants" USING btree ("DeletedOn");


--
-- Name: IX_Restaurant_IsDeleted; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Restaurant_IsDeleted" ON public."Restaurants" USING btree ("IsDeleted");


--
-- Name: IX_Restaurant_LastModified; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Restaurant_LastModified" ON public."Restaurants" USING btree ("LastModified");


--
-- Name: IX_Restaurants_CuisineType; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Restaurants_CuisineType" ON public."Restaurants" USING btree ("CuisineType");


--
-- Name: IX_Restaurants_IsAcceptingOrders; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Restaurants_IsAcceptingOrders" ON public."Restaurants" USING btree ("IsAcceptingOrders");


--
-- Name: IX_Restaurants_IsVerified; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Restaurants_IsVerified" ON public."Restaurants" USING btree ("IsVerified");


--
-- Name: IX_Restaurants_Name; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Restaurants_Name" ON public."Restaurants" USING btree ("Name");


--
-- Name: IX_Restaurants_Verified_AcceptingOrders; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Restaurants_Verified_AcceptingOrders" ON public."Restaurants" USING btree ("IsVerified", "IsAcceptingOrders");


--
-- Name: IX_Review_Created; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Review_Created" ON public."Reviews" USING btree ("Created");


--
-- Name: IX_Review_DeletedOn; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Review_DeletedOn" ON public."Reviews" USING btree ("DeletedOn");


--
-- Name: IX_Review_IsDeleted; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Review_IsDeleted" ON public."Reviews" USING btree ("IsDeleted");


--
-- Name: IX_Review_LastModified; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Review_LastModified" ON public."Reviews" USING btree ("LastModified");


--
-- Name: IX_Reviews_CustomerId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Reviews_CustomerId" ON public."Reviews" USING btree ("CustomerId");


--
-- Name: IX_Reviews_Restaurant_SubmissionTimestamp; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Reviews_Restaurant_SubmissionTimestamp" ON public."Reviews" USING btree ("RestaurantId", "SubmissionTimestamp");


--
-- Name: IX_RoleAssignment_Created; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_RoleAssignment_Created" ON public."RoleAssignments" USING btree ("Created");


--
-- Name: IX_RoleAssignments_RestaurantId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_RoleAssignments_RestaurantId" ON public."RoleAssignments" USING btree ("RestaurantId");


--
-- Name: IX_RoleAssignments_Role; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_RoleAssignments_Role" ON public."RoleAssignments" USING btree ("Role");


--
-- Name: IX_RoleAssignments_UserId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_RoleAssignments_UserId" ON public."RoleAssignments" USING btree ("UserId");


--
-- Name: IX_RoleAssignments_User_Restaurant_Role; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_RoleAssignments_User_Restaurant_Role" ON public."RoleAssignments" USING btree ("UserId", "RestaurantId", "Role");


--
-- Name: IX_SupportTicket_Created; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_SupportTicket_Created" ON public."SupportTickets" USING btree ("Created");


--
-- Name: IX_SupportTickets_AssignedToAdminId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_SupportTickets_AssignedToAdminId" ON public."SupportTickets" USING btree ("AssignedToAdminId");


--
-- Name: IX_SupportTickets_LastUpdateTimestamp; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_SupportTickets_LastUpdateTimestamp" ON public."SupportTickets" USING btree ("LastUpdateTimestamp");


--
-- Name: IX_SupportTickets_Priority; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_SupportTickets_Priority" ON public."SupportTickets" USING btree ("Priority");


--
-- Name: IX_SupportTickets_Status; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_SupportTickets_Status" ON public."SupportTickets" USING btree ("Status");


--
-- Name: IX_SupportTickets_SubmissionTimestamp; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_SupportTickets_SubmissionTimestamp" ON public."SupportTickets" USING btree ("SubmissionTimestamp");


--
-- Name: IX_SupportTickets_TicketNumber; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_SupportTickets_TicketNumber" ON public."SupportTickets" USING btree ("TicketNumber");


--
-- Name: IX_Tag_Created; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Tag_Created" ON public."Tags" USING btree ("Created");


--
-- Name: IX_Tag_DeletedOn; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Tag_DeletedOn" ON public."Tags" USING btree ("DeletedOn");


--
-- Name: IX_Tag_IsDeleted; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Tag_IsDeleted" ON public."Tags" USING btree ("IsDeleted");


--
-- Name: IX_Tag_LastModified; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Tag_LastModified" ON public."Tags" USING btree ("LastModified");


--
-- Name: IX_Tags_TagCategory; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Tags_TagCategory" ON public."Tags" USING btree ("TagCategory");


--
-- Name: IX_Tags_TagName; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Tags_TagName" ON public."Tags" USING btree ("TagName");


--
-- Name: IX_TeamCart_Created; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_TeamCart_Created" ON public."TeamCarts" USING btree ("Created");


--
-- Name: IX_TodoList_Created; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_TodoList_Created" ON public."TodoLists" USING btree ("Created");


--
-- Name: IX_TodoList_LastModified; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_TodoList_LastModified" ON public."TodoLists" USING btree ("LastModified");


--
-- Name: IX_UserDeviceSessions_DeviceId_IsActive; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_UserDeviceSessions_DeviceId_IsActive" ON public."UserDeviceSessions" USING btree ("DeviceId", "IsActive");


--
-- Name: IX_User_Created; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_User_Created" ON public."DomainUsers" USING btree ("Created");


--
-- Name: IX_User_DeletedOn; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_User_DeletedOn" ON public."DomainUsers" USING btree ("DeletedOn");


--
-- Name: IX_User_IsDeleted; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_User_IsDeleted" ON public."DomainUsers" USING btree ("IsDeleted");


--
-- Name: IX_User_LastModified; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_User_LastModified" ON public."DomainUsers" USING btree ("LastModified");


--
-- Name: RoleNameIndex; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "RoleNameIndex" ON public."AspNetRoles" USING btree ("NormalizedName");


--
-- Name: SIDX_Geo; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "SIDX_Geo" ON public."SearchIndexItems" USING gist ("Geo");


--
-- Name: SIDX_Lower_Cuisine; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "SIDX_Lower_Cuisine" ON public."SearchIndexItems" USING btree (lower("Cuisine"));


--
-- Name: SIDX_PriceBand; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "SIDX_PriceBand" ON public."SearchIndexItems" USING btree ("PriceBand");


--
-- Name: SIDX_Soft_Deleted; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "SIDX_Soft_Deleted" ON public."SearchIndexItems" USING btree ("SoftDeleted");


--
-- Name: SIDX_Tags_Gin; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "SIDX_Tags_Gin" ON public."SearchIndexItems" USING gin ("Tags");


--
-- Name: SIDX_Trgm_Cuisine; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "SIDX_Trgm_Cuisine" ON public."SearchIndexItems" USING gin ("Cuisine" public.gin_trgm_ops);


--
-- Name: SIDX_Trgm_Name; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "SIDX_Trgm_Name" ON public."SearchIndexItems" USING gin ("Name" public.gin_trgm_ops);


--
-- Name: SIDX_Tsv_All; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "SIDX_Tsv_All" ON public."SearchIndexItems" USING gin ("TsAll");


--
-- Name: SIDX_Tsv_Descr; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "SIDX_Tsv_Descr" ON public."SearchIndexItems" USING gin ("TsDescr");


--
-- Name: SIDX_Tsv_Name; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "SIDX_Tsv_Name" ON public."SearchIndexItems" USING gin ("TsName");


--
-- Name: SIDX_Type_Open; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "SIDX_Type_Open" ON public."SearchIndexItems" USING btree ("Type", "IsOpenNow", "IsAcceptingOrders");


--
-- Name: SIDX_Updated_At; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "SIDX_Updated_At" ON public."SearchIndexItems" USING btree ("UpdatedAt");


--
-- Name: UX_Reviews_OrderId_Unique; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "UX_Reviews_OrderId_Unique" ON public."Reviews" USING btree ("OrderId") WHERE ("IsDeleted" = false);


--
-- Name: UserNameIndex; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "UserNameIndex" ON public."AspNetUsers" USING btree ("NormalizedUserName");


--
-- Name: idx_active_coupons_view_id; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX idx_active_coupons_view_id ON public.active_coupons_view USING btree (coupon_id);


--
-- Name: idx_active_coupons_view_restaurant; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_active_coupons_view_restaurant ON public.active_coupons_view USING btree (restaurant_id, validity_end_date, code);


--
-- Name: idx_active_coupons_view_validity; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_active_coupons_view_validity ON public.active_coupons_view USING btree (validity_start_date, validity_end_date);


--
-- Name: SearchIndexItems searchindexitems_tsvector_trg; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER searchindexitems_tsvector_trg BEFORE INSERT OR UPDATE OF "Name", "Description", "Cuisine", "Tags", "Keywords" ON public."SearchIndexItems" FOR EACH ROW EXECUTE FUNCTION public.searchindexitems_tsv_update();


--
-- Name: AspNetRoleClaims FK_AspNetRoleClaims_AspNetRoles_RoleId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."AspNetRoleClaims"
    ADD CONSTRAINT "FK_AspNetRoleClaims_AspNetRoles_RoleId" FOREIGN KEY ("RoleId") REFERENCES public."AspNetRoles"("Id") ON DELETE CASCADE;


--
-- Name: AspNetUserClaims FK_AspNetUserClaims_AspNetUsers_UserId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."AspNetUserClaims"
    ADD CONSTRAINT "FK_AspNetUserClaims_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES public."AspNetUsers"("Id") ON DELETE CASCADE;


--
-- Name: AspNetUserLogins FK_AspNetUserLogins_AspNetUsers_UserId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."AspNetUserLogins"
    ADD CONSTRAINT "FK_AspNetUserLogins_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES public."AspNetUsers"("Id") ON DELETE CASCADE;


--
-- Name: AspNetUserRoles FK_AspNetUserRoles_AspNetRoles_RoleId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."AspNetUserRoles"
    ADD CONSTRAINT "FK_AspNetUserRoles_AspNetRoles_RoleId" FOREIGN KEY ("RoleId") REFERENCES public."AspNetRoles"("Id") ON DELETE CASCADE;


--
-- Name: AspNetUserRoles FK_AspNetUserRoles_AspNetUsers_UserId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."AspNetUserRoles"
    ADD CONSTRAINT "FK_AspNetUserRoles_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES public."AspNetUsers"("Id") ON DELETE CASCADE;


--
-- Name: AspNetUserTokens FK_AspNetUserTokens_AspNetUsers_UserId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."AspNetUserTokens"
    ADD CONSTRAINT "FK_AspNetUserTokens_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES public."AspNetUsers"("Id") ON DELETE CASCADE;


--
-- Name: CustomizationChoices FK_CustomizationChoices_CustomizationGroups_CustomizationGroup~; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."CustomizationChoices"
    ADD CONSTRAINT "FK_CustomizationChoices_CustomizationGroups_CustomizationGroup~" FOREIGN KEY ("CustomizationGroupId") REFERENCES public."CustomizationGroups"("Id") ON DELETE CASCADE;


--
-- Name: OrderItems FK_OrderItems_Orders_OrderId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."OrderItems"
    ADD CONSTRAINT "FK_OrderItems_Orders_OrderId" FOREIGN KEY ("OrderId") REFERENCES public."Orders"("Id") ON DELETE CASCADE;


--
-- Name: PaymentTransactions FK_PaymentTransactions_Orders_OrderId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."PaymentTransactions"
    ADD CONSTRAINT "FK_PaymentTransactions_Orders_OrderId" FOREIGN KEY ("OrderId") REFERENCES public."Orders"("Id") ON DELETE CASCADE;


--
-- Name: SupportTicketContextLinks FK_SupportTicketContextLinks_SupportTickets_SupportTicketId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."SupportTicketContextLinks"
    ADD CONSTRAINT "FK_SupportTicketContextLinks_SupportTickets_SupportTicketId" FOREIGN KEY ("SupportTicketId") REFERENCES public."SupportTickets"("Id") ON DELETE CASCADE;


--
-- Name: SupportTicketMessages FK_SupportTicketMessages_SupportTickets_SupportTicketId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."SupportTicketMessages"
    ADD CONSTRAINT "FK_SupportTicketMessages_SupportTickets_SupportTicketId" FOREIGN KEY ("SupportTicketId") REFERENCES public."SupportTickets"("Id") ON DELETE CASCADE;


--
-- Name: TeamCartItems FK_TeamCartItems_TeamCarts_TeamCartId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."TeamCartItems"
    ADD CONSTRAINT "FK_TeamCartItems_TeamCarts_TeamCartId" FOREIGN KEY ("TeamCartId") REFERENCES public."TeamCarts"("Id") ON DELETE CASCADE;


--
-- Name: TeamCartMemberPayments FK_TeamCartMemberPayments_TeamCarts_TeamCartId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."TeamCartMemberPayments"
    ADD CONSTRAINT "FK_TeamCartMemberPayments_TeamCarts_TeamCartId" FOREIGN KEY ("TeamCartId") REFERENCES public."TeamCarts"("Id") ON DELETE CASCADE;


--
-- Name: TeamCartMembers FK_TeamCartMembers_TeamCarts_TeamCartId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."TeamCartMembers"
    ADD CONSTRAINT "FK_TeamCartMembers_TeamCarts_TeamCartId" FOREIGN KEY ("TeamCartId") REFERENCES public."TeamCarts"("Id") ON DELETE CASCADE;


--
-- Name: TodoItems FK_TodoItems_TodoLists_TodoListId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."TodoItems"
    ADD CONSTRAINT "FK_TodoItems_TodoLists_TodoListId" FOREIGN KEY ("TodoListId") REFERENCES public."TodoLists"("Id") ON DELETE CASCADE;


--
-- Name: UserAddresses FK_UserAddresses_DomainUsers_UserId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."UserAddresses"
    ADD CONSTRAINT "FK_UserAddresses_DomainUsers_UserId" FOREIGN KEY ("UserId") REFERENCES public."DomainUsers"("Id") ON DELETE CASCADE;


--
-- Name: UserPaymentMethods FK_UserPaymentMethods_DomainUsers_UserId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."UserPaymentMethods"
    ADD CONSTRAINT "FK_UserPaymentMethods_DomainUsers_UserId" FOREIGN KEY ("UserId") REFERENCES public."DomainUsers"("Id") ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

