using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Payouts.Commands.CompletePayout;
using YummyZoom.Application.Payouts.Commands.FailPayout;
using YummyZoom.SharedKernel;
using MediatR;

namespace YummyZoom.Infrastructure.Payments.Mock;

public sealed class MockPayoutProvider : IPayoutProvider
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly MockPayoutProviderOptions _options;
    private readonly ILogger<MockPayoutProvider> _logger;

    public MockPayoutProvider(
        IServiceScopeFactory scopeFactory,
        IOptions<MockPayoutProviderOptions> options,
        ILogger<MockPayoutProvider> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<Result<PayoutProviderResult>> RequestPayoutAsync(
        PayoutProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return Task.FromResult(Result.Failure<PayoutProviderResult>(
                Error.Problem("MockPayoutProvider.Disabled", "Mock payout provider is disabled.")));
        }

        var providerReferenceId = $"mock_{request.PayoutId:N}";

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, _options.ProcessingDelaySeconds)), CancellationToken.None);

                using var scope = _scopeFactory.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                if (_options.ForceFailure)
                {
                    await mediator.Send(new FailPayoutCommand(request.PayoutId, _options.FailureReason));
                    _logger.LogInformation("Mock payout failed for {PayoutId}", request.PayoutId);
                }
                else
                {
                    await mediator.Send(new CompletePayoutCommand(request.PayoutId, providerReferenceId));
                    _logger.LogInformation("Mock payout completed for {PayoutId}", request.PayoutId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Mock payout processing failed for {PayoutId}", request.PayoutId);
            }
        }, CancellationToken.None);

        var result = new PayoutProviderResult(true, providerReferenceId);
        return Task.FromResult(Result.Success(result));
    }
}
