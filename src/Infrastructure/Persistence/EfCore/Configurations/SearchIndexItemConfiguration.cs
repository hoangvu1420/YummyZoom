using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YummyZoom.Infrastructure.Persistence.ReadModels.Search;

namespace YummyZoom.Infrastructure.Persistence.EfCore.Configurations;

public sealed class SearchIndexItemConfiguration : IEntityTypeConfiguration<SearchIndexItem>
{
    public void Configure(EntityTypeBuilder<SearchIndexItem> builder)
    {
        builder.ToTable("SearchIndexItems");

        // Columns (use EF default column names matching property names)
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Type).IsRequired();

        builder.Property(e => e.IsOpenNow).HasDefaultValue(false);
        builder.Property(e => e.IsAcceptingOrders).HasDefaultValue(false);
        builder.Property(e => e.ReviewCount).HasDefaultValue(0);
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
        builder.Property(e => e.SourceVersion).HasDefaultValue(0);
        builder.Property(e => e.SoftDeleted).HasDefaultValue(false);

        // Store as PostGIS geography with SRID 4326
        builder.Property(e => e.Geo)
            .HasColumnType("geography (point, 4326)");

        // tsvector columns (values maintained by database trigger)
        builder.Property(e => e.TsName)
            .HasColumnType("tsvector");

        builder.Property(e => e.TsDescr)
            .HasColumnType("tsvector");

        builder.Property(e => e.TsAll)
            .HasColumnType("tsvector");

        // Indexes
        builder.HasIndex(e => e.TsAll).HasDatabaseName("SIDX_Tsv_All").HasMethod("GIN");
        builder.HasIndex(e => e.TsName).HasDatabaseName("SIDX_Tsv_Name").HasMethod("GIN");
        builder.HasIndex(e => e.TsDescr).HasDatabaseName("SIDX_Tsv_Descr").HasMethod("GIN");

        builder.HasIndex(e => e.Name).HasDatabaseName("SIDX_Trgm_Name").HasMethod("GIN")
            .HasOperators("gin_trgm_ops");
        builder.HasIndex(e => e.Cuisine).HasDatabaseName("SIDX_Trgm_Cuisine").HasMethod("GIN")
            .HasOperators("gin_trgm_ops");

        builder.HasIndex(e => e.Tags).HasDatabaseName("SIDX_Tags_Gin").HasMethod("GIN");
        builder.HasIndex(e => e.Geo).HasDatabaseName("SIDX_Geo").HasMethod("GIST");
        builder.HasIndex(e => e.PriceBand).HasDatabaseName("SIDX_PriceBand");

        builder.HasIndex(e => new { e.Type, e.IsOpenNow, e.IsAcceptingOrders })
            .HasDatabaseName("SIDX_Type_Open");
        builder.HasIndex(e => e.SoftDeleted).HasDatabaseName("SIDX_Soft_Deleted");
        builder.HasIndex(e => e.UpdatedAt).HasDatabaseName("SIDX_Updated_At");
    }
}
