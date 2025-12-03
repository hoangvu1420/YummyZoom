using System.Collections.Concurrent;
using Dapper;
using MediatR;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.TeamCarts.Commands.AddItemToTeamCart;
using YummyZoom.Application.TeamCarts.Commands.CommitToCodPayment;
using YummyZoom.Application.TeamCarts.Commands.ConvertTeamCartToOrder;
using YummyZoom.Application.TeamCarts.Commands.CreateTeamCart;
using YummyZoom.Application.TeamCarts.Commands.JoinTeamCart;
using YummyZoom.Application.TeamCarts.Commands.LockTeamCartForPayment;
using YummyZoom.Application.TeamCarts.Commands.SetMemberReady;
using YummyZoom.SharedKernel.Constants;
using YummyZoom.Web.Security;
using YummyZoom.Web.Services.TeamCartFlowSimulator.Models;

namespace YummyZoom.Web.Services.TeamCartFlowSimulator;

public sealed class TeamCartFlowSimulator : ITeamCartFlowSimulator
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDevImpersonationService _impersonation;

    private sealed record Run(Guid RunId, Guid TeamCartId, string Scenario, DateTime StartedAtUtc);
    private static readonly ConcurrentDictionary<Guid, Run> Runs = new();

    public TeamCartFlowSimulator(
        IServiceScopeFactory scopeFactory,
        IDevImpersonationService impersonation)
    {
        _scopeFactory = scopeFactory;
        _impersonation = impersonation;
    }

    public async Task<SimulationStartResult> SimulateFullFlowAsync(
        string hostPhone,
        string[] memberPhones,
        SimulationRequest request,
        CancellationToken ct = default)
    {
        // Lookup host user
        var hostUserId = await GetUserIdByPhoneAsync(hostPhone, ct);
        if (hostUserId == null)
        {
            throw new InvalidOperationException($"Host user with phone '{hostPhone}' not found.");
        }

        // Lookup member users
        var memberUserIds = new List<Guid>();
        foreach (var phone in memberPhones)
        {
            var userId = await GetUserIdByPhoneAsync(phone, ct);
            if (userId == null)
            {
                throw new InvalidOperationException($"Member user with phone '{phone}' not found.");
            }
            memberUserIds.Add(userId.Value);
        }

        // Create team cart as host
        Guid teamCartId = Guid.Empty;
        string shareToken = string.Empty;
        
        await AsUserAsync(hostUserId.Value, async sp =>
        {
            var sender = sp.GetRequiredService<ISender>();
            
            // Use provided restaurant ID or get first available
            Guid? restaurantId = request.RestaurantId;
            if (restaurantId == null)
            {
                restaurantId = await GetFirstRestaurantIdAsync(ct);
                if (restaurantId == null)
                {
                    throw new InvalidOperationException("No restaurants available for simulation.");
                }
            }
            
            var createResult = await sender.Send(new CreateTeamCartCommand(
                restaurantId.Value,
                $"Host-{hostUserId.Value.ToString()[..8]}",
                null,
                null), ct);

            if (createResult.IsFailure)
            {
                Console.WriteLine($"[TeamCartSimulator] ERROR: Failed to create team cart: {createResult.Error.Description}");
                throw new InvalidOperationException($"Failed to create team cart: {createResult.Error.Description}");
            }

            teamCartId = createResult.Value.TeamCartId;
            shareToken = createResult.Value.ShareToken;
            Console.WriteLine($"[TeamCartSimulator] Team cart created successfully! ID: {teamCartId}");
        }, ct);

        // Check for existing simulation
        if (Runs.ContainsKey(teamCartId))
        {
            throw new InvalidOperationException("A simulation is already running for this team cart.");
        }

        var run = new Run(Guid.NewGuid(), teamCartId, request.Scenario, DateTime.UtcNow);
        if (!Runs.TryAdd(teamCartId, run))
        {
            throw new InvalidOperationException("A simulation is already running for this team cart.");
        }

        Console.WriteLine($"[TeamCartSimulator] Full flow simulation starting - RunId: {run.RunId}, Scenario: {request.Scenario}, Host: {hostPhone}, Members: [{string.Join(", ", memberPhones)}]");

        // Start background execution
        _ = Task.Run(() => ExecuteFullFlowAsync(run, hostUserId.Value, memberUserIds, shareToken, request, ct), ct);

        return new SimulationStartResult
        {
            RunId = run.RunId,
            TeamCartId = teamCartId,
            Scenario = request.Scenario,
            Status = "Started",
            StartedAtUtc = run.StartedAtUtc,
            NextStep = "MembersJoining",
            SimulatedMembers = memberPhones.ToList()
        };
    }

    public async Task<SimulationStartResult> SimulateMemberActionsAsync(
        Guid teamCartId,
        string[] memberPhones,
        SimulationRequest request,
        CancellationToken ct = default)
    {
        // Verify team cart exists and is in Open state
        var cartState = await GetTeamCartStateAsync(teamCartId, ct);
        if (cartState == null)
        {
            throw new InvalidOperationException($"TeamCart '{teamCartId}' not found.");
        }

        if (cartState.Status != "Open")
        {
            throw new InvalidOperationException($"TeamCart must be in 'Open' state. Current state: '{cartState.Status}'");
        }

        // Lookup member users
        var memberUserIds = new List<Guid>();
        foreach (var phone in memberPhones)
        {
            var userId = await GetUserIdByPhoneAsync(phone, ct);
            if (userId == null)
            {
                throw new InvalidOperationException($"Member user with phone '{phone}' not found.");
            }
            memberUserIds.Add(userId.Value);
        }

        // Check for existing simulation
        if (Runs.ContainsKey(teamCartId))
        {
            throw new InvalidOperationException("A simulation is already running for this team cart.");
        }

        var run = new Run(Guid.NewGuid(), teamCartId, request.Scenario, DateTime.UtcNow);
        if (!Runs.TryAdd(teamCartId, run))
        {
            throw new InvalidOperationException("A simulation is already running for this team cart.");
        }

        // Start background execution for member actions only
        _ = Task.Run(() => ExecuteMemberActionsAsync(run, memberUserIds, cartState.ShareToken, cartState.RestaurantId, request, ct), ct);

        return new SimulationStartResult
        {
            RunId = run.RunId,
            TeamCartId = teamCartId,
            Scenario = request.Scenario,
            Status = "Started",
            StartedAtUtc = run.StartedAtUtc,
            NextStep = "MembersJoining",
            SimulatedMembers = memberPhones.ToList()
        };
    }

    private async Task ExecuteFullFlowAsync(
        Run run,
        Guid hostUserId,
        List<Guid> memberUserIds,
        string shareToken,
        SimulationRequest request,
        CancellationToken ct)
    {
        try
        {
            Console.WriteLine($"[TeamCartSimulator] Starting full flow execution for TeamCart {run.TeamCartId}");
            var delays = BuildDelays(request);
            var restaurantId = await GetTeamCartRestaurantIdAsync(run.TeamCartId, ct);

            if (restaurantId == null)
            {
                Console.WriteLine($"[TeamCartSimulator] ERROR: Could not get restaurant ID for TeamCart {run.TeamCartId}");
                return;
            }

            // Step 1: Members join
            await DelayAsync(delays.HostCreateToGuestJoin, ct);
            foreach (var memberId in memberUserIds)
            {
                await AsUserAsync(memberId, async sp =>
                {
                    var sender = sp.GetRequiredService<ISender>();
                    await sender.Send(new JoinTeamCartCommand(
                        run.TeamCartId,
                        shareToken,
                        $"Member-{memberId.ToString()[..8]}"), ct);
                }, ct);
            }

            // Step 2: All members (including host) add items
            Console.WriteLine($"[TeamCartSimulator] Step 2: All members adding items");
            await DelayAsync(delays.GuestJoinToAddItems, ct);
            var allMembers = new List<Guid> { hostUserId };
            allMembers.AddRange(memberUserIds);

            foreach (var memberId in allMembers)
            {
                await AddRandomItemsForMemberAsync(memberId, run.TeamCartId, restaurantId.Value, ct);
            }

            // Step 3: All members mark ready
            Console.WriteLine($"[TeamCartSimulator] Step 3: All members marking ready");
            await DelayAsync(delays.AddItemsToMemberReady, ct);
            foreach (var memberId in allMembers)
            {
                await AsTeamCartMemberAsync(memberId, run.TeamCartId, async sp =>
                {
                    var sender = sp.GetRequiredService<ISender>();
                    await sender.Send(new SetMemberReadyCommand(run.TeamCartId, true), ct);
                }, ct);
            }

            // Step 4: Host locks cart
            Console.WriteLine($"[TeamCartSimulator] Step 4: Host locking cart for payment");
            await DelayAsync(delays.AllReadyToLock, ct);
            long quoteVersion = 0;
            await AsTeamCartHostAsync(hostUserId, run.TeamCartId, async sp =>
            {
                var sender = sp.GetRequiredService<ISender>();
                var lockResult = await sender.Send(new LockTeamCartForPaymentCommand(run.TeamCartId), ct);
                if (lockResult.IsSuccess)
                {
                    quoteVersion = lockResult.Value.QuoteVersion;
                }
                else
                {
                    Console.WriteLine($"[TeamCartSimulator] ERROR: Failed to lock cart: {lockResult.Error.Description}");
                }
            }, ct);

            // Step 5: Members commit COD payments
            Console.WriteLine($"[TeamCartSimulator] Step 5: All members committing payments");
            await DelayAsync(delays.LockToMemberPayment, ct);
            foreach (var memberId in allMembers)
            {
                await AsTeamCartMemberAsync(memberId, run.TeamCartId, async sp =>
                {
                    var sender = sp.GetRequiredService<ISender>();
                    var paymentResult = await sender.Send(new CommitToCodPaymentCommand(run.TeamCartId, quoteVersion), ct);
                    if (paymentResult.IsFailure)
                    {
                        Console.WriteLine($"[TeamCartSimulator] ERROR: Member payment failed: {paymentResult.Error.Description}");
                    }
                }, ct);
            }

            // Step 6: Host converts to order
            Console.WriteLine($"[TeamCartSimulator] Step 6: Converting cart to order");
            await DelayAsync(delays.PaymentToConvert, ct);
            await AsTeamCartHostAsync(hostUserId, run.TeamCartId, async sp =>
            {
                var sender = sp.GetRequiredService<ISender>();
                var convertResult = await sender.Send(new ConvertTeamCartToOrderCommand(
                    run.TeamCartId,
                    "123 Test Street",
                    "Test City",
                    "CA",
                    "12345",
                    "USA",
                    "Simulated order",
                    null,
                    quoteVersion), ct);
                if (convertResult.IsSuccess)
                {
                    Console.WriteLine($"[TeamCartSimulator] ✅ Full flow completed successfully! Order ID: {convertResult.Value}");
                }
                else
                {
                    Console.WriteLine($"[TeamCartSimulator] ERROR: Failed to convert to order: {convertResult.Error.Description}");
                }
            }, ct);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TeamCartSimulator] ERROR: Exception in ExecuteFullFlowAsync: {ex.Message}");
            Console.WriteLine($"[TeamCartSimulator] Exception details: {ex}");
        }
        finally
        {
            Runs.TryRemove(run.TeamCartId, out _);
        }
    }

    private async Task ExecuteMemberActionsAsync(
        Run run,
        List<Guid> memberUserIds,
        string shareToken,
        Guid restaurantId,
        SimulationRequest request,
        CancellationToken ct)
    {
        try
        {
            var delays = BuildDelays(request);

            // Step 1: Members join
            await DelayAsync(delays.HostCreateToGuestJoin, ct);
            foreach (var memberId in memberUserIds)
            {
                await AsUserAsync(memberId, async sp =>
                {
                    var sender = sp.GetRequiredService<ISender>();
                    await sender.Send(new JoinTeamCartCommand(
                        run.TeamCartId,
                        shareToken,
                        $"Member-{memberId.ToString()[..8]}"), ct);
                }, ct);
            }

            // Step 2: Members add items
            await DelayAsync(delays.GuestJoinToAddItems, ct);
            foreach (var memberId in memberUserIds)
            {
                await AddRandomItemsForMemberAsync(memberId, run.TeamCartId, restaurantId, ct);
            }

            // Step 3: Members mark ready
            await DelayAsync(delays.AddItemsToMemberReady, ct);
            foreach (var memberId in memberUserIds)
            {
                await AsTeamCartMemberAsync(memberId, run.TeamCartId, async sp =>
                {
                    var sender = sp.GetRequiredService<ISender>();
                    await sender.Send(new SetMemberReadyCommand(run.TeamCartId, true), ct);
                }, ct);
            }

            // Step 4: Members commit COD payments (wait for lock first)
            await DelayAsync(delays.LockToMemberPayment, ct);
            foreach (var memberId in memberUserIds)
            {
                await AsTeamCartMemberAsync(memberId, run.TeamCartId, async sp =>
                {
                    var sender = sp.GetRequiredService<ISender>();
                    await sender.Send(new CommitToCodPaymentCommand(run.TeamCartId, null), ct);
                }, ct);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TeamCartSimulator] ERROR: Exception in ExecuteMemberActionsAsync: {ex.Message}");
            Console.WriteLine($"[TeamCartSimulator] Exception details: {ex}");
        }
        finally
        {
            Runs.TryRemove(run.TeamCartId, out _);
        }
    }

    private async Task AddRandomItemsForMemberAsync(Guid userId, Guid teamCartId, Guid restaurantId, CancellationToken ct)
    {
        var menuItems = await GetAvailableMenuItemsAsync(restaurantId, ct);
        if (menuItems.Count == 0)
        {
            Console.WriteLine($"[TeamCartSimulator] WARNING: No menu items found for restaurant {restaurantId}");
            return;
        }

        // Add 1-2 random items, with retry on failure (e.g., required customizations)
        var random = new Random();
        var itemCount = random.Next(1, 3);
        var addedCount = 0;
        var attemptCount = 0;
        var maxAttempts = Math.Min(menuItems.Count, 10); // Try up to 10 different items
        
        while (addedCount < itemCount && attemptCount < maxAttempts)
        {
            attemptCount++;
            var menuItemId = menuItems[random.Next(menuItems.Count)];
            var quantity = random.Next(1, 3);
            
            try
            {
                await AsTeamCartMemberAsync(userId, teamCartId, async sp =>
                {
                    var sender = sp.GetRequiredService<ISender>();
                    var result = await sender.Send(new AddItemToTeamCartCommand(
                        teamCartId,
                        menuItemId,
                        quantity,
                        null,
                        null), ct);
                    
                    if (result.IsSuccess)
                    {
                        addedCount++;
                    }
                    else
                    {
                        Console.WriteLine($"[TeamCartSimulator] Failed to add menu item: {result.Error.Description}");
                    }
                }, ct);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TeamCartSimulator] Exception adding menu item: {ex.Message}");
            }
        }
    }

    private async Task AsUserAsync(Guid userId, Func<IServiceProvider, Task> action, CancellationToken ct)
        => await _impersonation.RunAsUserAsync(userId, _scopeFactory, action, null, ct);

    private async Task AsTeamCartMemberAsync(Guid userId, Guid teamCartId, Func<IServiceProvider, Task> action, CancellationToken ct)
    {
        // Add TeamCartMember permission for this user and teamCart
        var permissions = new[] { $"{Roles.TeamCartMember}:{teamCartId}" };
        await _impersonation.RunAsUserAsync(userId, _scopeFactory, action, permissions, ct);
    }

    private async Task AsTeamCartHostAsync(Guid userId, Guid teamCartId, Func<IServiceProvider, Task> action, CancellationToken ct)
    {
        // Add TeamCartHost permission for this user and teamCart
        var permissions = new[] { $"{Roles.TeamCartHost}:{teamCartId}" };
        await _impersonation.RunAsUserAsync(userId, _scopeFactory, action, permissions, ct);
    }

    private static async Task DelayAsync(TimeSpan delay, CancellationToken ct)
    {
        if (delay <= TimeSpan.Zero) return;
        await Task.Delay(delay, ct);
    }

    private static (
        TimeSpan HostCreateToGuestJoin,
        TimeSpan GuestJoinToAddItems,
        TimeSpan AddItemsToMemberReady,
        TimeSpan AllReadyToLock,
        TimeSpan LockToMemberPayment,
        TimeSpan PaymentToConvert
    ) BuildDelays(SimulationRequest request)
    {
        var custom = request.DelaysMs;
        var isFast = string.Equals(request.Scenario, "fastHappyPath", StringComparison.OrdinalIgnoreCase);

        int Default(int? customMs, int defaultMs, int fastMs)
            => customMs ?? (isFast ? fastMs : defaultMs);

        return (
            TimeSpan.FromMilliseconds(Default(custom?.HostCreateToGuestJoinMs, 2000, 500)),
            TimeSpan.FromMilliseconds(Default(custom?.GuestJoinToAddItemsMs, 2000, 500)),
            TimeSpan.FromMilliseconds(Default(custom?.AddItemsToMemberReadyMs, 3000, 500)),
            TimeSpan.FromMilliseconds(Default(custom?.AllReadyToLockMs, 1500, 300)),
            TimeSpan.FromMilliseconds(Default(custom?.LockToMemberPaymentMs, 3000, 500)),
            TimeSpan.FromMilliseconds(Default(custom?.PaymentToConvertMs, 1500, 300))
        );
    }

    private async Task<Guid?> GetUserIdByPhoneAsync(string phoneNumber, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();
        using var conn = dbFactory.CreateConnection();
        const string sql = "SELECT \"Id\" FROM \"AspNetUsers\" WHERE \"PhoneNumber\" = @PhoneNumber LIMIT 1";
        return await conn.QuerySingleOrDefaultAsync<Guid?>(
            new CommandDefinition(sql, new { PhoneNumber = phoneNumber }, cancellationToken: ct));
    }

    private async Task<Guid?> GetFirstRestaurantIdAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();
        using var conn = dbFactory.CreateConnection();
        
        // First try to get a restaurant that's accepting orders
        const string acceptingSql = "SELECT \"Id\" FROM \"Restaurants\" WHERE \"IsAcceptingOrders\" = true LIMIT 1";
        var acceptingRestaurant = await conn.QuerySingleOrDefaultAsync<Guid?>(
            new CommandDefinition(acceptingSql, cancellationToken: ct));
            
        if (acceptingRestaurant.HasValue)
        {
            return acceptingRestaurant.Value;
        }
        
        // Fallback to any restaurant
        const string anySql = "SELECT \"Id\" FROM \"Restaurants\" LIMIT 1";
        var anyRestaurant = await conn.QuerySingleOrDefaultAsync<Guid?>(
            new CommandDefinition(anySql, cancellationToken: ct));
            
        if (anyRestaurant == null)
        {
            Console.WriteLine("[TeamCartSimulator] WARNING: No restaurants found in database");
        }
        return anyRestaurant;
    }

    private async Task<Guid?> GetTeamCartRestaurantIdAsync(Guid teamCartId, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();
        using var conn = dbFactory.CreateConnection();
        const string sql = "SELECT \"RestaurantId\" FROM \"TeamCarts\" WHERE \"Id\" = @TeamCartId";
        return await conn.QuerySingleOrDefaultAsync<Guid?>(
            new CommandDefinition(sql, new { TeamCartId = teamCartId }, cancellationToken: ct));
    }

    private async Task<TeamCartState?> GetTeamCartStateAsync(Guid teamCartId, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();
        using var conn = dbFactory.CreateConnection();
        const string sql = """
            SELECT "Id", "Status", "ShareToken", "RestaurantId"
            FROM "TeamCarts"
            WHERE "Id" = @TeamCartId
            """;
        return await conn.QuerySingleOrDefaultAsync<TeamCartState>(
            new CommandDefinition(sql, new { TeamCartId = teamCartId }, cancellationToken: ct));
    }

    private async Task<List<Guid>> GetAvailableMenuItemsAsync(Guid restaurantId, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();
        using var conn = dbFactory.CreateConnection();
        
        // First check if restaurant is accepting orders
        const string restaurantSql = "SELECT \"IsAcceptingOrders\" FROM \"Restaurants\" WHERE \"Id\" = @RestaurantId";
        var isAcceptingOrders = await conn.QuerySingleOrDefaultAsync<bool?>(
            new CommandDefinition(restaurantSql, new { RestaurantId = restaurantId }, cancellationToken: ct));
        
        if (isAcceptingOrders != true)
        {
            Console.WriteLine($"[TeamCartSimulator] WARNING: Restaurant is not accepting orders!");
        }
        
        // Get all available menu items for the restaurant
        // Note: Items with required customizations may fail validation, but that's acceptable for simulation
        const string sql = """
            SELECT "Id"
            FROM "MenuItems"
            WHERE "RestaurantId" = @RestaurantId
              AND "IsAvailable" = true
              AND "DeletedOn" IS NULL
            LIMIT 50
            """;
        var items = await conn.QueryAsync<Guid>(
            new CommandDefinition(sql, new { RestaurantId = restaurantId }, cancellationToken: ct));
        return items.ToList();
    }

    private sealed record TeamCartState(Guid Id, string Status, string ShareToken, Guid RestaurantId);
}
