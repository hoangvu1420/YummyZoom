look at the Order seeding process. ok, now some orders are seeded with no issue, some get the issue:

info: YummyZoom.Infrastructure.Messaging.Outbox.OutboxProcessor[0]
      Outbox: published event YummyZoom.Domain.OrderAggregate.Events.OrderPlaced, YummyZoom.Domain, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null OutboxId=09dabe19-c126-42f6-8585-8ff50a22390f
info: YummyZoom.Application.Orders.EventHandlers.OrderDeliveredEventHandler[0]
      Handling OrderDelivered (EventId=79b6606b-a6b3-4529-8a86-7e72e9f79618, OrderId=ad5784e1-416f-4025-b4ed-551896d0f678)
fail: YummyZoom.Application.Orders.EventHandlers.OrderDeliveredEventHandler[0]
      Failed to record revenue for order ad5784e1-416f-4025-b4ed-551896d0f678
      System.InvalidOperationException: Cannot add money with different currencies.
         at YummyZoom.Domain.Common.ValueObjects.Money.op_Addition(Money a, Money b) in E:\source\repos\CA\YummyZoom\src\Domain\Common\ValueObjects\Money.cs:line 28
         at YummyZoom.Domain.RestaurantAccountAggregate.RestaurantAccount.RecordRevenue(Money amount, OrderId orderId) in E:\source\repos\CA\YummyZoom\src\Domain\RestaurantAccountAggregate\RestaurantAccount.cs:line 58
         at YummyZoom.Application.Orders.EventHandlers.OrderDeliveredEventHandler.RecordRevenueAsync(Order order, CancellationToken ct) in E:\source\repos\CA\YummyZoom\src\Application\Orders\EventHandlers\OrderDeliveredEventHandler.cs:line 83
fail: YummyZoom.Infrastructure.Messaging.Outbox.OutboxProcessor[0]
      Outbox: failed to publish event YummyZoom.Domain.OrderAggregate.Events.OrderDelivered, YummyZoom.Domain, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null OutboxId=0b20adde-2926-494e-b91e-75504487a173
      System.InvalidOperationException: Cannot add money with different currencies.
         at YummyZoom.Domain.Common.ValueObjects.Money.op_Addition(Money a, Money b) in E:\source\repos\CA\YummyZoom\src\Domain\Common\ValueObjects\Money.cs:line 28
         at YummyZoom.Domain.RestaurantAccountAggregate.RestaurantAccount.RecordRevenue(Money amount, OrderId orderId) in E:\source\repos\CA\YummyZoom\src\Domain\RestaurantAccountAggregate\RestaurantAccount.cs:line 58
         at YummyZoom.Application.Orders.EventHandlers.OrderDeliveredEventHandler.RecordRevenueAsync(Order order, CancellationToken ct) in E:\source\repos\CA\YummyZoom\src\Application\Orders\EventHandlers\OrderDeliveredEventHandler.cs:line 83
         at YummyZoom.Application.Orders.EventHandlers.OrderDeliveredEventHandler.HandleCore(OrderDelivered notification, CancellationToken ct) in E:\source\repos\CA\YummyZoom\src\Application\Orders\EventHandlers\OrderDeliveredEventHandler.cs:line 54
         at YummyZoom.Application.Common.Notifications.IdempotentNotificationHandler`1.<>c__DisplayClass4_0.<<Handle>b__0>d.MoveNext() in E:\source\repos\CA\YummyZoom\src\Application\Common\Notifications\IdempotentNotificationHandler.cs:line 37
      --- End of stack trace from previous location ---
         at YummyZoom.Infrastructure.Persistence.EfCore.ApplicationDbContext.<>c__DisplayClass64_0.<<ExecuteInTransactionAsync>b__0>d.MoveNext() in E:\source\repos\CA\YummyZoom\src\Infrastructure\Persistence\EfCore\ApplicationDbContext.cs:line 154
      --- End of stack trace from previous location ---
         at YummyZoom.Infrastructure.Persistence.EfCore.ApplicationDbContext.<>c__DisplayClass64_0.<<ExecuteInTransactionAsync>b__0>d.MoveNext() in E:\source\repos\CA\YummyZoom\src\Infrastructure\Persistence\EfCore\ApplicationDbContext.cs:line 165
      --- End of stack trace from previous location ---
         at Microsoft.EntityFrameworkCore.Storage.ExecutionStrategy.ExecuteAsync[TState,TResult](TState state, Func`4 operation, Func`4 verifySucceeded, CancellationToken cancellationToken)
         at YummyZoom.Infrastructure.Persistence.EfCore.ApplicationDbContext.ExecuteInTransactionAsync(Func`1 work, CancellationToken ct) in E:\source\repos\CA\YummyZoom\src\Infrastructure\Persistence\EfCore\ApplicationDbContext.cs:line 148
         at YummyZoom.Application.Common.Notifications.IdempotentNotificationHandler`1.Handle(TEvent notification, CancellationToken ct) in E:\source\repos\CA\YummyZoom\src\Application\Common\Notifications\IdempotentNotificationHandler.cs:line 28
         at MediatR.NotificationPublishers.ForeachAwaitPublisher.Publish(IEnumerable`1 handlerExecutors, INotification notification, CancellationToken cancellationToken)
         at YummyZoom.Infrastructure.Messaging.Outbox.OutboxProcessor.<>c__DisplayClass5_0.<<ProcessOnceAsync>b__0>d.MoveNext() in E:\source\repos\CA\YummyZoom\src\Infrastructure\Messaging\Outbox\OutboxProcessor.cs:line 67

analyze the order related logic in domain, application, event handlers,... in the project. identify and fix the issue. make sure everythings are consistent.
