using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Domain.TodoListAggregate.Entities;
using YummyZoom.Infrastructure.Data;
using YummyZoom.Application.TodoLists.Commands.CreateTodoList;
using YummyZoom.Application.TodoItems.Commands.CreateTodoItem;
using YummyZoom.Application.TodoItems.EventHandlers;
using YummyZoom.Application.TodoItems.Commands.UpdateTodoItem;
using YummyZoom.Domain.TodoListAggregate;
using YummyZoom.Domain.TodoListAggregate.ValueObjects;
using YummyZoom.Infrastructure.Data.Models;

namespace YummyZoom.Application.FunctionalTests.OutboxInbox;

public class OutboxFlowTests : BaseTestFixture
{
	[Test]
	public async Task CreateTodoItem_Should_EnqueueToOutbox_Then_Process_ToInbox_And_MarkOutboxProcessed()
	{
		// Arrange: create a list
		var listId = await Testing.SendAndUnwrapAsync(new CreateTodoListCommand { Title = "test" });

		// Act: create an item (emits TodoItemCreatedEvent)
		var createItemResult = await Testing.SendAsync(new CreateTodoItemCommand
		{
			ListId = listId,
			Title = "Item 1",
			Note = "n",
			Priority = Domain.TodoListAggregate.Enums.PriorityLevel.Medium
		});

		await Testing.DrainOutboxAsync();

		// Assert: item exists
		using var scope = Testing.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		var list = await Testing.FindAsync<TodoList>(TodoListId.Create(listId));
		var item = list?.Items.FirstOrDefault(i => i.Title == "Item 1");
		item.Should().NotBeNull();

		// Assert: outbox processed for created event
		var processedOutbox = await db.Set<OutboxMessage>()
			.Where(m => m.Type.Contains("TodoItemCreatedEvent"))
			.ToListAsync();
		processedOutbox.Should().NotBeEmpty();
		processedOutbox.Should().OnlyContain(m => m.ProcessedOnUtc != null && m.Error == null);

		// Assert: inbox has idempotency record for the handler
		var handlerName = typeof(TodoItemCreatedEventHandler).FullName!;
		var hasInbox = await db.Set<InboxMessage>()
			.AnyAsync(x => x.Handler == handlerName);
		hasInbox.Should().BeTrue();
	}

	[Test]
	public async Task CompleteTodoItem_Should_EnqueueToOutbox_Then_Process_ToInbox_And_MarkOutboxProcessed()
	{
		// Arrange: create a list and an item
		var listId = await Testing.SendAndUnwrapAsync(new CreateTodoListCommand { Title = "test" });
		var itemId = await Testing.SendAndUnwrapAsync(new CreateTodoItemCommand
		{
			ListId = listId,
			Title = "To Complete",
			Note = "n",
			Priority = Domain.TodoListAggregate.Enums.PriorityLevel.Medium
		});

		// Act: complete the item (emits TodoItemCompletedEvent)
		await Testing.SendAsync(new UpdateTodoItemCommand
		{
			Id = itemId,
			ListId = listId,
			IsDone = true
		});

		await Testing.DrainOutboxAsync();

		// Assert: item is completed
		using var scope = Testing.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		var list = await Testing.FindAsync<TodoList>(TodoListId.Create(listId));
		var item = list?.Items.FirstOrDefault(i => i.Id.Value == itemId);
		item.Should().NotBeNull();
		item!.IsDone.Should().BeTrue();

		// Assert: outbox processed for completed event
		var processedOutbox = await db.Set<OutboxMessage>()
			.Where(m => m.Type.Contains("TodoItemCompletedEvent"))
			.ToListAsync();
		processedOutbox.Should().NotBeEmpty();
		processedOutbox.Should().OnlyContain(m => m.ProcessedOnUtc != null && m.Error == null);

		// Assert: inbox has idempotency record for the handler
		var handlerName = typeof(TodoItemCompletedEventHandler).FullName!;
		var hasInbox = await db.Set<InboxMessage>()
			.AnyAsync(x => x.Handler == handlerName);
		hasInbox.Should().BeTrue();
	}

	[Test]
	public async Task DrainingTwice_Should_Not_Duplicate_Inbox_Entries_Or_SideEffects()
	{
		// Arrange: create a list and an item to complete
		var listId = await Testing.SendAndUnwrapAsync(new CreateTodoListCommand { Title = "test" });
		var itemId = await Testing.SendAndUnwrapAsync(new CreateTodoItemCommand
		{
			ListId = listId,
			Title = "Once Only",
			Note = "n",
			Priority = Domain.TodoListAggregate.Enums.PriorityLevel.Medium
		});

		await Testing.SendAsync(new UpdateTodoItemCommand
		{
			Id = itemId,
			ListId = listId,
			IsDone = true
		});

		// Act: drain twice
		await Testing.DrainOutboxAsync();
		await Testing.DrainOutboxAsync();

		// Assert: item is completed
		using var scope = Testing.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		var list = await Testing.FindAsync<TodoList>(TodoListId.Create(listId));
		var item = list?.Items.FirstOrDefault(i => i.Id.Value == itemId);
		item.Should().NotBeNull();
		item!.IsDone.Should().BeTrue();

		// Assert: single inbox entry for the handler+eventId
		var handlerName = typeof(TodoItemCompletedEventHandler).FullName!;
		var inboxEntries = await db.Set<InboxMessage>()
			.Where(x => x.Handler == handlerName)
			.ToListAsync();
		inboxEntries.Should().NotBeEmpty();
		inboxEntries.Select(x => new { x.Handler, x.EventId })
			.Distinct()
			.Count().Should().Be(inboxEntries.Count);

		// Assert: outbox messages for the event are processed, no errors
		var processedOutbox = await db.Set<OutboxMessage>()
			.Where(m => m.Type.Contains("TodoItemCompletedEvent"))
			.ToListAsync();
		processedOutbox.Should().NotBeEmpty();
		processedOutbox.Should().OnlyContain(m => m.ProcessedOnUtc != null && m.Error == null);
	}

	[Test]
	public async Task BeforeDrain_OutboxHasPending_And_InboxHasNoEntry()
	{
		// Arrange: create a list
		var listId = await Testing.SendAndUnwrapAsync(new CreateTodoListCommand { Title = "pre-drain" });

		// Act: create an item (emits TodoItemCreatedEvent)
		await Testing.SendAsync(new CreateTodoItemCommand
		{
			ListId = listId,
			Title = "PreDrain Item",
			Note = "n",
			Priority = Domain.TodoListAggregate.Enums.PriorityLevel.Low
		});

		// Assert pre-drain: outbox has unprocessed message; inbox has no entry yet
		using (var scope = Testing.CreateScope())
		{
			var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
			var pendingOutbox = await db.Set<OutboxMessage>()
				.Where(m => m.Type.Contains("TodoItemCreatedEvent") && m.ProcessedOnUtc == null)
				.ToListAsync();
			pendingOutbox.Should().NotBeEmpty();

			var handlerName = typeof(TodoItemCreatedEventHandler).FullName!;
			var hasInboxPre = await db.Set<InboxMessage>()
				.AnyAsync(x => x.Handler == handlerName);
			hasInboxPre.Should().BeFalse();
		}

		// Now drain and verify it flips to processed
		await Testing.DrainOutboxAsync();

		using (var scope = Testing.CreateScope())
		{
			var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
			var processedOutbox = await db.Set<OutboxMessage>()
				.Where(m => m.Type.Contains("TodoItemCreatedEvent"))
				.ToListAsync();
			processedOutbox.Should().NotBeEmpty();
			processedOutbox.Should().OnlyContain(m => m.ProcessedOnUtc != null && m.Error == null);

			var handlerName = typeof(TodoItemCreatedEventHandler).FullName!;
			var hasInboxPost = await db.Set<InboxMessage>()
				.AnyAsync(x => x.Handler == handlerName);
			hasInboxPost.Should().BeTrue();
		}
	}
}
