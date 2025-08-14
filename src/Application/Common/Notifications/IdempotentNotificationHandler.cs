using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Domain.Common.Models;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Common.Notifications;

public abstract class IdempotentNotificationHandler<TEvent> : INotificationHandler<TEvent>
	where TEvent : IDomainEvent, IHasEventId
{
	private readonly IUnitOfWork _uow;
	private readonly IInboxStore _inbox;

	protected IdempotentNotificationHandler(IUnitOfWork uow, IInboxStore inbox)
	{
		_uow = uow;
		_inbox = inbox;
	}

	protected abstract Task HandleCore(TEvent notification, CancellationToken ct);

	public async Task Handle(TEvent notification, CancellationToken ct)
	{
		var handlerName = GetType().FullName!;

		var already = await _inbox.ExistsAsync(notification.EventId, handlerName, ct);
		if (already) return;

		await _uow.ExecuteInTransactionAsync(async () =>
		{
			// Re-check inside tx
			var insideAlready = await _inbox.ExistsAsync(notification.EventId, handlerName, ct);
			if (insideAlready)
			{
				return Result.Success();
			}

			await HandleCore(notification, ct);

			await _inbox.AddAsync(notification.EventId, handlerName, null, ct);
			await _uow.SaveChangesAsync(ct);
			return Result.Success();
		}, ct);
	}
}
