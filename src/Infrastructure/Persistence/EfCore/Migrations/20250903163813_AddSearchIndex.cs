#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;
using NpgsqlTypes;

namespace YummyZoom.Infrastructure.Persistence.EfCore.Migrations
{
    /// <inheritdoc />
    public partial class AddSearchIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .Annotation("Npgsql:PostgresExtension:postgis", ",,")
                .Annotation("Npgsql:PostgresExtension:unaccent", ",,");

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
                    Geo = table.Column<Point>(type: "geography (point, 4326)", nullable: true),
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
                name: "SIDX_PriceBand",
                table: "SearchIndexItems",
                column: "PriceBand");

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


            // --- Manual SQL carried forward from consolidated migrations ---
            // Ensure required extensions are present before any operations
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS unaccent;", suppressTransaction: false);
            // PostGIS for spatial types and functions
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS postgis;", suppressTransaction: false);

            // Ensure SRID 4326 (WGS 84) exists in spatial_ref_sys for geography/geometry ops
            migrationBuilder.Sql(@"
INSERT INTO spatial_ref_sys (srid, auth_name, auth_srid, proj4text, srtext)
SELECT 4326, 'EPSG', 4326,
       '+proj=longlat +datum=WGS84 +no_defs',
       'GEOGCS[""WGS 84"",DATUM[""WGS_1984"",SPHEROID[""WGS 84"",6378137,298.257223563,AUTHORITY[""EPSG"",""7030""]],AUTHORITY[""EPSG"",""6326""]],PRIMEM[""Greenwich"",0,AUTHORITY[""EPSG"",""8901""]],UNIT[""degree"",0.0174532925199433,AUTHORITY[""EPSG"",""9122""]],AUTHORITY[""EPSG"",""4326""]]'
WHERE NOT EXISTS (SELECT 1 FROM spatial_ref_sys WHERE srid = 4326);
            ");

            // Create or replace the trigger function to maintain tsvector columns (accent-insensitive via unaccent)
            migrationBuilder.Sql(@"CREATE OR REPLACE FUNCTION searchindexitems_tsv_update() RETURNS trigger AS $$
BEGIN
  NEW.""TsName"" := to_tsvector('simple', unaccent(coalesce(NEW.""Name"",'')));
  NEW.""TsDescr"" := to_tsvector('simple', unaccent(coalesce(NEW.""Description"",'')));
  NEW.""TsAll"" :=
      setweight(to_tsvector('simple', unaccent(coalesce(NEW.""Name"",''))), 'A') ||
      setweight(to_tsvector('simple', unaccent(coalesce(NEW.""Cuisine"",''))), 'B') ||
      setweight(to_tsvector('simple', unaccent(coalesce(array_to_string(NEW.""Tags"", ' '),''))), 'B') ||
      setweight(to_tsvector('simple', unaccent(coalesce(NEW.""Description"",''))), 'C') ||
      setweight(to_tsvector('simple', unaccent(coalesce(array_to_string(NEW.""Keywords"", ' '),''))), 'C');
  RETURN NEW;
END
$$ LANGUAGE plpgsql;",
                suppressTransaction: false);

            // Create the trigger if it does not already exist
            migrationBuilder.Sql(@"DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM pg_trigger WHERE tgname = 'searchindexitems_tsvector_trg'
  ) THEN
    CREATE TRIGGER searchindexitems_tsvector_trg
    BEFORE INSERT OR UPDATE OF ""Name"", ""Description"", ""Cuisine"", ""Tags"", ""Keywords""
    ON ""SearchIndexItems""
    FOR EACH ROW
    EXECUTE FUNCTION searchindexitems_tsv_update();
  END IF;
END
$$;",
                suppressTransaction: false);

            // Backfill existing rows so tsvector columns are populated
            migrationBuilder.Sql("UPDATE \"SearchIndexItems\" SET \"Name\" = \"Name\";", suppressTransaction: false);

            // Expression index to speed case-insensitive grouping and equality on Cuisine
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"SIDX_Lower_Cuisine\" ON \"SearchIndexItems\" (LOWER(\"Cuisine\"));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop trigger if exists (must happen before dropping the table)
            migrationBuilder.Sql(@"DO $$
BEGIN
  IF EXISTS (
    SELECT 1 FROM pg_trigger WHERE tgname = 'searchindexitems_tsvector_trg'
  ) THEN
    DROP TRIGGER searchindexitems_tsvector_trg ON ""SearchIndexItems"";
  END IF;
END
$$;",
                suppressTransaction: false);

            // Drop function if exists
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS searchindexitems_tsv_update();", suppressTransaction: false);

            // Drop expression index if exists (in case table still exists at this point)
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"SIDX_Lower_Cuisine\";");

            migrationBuilder.DropTable(
                name: "SearchIndexItems");

            migrationBuilder.DropTable(
                name: "SearchIndexItems");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:postgis", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:unaccent", ",,");
        }
    }
}
