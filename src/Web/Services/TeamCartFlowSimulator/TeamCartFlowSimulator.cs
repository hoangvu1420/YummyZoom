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

    private sealed record Run(
        Guid RunId,
        Guid TeamCartId,
        string Scenario,
        SimulationMode Mode,
        SimulationState CurrentState,
        DateTime StartedAtUtc,
        Guid HostUserId,
        List<Guid> MemberUserIds,
        SimulationDelays Delays,
        CancellationTokenSource? CancellationTokenSource = null);
    
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
        Console.WriteLine($"[TeamCartSimulator] SimulateFullFlowAsync called - Mode: {request.Mode}, Scenario: {request.Scenario}, Host: {hostPhone}, Members: [{string.Join(", ", memberPhones)}]");
        
        // Determine mode (default to Automatic for backward compatibility)
        var mode = request.Mode;
        Console.WriteLine($"[TeamCartSimulator] Simulation mode: {mode}");
        
        // Lookup host user
        Console.WriteLine($"[TeamCartSimulator] Looking up host user with phone: {hostPhone}");
        var hostUserId = await GetUserIdByPhoneAsync(hostPhone, ct);
        if (hostUserId == null)
        {
            Console.WriteLine($"[TeamCartSimulator] ERROR: Host user with phone '{hostPhone}' not found.");
            throw new InvalidOperationException($"Host user with phone '{hostPhone}' not found.");
        }
        Console.WriteLine($"[TeamCartSimulator] Host user found: {hostUserId.Value}");

        // Lookup member users
        Console.WriteLine($"[TeamCartSimulator] Looking up {memberPhones.Length} member user(s)");
        var memberUserIds = new List<Guid>();
        foreach (var phone in memberPhones)
        {
            var userId = await GetUserIdByPhoneAsync(phone, ct);
            if (userId == null)
            {
                Console.WriteLine($"[TeamCartSimulator] ERROR: Member user with phone '{phone}' not found.");
                throw new InvalidOperationException($"Member user with phone '{phone}' not found.");
            }
            memberUserIds.Add(userId.Value);
            Console.WriteLine($"[TeamCartSimulator] Member user found: {phone} -> {userId.Value}");
        }

        // Create team cart as host
        Console.WriteLine($"[TeamCartSimulator] Creating team cart as host...");
        Guid teamCartId = Guid.Empty;
        string shareToken = string.Empty;
        
        await AsUserAsync(hostUserId.Value, async sp =>
        {
            var sender = sp.GetRequiredService<ISender>();
            
            // Use provided restaurant ID or get first available
            Guid? restaurantId = request.RestaurantId;
            if (restaurantId == null)
            {
                Console.WriteLine($"[TeamCartSimulator] No restaurant ID provided, looking up first available restaurant...");
                restaurantId = await GetFirstRestaurantIdAsync(ct);
                if (restaurantId == null)
                {
                    Console.WriteLine($"[TeamCartSimulator] ERROR: No restaurants available for simulation.");
                    throw new InvalidOperationException("No restaurants available for simulation.");
                }
                Console.WriteLine($"[TeamCartSimulator] Using restaurant: {restaurantId.Value}");
            }
            else
            {
                Console.WriteLine($"[TeamCartSimulator] Using provided restaurant: {restaurantId.Value}");
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
            Console.WriteLine($"[TeamCartSimulator] ✅ Team cart created successfully! ID: {teamCartId}, ShareToken: {shareToken}");
        }, ct);

        // Check for existing simulation
        Console.WriteLine($"[TeamCartSimulator] Checking for existing simulation on team cart {teamCartId}...");
        if (Runs.ContainsKey(teamCartId))
        {
            Console.WriteLine($"[TeamCartSimulator] ERROR: A simulation is already running for team cart {teamCartId}.");
            throw new InvalidOperationException("A simulation is already running for this team cart.");
        }

        // Build delays object for the simulation
        var delays = BuildDelaysObject(request);
        Console.WriteLine($"[TeamCartSimulator] Delays configured - MemberJoinDelay: {delays.MemberJoinDelayMs}ms, ItemAdditionDelay: {delays.ItemAdditionDelayMs}ms, MemberPaymentDelay: {delays.MemberPaymentDelayMs}ms");

        // Determine initial state based on mode
        var initialState = mode == SimulationMode.Manual 
            ? SimulationState.WaitingForMembersJoin 
            : SimulationState.Initialized;
        
        var runId = Guid.NewGuid();
        var startedAtUtc = DateTime.UtcNow;
        
        // Create cancellation token source for manual mode (can be used to cancel later)
        var cts = mode == SimulationMode.Manual ? new CancellationTokenSource() : null;
        
        var run = new Run(
            runId,
            teamCartId,
            request.Scenario,
            mode,
            initialState,
            startedAtUtc,
            hostUserId.Value,
            memberUserIds,
            delays,
            cts);
        
        if (!Runs.TryAdd(teamCartId, run))
        {
            Console.WriteLine($"[TeamCartSimulator] ERROR: Failed to register simulation for team cart {teamCartId}.");
            throw new InvalidOperationException("A simulation is already running for this team cart.");
        }

        Console.WriteLine($"[TeamCartSimulator] ✅ Simulation registered - RunId: {runId}, Mode: {mode}, InitialState: {initialState}");

        // Start background execution only in automatic mode
        if (mode == SimulationMode.Automatic)
        {
            Console.WriteLine($"[TeamCartSimulator] Starting automatic execution in background...");
            _ = Task.Run(() => ExecuteFullFlowAsync(run, hostUserId.Value, memberUserIds, shareToken, request, ct), ct);
        }
        else
        {
            Console.WriteLine($"[TeamCartSimulator] Manual mode - simulation initialized and waiting for step commands. Use manual control endpoints to proceed.");
        }

        var status = mode == SimulationMode.Automatic ? "Started" : "Initialized";
        var nextStep = mode == SimulationMode.Automatic ? "MembersJoining" : "WaitingForMembersJoin";
        
        Console.WriteLine($"[TeamCartSimulator] Simulation setup complete - Status: {status}, NextStep: {nextStep}");

        return new SimulationStartResult
        {
            RunId = runId,
            TeamCartId = teamCartId,
            ShareToken = shareToken,
            Scenario = request.Scenario,
            Mode = mode,
            Status = status,
            StartedAtUtc = startedAtUtc,
            NextStep = nextStep,
            CurrentStep = initialState.ToString(),
            SimulatedMembers = memberPhones.ToList()
        };
    }

    public async Task<SimulationStartResult> SimulateMemberActionsAsync(
        Guid teamCartId,
        string[] memberPhones,
        SimulationRequest request,
        CancellationToken ct = default)
    {
        Console.WriteLine($"[TeamCartSimulator] SimulateMemberActionsAsync called - TeamCartId: {teamCartId}, Members: [{string.Join(", ", memberPhones)}], Scenario: {request.Scenario}");
        
        // Verify team cart exists and is in Open state
        Console.WriteLine($"[TeamCartSimulator] Verifying team cart state...");
        var cartState = await GetTeamCartStateAsync(teamCartId, ct);
        if (cartState == null)
        {
            Console.WriteLine($"[TeamCartSimulator] ERROR: TeamCart '{teamCartId}' not found.");
            throw new InvalidOperationException($"TeamCart '{teamCartId}' not found.");
        }

        Console.WriteLine($"[TeamCartSimulator] Team cart found - Status: {cartState.Status}, RestaurantId: {cartState.RestaurantId}");
        
        if (cartState.Status != "Open")
        {
            Console.WriteLine($"[TeamCartSimulator] ERROR: TeamCart must be in 'Open' state. Current state: '{cartState.Status}'");
            throw new InvalidOperationException($"TeamCart must be in 'Open' state. Current state: '{cartState.Status}'");
        }

        // Lookup member users
        Console.WriteLine($"[TeamCartSimulator] Looking up {memberPhones.Length} member user(s)...");
        var memberUserIds = new List<Guid>();
        foreach (var phone in memberPhones)
        {
            var userId = await GetUserIdByPhoneAsync(phone, ct);
            if (userId == null)
            {
                Console.WriteLine($"[TeamCartSimulator] ERROR: Member user with phone '{phone}' not found.");
                throw new InvalidOperationException($"Member user with phone '{phone}' not found.");
            }
            memberUserIds.Add(userId.Value);
            Console.WriteLine($"[TeamCartSimulator] Member user found: {phone} -> {userId.Value}");
        }

        // Check for existing simulation
        Console.WriteLine($"[TeamCartSimulator] Checking for existing simulation on team cart {teamCartId}...");
        if (Runs.ContainsKey(teamCartId))
        {
            Console.WriteLine($"[TeamCartSimulator] ERROR: A simulation is already running for team cart {teamCartId}.");
            throw new InvalidOperationException("A simulation is already running for this team cart.");
        }

        // Build delays object
        var delays = BuildDelaysObject(request);
        Console.WriteLine($"[TeamCartSimulator] Delays configured - MemberJoinDelay: {delays.MemberJoinDelayMs}ms, ItemAdditionDelay: {delays.ItemAdditionDelayMs}ms, MemberPaymentDelay: {delays.MemberPaymentDelayMs}ms");
        
        // Get host user ID from cart (for Run record, even though not used in member-only simulation)
        var hostUserId = await GetTeamCartHostUserIdAsync(teamCartId, ct);
        if (hostUserId == null)
        {
            Console.WriteLine($"[TeamCartSimulator] WARNING: Could not find host user for cart, using Guid.Empty as placeholder");
            hostUserId = Guid.Empty;
        }
        else
        {
            Console.WriteLine($"[TeamCartSimulator] Host user ID retrieved: {hostUserId.Value}");
        }

        var runId = Guid.NewGuid();
        var startedAtUtc = DateTime.UtcNow;
        
        var run = new Run(
            runId,
            teamCartId,
            request.Scenario,
            SimulationMode.Automatic, // Member actions always run automatically
            SimulationState.Initialized,
            startedAtUtc,
            hostUserId.Value,
            memberUserIds,
            delays,
            null);
        
        if (!Runs.TryAdd(teamCartId, run))
        {
            Console.WriteLine($"[TeamCartSimulator] ERROR: Failed to register simulation for team cart {teamCartId}.");
            throw new InvalidOperationException("A simulation is already running for this team cart.");
        }

        Console.WriteLine($"[TeamCartSimulator] ✅ Simulation registered - RunId: {runId}");

        // Start background execution for member actions only
        Console.WriteLine($"[TeamCartSimulator] Starting member actions execution in background...");
        _ = Task.Run(() => ExecuteMemberActionsAsync(run, memberUserIds, cartState.ShareToken, cartState.RestaurantId, request, ct), ct);

        Console.WriteLine($"[TeamCartSimulator] Member actions simulation started successfully");

        return new SimulationStartResult
        {
            RunId = runId,
            TeamCartId = teamCartId,
            ShareToken = cartState.ShareToken,
            Scenario = request.Scenario,
            Mode = SimulationMode.Automatic,
            Status = "Started",
            StartedAtUtc = startedAtUtc,
            NextStep = "MembersJoining",
            CurrentStep = SimulationState.Initialized.ToString(),
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
            var delays = BuildLegacyDelays(request);
            var restaurantId = await GetTeamCartRestaurantIdAsync(run.TeamCartId, ct);

            if (restaurantId == null)
            {
                Console.WriteLine($"[TeamCartSimulator] ERROR: Could not get restaurant ID for TeamCart {run.TeamCartId}");
                return;
            }

            // Step 1: Members join
            Console.WriteLine($"[TeamCartSimulator] Step 1: Waiting {delays.HostCreateToGuestJoin.TotalMilliseconds}ms before members join...");
            await DelayAsync(delays.HostCreateToGuestJoin, ct);
            Console.WriteLine($"[TeamCartSimulator] Step 1: Starting members join - {memberUserIds.Count} member(s) to join");
            foreach (var memberId in memberUserIds)
            {
                Console.WriteLine($"[TeamCartSimulator] Member {memberId.ToString()[..8]} joining team cart...");
                await AsUserAsync(memberId, async sp =>
                {
                    var sender = sp.GetRequiredService<ISender>();
                    await sender.Send(new JoinTeamCartCommand(
                        run.TeamCartId,
                        shareToken,
                        $"Member-{memberId.ToString()[..8]}"), ct);
                }, ct);
                Console.WriteLine($"[TeamCartSimulator] ✅ Member {memberId.ToString()[..8]} joined successfully");
            }
            Console.WriteLine($"[TeamCartSimulator] ✅ Step 1 complete: All members have joined");

            // Step 2: All members (including host) add items
            Console.WriteLine($"[TeamCartSimulator] Step 2: Waiting {delays.GuestJoinToAddItems.TotalMilliseconds}ms before adding items...");
            await DelayAsync(delays.GuestJoinToAddItems, ct);
            var allMembers = new List<Guid> { hostUserId };
            allMembers.AddRange(memberUserIds);
            Console.WriteLine($"[TeamCartSimulator] Step 2: Starting item addition - {allMembers.Count} member(s) will add items");

            foreach (var memberId in allMembers)
            {
                Console.WriteLine($"[TeamCartSimulator] Member {memberId.ToString()[..8]} adding items...");
                await AddRandomItemsForMemberAsync(memberId, run.TeamCartId, restaurantId.Value, ct);
                Console.WriteLine($"[TeamCartSimulator] ✅ Member {memberId.ToString()[..8]} finished adding items");
            }
            Console.WriteLine($"[TeamCartSimulator] ✅ Step 2 complete: All members have added items");

            // Step 3: All members mark ready
            Console.WriteLine($"[TeamCartSimulator] Step 3: Waiting {delays.AddItemsToMemberReady.TotalMilliseconds}ms before marking ready...");
            await DelayAsync(delays.AddItemsToMemberReady, ct);
            Console.WriteLine($"[TeamCartSimulator] Step 3: All members marking ready - {allMembers.Count} member(s)");
            foreach (var memberId in allMembers)
            {
                Console.WriteLine($"[TeamCartSimulator] Member {memberId.ToString()[..8]} marking as ready...");
                await AsTeamCartMemberAsync(memberId, run.TeamCartId, async sp =>
                {
                    var sender = sp.GetRequiredService<ISender>();
                    await sender.Send(new SetMemberReadyCommand(run.TeamCartId, true), ct);
                }, ct);
                Console.WriteLine($"[TeamCartSimulator] ✅ Member {memberId.ToString()[..8]} marked as ready");
            }
            Console.WriteLine($"[TeamCartSimulator] ✅ Step 3 complete: All members are ready");

            // Step 4: Host locks cart
            Console.WriteLine($"[TeamCartSimulator] Step 4: Waiting {delays.AllReadyToLock.TotalMilliseconds}ms before host locks cart...");
            await DelayAsync(delays.AllReadyToLock, ct);
            Console.WriteLine($"[TeamCartSimulator] Step 4: Host {hostUserId.ToString()[..8]} locking cart for payment...");
            long quoteVersion = 0;
            await AsTeamCartHostAsync(hostUserId, run.TeamCartId, async sp =>
            {
                var sender = sp.GetRequiredService<ISender>();
                var lockResult = await sender.Send(new LockTeamCartForPaymentCommand(run.TeamCartId), ct);
                if (lockResult.IsSuccess)
                {
                    quoteVersion = lockResult.Value.QuoteVersion;
                    Console.WriteLine($"[TeamCartSimulator] ✅ Step 4 complete: Cart locked successfully - QuoteVersion: {quoteVersion}");
                }
                else
                {
                    Console.WriteLine($"[TeamCartSimulator] ERROR: Failed to lock cart: {lockResult.Error.Description}");
                }
            }, ct);

            // Step 5: Members commit COD payments
            Console.WriteLine($"[TeamCartSimulator] Step 5: Waiting {delays.LockToMemberPayment.TotalMilliseconds}ms before members commit payments...");
            await DelayAsync(delays.LockToMemberPayment, ct);
            Console.WriteLine($"[TeamCartSimulator] Step 5: All members committing COD payments - {allMembers.Count} member(s), QuoteVersion: {quoteVersion}");
            foreach (var memberId in allMembers)
            {
                Console.WriteLine($"[TeamCartSimulator] Member {memberId.ToString()[..8]} committing COD payment...");
                await AsTeamCartMemberAsync(memberId, run.TeamCartId, async sp =>
                {
                    var sender = sp.GetRequiredService<ISender>();
                    var paymentResult = await sender.Send(new CommitToCodPaymentCommand(run.TeamCartId, quoteVersion), ct);
                    if (paymentResult.IsFailure)
                    {
                        Console.WriteLine($"[TeamCartSimulator] ERROR: Member {memberId.ToString()[..8]} payment failed: {paymentResult.Error.Description}");
                    }
                    else
                    {
                        Console.WriteLine($"[TeamCartSimulator] ✅ Member {memberId.ToString()[..8]} payment committed successfully");
                    }
                }, ct);
            }
            Console.WriteLine($"[TeamCartSimulator] ✅ Step 5 complete: All members have committed payments");

            // Step 6: Host converts to order
            Console.WriteLine($"[TeamCartSimulator] Step 6: Waiting {delays.PaymentToConvert.TotalMilliseconds}ms before converting to order...");
            await DelayAsync(delays.PaymentToConvert, ct);
            Console.WriteLine($"[TeamCartSimulator] Step 6: Host {hostUserId.ToString()[..8]} converting cart to order...");
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
                    Console.WriteLine($"[TeamCartSimulator] ✅ Step 6 complete: Cart converted to order successfully! Order ID: {convertResult.Value}");
                    Console.WriteLine($"[TeamCartSimulator] ✅✅✅ Full flow completed successfully! Order ID: {convertResult.Value}");
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
            Console.WriteLine($"[TeamCartSimulator] Starting member actions execution for TeamCart {run.TeamCartId}");
            var delays = BuildLegacyDelays(request);

            // Step 1: Members join
            Console.WriteLine($"[TeamCartSimulator] Step 1: Waiting {delays.HostCreateToGuestJoin.TotalMilliseconds}ms before members join...");
            await DelayAsync(delays.HostCreateToGuestJoin, ct);
            Console.WriteLine($"[TeamCartSimulator] Step 1: Starting members join - {memberUserIds.Count} member(s) to join");
            foreach (var memberId in memberUserIds)
            {
                Console.WriteLine($"[TeamCartSimulator] Member {memberId.ToString()[..8]} joining team cart...");
                await AsUserAsync(memberId, async sp =>
                {
                    var sender = sp.GetRequiredService<ISender>();
                    await sender.Send(new JoinTeamCartCommand(
                        run.TeamCartId,
                        shareToken,
                        $"Member-{memberId.ToString()[..8]}"), ct);
                }, ct);
                Console.WriteLine($"[TeamCartSimulator] ✅ Member {memberId.ToString()[..8]} joined successfully");
            }
            Console.WriteLine($"[TeamCartSimulator] ✅ Step 1 complete: All members have joined");

            // Step 2: Members add items
            Console.WriteLine($"[TeamCartSimulator] Step 2: Waiting {delays.GuestJoinToAddItems.TotalMilliseconds}ms before adding items...");
            await DelayAsync(delays.GuestJoinToAddItems, ct);
            Console.WriteLine($"[TeamCartSimulator] Step 2: Starting item addition - {memberUserIds.Count} member(s) will add items");
            foreach (var memberId in memberUserIds)
            {
                Console.WriteLine($"[TeamCartSimulator] Member {memberId.ToString()[..8]} adding items...");
                await AddRandomItemsForMemberAsync(memberId, run.TeamCartId, restaurantId, ct);
                Console.WriteLine($"[TeamCartSimulator] ✅ Member {memberId.ToString()[..8]} finished adding items");
            }
            Console.WriteLine($"[TeamCartSimulator] ✅ Step 2 complete: All members have added items");

            // Step 3: Members mark ready
            Console.WriteLine($"[TeamCartSimulator] Step 3: Waiting {delays.AddItemsToMemberReady.TotalMilliseconds}ms before marking ready...");
            await DelayAsync(delays.AddItemsToMemberReady, ct);
            Console.WriteLine($"[TeamCartSimulator] Step 3: All members marking ready - {memberUserIds.Count} member(s)");
            foreach (var memberId in memberUserIds)
            {
                Console.WriteLine($"[TeamCartSimulator] Member {memberId.ToString()[..8]} marking as ready...");
                await AsTeamCartMemberAsync(memberId, run.TeamCartId, async sp =>
                {
                    var sender = sp.GetRequiredService<ISender>();
                    await sender.Send(new SetMemberReadyCommand(run.TeamCartId, true), ct);
                }, ct);
                Console.WriteLine($"[TeamCartSimulator] ✅ Member {memberId.ToString()[..8]} marked as ready");
            }
            Console.WriteLine($"[TeamCartSimulator] ✅ Step 3 complete: All members are ready");

            // Step 4: Members commit COD payments (wait for lock first)
            Console.WriteLine($"[TeamCartSimulator] Step 4: Waiting {delays.LockToMemberPayment.TotalMilliseconds}ms before members commit payments...");
            await DelayAsync(delays.LockToMemberPayment, ct);
            Console.WriteLine($"[TeamCartSimulator] Step 4: All members committing COD payments - {memberUserIds.Count} member(s)");
            foreach (var memberId in memberUserIds)
            {
                Console.WriteLine($"[TeamCartSimulator] Member {memberId.ToString()[..8]} committing COD payment...");
                await AsTeamCartMemberAsync(memberId, run.TeamCartId, async sp =>
                {
                    var sender = sp.GetRequiredService<ISender>();
                    var paymentResult = await sender.Send(new CommitToCodPaymentCommand(run.TeamCartId, null), ct);
                    if (paymentResult.IsFailure)
                    {
                        Console.WriteLine($"[TeamCartSimulator] ERROR: Member {memberId.ToString()[..8]} payment failed: {paymentResult.Error.Description}");
                    }
                    else
                    {
                        Console.WriteLine($"[TeamCartSimulator] ✅ Member {memberId.ToString()[..8]} payment committed successfully");
                    }
                }, ct);
            }
            Console.WriteLine($"[TeamCartSimulator] ✅ Step 4 complete: All members have committed payments");
            Console.WriteLine($"[TeamCartSimulator] ✅✅✅ Member actions simulation completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TeamCartSimulator] ERROR: Exception in ExecuteMemberActionsAsync: {ex.Message}");
            Console.WriteLine($"[TeamCartSimulator] Exception details: {ex}");
        }
        finally
        {
            Console.WriteLine($"[TeamCartSimulator] Cleaning up simulation for TeamCart {run.TeamCartId}");
            Runs.TryRemove(run.TeamCartId, out _);
        }
    }

    private async Task AddRandomItemsForMemberAsync(Guid userId, Guid teamCartId, Guid restaurantId, CancellationToken ct)
    {
        Console.WriteLine($"[TeamCartSimulator] Getting available menu items for restaurant {restaurantId}...");
        var menuItems = await GetAvailableMenuItemsAsync(restaurantId, ct);
        if (menuItems.Count == 0)
        {
            Console.WriteLine($"[TeamCartSimulator] WARNING: No menu items found for restaurant {restaurantId}");
            return;
        }
        Console.WriteLine($"[TeamCartSimulator] Found {menuItems.Count} available menu item(s)");

        // Add 1-2 random items, with retry on failure (e.g., required customizations)
        var random = new Random();
        var itemCount = random.Next(1, 3);
        Console.WriteLine($"[TeamCartSimulator] Member {userId.ToString()[..8]} will add {itemCount} random item(s)");
        var addedCount = 0;
        var attemptCount = 0;
        var maxAttempts = Math.Min(menuItems.Count, 10); // Try up to 10 different items
        
        while (addedCount < itemCount && attemptCount < maxAttempts)
        {
            attemptCount++;
            var menuItemId = menuItems[random.Next(menuItems.Count)];
            var quantity = random.Next(1, 3);
            
            Console.WriteLine($"[TeamCartSimulator] Attempt {attemptCount}/{maxAttempts}: Adding menu item {menuItemId} with quantity {quantity}...");
            
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
                        Console.WriteLine($"[TeamCartSimulator] ✅ Successfully added menu item {menuItemId} (quantity: {quantity}) - {addedCount}/{itemCount} items added");
                    }
                    else
                    {
                        Console.WriteLine($"[TeamCartSimulator] Failed to add menu item {menuItemId}: {result.Error.Description}");
                    }
                }, ct);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TeamCartSimulator] Exception adding menu item {menuItemId}: {ex.Message}");
            }
        }
        
        if (addedCount < itemCount)
        {
            Console.WriteLine($"[TeamCartSimulator] WARNING: Only {addedCount}/{itemCount} items were added for member {userId.ToString()[..8]}");
        }
        else
        {
            Console.WriteLine($"[TeamCartSimulator] ✅ Member {userId.ToString()[..8]} successfully added {addedCount} item(s)");
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

    private static SimulationDelays BuildDelaysObject(SimulationRequest request)
    {
        var custom = request.DelaysMs ?? new SimulationDelays();
        var isFast = string.Equals(request.Scenario, "fastHappyPath", StringComparison.OrdinalIgnoreCase);

        int Default(int? customMs, int defaultMs, int fastMs)
            => customMs ?? (isFast ? fastMs : defaultMs);

        return new SimulationDelays
        {
            HostCreateToGuestJoinMs = custom.HostCreateToGuestJoinMs,
            GuestJoinToAddItemsMs = custom.GuestJoinToAddItemsMs,
            AddItemsToMemberReadyMs = custom.AddItemsToMemberReadyMs,
            AllReadyToLockMs = custom.AllReadyToLockMs,
            LockToMemberPaymentMs = custom.LockToMemberPaymentMs,
            PaymentToConvertMs = custom.PaymentToConvertMs,
            
            // Manual mode delays with defaults
            MemberJoinDelayMs = Default(custom.MemberJoinDelayMs, 1000, 300),
            ItemAdditionDelayMs = Default(custom.ItemAdditionDelayMs, 1500, 500),
            MemberPaymentDelayMs = Default(custom.MemberPaymentDelayMs, 2000, 500)
        };
    }

    private static (
        TimeSpan HostCreateToGuestJoin,
        TimeSpan GuestJoinToAddItems,
        TimeSpan AddItemsToMemberReady,
        TimeSpan AllReadyToLock,
        TimeSpan LockToMemberPayment,
        TimeSpan PaymentToConvert
    ) BuildLegacyDelays(SimulationRequest request)
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
            SELECT "Id", "Status", "ShareToken_Value" AS "ShareToken", "RestaurantId"
            FROM "TeamCarts"
            WHERE "Id" = @TeamCartId
            """;
        return await conn.QuerySingleOrDefaultAsync<TeamCartState>(
            new CommandDefinition(sql, new { TeamCartId = teamCartId }, cancellationToken: ct));
    }

    private async Task<Guid?> GetTeamCartHostUserIdAsync(Guid teamCartId, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();
        using var conn = dbFactory.CreateConnection();
        const string sql = "SELECT \"HostUserId\" FROM \"TeamCarts\" WHERE \"Id\" = @TeamCartId LIMIT 1";
        return await conn.QuerySingleOrDefaultAsync<Guid?>(
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

    #region Manual Control Methods - State Validation & Helpers

    private Run? GetRun(Guid teamCartId)
    {
        return Runs.TryGetValue(teamCartId, out var run) ? run : null;
    }

    private bool TryUpdateRunState(Guid teamCartId, SimulationState newState, Run? expectedRun = null)
    {
        if (expectedRun != null)
        {
            var updated = expectedRun with { CurrentState = newState };
            return Runs.TryUpdate(teamCartId, updated, expectedRun);
        }
        else
        {
            if (Runs.TryGetValue(teamCartId, out var run))
            {
                var updated = run with { CurrentState = newState };
                return Runs.TryUpdate(teamCartId, updated, run);
            }
            return false;
        }
    }

    private void ValidateStateTransition(Run run, SimulationState expectedState, string actionName)
    {
        // Check if simulation is in a "in progress" state
        var inProgressStates = new[]
        {
            SimulationState.MembersJoining,
            SimulationState.AddingItems,
            SimulationState.ProcessingPayments
        };

        if (inProgressStates.Contains(run.CurrentState))
        {
            throw new InvalidOperationException(
                $"Cannot trigger {actionName}. Phase is already in progress. Current state: '{run.CurrentState}'.");
        }

        // Check if simulation is completed
        if (run.CurrentState == SimulationState.Completed)
        {
            throw new InvalidOperationException(
                $"Cannot trigger {actionName}. Simulation is already completed.");
        }

        // Check if simulation is in expected state
        if (run.CurrentState != expectedState)
        {
            throw new InvalidOperationException(
                $"Cannot trigger {actionName}. Current state: '{run.CurrentState}'. Expected state: '{expectedState}'.");
        }

        // Check if simulation is in manual mode
        if (run.Mode != SimulationMode.Manual)
        {
            throw new InvalidOperationException(
                $"Cannot trigger {actionName}. Simulation is not in manual mode. Current mode: '{run.Mode}'.");
        }
    }

    #endregion

    #region Manual Control Methods - Implementation

    public async Task<SimulationActionResult> TriggerMembersJoinAsync(
        Guid teamCartId,
        int? delayBetweenMembersMs = null,
        CancellationToken ct = default)
    {
        Console.WriteLine($"[TeamCartSimulator] TriggerMembersJoinAsync called for TeamCart {teamCartId}");
        
        var run = GetRun(teamCartId);
        if (run == null)
        {
            Console.WriteLine($"[TeamCartSimulator] ERROR: No active simulation found for team cart {teamCartId}");
            throw new InvalidOperationException($"No active simulation found for team cart '{teamCartId}'.");
        }

        ValidateStateTransition(run, SimulationState.WaitingForMembersJoin, "members-join");

        // Get share token
        var cartState = await GetTeamCartStateAsync(teamCartId, ct);
        if (cartState == null)
        {
            throw new InvalidOperationException($"TeamCart '{teamCartId}' not found.");
        }

        // Update state to "in progress"
        if (!TryUpdateRunState(teamCartId, SimulationState.MembersJoining, run))
        {
            throw new InvalidOperationException("Failed to update simulation state. Please try again.");
        }

        // Use override delay or default from run
        var delay = delayBetweenMembersMs.HasValue
            ? TimeSpan.FromMilliseconds(delayBetweenMembersMs.Value)
            : TimeSpan.FromMilliseconds(run.Delays.MemberJoinDelayMs ?? 1000);

        Console.WriteLine($"[TeamCartSimulator] Starting members join - {run.MemberUserIds.Count} member(s), delay: {delay.TotalMilliseconds}ms");

        // Start background task
        _ = Task.Run(async () =>
        {
            try
            {
                await ExecuteMembersJoinAsync(run, cartState.ShareToken, delay, ct);
                
                // Update state to "MembersJoined" when complete
                if (Runs.TryGetValue(teamCartId, out var updatedRun))
                {
                    TryUpdateRunState(teamCartId, SimulationState.MembersJoined, updatedRun);
                    Console.WriteLine($"[TeamCartSimulator] ✅ Members join completed - state updated to MembersJoined");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TeamCartSimulator] ERROR: Exception in ExecuteMembersJoinAsync: {ex.Message}");
                if (Runs.TryGetValue(teamCartId, out var failedRun))
                {
                    TryUpdateRunState(teamCartId, SimulationState.Failed, failedRun);
                }
            }
        }, ct);

        var estimatedCompletion = DateTime.UtcNow.AddMilliseconds(run.MemberUserIds.Count * delay.TotalMilliseconds);

        return new SimulationActionResult
        {
            TeamCartId = teamCartId,
            Status = "MembersJoining",
            CurrentStep = SimulationState.MembersJoining.ToString(),
            ActionPerformedAtUtc = DateTime.UtcNow,
            Members = run.MemberUserIds.Select(id => id.ToString()).ToList(),
            EstimatedCompletionTimeUtc = estimatedCompletion
        };
    }

    private async Task ExecuteMembersJoinAsync(
        Run run,
        string shareToken,
        TimeSpan delayBetweenMembers,
        CancellationToken ct)
    {
        foreach (var memberId in run.MemberUserIds)
        {
            Console.WriteLine($"[TeamCartSimulator] Member {memberId.ToString()[..8]} joining team cart...");
            await AsUserAsync(memberId, async sp =>
            {
                var sender = sp.GetRequiredService<ISender>();
                await sender.Send(new JoinTeamCartCommand(
                    run.TeamCartId,
                    shareToken,
                    $"Member-{memberId.ToString()[..8]}"), ct);
            }, ct);
            Console.WriteLine($"[TeamCartSimulator] ✅ Member {memberId.ToString()[..8]} joined successfully");
            
            if (memberId != run.MemberUserIds.Last())
            {
                await DelayAsync(delayBetweenMembers, ct);
            }
        }
    }

    public async Task<SimulationActionResult> TriggerStartAddingItemsAsync(
        Guid teamCartId,
        int? delayBetweenItemsMs = null,
        CancellationToken ct = default)
    {
        Console.WriteLine($"[TeamCartSimulator] TriggerStartAddingItemsAsync called for TeamCart {teamCartId}");
        
        var run = GetRun(teamCartId);
        if (run == null)
        {
            Console.WriteLine($"[TeamCartSimulator] ERROR: No active simulation found for team cart {teamCartId}");
            throw new InvalidOperationException($"No active simulation found for team cart '{teamCartId}'.");
        }

        ValidateStateTransition(run, SimulationState.MembersJoined, "start-adding-items");

        var restaurantId = await GetTeamCartRestaurantIdAsync(teamCartId, ct);
        if (restaurantId == null)
        {
            throw new InvalidOperationException($"Could not get restaurant ID for TeamCart {teamCartId}");
        }

        // Update state to "in progress"
        if (!TryUpdateRunState(teamCartId, SimulationState.AddingItems, run))
        {
            throw new InvalidOperationException("Failed to update simulation state. Please try again.");
        }

        // Use override delay or default from run
        var delay = delayBetweenItemsMs.HasValue
            ? TimeSpan.FromMilliseconds(delayBetweenItemsMs.Value)
            : TimeSpan.FromMilliseconds(run.Delays.ItemAdditionDelayMs ?? 1500);

        var allMembers = new List<Guid> { run.HostUserId };
        allMembers.AddRange(run.MemberUserIds);

        Console.WriteLine($"[TeamCartSimulator] Starting item addition - {allMembers.Count} member(s), delay: {delay.TotalMilliseconds}ms");

        // Start background task
        _ = Task.Run(async () =>
        {
            try
            {
                await ExecuteAddItemsAsync(run, allMembers, restaurantId.Value, delay, ct);
                
                // Update state to "ItemsAdded" when complete
                if (Runs.TryGetValue(teamCartId, out var updatedRun))
                {
                    TryUpdateRunState(teamCartId, SimulationState.ItemsAdded, updatedRun);
                    Console.WriteLine($"[TeamCartSimulator] ✅ Item addition completed - state updated to ItemsAdded");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TeamCartSimulator] ERROR: Exception in ExecuteAddItemsAsync: {ex.Message}");
                if (Runs.TryGetValue(teamCartId, out var failedRun))
                {
                    TryUpdateRunState(teamCartId, SimulationState.Failed, failedRun);
                }
            }
        }, ct);

        return new SimulationActionResult
        {
            TeamCartId = teamCartId,
            Status = "AddingItems",
            CurrentStep = SimulationState.AddingItems.ToString(),
            ActionPerformedAtUtc = DateTime.UtcNow,
            Members = allMembers.Select(id => id.ToString()).ToList()
        };
    }

    private async Task ExecuteAddItemsAsync(
        Run run,
        List<Guid> allMembers,
        Guid restaurantId,
        TimeSpan delayBetweenItems,
        CancellationToken ct)
    {
        foreach (var memberId in allMembers)
        {
            Console.WriteLine($"[TeamCartSimulator] Member {memberId.ToString()[..8]} adding items...");
            await AddRandomItemsForMemberAsync(memberId, run.TeamCartId, restaurantId, ct);
            
            // Add delay between members (but not after the last one)
            if (memberId != allMembers.Last())
            {
                await DelayAsync(delayBetweenItems, ct);
            }
        }
    }

    public async Task<SimulationActionResult> TriggerMarkReadyAsync(
        Guid teamCartId,
        CancellationToken ct = default)
    {
        Console.WriteLine($"[TeamCartSimulator] TriggerMarkReadyAsync called for TeamCart {teamCartId}");
        
        var run = GetRun(teamCartId);
        if (run == null)
        {
            Console.WriteLine($"[TeamCartSimulator] ERROR: No active simulation found for team cart {teamCartId}");
            throw new InvalidOperationException($"No active simulation found for team cart '{teamCartId}'.");
        }

        ValidateStateTransition(run, SimulationState.ItemsAdded, "mark-ready");

        var allMembers = new List<Guid> { run.HostUserId };
        allMembers.AddRange(run.MemberUserIds);

        Console.WriteLine($"[TeamCartSimulator] Marking all members ready - {allMembers.Count} member(s)");

        foreach (var memberId in allMembers)
        {
            Console.WriteLine($"[TeamCartSimulator] Member {memberId.ToString()[..8]} marking as ready...");
            await AsTeamCartMemberAsync(memberId, teamCartId, async sp =>
            {
                var sender = sp.GetRequiredService<ISender>();
                await sender.Send(new SetMemberReadyCommand(teamCartId, true), ct);
            }, ct);
            Console.WriteLine($"[TeamCartSimulator] ✅ Member {memberId.ToString()[..8]} marked as ready");
        }

        // Update state
        if (!TryUpdateRunState(teamCartId, SimulationState.AllMembersReady, run))
        {
            throw new InvalidOperationException("Failed to update simulation state. Please try again.");
        }

        Console.WriteLine($"[TeamCartSimulator] ✅ All members marked ready - state updated to AllMembersReady");

        return new SimulationActionResult
        {
            TeamCartId = teamCartId,
            Status = "MembersReady",
            CurrentStep = SimulationState.AllMembersReady.ToString(),
            ActionPerformedAtUtc = DateTime.UtcNow,
            Members = allMembers.Select(id => id.ToString()).ToList()
        };
    }

    public async Task<SimulationActionResult> TriggerLockAsync(
        Guid teamCartId,
        CancellationToken ct = default)
    {
        Console.WriteLine($"[TeamCartSimulator] TriggerLockAsync called for TeamCart {teamCartId}");
        
        var run = GetRun(teamCartId);
        if (run == null)
        {
            Console.WriteLine($"[TeamCartSimulator] ERROR: No active simulation found for team cart {teamCartId}");
            throw new InvalidOperationException($"No active simulation found for team cart '{teamCartId}'.");
        }

        ValidateStateTransition(run, SimulationState.AllMembersReady, "lock");

        Console.WriteLine($"[TeamCartSimulator] Host {run.HostUserId.ToString()[..8]} locking cart for payment...");

        long quoteVersion = 0;

        await AsTeamCartHostAsync(run.HostUserId, teamCartId, async sp =>
        {
            var sender = sp.GetRequiredService<ISender>();
            var lockResult = await sender.Send(new LockTeamCartForPaymentCommand(teamCartId), ct);
            if (lockResult.IsSuccess)
            {
                quoteVersion = lockResult.Value.QuoteVersion;
                Console.WriteLine($"[TeamCartSimulator] ✅ Cart locked successfully - QuoteVersion: {quoteVersion}");
            }
            else
            {
                Console.WriteLine($"[TeamCartSimulator] ERROR: Failed to lock cart: {lockResult.Error.Description}");
                throw new InvalidOperationException($"Failed to lock cart: {lockResult.Error.Description}");
            }
        }, ct);

        // Update state
        if (!TryUpdateRunState(teamCartId, SimulationState.Locked, run))
        {
            throw new InvalidOperationException("Failed to update simulation state. Please try again.");
        }

        Console.WriteLine($"[TeamCartSimulator] ✅ State updated to Locked");

        return new SimulationActionResult
        {
            TeamCartId = teamCartId,
            Status = "Locked",
            CurrentStep = SimulationState.Locked.ToString(),
            ActionPerformedAtUtc = DateTime.UtcNow,
            QuoteVersion = quoteVersion
        };
    }

    public async Task<SimulationActionResult> TriggerStartPaymentsAsync(
        Guid teamCartId,
        int? delayBetweenPaymentsMs = null,
        CancellationToken ct = default)
    {
        Console.WriteLine($"[TeamCartSimulator] TriggerStartPaymentsAsync called for TeamCart {teamCartId}");
        
        var run = GetRun(teamCartId);
        if (run == null)
        {
            Console.WriteLine($"[TeamCartSimulator] ERROR: No active simulation found for team cart {teamCartId}");
            throw new InvalidOperationException($"No active simulation found for team cart '{teamCartId}'.");
        }

        ValidateStateTransition(run, SimulationState.Locked, "start-payments");

        // Get quote version from cart
        var quoteVersion = await GetTeamCartQuoteVersionAsync(teamCartId, ct);
        if (quoteVersion == null)
        {
            throw new InvalidOperationException($"Could not get quote version for TeamCart {teamCartId}. Cart may not be locked.");
        }

        // Update state to "in progress"
        if (!TryUpdateRunState(teamCartId, SimulationState.ProcessingPayments, run))
        {
            throw new InvalidOperationException("Failed to update simulation state. Please try again.");
        }

        // Use override delay or default from run
        var delay = delayBetweenPaymentsMs.HasValue
            ? TimeSpan.FromMilliseconds(delayBetweenPaymentsMs.Value)
            : TimeSpan.FromMilliseconds(run.Delays.MemberPaymentDelayMs ?? 2000);

        var allMembers = new List<Guid> { run.HostUserId };
        allMembers.AddRange(run.MemberUserIds);

        Console.WriteLine($"[TeamCartSimulator] Starting payments - {allMembers.Count} member(s), QuoteVersion: {quoteVersion}, delay: {delay.TotalMilliseconds}ms");

        // Start background task
        _ = Task.Run(async () =>
        {
            try
            {
                await ExecutePaymentsAsync(run, allMembers, quoteVersion.Value, delay, ct);
                
                // Update state to "AllPaymentsCommitted" when complete
                if (Runs.TryGetValue(teamCartId, out var updatedRun))
                {
                    TryUpdateRunState(teamCartId, SimulationState.AllPaymentsCommitted, updatedRun);
                    Console.WriteLine($"[TeamCartSimulator] ✅ Payments completed - state updated to AllPaymentsCommitted");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TeamCartSimulator] ERROR: Exception in ExecutePaymentsAsync: {ex.Message}");
                if (Runs.TryGetValue(teamCartId, out var failedRun))
                {
                    TryUpdateRunState(teamCartId, SimulationState.Failed, failedRun);
                }
            }
        }, ct);

        var estimatedCompletion = DateTime.UtcNow.AddMilliseconds(allMembers.Count * delay.TotalMilliseconds);

        return new SimulationActionResult
        {
            TeamCartId = teamCartId,
            Status = "ProcessingPayments",
            CurrentStep = SimulationState.ProcessingPayments.ToString(),
            ActionPerformedAtUtc = DateTime.UtcNow,
            Members = allMembers.Select(id => id.ToString()).ToList(),
            QuoteVersion = quoteVersion,
            EstimatedCompletionTimeUtc = estimatedCompletion
        };
    }

    private async Task ExecutePaymentsAsync(
        Run run,
        List<Guid> allMembers,
        long quoteVersion,
        TimeSpan delayBetweenPayments,
        CancellationToken ct)
    {
        foreach (var memberId in allMembers)
        {
            Console.WriteLine($"[TeamCartSimulator] Member {memberId.ToString()[..8]} committing COD payment...");
            await AsTeamCartMemberAsync(memberId, run.TeamCartId, async sp =>
            {
                var sender = sp.GetRequiredService<ISender>();
                var paymentResult = await sender.Send(new CommitToCodPaymentCommand(run.TeamCartId, quoteVersion), ct);
                if (paymentResult.IsFailure)
                {
                    Console.WriteLine($"[TeamCartSimulator] ERROR: Member {memberId.ToString()[..8]} payment failed: {paymentResult.Error.Description}");
                }
                else
                {
                    Console.WriteLine($"[TeamCartSimulator] ✅ Member {memberId.ToString()[..8]} payment committed successfully");
                }
            }, ct);
            
            if (memberId != allMembers.Last())
            {
                await DelayAsync(delayBetweenPayments, ct);
            }
        }
    }

    public async Task<SimulationActionResult> TriggerConvertAsync(
        Guid teamCartId,
        DeliveryAddress? address = null,
        string? deliveryNotes = null,
        CancellationToken ct = default)
    {
        Console.WriteLine($"[TeamCartSimulator] TriggerConvertAsync called for TeamCart {teamCartId}");
        
        var run = GetRun(teamCartId);
        if (run == null)
        {
            Console.WriteLine($"[TeamCartSimulator] ERROR: No active simulation found for team cart {teamCartId}");
            throw new InvalidOperationException($"No active simulation found for team cart '{teamCartId}'.");
        }

        ValidateStateTransition(run, SimulationState.AllPaymentsCommitted, "convert");

        // Get quote version
        var quoteVersion = await GetTeamCartQuoteVersionAsync(teamCartId, ct);
        if (quoteVersion == null)
        {
            throw new InvalidOperationException($"Could not get quote version for TeamCart {teamCartId}.");
        }

        // Use provided address or defaults
        var deliveryAddress = address ?? new DeliveryAddress
        {
            Street = "123 Test Street",
            City = "Test City",
            State = "CA",
            PostalCode = "12345",
            Country = "USA"
        };

        var notes = deliveryNotes ?? "Simulated order";

        Console.WriteLine($"[TeamCartSimulator] Host {run.HostUserId.ToString()[..8]} converting cart to order...");

        Guid? orderId = null;

        await AsTeamCartHostAsync(run.HostUserId, teamCartId, async sp =>
        {
            var sender = sp.GetRequiredService<ISender>();
            var convertResult = await sender.Send(new ConvertTeamCartToOrderCommand(
                teamCartId,
                deliveryAddress.Street,
                deliveryAddress.City,
                deliveryAddress.State,
                deliveryAddress.PostalCode,
                deliveryAddress.Country,
                notes,
                null,
                quoteVersion.Value), ct);
            
            if (convertResult.IsSuccess)
            {
                orderId = convertResult.Value.OrderId;
                Console.WriteLine($"[TeamCartSimulator] ✅ Cart converted to order successfully! Order ID: {orderId}");
            }
            else
            {
                Console.WriteLine($"[TeamCartSimulator] ERROR: Failed to convert to order: {convertResult.Error.Description}");
                throw new InvalidOperationException($"Failed to convert to order: {convertResult.Error.Description}");
            }
        }, ct);

        // Update state to completed
        if (!TryUpdateRunState(teamCartId, SimulationState.Completed, run))
        {
            throw new InvalidOperationException("Failed to update simulation state. Please try again.");
        }

        // Clean up simulation state
        Runs.TryRemove(teamCartId, out _);
        Console.WriteLine($"[TeamCartSimulator] ✅✅✅ Simulation completed and cleaned up! Order ID: {orderId}");

        return new SimulationActionResult
        {
            TeamCartId = teamCartId,
            Status = "Completed",
            CurrentStep = SimulationState.Completed.ToString(),
            ActionPerformedAtUtc = DateTime.UtcNow,
            OrderId = orderId
        };
    }

    private async Task<long?> GetTeamCartQuoteVersionAsync(Guid teamCartId, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();
        using var conn = dbFactory.CreateConnection();
        const string sql = "SELECT \"QuoteVersion\" FROM \"TeamCarts\" WHERE \"Id\" = @TeamCartId LIMIT 1";
        return await conn.QuerySingleOrDefaultAsync<long?>(
            new CommandDefinition(sql, new { TeamCartId = teamCartId }, cancellationToken: ct));
    }

    #endregion
}
