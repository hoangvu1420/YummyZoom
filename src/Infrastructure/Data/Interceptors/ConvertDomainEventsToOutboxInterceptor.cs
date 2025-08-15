using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using YummyZoom.Domain.Common.Models;
using YummyZoom.Infrastructure.Data.Models;
using YummyZoom.Infrastructure.Serialization;

namespace YummyZoom.Infrastructure.Data.Interceptors;

public sealed class ConvertDomainEventsToOutboxInterceptor : SaveChangesInterceptor
{
	private static readonly JsonSerializerOptions JsonOptions = OutboxJson.Options;

	public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
	{
		EnqueueDomainEvents(eventData.Context);
		return base.SavingChanges(eventData, result);
	}

	public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
	{
		EnqueueDomainEvents(eventData.Context);
		return base.SavingChangesAsync(eventData, result, cancellationToken);
	}

	private static void EnqueueDomainEvents(DbContext? context)
	{
		if (context is null) return;

		var entities = context.ChangeTracker
			.Entries<IHasDomainEvent>()
			.Where(e => e.Entity.DomainEvents.Any())
			.Select(e => e.Entity)
			.ToList();

		if (entities.Count == 0) return;

		var now = DateTime.UtcNow;

		var outboxMessages = entities
			.SelectMany(e => e.DomainEvents.Select(domainEvent =>
				OutboxMessage.FromDomainEvent(
					domainEvent.GetType().AssemblyQualifiedName!,
					JsonSerializer.Serialize(domainEvent, JsonOptions),
					nowUtc: now,
					correlationId: null, // TODO: temporary
					causationId: null, // TODO: temporary
					aggregateId: null, // TODO: wire up aggregate id string provider later
					aggregateType: e.GetType().Name)))
			.ToList();

		context.Set<OutboxMessage>().AddRange(outboxMessages);

		entities.ForEach(e => e.ClearDomainEvents());
	}
}


