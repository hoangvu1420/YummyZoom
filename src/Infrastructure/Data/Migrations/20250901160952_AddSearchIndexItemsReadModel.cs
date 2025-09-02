using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;
using NpgsqlTypes;

#nullable disable

namespace YummyZoom.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSearchIndexItemsReadModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.CreateTable(
                name: "SearchIndexItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    RestaurantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Cuisine = table.Column<string>(type: "text", nullable: true),
                    Tags = table.Column<string[]>(type: "text[]", nullable: true),
                    Keywords = table.Column<string[]>(type: "text[]", nullable: true),
                    IsOpenNow = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsAcceptingOrders = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    AvgRating = table.Column<double>(type: "double precision", nullable: true),
                    ReviewCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    PriceBand = table.Column<short>(type: "smallint", nullable: true),
                    Geo = table.Column<Point>(type: "geography(Point,4326)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    SourceVersion = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    SoftDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    TsAll = table.Column<NpgsqlTsVector>(type: "tsvector", nullable: false),
                    TsName = table.Column<NpgsqlTsVector>(type: "tsvector", nullable: false),
                    TsDescr = table.Column<NpgsqlTsVector>(type: "tsvector", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchIndexItems", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "SIDX_Geo",
                table: "SearchIndexItems",
                column: "Geo")
                .Annotation("Npgsql:IndexMethod", "GIST");

            migrationBuilder.CreateIndex(
                name: "SIDX_Soft_Deleted",
                table: "SearchIndexItems",
                column: "SoftDeleted");

            migrationBuilder.CreateIndex(
                name: "SIDX_Tags_Gin",
                table: "SearchIndexItems",
                column: "Tags")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "SIDX_Trgm_Cuisine",
                table: "SearchIndexItems",
                column: "Cuisine")
                .Annotation("Npgsql:IndexMethod", "GIN")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "SIDX_Trgm_Name",
                table: "SearchIndexItems",
                column: "Name")
                .Annotation("Npgsql:IndexMethod", "GIN")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "SIDX_Tsv_All",
                table: "SearchIndexItems",
                column: "TsAll")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "SIDX_Tsv_Descr",
                table: "SearchIndexItems",
                column: "TsDescr")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "SIDX_Tsv_Name",
                table: "SearchIndexItems",
                column: "TsName")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "SIDX_Type_Open",
                table: "SearchIndexItems",
                columns: new[] { "Type", "IsOpenNow", "IsAcceptingOrders" });

            migrationBuilder.CreateIndex(
                name: "SIDX_Updated_At",
                table: "SearchIndexItems",
                column: "UpdatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SearchIndexItems");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:postgis", ",,");
        }
    }
}
