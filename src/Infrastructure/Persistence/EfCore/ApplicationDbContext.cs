using System.Reflection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Models;
using YummyZoom.Domain.AccountTransactionEntity;
using YummyZoom.Domain.Common.Models;
using YummyZoom.Domain.CouponAggregate;
using YummyZoom.Domain.CustomizationGroupAggregate;
using YummyZoom.Domain.MenuEntity;
using YummyZoom.Domain.MenuItemAggregate;
using YummyZoom.Domain.OrderAggregate;
using YummyZoom.Domain.PayoutAggregate;
using YummyZoom.Domain.RestaurantAccountAggregate;
using YummyZoom.Domain.RestaurantAggregate;
using YummyZoom.Domain.ReviewAggregate;
using YummyZoom.Domain.RoleAssignmentAggregate;
using YummyZoom.Domain.SupportTicketAggregate;
using YummyZoom.Domain.TagEntity;
using YummyZoom.Domain.TeamCartAggregate;
using YummyZoom.Domain.TodoListAggregate;
using YummyZoom.Domain.UserAggregate;
using YummyZoom.Infrastructure.Identity;
using YummyZoom.Infrastructure.Persistence.EfCore.Models;
using YummyZoom.Infrastructure.Persistence.ReadModels.Admin;
using YummyZoom.Infrastructure.Persistence.ReadModels.Coupons;
using YummyZoom.Infrastructure.Persistence.ReadModels.FullMenu;
using YummyZoom.Infrastructure.Persistence.ReadModels.MenuItemSales;
using YummyZoom.Infrastructure.Persistence.ReadModels.Reviews;
using YummyZoom.Infrastructure.Persistence.ReadModels.Search;
using YummyZoom.SharedKernel;
using Result = YummyZoom.SharedKernel.Result;

namespace YummyZoom.Infrastructure.Persistence.EfCore;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>, IApplicationDbContext, IUnitOfWork
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    // Infrastructure Entities
    public DbSet<TodoList> TodoLists => Set<TodoList>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<UserDeviceSession> UserDeviceSessions => Set<UserDeviceSession>();
    public DbSet<ProcessedWebhookEvent> ProcessedWebhookEvents => Set<ProcessedWebhookEvent>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    // Read Models
    public DbSet<CouponUserUsage> CouponUserUsages => Set<CouponUserUsage>();
    public DbSet<ActiveCouponView> ActiveCouponViews => Set<ActiveCouponView>();
    public DbSet<RestaurantReviewSummary> RestaurantReviewSummaries => Set<RestaurantReviewSummary>();
    public DbSet<AdminPlatformMetricsSnapshot> AdminPlatformMetricsSnapshots => Set<AdminPlatformMetricsSnapshot>();
    public DbSet<AdminDailyPerformanceSeries> AdminDailyPerformanceSeries => Set<AdminDailyPerformanceSeries>();
    public DbSet<AdminRestaurantHealthSummary> AdminRestaurantHealthSummaries => Set<AdminRestaurantHealthSummary>();
    public DbSet<FullMenuView> FullMenuViews => Set<FullMenuView>();
    public DbSet<SearchIndexItem> SearchIndexItems => Set<SearchIndexItem>();
    public DbSet<MenuItemSalesSummary> MenuItemSalesSummaries => Set<MenuItemSalesSummary>();

    // Domain Entities
    public DbSet<User> DomainUsers => Set<User>();
    public DbSet<RoleAssignment> RoleAssignments => Set<RoleAssignment>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Restaurant> Restaurants => Set<Restaurant>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<Menu> Menus => Set<Menu>();
    public DbSet<MenuCategory> MenuCategories => Set<MenuCategory>();
    public DbSet<CustomizationGroup> CustomizationGroups => Set<CustomizationGroup>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<RestaurantAccount> RestaurantAccounts => Set<RestaurantAccount>();
    public DbSet<AccountTransaction> AccountTransactions => Set<AccountTransaction>();
    public DbSet<Payout> Payouts => Set<Payout>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<TeamCart> TeamCarts => Set<TeamCart>();
    public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();
    public DbSet<YummyZoom.Domain.RestaurantRegistrationAggregate.RestaurantRegistration> RestaurantRegistrations => Set<YummyZoom.Domain.RestaurantRegistrationAggregate.RestaurantRegistration>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Enable PostGIS and trigram extensions
        builder.HasPostgresExtension("pg_trgm");
        builder.HasPostgresExtension("postgis");
        builder.HasPostgresExtension("unaccent");

        builder.Entity<ProcessedWebhookEvent>().HasKey(e => e.Id);

        // Unique phone number index for Identity users (nullable unique)
        builder.Entity<ApplicationUser>()
            .HasIndex(u => u.PhoneNumber)
            .IsUnique()
            .HasFilter("\"PhoneNumber\" IS NOT NULL");

        // Apply global query filters for soft delete
        ApplySoftDeleteQueryFilters(builder);
    }

    private static void ApplySoftDeleteQueryFilters(ModelBuilder modelBuilder)
    {
        // Apply soft delete filter to all entities that implement ISoftDeletableEntity
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;

            if (typeof(ISoftDeletableEntity).IsAssignableFrom(clrType))
            {
                var parameter = System.Linq.Expressions.Expression.Parameter(clrType, "e");
                var propertyMethodInfo = typeof(EF).GetMethod("Property")?.MakeGenericMethod(typeof(bool));
                var isDeletedProperty = System.Linq.Expressions.Expression.Call(propertyMethodInfo!, parameter, System.Linq.Expressions.Expression.Constant("IsDeleted"));
                var compareExpression = System.Linq.Expressions.Expression.MakeBinary(System.Linq.Expressions.ExpressionType.Equal, isDeletedProperty, System.Linq.Expressions.Expression.Constant(false));
                var lambda = System.Linq.Expressions.Expression.Lambda(compareExpression, parameter);

                modelBuilder.Entity(clrType).HasQueryFilter(lambda);
            }
        }
    }

    public async Task<Result<T>> ExecuteInTransactionAsync<T>(
        Func<Task<Result<T>>> work,
        CancellationToken ct = default)
    {
        var strategy = Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            // Start EF transaction
            await using var tx = await Database.BeginTransactionAsync(ct);

            // Do the work
            var result = await work();

            if (result.IsFailure)
            {
                await tx.RollbackAsync(ct);
                return Result.Failure<T>(result.Error);
            }

            // Save & commit
            await SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return result;
        });
    }

    public async Task<Result> ExecuteInTransactionAsync(
        Func<Task<Result>> work,
        CancellationToken ct = default)
    {
        var strategy = Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            // Start EF transaction
            await using var tx = await Database.BeginTransactionAsync(ct);

            // Do the work
            var result = await work();

            if (result.IsFailure)
            {
                await tx.RollbackAsync(ct);
                return Result.Failure(result.Error);
            }

            // Save & commit
            await SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return result;
        });
    }
}
