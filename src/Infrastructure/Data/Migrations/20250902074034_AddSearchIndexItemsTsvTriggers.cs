using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;
using NpgsqlTypes;

#nullable disable

namespace YummyZoom.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSearchIndexItemsTsvTriggers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Ensure PostGIS extension is present before any spatial operations
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS postgis;", suppressTransaction: false);

            // Ensure SRID 4326 (WGS 84) exists in spatial_ref_sys for geography/geometry ops
            migrationBuilder.Sql(@"
INSERT INTO spatial_ref_sys (srid, auth_name, auth_srid, proj4text, srtext)
SELECT 4326, 'EPSG', 4326,
       '+proj=longlat +datum=WGS84 +no_defs',
       'GEOGCS[""WGS 84"",DATUM[""WGS_1984"",SPHEROID[""WGS 84"",6378137,298.257223563,AUTHORITY[""EPSG"",""7030""]],AUTHORITY[""EPSG"",""6326""]],PRIMEM[""Greenwich"",0,AUTHORITY[""EPSG"",""8901""]],UNIT[""degree"",0.0174532925199433,AUTHORITY[""EPSG"",""9122""]],AUTHORITY[""EPSG"",""4326""]]'
WHERE NOT EXISTS (SELECT 1 FROM spatial_ref_sys WHERE srid = 4326);
            ");

            migrationBuilder.AlterColumn<NpgsqlTsVector>(
                name: "TsName",
                table: "SearchIndexItems",
                type: "tsvector",
                nullable: false,
                oldClrType: typeof(NpgsqlTsVector),
                oldType: "tsvector",
                oldComputedColumnSql: "to_tsvector('simple', coalesce(\"Name\",''))");

            migrationBuilder.AlterColumn<NpgsqlTsVector>(
                name: "TsDescr",
                table: "SearchIndexItems",
                type: "tsvector",
                nullable: false,
                oldClrType: typeof(NpgsqlTsVector),
                oldType: "tsvector",
                oldComputedColumnSql: "to_tsvector('simple', coalesce(\"Description\",''))");

            migrationBuilder.AlterColumn<NpgsqlTsVector>(
                name: "TsAll",
                table: "SearchIndexItems",
                type: "tsvector",
                nullable: false,
                oldClrType: typeof(NpgsqlTsVector),
                oldType: "tsvector",
                oldComputedColumnSql: "setweight(to_tsvector('simple', coalesce(\"Name\",'')), 'A') || setweight(to_tsvector('simple', coalesce(\"Cuisine\",'')), 'B') || setweight(to_tsvector('simple', coalesce(array_to_string(\"Tags\",' '),'')), 'B') || setweight(to_tsvector('simple', coalesce(\"Description\",'')), 'C') || setweight(to_tsvector('simple', coalesce(array_to_string(\"Keywords\",' '),'')), 'C')");

            migrationBuilder.AlterColumn<Point>(
                name: "Geo",
                table: "SearchIndexItems",
                type: "geography (point, 4326)",
                nullable: true,
                oldClrType: typeof(Point),
                oldType: "geography(Point,4326)",
                oldNullable: true);

            // Create or replace the trigger function to maintain tsvector columns
            migrationBuilder.Sql(@"CREATE OR REPLACE FUNCTION searchindexitems_tsv_update() RETURNS trigger AS $$
BEGIN
  NEW.""TsName"" := to_tsvector('simple', coalesce(NEW.""Name"",''));
  NEW.""TsDescr"" := to_tsvector('simple', coalesce(NEW.""Description"",''));
  NEW.""TsAll"" :=
      setweight(to_tsvector('simple', coalesce(NEW.""Name"",'')), 'A') ||
      setweight(to_tsvector('simple', coalesce(NEW.""Cuisine"",'')), 'B') ||
      setweight(to_tsvector('simple', coalesce(array_to_string(NEW.""Tags"", ' '),'')), 'B') ||
      setweight(to_tsvector('simple', coalesce(NEW.""Description"",'')), 'C') ||
      setweight(to_tsvector('simple', coalesce(array_to_string(NEW.""Keywords"", ' '),'')), 'C');
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop trigger if exists
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

            migrationBuilder.AlterColumn<Point>(
                name: "Geo",
                table: "SearchIndexItems",
                type: "geography(Point,4326)",
                nullable: true,
                oldClrType: typeof(Point),
                oldType: "geography (point, 4326)",
                oldNullable: true);

            migrationBuilder.AlterColumn<NpgsqlTsVector>(
                name: "TsName",
                table: "SearchIndexItems",
                type: "tsvector",
                nullable: false,
                computedColumnSql: "to_tsvector('simple', coalesce(\"Name\",''))",
                stored: true,
                oldClrType: typeof(NpgsqlTsVector),
                oldType: "tsvector");

            migrationBuilder.AlterColumn<NpgsqlTsVector>(
                name: "TsDescr",
                table: "SearchIndexItems",
                type: "tsvector",
                nullable: false,
                computedColumnSql: "to_tsvector('simple', coalesce(\"Description\",''))",
                stored: true,
                oldClrType: typeof(NpgsqlTsVector),
                oldType: "tsvector");

            migrationBuilder.AlterColumn<NpgsqlTsVector>(
                name: "TsAll",
                table: "SearchIndexItems",
                type: "tsvector",
                nullable: false,
                computedColumnSql: "setweight(to_tsvector('simple', coalesce(\"Name\",'')), 'A') || setweight(to_tsvector('simple', coalesce(\"Cuisine\",'')), 'B') || setweight(to_tsvector('simple', coalesce(array_to_string(\"Tags\",' '),'')), 'B') || setweight(to_tsvector('simple', coalesce(\"Description\",'')), 'C') || setweight(to_tsvector('simple', coalesce(array_to_string(\"Keywords\",' '),'')), 'C')",
                stored: true,
                oldClrType: typeof(NpgsqlTsVector),
                oldType: "tsvector");
        }
    }
}
