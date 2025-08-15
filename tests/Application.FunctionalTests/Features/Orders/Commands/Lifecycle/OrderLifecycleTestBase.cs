using YummyZoom.Application.FunctionalTests.Common;

namespace YummyZoom.Application.FunctionalTests.Features.Orders.Commands.Lifecycle;

/// <summary>
/// Base class for order lifecycle command tests. Currently thin – inherits reset behavior
/// from BaseTestFixture. Hook for future shared setup (e.g., clock injection or mock services).
/// </summary>
public abstract class OrderLifecycleTestBase : BaseTestFixture
{
    // Intentionally left blank.
}
