## Current project status (patterns and infrastructure)

- Domain
  - Restaurant aggregate has Name, CuisineType, Location VO, GeoCoordinates VO (optional), BusinessHours VO (string-based for now), IsAcceptingOrders, IsVerified, and events including RestaurantCreated, RestaurantVerified, RestaurantAcceptingOrders/RestaurantNotAcceptingOrders, RestaurantBusinessHoursChanged, etc.
  - Menu, MenuItem aggregates are present.
- Application
  - Queries use MediatR + Result pattern (YummyZoom.SharedKernel.Result and YummyZoom.Application.Common.Models.Result wrapper exists).
  - Dapper is used for read-side performance via IDbConnectionFactory and helpers.
  - Dapper pagination helper exists: Application/Orders/Queries/Common/DapperPagination.cs with BuildPagedSql + QueryPageAsync<T>.
  - Existing search: Restaurants search is a simple ILIKE + Dapper query (SearchRestaurantsQuery/Handler) and exposed in Web/Endpoints/Restaurants.cs.
  - IdempotentNotificationHandler<TEvent> is available for Outbox/Inbox idempotent projections.
- Infrastructure
  - EF Core ApplicationDbContext with migrations.
  - Outbox implemented: Infrastructure/Outbox with OutboxProcessor, OutboxPublisherHostedService; Inbox store used by IdempotentNotificationHandler.
  - Read models: an example FullMenu read model/utility exists under Infrastructure/ReadModels/FullMenu.
  - DbConnectionFactory wraps Npgsql with a configured connection string.

These are sufficient to implement a Postgres-only Universal Search MVP exactly as specified in your doc.

## Universal Search MVP scope (what’s in v1)

- Entities included: restaurants and menu items only.
- Features included:
  - Single denormalized read model table SearchIndexItem with FTS vectors and minimal ranking features.
  - Search endpoint: GET /api/v1/search (text, optional filters, optional lat/lon for distance score).
  - Autocomplete endpoint: GET /api/v1/search/autocomplete (prefix + trigram).
  - Outbox-driven incremental upserts for Restaurant and MenuItem changes (idempotent).
  - Full rebuild command (admin-only) to repopulate the read model.
- Deferred to later iterations:
  - Tags/cuisines facets and aggregation responses.
  - Diversification logic and explanations/badges.
  - Multilingual configurations (vi_search) and synonym dictionaries.
  - PostGIS usage is included in MVP (geography point + GiST index + ST_Distance for ranking).
  - Promos and reviews integration (stub ratings for now; wire in later).

## Database schema (EF migration)

Create EF migration: AddSearchIndexItemsReadModel

- Table: SearchIndexItems
  - Id uuid PK
  - Type text CHECK IN ('restaurant','menu_item')
  - RestaurantId uuid NULL
  - Name text NOT NULL
  - Description text NULL
  - Cuisine text NULL
  - Tags text[] NULL
  - Keywords text[] NULL
  - IsOpenNow boolean NOT NULL DEFAULT false
  - IsAcceptingOrders boolean NOT NULL DEFAULT false
  - PriceBand smallint NULL
  - AvgRating double precision NULL
  - ReviewCount int NOT NULL DEFAULT 0
  - Geo geography(Point, 4326) NULL
  - CreatedAt timestamptz NOT NULL DEFAULT now()
  - UpdatedAt timestamptz NOT NULL DEFAULT now()
  - SourceVersion bigint NOT NULL DEFAULT 0
  - SoftDeleted boolean NOT NULL DEFAULT false
  - TsName tsvector GENERATED ALWAYS AS (to_tsvector('simple', coalesce("Name",''))) STORED
  - TsDescr tsvector GENERATED ALWAYS AS (to_tsvector('simple', coalesce("Description",''))) STORED
  - TsAll tsvector GENERATED ALWAYS AS (
      setweight(to_tsvector('simple', coalesce("Name",'')), 'A') ||
      setweight(to_tsvector('simple', coalesce("Cuisine",'')), 'B') ||
      setweight(to_tsvector('simple', coalesce(array_to_string("Tags",' '),'')), 'B') ||
      setweight(to_tsvector('simple', coalesce("Description",'')), 'C') ||
      setweight(to_tsvector('simple', coalesce(array_to_string("Keywords",' '),'')), 'C')
    ) STORED

- Required extensions:
  - pg_trgm
  - postgis
  - Optional: unaccent (future)

- Indexes:
  - GIN(TsAll) as SIDX_Tsv_All
  - GIN(TsName) as SIDX_Tsv_Name (optional)
  - GIN(TsDescr) as SIDX_Tsv_Descr (optional)
  - GIN(Name gin_trgm_ops) as SIDX_Trgm_Name
  - GIN(Cuisine gin_trgm_ops) as SIDX_Trgm_Cuisine (optional)
  - GIN(Tags) as SIDX_Tags_Gin (later)
  - GiST(Geo) as SIDX_Geo
  - btree(Type, IsOpenNow, IsAcceptingOrders) as SIDX_Type_Open
  - btree(SoftDeleted) as SIDX_Soft_Deleted
  - btree(UpdatedAt desc) as SIDX_Updated_At

Implementation notes:
- Define schema purely via EF Core model + Fluent API: modelBuilder.HasPostgresExtension("pg_trgm"); modelBuilder.HasPostgresExtension("postgis"); configure generated tsvector via HasGeneratedTsVectorColumn or HasComputedColumnSql for weighted vectors; configure indexes with .HasMethod("GIN"/"GIST") and trigram operator class (e.g., HasOperators("gin_trgm_ops"))
- Using PostGIS now: store a geography(Point,4326) column geo, create GiST(geo) index, use ST_GeogFromText on upserts and ST_Distance/ST_DWithin in queries. No manual edits to migrations.

Files:
- src/Infrastructure/Data/Models/SearchIndexItem.cs (EF entity for read model)
- src/Infrastructure/Data/Configurations/SearchIndexItemConfiguration.cs (Fluent API configuration)
- src/Infrastructure/Data/Migrations/XXXXXX_AddSearchIndexItemsReadModel.cs (generated; do not edit)
- src/Infrastructure/DependencyInjection.cs: ensure UseNpgsql(..., o => o.UseNetTopologySuite()) and DbContext is configured

### Entity + Configuration skeletons (EF + Fluent API)

- src/Infrastructure/Data/Models/SearchIndexItem.cs

````csharp
using NetTopologySuite.Geometries;
using NpgsqlTypes;

namespace YummyZoom.Infrastructure.Data.Models;

public sealed class SearchIndexItem
{
    public Guid Id { get; set; }
    public string Type { get; set; } = default!; // 'restaurant' | 'menu_item'
    public Guid? RestaurantId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Cuisine { get; set; }
    public string[]? Tags { get; set; }
    public string[]? Keywords { get; set; }

    public bool IsOpenNow { get; set; }
    public bool IsAcceptingOrders { get; set; }
    public double? AvgRating { get; set; }
    public int ReviewCount { get; set; }
    public short? PriceBand { get; set; }

    public Point? Geo { get; set; } // geography(Point,4326)

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public long SourceVersion { get; set; }
    public bool SoftDeleted { get; set; }

    // Generated columns (tsvector)
    public NpgsqlTsVector TsAll { get; private set; } = null!;
    public NpgsqlTsVector TsName { get; private set; } = null!;
    public NpgsqlTsVector TsDescr { get; private set; } = null!;
}
````

- src/Infrastructure/Data/Configurations/SearchIndexItemConfiguration.cs

````csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YummyZoom.Infrastructure.Data.Models;

namespace YummyZoom.Infrastructure.Data.Configurations;

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

        builder.Property(e => e.Geo)
            .HasColumnType("geography(Point,4326)");

        // Generated tsvector columns
        builder.Property(e => e.TsName)
            .HasComputedColumnSql("to_tsvector('simple', coalesce(\"Name\",''))", stored: true);

        builder.Property(e => e.TsDescr)
            .HasComputedColumnSql("to_tsvector('simple', coalesce(\"Description\",''))", stored: true);

        builder.Property(e => e.TsAll)
            .HasComputedColumnSql(
                "setweight(to_tsvector('simple', coalesce(\"Name\",'')), 'A') || " +
                "setweight(to_tsvector('simple', coalesce(\"Cuisine\",'')), 'B') || " +
                "setweight(to_tsvector('simple', coalesce(array_to_string(\"Tags\",' '),'')), 'B') || " +
                "setweight(to_tsvector('simple', coalesce(\"Description\",'')), 'C') || " +
                "setweight(to_tsvector('simple', coalesce(array_to_string(\"Keywords\",' '),'')), 'C')",
                stored: true);

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

        builder.HasIndex(e => new { e.Type, e.IsOpenNow, e.IsAcceptingOrders })
            .HasDatabaseName("SIDX_Type_Open");
        builder.HasIndex(e => e.SoftDeleted).HasDatabaseName("SIDX_Soft_Deleted");
        builder.HasIndex(e => e.UpdatedAt).HasDatabaseName("SIDX_Updated_At");
    }
}
````

### DbContext and DI registration (NetTopologySuite + extensions)

- src/Infrastructure/Data/ApplicationDbContext.cs (excerpt)

````csharp
using Microsoft.EntityFrameworkCore;
using YummyZoom.Infrastructure.Data.Configurations;

public partial class ApplicationDbContext : DbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasPostgresExtension("pg_trgm");
        modelBuilder.HasPostgresExtension("postgis");
        modelBuilder.ApplyConfiguration(new SearchIndexItemConfiguration());
    }
}
````

- src/Infrastructure/DependencyInjection.cs (excerpt)

````csharp
services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(
        configuration.GetConnectionString("YummyZoomDb"),
        npgsql => npgsql.UseNetTopologySuite());
});
````


## Infrastructure: read model maintainer (Dapper) + rebuild

- Maintainer class (Dapper) holding all read model operations (pattern mirrors FullMenuViewRebuilder)
  - Path: src/Infrastructure/ReadModels/Search/SearchIndexMaintainer.cs
  - Methods (ID-driven; maintainer fetches latest state via Dapper):
    - Task UpsertRestaurantByIdAsync(Guid restaurantId, long sourceVersion, CancellationToken ct)
    - Task UpsertMenuItemByIdAsync(Guid menuItemId, long sourceVersion, CancellationToken ct)
    - Task SoftDeleteByIdAsync(Guid id, long sourceVersion, CancellationToken ct)
    - Optional: Task RebuildAsync(CancellationToken ct), Task RebuildRestaurantAsync(Guid restaurantId, CancellationToken ct)
  - SQL pattern: one PK-based SELECT to fetch, then a single INSERT ... ON CONFLICT ("Id") DO UPDATE with SourceVersion guard ("SearchIndexItems"."SourceVersion" <= EXCLUDED."SourceVersion")

- No external DTO required at call sites: callers pass only Id + Version; maintainer performs Dapper fetches and composes parameters internally (including WKT from Geo_Latitude/Geo_Longitude).

- Event handlers (idempotent) that project domain events into upserts:
  - Folder: src/Application/Search/EventHandlers/
  - Derive from IdempotentNotificationHandler<TEvent>
  - Inject ISearchReadModelMaintainer (Application interface) and call ById methods
  - Restaurant handlers (MVP):
    - RestaurantCreated/Verified/AcceptingOrdersChanged/BusinessHoursChanged → UpsertRestaurantByIdAsync(RestaurantId, Version)
    - Optional: other updates (e.g., Location/Geo change) → UpsertRestaurantByIdAsync
  - Menu item handlers (MVP):
    - MenuItemCreated/Updated → UpsertMenuItemByIdAsync(MenuItemId, Version)
    - MenuItemDeleted → SoftDeleteByIdAsync(MenuItemId, Version)
  - Ratings integration (defer): use RestaurantReviewSummaries in maintainer to enrich AvgRating/ReviewCount

- Full rebuild job (in the same maintainer class):
  - RebuildAsync and/or RebuildRestaurantAsync enumerate IDs and reuse the same ById methods
  - Reads Restaurants + MenuItems from canonical tables
  - Batches upserts; optionally truncates "SearchIndexItems" first (inside a tx)

- Dependency injection:
  - src/Infrastructure/DependencyInjection.cs: register ISearchReadModelMaintainer (Application) implemented by SearchIndexMaintainer (Infrastructure) as a scoped service

## Application: queries, handlers, DTOs

- Feature folder: src/Application/Search/

1) Universal search query
- Request type:
  - Path: src/Application/Search/Queries/UniversalSearch/UniversalSearchQuery.cs
  - Fields:
    - string? Term
    - double? Latitude, double? Longitude
    - bool? OpenNow
    - string[]? Cuisines
    - int PageNumber, int PageSize
- Response:
  - Result<PaginatedList<SearchResultDto>>

- DTO:
  - Path: src/Application/Search/Queries/UniversalSearch/SearchResultDto.cs
  - Fields:
    - Guid Id
    - string Type
    - Guid? RestaurantId
    - string Name
    - string? DescriptionSnippet
    - double Score
    - double? DistanceKm
    - string? Cuisine

- Handler:
  - Path: src/Application/Search/Queries/UniversalSearch/UniversalSearchQueryHandler.cs
  - Inject IDbConnectionFactory
  - Build SQL (Dapper) using ts_all with websearch_to_tsquery('simple', @q) if q provided; fallback to ILIKE and trigram for very short queries or empty q.
  - Use DapperPagination.BuildPagedSql + QueryPageAsync<SearchResultRow>
  - Ranking blend (PostGIS):
    - text_score = ts_rank_cd(ts_all, query)
    - open_boost = CASE WHEN is_open_now AND is_accepting_orders THEN 1 ELSE 0 END
    - distance_score = CASE WHEN @lat IS NOT NULL AND @lon IS NOT NULL AND geo IS NOT NULL THEN 1.0 / (1.0 + (ST_Distance(geo, ST_SetSRID(ST_MakePoint(@lon,@lat),4326)::geography) / 1000.0)) ELSE 0.5 END
    - score = 0.6*text_score + 0.2*distance_score + 0.2*open_boost
  - Return PaginatedList<SearchResultDto>

2) Autocomplete query
- Request:
  - Path: src/Application/Search/Queries/Autocomplete/AutocompleteQuery.cs
  - Fields: string Term
- Response: Result<IReadOnlyList<SuggestionDto>>
- DTO:
  - Path: src/Application/Search/Queries/Autocomplete/SuggestionDto.cs
  - Fields: Guid Id, string Type, string Name
- Handler:
  - Path: src/Application/Search/Queries/Autocomplete/AutocompleteQueryHandler.cs
  - Query:
    - SELECT id, type, name FROM search_index_items
      WHERE name ILIKE @prefix || '%' OR similarity(name, @q) > 0.3
      ORDER BY GREATEST(similarity(name,@q), CASE WHEN name ILIKE @prefix || '%' THEN 1 ELSE 0 END) DESC
      LIMIT 10

- Validators (FluentValidation):
  - UniversalSearchQueryValidator.cs: page bounds, term length, lat/lon range
  - AutocompleteQueryValidator.cs: term length >= 1 and <= 64

## Web endpoints

- Create new endpoint group: src/Web/Endpoints/Search.cs
- Pattern mirrors Restaurants.cs and uses EndpointGroupBase.
- Endpoints:
  - GET /api/v1/search
    - Maps to UniversalSearchQuery (Term, lat, lon, openNow, cuisine[], pageNumber, pageSize)
  - GET /api/v1/search/autocomplete
    - Maps to AutocompleteQuery (q)

Example map snippets:

````csharp path=src/Web/Endpoints/Search.cs mode=EXCERPT
public class Search : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var publicGroup = app.MapGroup(this);
        publicGroup.MapGet("/", async (string? q, double? lat, double? lon, bool? openNow, string[]? cuisines, int pageNumber, int pageSize, ISender sender) =>
        {
            var rq = new UniversalSearchQuery(q, lat, lon, openNow, cuisines, pageNumber, pageSize);
            var res = await sender.Send(rq);
            return res.IsSuccess ? Results.Ok(res.Value) : res.ToIResult();
        }).WithName("UniversalSearch").WithSummary("Universal search");
    }
}
````

## Integration points

- Outbox/Inbox:
  - Add event handlers under Infrastructure/ReadModels/Search/EventHandlers deriving from IdempotentNotificationHandler<TEvent>. They will:
    - Load minimum upstream state (via Dapper or EF) if needed
    - Map to SearchIndexUpsert
    - Call SearchIndexMaintainer.UpsertAsync()
- AppHost:
  - Optionally expose a rebuild command/endpoint or hosted service to trigger SearchIndexMaintainer.RebuildAsync().

## Concrete file map

- Database
  - src/Infrastructure/Data/Migrations/XXXXXX_AddSearchIndexItemsReadModel.cs
- Infrastructure/Search
  - ISearchIndexItemRepository.cs
  - SearchIndexItemRepository.cs
  - SearchIndexUpsert.cs
  - EventHandlers/
    - RestaurantCreatedSearchHandler.cs
    - RestaurantBusinessHoursChangedSearchHandler.cs
    - RestaurantAcceptingOrdersSearchHandler.cs
    - RestaurantNotAcceptingOrdersSearchHandler.cs
    - MenuItemCreatedSearchHandler.cs
    - MenuItemUpdatedSearchHandler.cs
    - MenuItemDeletedSearchHandler.cs
  - ReadModels/Search/SearchIndexRebuilder.cs
- Application/Search/Queries
  - UniversalSearch/
    - UniversalSearchQuery.cs
    - UniversalSearchQueryHandler.cs
    - UniversalSearchQueryValidator.cs
    - SearchResultDto.cs
  - Autocomplete/
    - AutocompleteQuery.cs
    - AutocompleteQueryHandler.cs
    - AutocompleteQueryValidator.cs
- Web/Endpoints
  - Search.cs

## Implementation details (selected)

- UniversalSearch SQL (PostGIS-based)

  - WHERE base:
    - soft_deleted = FALSE
    - (q is null OR ts_all @@ websearch_to_tsquery('simple', @q) OR name ILIKE '%' || @q || '%')
    - (openNow is null OR (is_open_now AND is_accepting_orders))
    - (cuisines is null OR cuisine = ANY(@cuisines))

  - SELECT computed:
    - text_score = ts_rank_cd(ts_all, websearch_to_tsquery('simple', @q))
    - open_boost = CASE WHEN is_open_now AND is_accepting_orders THEN 1 ELSE 0 END
    - user_geo = CASE WHEN @lat IS NOT NULL AND @lon IS NOT NULL THEN ST_SetSRID(ST_MakePoint(@lon, @lat), 4326)::geography ELSE NULL END
    - distance_km = CASE WHEN user_geo IS NOT NULL AND geo IS NOT NULL THEN ST_Distance(geo, user_geo) / 1000.0 ELSE NULL END
    - distance_score = COALESCE(1.0 / (1.0 + distance_km), 0.5)
    - score = 0.6*text_score + 0.2*distance_score + 0.2*open_boost

  - ORDER BY score DESC, updated_at DESC

- Upsert SQL:

````csharp path=src/Infrastructure/Search/SearchIndexItemRepository.cs mode=EXCERPT
const string sql = """
INSERT INTO search_index_items (id,type,restaurant_id,name,description,cuisine,tags,keywords,
  is_open_now,is_accepting_orders,avg_rating,review_count,price_band,geo,
  created_at,updated_at,source_version,soft_deleted)
VALUES (@Id,@Type,@RestaurantId,@Name,@Description,@Cuisine,@Tags,@Keywords,
  @IsOpenNow,@IsAcceptingOrders,@AvgRating,@ReviewCount,@PriceBand,ST_GeogFromText(@WktPoint),
  @CreatedAt,@UpdatedAt,@SourceVersion,@SoftDeleted)
ON CONFLICT (id) DO UPDATE SET
  name=EXCLUDED.name, description=EXCLUDED.description, cuisine=EXCLUDED.cuisine,
  tags=EXCLUDED.tags, keywords=EXCLUDED.keywords,
  is_open_now=EXCLUDED.is_open_now, is_accepting_orders=EXCLUDED.is_accepting_orders,
  avg_rating=EXCLUDED.avg_rating, review_count=EXCLUDED.review_count, price_band=EXCLUDED.price_band,
  geo=EXCLUDED.geo,
  updated_at=EXCLUDED.updated_at, soft_deleted=EXCLUDED.soft_deleted,
  source_version=EXCLUDED.source_version
WHERE search_index_items.source_version <= EXCLUDED.source_version;
""";
````

- Idempotent handler skeleton:

````csharp path=src/Infrastructure/Search/EventHandlers/RestaurantCreatedSearchHandler.cs mode=EXCERPT
public sealed class RestaurantCreatedSearchHandler : IdempotentNotificationHandler<RestaurantCreated>
{
    private readonly ISearchIndexItemRepository _repo;
    public RestaurantCreatedSearchHandler(IUnitOfWork uow, IInboxStore inbox, ISearchIndexItemRepository repo) : base(uow, inbox) { _repo = repo; }
    protected override Task HandleCore(RestaurantCreated e, CancellationToken ct)
    {
        // Load restaurant, map to upsert DTO, call _repo.UpsertAsync(...)
        return Task.CompletedTask;
    }
}
````

## Testing

- tests/Application.FunctionalTests/Search/
  - UniversalSearchTests.cs
    - ReturnsResults_WithTerm
    - RespectsOpenNowFilter
    - PaginatesResults
    - DistanceAffectsRanking_WhenLatLonProvided
  - AutocompleteTests.cs
    - ReturnsTopSuggestions_ForPrefix
    - TrigramMatches_Misspellings

- Infrastructure Integration tests
  - tests/Infrastructure.IntegrationTests/Search/SearchIndexItemRepositoryTests.cs
    - Upsert_IsIdempotentBySourceVersion
  - tests/Infrastructure.IntegrationTests/Search/EventHandlersTests.cs
    - RestaurantCreated_IndexesRow
    - MenuItemUpdated_UpdatesRow
    - MenuItemDeleted_SoftDeletesRow

## Prioritized implementation steps (MVP first)

1) Database (EF model + migration)
- Add SearchIndexItem entity + Fluent configuration (generated tsvector columns via HasGeneratedTsVectorColumn/HasComputedColumnSql; GIN/GiST indexes; trigram operator class; modelBuilder.HasPostgresExtension("pg_trgm"), ("postgis"); configure NetTopologySuite). Then run `dotnet ef migrations add AddSearchIndexItemsReadModel` and do not edit the migration.

2) Infrastructure repo
- Implement ISearchIndexItemRepository + concrete Dapper repository with UpsertAsync/SoftDeleteAsync.

3) Application queries
- Implement UniversalSearchQuery/Handler with Dapper using ts_all FTS + DapperPagination; Validator.
- Implement AutocompleteQuery/Handler; Validator.

4) Web endpoints
- Add Search.cs endpoint group with GET /api/v1/search and /api/v1/search/autocomplete.

5) Minimal projection (one or two handlers)
- Implement RestaurantCreatedSearchHandler and MenuItemCreatedSearchHandler to populate essential fields.
- Add SoftDelete on MenuItemDeleted.

6) Tests
- Functional tests for search and autocomplete.
- Minimal integration tests for repository idempotency.

7) (Optional) Rebuilder
- Implement SearchIndexRebuilder to backfill from Restaurants + MenuItems.

## Notes on future iterations

- Tune PostGIS usage (ST_DWithin for radius filters, index maintenance, performance tuning).
- Add ratings/review_count join from RestaurantReviewSummary read model.
- Add facets, diversification, and explanations.
- Replace 'simple' config with language-aware vi_search per documentation and DBA provisioning.
- Add caching (autocomplete prefix, popular queries) and API config weights (A/B).

## Small reference: how to call Result and Dapper pagination in handlers

````csharp path=src/Application/Search/Queries/UniversalSearch/UniversalSearchQueryHandler.cs mode=EXCERPT
public async Task<Result<PaginatedList<SearchResultDto>>> Handle(UniversalSearchQuery request, CancellationToken ct)
{
    using var conn = _db.CreateConnection();
    var (countSql, pageSql) = DapperPagination.BuildPagedSql(selectCols, fromWhere, orderBy, request.PageNumber, request.PageSize);
    var page = await conn.QueryPageAsync<SearchResultRow>(countSql, pageSql, parameters, request.PageNumber, request.PageSize, ct);
    var mapped = page.Items.Select(Map).ToList();
    return Result.Success(new PaginatedList<SearchResultDto>(mapped, page.TotalCount, page.PageNumber, request.PageSize));
}
````
