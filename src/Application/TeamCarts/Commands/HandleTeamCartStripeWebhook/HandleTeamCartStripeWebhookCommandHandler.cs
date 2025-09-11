using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Common.Models;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.TeamCartAggregate.Errors;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using Result = YummyZoom.SharedKernel.Result;

namespace YummyZoom.Application.TeamCarts.Commands.HandleTeamCartStripeWebhook;

public class HandleTeamCartStripeWebhookCommandHandler : IRequestHandler<HandleTeamCartStripeWebhookCommand, Result>
{
    private readonly IPaymentGatewayService _paymentGatewayService;
    private readonly ITeamCartRepository _teamCartRepository;
    private readonly IApplicationDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<HandleTeamCartStripeWebhookCommandHandler> _logger;

    public HandleTeamCartStripeWebhookCommandHandler(
        IPaymentGatewayService paymentGatewayService,
        ITeamCartRepository teamCartRepository,
        IApplicationDbContext dbContext,
        IUnitOfWork unitOfWork,
        ILogger<HandleTeamCartStripeWebhookCommandHandler> logger)
    {
        _paymentGatewayService = paymentGatewayService ?? throw new ArgumentNullException(nameof(paymentGatewayService));
        _teamCartRepository = teamCartRepository ?? throw new ArgumentNullException(nameof(teamCartRepository));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> Handle(HandleTeamCartStripeWebhookCommand request, CancellationToken cancellationToken)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            _logger.LogInformation("Processing Stripe webhook event for TeamCart");

            // 1) Verify webhook and parse
            var webhookEventResult = _paymentGatewayService.ConstructWebhookEvent(request.RawJson, request.StripeSignatureHeader);
            if (webhookEventResult.IsFailure)
            {
                _logger.LogWarning("Webhook signature verification failed: {Error}", webhookEventResult.Error.Description);
                return Result.Failure(webhookEventResult.Error);
            }

            var webhookEvent = webhookEventResult.Value;
            var metadata = webhookEvent.Metadata ?? new Dictionary<string, string>();

            metadata.TryGetValue("teamcart_id", out var teamCartIdStr);
            metadata.TryGetValue("member_user_id", out var memberUserIdStr);

            _logger.LogInformation("Webhook verified. EventId={EventId} Type={EventType} ObjectId={ObjectId} TeamCartId={TeamCartId} MemberUserId={MemberUserId}",
                webhookEvent.EventId, webhookEvent.EventType, webhookEvent.RelevantObjectId, teamCartIdStr, memberUserIdStr);

            // If no TeamCart metadata, mark processed and exit successfully (not for us)
            if (string.IsNullOrWhiteSpace(teamCartIdStr) || string.IsNullOrWhiteSpace(memberUserIdStr))
            {
                await MarkEventAsProcessed(webhookEvent.EventId, cancellationToken);
                return Result.Success();
            }

            // 2) Idempotency check
            var existingEvent = await _dbContext.ProcessedWebhookEvents
                .FirstOrDefaultAsync(e => e.Id == webhookEvent.EventId, cancellationToken);
            if (existingEvent is not null)
            {
                _logger.LogInformation("Webhook event {EventId} already processed at {ProcessedAt}", webhookEvent.EventId, existingEvent.ProcessedAt);
                return Result.Success();
            }

            // 3) Load TeamCart
            if (!Guid.TryParse(teamCartIdStr, out var teamCartGuid) || !Guid.TryParse(memberUserIdStr, out var memberUserGuid))
            {
                _logger.LogWarning("Invalid metadata IDs. teamcart_id={TeamCartId} member_user_id={MemberUserId}", teamCartIdStr, memberUserIdStr);
                await MarkEventAsProcessed(webhookEvent.EventId, cancellationToken);
                return Result.Success();
            }

            var cartId = TeamCartId.Create(teamCartGuid);
            var memberUserId = Domain.UserAggregate.ValueObjects.UserId.Create(memberUserGuid);

            var cart = await _teamCartRepository.GetByIdAsync(cartId, cancellationToken);
            if (cart is null)
            {
                _logger.LogWarning("TeamCart not found for webhook. TeamCartId={TeamCartId}", teamCartGuid);
                await MarkEventAsProcessed(webhookEvent.EventId, cancellationToken);
                return Result.Success();
            }

            // 4) Process event types
            Result processingResult = webhookEvent.EventType switch
            {
                "payment_intent.succeeded" => HandleSucceeded(cart, memberUserId, webhookEvent.RelevantObjectId),
                "payment_intent.payment_failed" => HandleFailed(cart, memberUserId, webhookEvent.RelevantObjectId),
                _ => Result.Success()
            };

            if (processingResult.IsFailure)
            {
                _logger.LogError("Failed to process TeamCart webhook event {EventId}: {Error}", webhookEvent.EventId, processingResult.Error.Description);
                return processingResult;
            }

            // Persist only when we changed state
            if (webhookEvent.EventType is "payment_intent.succeeded" or "payment_intent.payment_failed")
            {
                await _teamCartRepository.UpdateAsync(cart, cancellationToken);
            }

            await MarkEventAsProcessed(webhookEvent.EventId, cancellationToken);
            _logger.LogInformation("TeamCart webhook event {EventId} processed successfully", webhookEvent.EventId);
            return Result.Success();
        }, cancellationToken);
    }

    private Result HandleSucceeded(Domain.TeamCartAggregate.TeamCart cart, Domain.UserAggregate.ValueObjects.UserId memberUserId, string paymentGatewayReferenceId)
    {
        // Amount cross-check: recompute from items
        var currency = cart.TipAmount.Currency;
        var amount = new Money(cart.Items.Where(i => i.AddedByUserId == memberUserId).Sum(i => i.LineItemTotal.Amount), currency);

        var result = cart.RecordSuccessfulOnlinePayment(memberUserId, amount, paymentGatewayReferenceId);
        return result.IsSuccess ? Result.Success() : Result.Failure(result.Error);
    }

    private Result HandleFailed(Domain.TeamCartAggregate.TeamCart cart, Domain.UserAggregate.ValueObjects.UserId memberUserId, string paymentGatewayReferenceId)
    {
        // Recompute expected member amount to keep parity with success path
        var currency = cart.TipAmount.Currency;
        var amount = new Money(cart.Items.Where(i => i.AddedByUserId == memberUserId).Sum(i => i.LineItemTotal.Amount), currency);

        var result = cart.RecordFailedOnlinePayment(memberUserId, amount);
        return result.IsSuccess ? Result.Success() : Result.Failure(result.Error);
    }

    private async Task MarkEventAsProcessed(string eventId, CancellationToken cancellationToken)
    {
        var processedEvent = new ProcessedWebhookEvent
        {
            Id = eventId,
            ProcessedAt = DateTime.UtcNow
        };

        _dbContext.ProcessedWebhookEvents.Add(processedEvent);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

