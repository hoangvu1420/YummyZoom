server logs:
info: YummyZoom.Application.TeamCarts.Commands.AddItemToTeamCart.AddItemToTeamCartCommand[0]
      YummyZoom Request: AddItemToTeamCartCommand 019bc08b-4428-7f2d-9e1a-3ffbf8cef68f +84901234560 AddItemToTeamCartCommand { TeamCartId = 8c66dca5-db66-415f-9895-e322f9c58f54, MenuItemId = ccb43db3-861c-4b31-8d4f-faef8a58a4bf, Quantity = 1, SelectedCustomizations = System.Collections.Generic.List`1[YummyZoom.Application.TeamCarts.Commands.AddItemToTeamCart.AddItemToTeamCartCustomizationSelection], IdempotencyKey = f78cb286-c656-4b88-9c17-5cb238c1518d }
info: YummyZoom.Application.TeamCarts.Queries.GetTeamCartRealTimeViewModel.GetTeamCartRealTimeViewModelQuery[0]
      YummyZoom Request: GetTeamCartRealTimeViewModelQuery 019bc08b-4428-7f2d-9e1a-3ffbf8cef68f +84901234560 GetTeamCartRealTimeViewModelQuery { TeamCartIdGuid = 8c66dca5-db66-415f-9895-e322f9c58f54 }
info: YummyZoom.Application.TeamCarts.Commands.AddItemToTeamCart.AddItemToTeamCartCommandHandler[0]
      TeamCart item added. CartId=8c66dca5-db66-415f-9895-e322f9c58f54 UserId=019bc08b-4428-7f2d-9e1a-3ffbf8cef68f RestaurantId=ffcb234b-eb68-4a1d-b2b5-130c51bddb94 MenuItemId=ccb43db3-861c-4b31-8d4f-faef8a58a4bf Qty=1 Customizations=1
info: YummyZoom.Infrastructure.Notifications.Firebase.FcmService[0]
      Successfully sent multicast data message to 1 tokens.
info: YummyZoom.Infrastructure.Notifications.TeamCarts.TeamCartPushNotifier[0]
      Sent TeamCart FCM data-only push to 1 tokens (CartId=8c66dca5-db66-415f-9895-e322f9c58f54, Target=Others, EventType=ItemAdded, Version=4)
info: YummyZoom.Infrastructure.Messaging.Outbox.OutboxProcessor[0]
      Outbox: processed 1 events in batch
      YummyZoom Request: GetTeamCartRealTimeViewModelQuery 019bc08b-4428-7f2d-9e1a-3ffbf8cef68f +84901234560 GetTeamCartRealTimeViewModelQuery { TeamCartIdGuid = 8c66dca5-db66-415f-9895-e322f9c58f54 }
info: YummyZoom.Application.TeamCarts.Commands.SetMemberReady.SetMemberReadyCommand[0]
      YummyZoom Request: SetMemberReadyCommand 019bc08b-4428-7f2d-9e1a-3ffbf8cef68f +84901234560 SetMemberReadyCommand { TeamCartId = 8c66dca5-db66-415f-9895-e322f9c58f54, IsReady = True }
info: YummyZoom.Application.TeamCarts.Queries.GetTeamCartRealTimeViewModel.GetTeamCartRealTimeViewModelQuery[0]
      YummyZoom Request: GetTeamCartRealTimeViewModelQuery 019bc08b-4428-7f2d-9e1a-3ffbf8cef68f +84901234560 GetTeamCartRealTimeViewModelQuery { TeamCartIdGuid = 8c66dca5-db66-415f-9895-e322f9c58f54 }
info: YummyZoom.Application.TeamCarts.Commands.SetMemberReady.SetMemberReadyCommand[0]
      YummyZoom Request: SetMemberReadyCommand 019bc08b-4695-7551-91e8-b5c5413231ba +84901234561 SetMemberReadyCommand { TeamCartId = 8c66dca5-db66-415f-9895-e322f9c58f54, IsReady = True }

      YummyZoom Request: GetTeamCartRealTimeViewModelQuery 019bc08b-4695-7551-91e8-b5c5413231ba +84901234561 GetTeamCartRealTimeViewModelQuery { TeamCartIdGuid = 8c66dca5-db66-415f-9895-e322f9c58f54 }
info: YummyZoom.Application.TeamCarts.Commands.LockTeamCartForPayment.LockTeamCartForPaymentCommand[0]
      YummyZoom Request: LockTeamCartForPaymentCommand 019bc08b-4428-7f2d-9e1a-3ffbf8cef68f +84901234560 LockTeamCartForPaymentCommand { TeamCartId = 8c66dca5-db66-415f-9895-e322f9c58f54, IdempotencyKey = 3578723e-5bac-45c1-b86c-c77f04f7f6b1 }
info: YummyZoom.Application.TeamCarts.Commands.LockTeamCartForPayment.LockTeamCartForPaymentCommandHandler[0]
      TeamCart locked for payment. CartId=8c66dca5-db66-415f-9895-e322f9c58f54 HostUserId=019bc08b-4428-7f2d-9e1a-3ffbf8cef68f Status=Locked QuoteVersion=1
info: YummyZoom.Application.TeamCarts.Queries.GetTeamCartRealTimeViewModel.GetTeamCartRealTimeViewModelQuery[0]
      YummyZoom Request: GetTeamCartRealTimeViewModelQuery 019bc08b-4428-7f2d-9e1a-3ffbf8cef68f +84901234560 GetTeamCartRealTimeViewModelQuery { TeamCartIdGuid = 8c66dca5-db66-415f-9895-e322f9c58f54 }
info: YummyZoom.Infrastructure.Notifications.Firebase.FcmService[0]
      Successfully sent multicast message to 1 tokens.
info: YummyZoom.Infrastructure.Notifications.TeamCarts.TeamCartPushNotifier[0]
      Sent TeamCart FCM hybrid push to 1 tokens (CartId=8c66dca5-db66-415f-9895-e322f9c58f54, Target=Members, EventType=TeamCartLockedForPayment, Version=8)
info: YummyZoom.Infrastructure.Messaging.Outbox.OutboxProcessor[0]
      Outbox: processed 2 events in batch
      YummyZoom Request: GetTeamCartRealTimeViewModelQuery 019bc08b-4695-7551-91e8-b5c5413231ba +84901234561 GetTeamCartRealTimeViewModelQuery { TeamCartIdGuid = 8c66dca5-db66-415f-9895-e322f9c58f54 }
info: YummyZoom.Application.TeamCarts.Commands.ApplyTipToTeamCart.ApplyTipToTeamCartCommand[0]
      YummyZoom Request: ApplyTipToTeamCartCommand 019bc08b-4428-7f2d-9e1a-3ffbf8cef68f +84901234560 ApplyTipToTeamCartCommand { TeamCartId = 8c66dca5-db66-415f-9895-e322f9c58f54, TipAmount = 10000 }
info: YummyZoom.Application.TeamCarts.Commands.ApplyTipToTeamCart.ApplyTipToTeamCartCommandHandler[0]
      Applied tip to TeamCart. CartId=8c66dca5-db66-415f-9895-e322f9c58f54 HostUserId=019bc08b-4428-7f2d-9e1a-3ffbf8cef68f Tip=10000
info: YummyZoom.Application.TeamCarts.Queries.GetTeamCartRealTimeViewModel.GetTeamCartRealTimeViewModelQuery[0]
      YummyZoom Request: GetTeamCartRealTimeViewModelQuery 019bc08b-4428-7f2d-9e1a-3ffbf8cef68f +84901234560 GetTeamCartRealTimeViewModelQuery { TeamCartIdGuid = 8c66dca5-db66-415f-9895-e322f9c58f54 }
info: YummyZoom.Infrastructure.Notifications.Firebase.FcmService[0]
      Successfully sent multicast data message to 1 tokens.
info: YummyZoom.Infrastructure.Notifications.TeamCarts.TeamCartPushNotifier[0]
      Sent TeamCart FCM data-only push to 1 tokens (CartId=8c66dca5-db66-415f-9895-e322f9c58f54, Target=Members, EventType=TipApplied, Version=10)
info: YummyZoom.Infrastructure.Messaging.Outbox.OutboxProcessor[0]
      Outbox: processed 2 events in batch
info: YummyZoom.Application.TeamCarts.Queries.GetTeamCartRealTimeViewModel.GetTeamCartRealTimeViewModelQuery[0]
      YummyZoom Request: GetTeamCartRealTimeViewModelQuery 019bc08b-4695-7551-91e8-b5c5413231ba +84901234561 GetTeamCartRealTimeViewModelQuery { TeamCartIdGuid = 8c66dca5-db66-415f-9895-e322f9c58f54 }
info: YummyZoom.Application.TeamCarts.Commands.FinalizePricing.FinalizePricingCommand[0]
      YummyZoom Request: FinalizePricingCommand 019bc08b-4428-7f2d-9e1a-3ffbf8cef68f +84901234560 FinalizePricingCommand { TeamCartId = 8c66dca5-db66-415f-9895-e322f9c58f54 }
info: YummyZoom.Application.TeamCarts.Commands.FinalizePricing.FinalizePricingCommandHandler[0]
      Pricing finalized for TeamCart. CartId=8c66dca5-db66-415f-9895-e322f9c58f54 HostUserId=019bc08b-4428-7f2d-9e1a-3ffbf8cef68f Status=Finalized
warn: YummyZoom.Application.TeamCarts.Commands.FinalizePricing.FinalizePricingCommand[0]
      YummyZoom Long Running Request: FinalizePricingCommand (947 milliseconds) 019bc08b-4428-7f2d-9e1a-3ffbf8cef68f +84901234560 FinalizePricingCommand { TeamCartId = 8c66dca5-db66-415f-9895-e322f9c58f54 }
info: YummyZoom.Infrastructure.Notifications.Firebase.FcmService[0]
      Successfully sent multicast data message to 1 tokens.
info: YummyZoom.Infrastructure.Notifications.TeamCarts.TeamCartPushNotifier[0]
      Sent TeamCart FCM data-only push to 1 tokens (CartId=8c66dca5-db66-415f-9895-e322f9c58f54, Target=All, EventType=TeamCartPricingFinalized, Version=11)
info: YummyZoom.Infrastructure.Messaging.Outbox.OutboxProcessor[0]
      Outbox: processed 1 events in batch
      YummyZoom Request: GetTeamCartRealTimeViewModelQuery 019bc08b-4428-7f2d-9e1a-3ffbf8cef68f +84901234560 GetTeamCartRealTimeViewModelQuery { TeamCartIdGuid = 8c66dca5-db66-415f-9895-e322f9c58f54 }
info: YummyZoom.Application.TeamCarts.Commands.CommitToCodPayment.CommitToCodPaymentCommand[0]
      YummyZoom Request: CommitToCodPaymentCommand 019bc08b-4428-7f2d-9e1a-3ffbf8cef68f +84901234560 CommitToCodPaymentCommand { TeamCartId = 8c66dca5-db66-415f-9895-e322f9c58f54, QuoteVersion =  }
info: YummyZoom.Application.TeamCarts.Commands.CommitToCodPayment.CommitToCodPaymentCommandHandler[0]
      Member committed to COD payment. CartId=8c66dca5-db66-415f-9895-e322f9c58f54 UserId=019bc08b-4428-7f2d-9e1a-3ffbf8cef68f Amount=147940.00
warn: YummyZoom.Application.TeamCarts.Commands.CommitToCodPayment.CommitToCodPaymentCommand[0]
      YummyZoom Long Running Request: CommitToCodPaymentCommand (683 milliseconds) 019bc08b-4428-7f2d-9e1a-3ffbf8cef68f +84901234560 CommitToCodPaymentCommand { TeamCartId = 8c66dca5-db66-415f-9895-e322f9c58f54, QuoteVersion =  }
info: YummyZoom.Infrastructure.Messaging.Outbox.OutboxProcessor[0]
      Outbox: processed 1 events in batch
      YummyZoom Request: InitiateMemberOnlinePaymentCommand 019bc08b-4695-7551-91e8-b5c5413231ba +84901234561 InitiateMemberOnlinePaymentCommand { TeamCartId = 8c66dca5-db66-415f-9895-e322f9c58f54, QuoteVersion =  }
info: YummyZoom.Infrastructure.Payments.Stripe.StripeService[0]
      Creating Stripe Payment Intent for Order ID: (null)
info: YummyZoom.Infrastructure.Payments.Stripe.StripeService[0]
      Successfully created Stripe Payment Intent ID: pi_3Spl7x30kTfMafYR0ZJluH77 for Order ID: (null)
info: YummyZoom.Application.TeamCarts.Commands.InitiateMemberOnlinePayment.InitiateMemberOnlinePaymentCommandHandler[0]
      Created member payment intent pi_3Spl7x30kTfMafYR0ZJluH77 for team cart 8c66dca5-db66-415f-9895-e322f9c58f54
warn: YummyZoom.Application.TeamCarts.Commands.InitiateMemberOnlinePayment.InitiateMemberOnlinePaymentCommand[0]
      YummyZoom Long Running Request: InitiateMemberOnlinePaymentCommand (1431 milliseconds) 019bc08b-4695-7551-91e8-b5c5413231ba +84901234561 InitiateMemberOnlinePaymentCommand { TeamCartId = 8c66dca5-db66-415f-9895-e322f9c58f54, QuoteVersion =  }
info: YummyZoom.Application.TeamCarts.Commands.HandleTeamCartStripeWebhook.HandleTeamCartStripeWebhookCommand[0]
      YummyZoom Request: HandleTeamCartStripeWebhookCommand  Unknown evt_3Spl7x30kTfMafYR00SfpNz4 payment_intent.created (payload omitted)
info: YummyZoom.Application.TeamCarts.Commands.HandleTeamCartStripeWebhook.HandleTeamCartStripeWebhookCommandHandler[0]
      Processing Stripe webhook event for TeamCart
info: YummyZoom.Application.TeamCarts.Commands.HandleTeamCartStripeWebhook.HandleTeamCartStripeWebhookCommandHandler[0]
      Webhook verified. EventId=evt_3Spl7x30kTfMafYR00SfpNz4 Type=payment_intent.created ObjectId=pi_3Spl7x30kTfMafYR0ZJluH77 TeamCartId=8c66dca5-db66-415f-9895-e322f9c58f54 MemberUserId=019bc08b-4695-7551-91e8-b5c5413231ba
info: YummyZoom.Application.TeamCarts.Commands.HandleTeamCartStripeWebhook.HandleTeamCartStripeWebhookCommandHandler[0]
      TeamCart webhook event evt_3Spl7x30kTfMafYR00SfpNz4 processed successfully
info: YummyZoom.Application.TeamCarts.Queries.GetTeamCartRealTimeViewModel.GetTeamCartRealTimeViewModelQuery[0]
      YummyZoom Request: GetTeamCartRealTimeViewModelQuery 019bc08b-4695-7551-91e8-b5c5413231ba +84901234561 GetTeamCartRealTimeViewModelQuery { TeamCartIdGuid = 8c66dca5-db66-415f-9895-e322f9c58f54 }

      YummyZoom Request: HandleTeamCartStripeWebhookCommand  Unknown evt_3Spl7x30kTfMafYR05EYik0j payment_intent.succeeded (payload omitted)
info: YummyZoom.Application.TeamCarts.Commands.HandleTeamCartStripeWebhook.HandleTeamCartStripeWebhookCommandHandler[0]
      Processing Stripe webhook event for TeamCart
info: YummyZoom.Application.TeamCarts.Commands.HandleTeamCartStripeWebhook.HandleTeamCartStripeWebhookCommandHandler[0]
      Webhook verified. EventId=evt_3Spl7x30kTfMafYR05EYik0j Type=payment_intent.succeeded ObjectId=pi_3Spl7x30kTfMafYR0ZJluH77 TeamCartId=8c66dca5-db66-415f-9895-e322f9c58f54 MemberUserId=019bc08b-4695-7551-91e8-b5c5413231ba
fail: YummyZoom.Application.TeamCarts.Commands.HandleTeamCartStripeWebhook.HandleTeamCartStripeWebhookCommandHandler[0]
      Failed to process TeamCart webhook event evt_3Spl7x30kTfMafYR05EYik0j: Payment amount is invalid or does not match member's total

client logs:
14:37:05.912  I  [38;5;8m14:37:05.911 (+0:29:42.360) 菅 [LoggingInterceptor] Request: GET https://yz.hnv1420.org/api/v1/team-carts/8c66dca5-db66-415f-9895-e322f9c58f54/rt [auth: yes][0m
14:37:06.190  I  [38;5;8m14:37:06.189 (+0:29:42.638) 菅 [LoggingInterceptor] Response: 200 GET /api/v1/team-carts/8c66dca5-db66-415f-9895-e322f9c58f54/rt[0m
14:37:06.205  I  [38;5;8m14:37:06.205 (+0:29:42.654) 菅 [TeamCartTracking] TeamCart poke: Got FRESH data on attempt 1[0m
14:37:06.890  I  [38;5;12m14:37:06.890 (+0:29:43.339) 庁 [FcmService] Local notification shown - Title: Gi盻� nhﾃｳm ﾄ妥｣ khﾃｳa, Body: Gi盻� nhﾃｳm ﾄ妥｣ ﾄ柁ｰ盻｣c khﾃｳa. Vui lﾃｲng thanh toﾃ｡n ph蘯ｧn c盻ｧa b蘯｡n 72,940 VND.[0m
14:37:07.053  I  [38;5;8m14:37:07.053 (+0:29:43.502) 菅 [GoRouterNavigationService] Replacing with route: /teamcart/8c66dca5-db66-415f-9895-e322f9c58f54/checkout[0m
14:37:07.057  I  [38;5;8m14:37:07.057 (+0:29:43.506) 菅 [AuthGuard] Auth guard check - path: /teamcart/8c66dca5-db66-415f-9895-e322f9c58f54/checkout, isLoggedIn: true, requiresAuth: false, isAuth: false[0m
14:37:07.209  I  [38;5;8m14:37:07.209 (+0:29:43.658) 菅 [TeamCartVM] TeamCartViewModel disposing[0m
14:37:07.210  I  [38;5;8m14:37:07.210 (+0:29:43.659) 菅 [TeamCartVM] TeamCart watching stopped for cartId: 8c66dca5-db66-415f-9895-e322f9c58f54[0m
14:37:07.214  I  [38;5;8m14:37:07.214 (+0:29:43.663) 菅 [TeamCartTracking] TeamCart unwatch() for 8c66dca5-db66-415f-9895-e322f9c58f54 (refs=0)[0m
14:37:07.229  I  [38;5;8m14:37:07.229 (+0:29:43.678) 菅 [TeamCartTracking] TeamCart poke: no active tracker for cartId=8c66dca5-db66-415f-9895-e322f9c58f54[0m
14:37:07.232  I  [38;5;8m14:37:07.232 (+0:29:43.681) 菅 [TeamCartTracking] TeamCart tracker disposed for cartId=8c66dca5-db66-415f-9895-e322f9c58f54[0m
14:37:07.233  I  [38;5;8m14:37:07.232 (+0:29:43.681) 菅 [TeamCartVM] TeamCart watching started for cartId: 8c66dca5-db66-415f-9895-e322f9c58f54[0m
14:37:07.233  I  [38;5;8m14:37:07.233 (+0:29:43.682) 菅 [TeamCartTracking] TeamCart watch() create tracker for 8c66dca5-db66-415f-9895-e322f9c58f54 (refs=1)[0m
14:37:07.233  I  [38;5;8m14:37:07.233 (+0:29:43.682) 菅 [TeamCartTracking] TeamCart polling tick: cartId=8c66dca5-db66-415f-9895-e322f9c58f54, etag=null[0m
14:37:07.235  I  [38;5;12m14:37:07.235 (+0:29:43.684) 庁 [TeamCartVM] Loaded team cart: 8c66dca5-db66-415f-9895-e322f9c58f54[0m
14:37:07.246  I  [38;5;8m14:37:07.246 (+0:29:43.695) 菅 [LoggingInterceptor] Request: GET https://yz.hnv1420.org/api/v1/team-carts/8c66dca5-db66-415f-9895-e322f9c58f54/rt [auth: yes][0m
14:37:07.513  I  [38;5;8m14:37:07.513 (+0:29:43.962) 菅 [LoggingInterceptor] Response: 200 GET /api/v1/team-carts/8c66dca5-db66-415f-9895-e322f9c58f54/rt[0m
14:37:09.454  I  [38;5;12m14:37:09.447 (+0:29:45.896) 庁 [FcmService] Foreground message received: 0:1768462635808000%400c55db48ed32f9[0m
14:37:09.455  I  [38;5;12m14:37:09.454 (+0:29:45.903) 庁 [FcmService] Complete Message Information: ID: 0:1768462635808000%400c55db48ed32f9, Title: null, Body: null, Data: {actorId: 019bc08b-4428-7f2d-9e1a-3ffbf8cef68f, route: /teamcart/8c66dca5-db66-415f-9895-e322f9c58f54, teamCartId: 8c66dca5-db66-415f-9895-e322f9c58f54, state: Locked, body: B蘯｡n ﾄ妥｣ thﾃｪm tip 10,000 VND, type: teamcart, event: tip_applied, title: Tip ﾄ妥｣ thﾃｪm, click_action: FLUTTER_NOTIFICATION_CLICK, version: 10}, From: 263467716980, Type: null[0m
14:37:09.455  I  [38;5;12m14:37:09.455 (+0:29:45.904) 庁 [FcmService] Route found in message (/teamcart/8c66dca5-db66-415f-9895-e322f9c58f54) but navigation skipped (foreground)[0m
14:37:09.507  I  [38;5;8m14:37:09.507 (+0:29:45.956) 菅 [LoggingInterceptor] Request: GET https://yz.hnv1420.org/api/v1/team-carts/8c66dca5-db66-415f-9895-e322f9c58f54/rt [auth: yes][0m
14:37:09.844  I  [38;5;8m14:37:09.843 (+0:29:46.292) 菅 [LoggingInterceptor] Response: 200 GET /api/v1/team-carts/8c66dca5-db66-415f-9895-e322f9c58f54/rt[0m
14:37:09.846  I  [38;5;8m14:37:09.846 (+0:29:46.295) 菅 [TeamCartTracking] TeamCart poke: Got FRESH data on attempt 1[0m
14:37:14.482  I  [38;5;12m14:37:14.482 (+0:29:50.931) 庁 [FcmService] Foreground message received: 0:1768462640867457%400c55db48ed32f9[0m
14:37:14.482  I  [38;5;12m14:37:14.482 (+0:29:50.931) 庁 [FcmService] Complete Message Information: ID: 0:1768462640867457%400c55db48ed32f9, Title: null, Body: null, Data: {actorId: 019bc08b-4428-7f2d-9e1a-3ffbf8cef68f, route: /teamcart/8c66dca5-db66-415f-9895-e322f9c58f54, teamCartId: 8c66dca5-db66-415f-9895-e322f9c58f54, state: Finalized, body: Gi盻� nhﾃｳm c盻ｧa b蘯｡n ﾄ妥｣ ﾄ柁ｰ盻｣c c蘯ｭp nh蘯ｭt., type: teamcart, event: state_changed, title: C蘯ｭp nh蘯ｭt gi盻� nhﾃｳm, click_action: FLUTTER_NOTIFICATION_CLICK, version: 11}, From: 263467716980, Type: null[0m
14:37:14.483  I  [38;5;12m14:37:14.482 (+0:29:50.931) 庁 [FcmService] Route found in message (/teamcart/8c66dca5-db66-415f-9895-e322f9c58f54) but navigation skipped (foreground)[0m
14:37:14.500  I  [38;5;8m14:37:14.500 (+0:29:50.949) 菅 [LoggingInterceptor] Request: GET https://yz.hnv1420.org/api/v1/team-carts/8c66dca5-db66-415f-9895-e322f9c58f54/rt [auth: yes][0m
14:37:14.871  I  [38;5;8m14:37:14.870 (+0:29:51.319) 菅 [LoggingInterceptor] Response: 200 GET /api/v1/team-carts/8c66dca5-db66-415f-9895-e322f9c58f54/rt[0m
14:37:14.872  I  [38;5;8m14:37:14.872 (+0:29:51.321) 菅 [TeamCartTracking] TeamCart poke: Got FRESH data on attempt 1[0m
14:37:27.137  I  [38;5;8m14:37:27.137 (+0:30:03.586) 菅 [TeamCartTracking] TeamCart polling tick: cartId=8c66dca5-db66-415f-9895-e322f9c58f54, etag=W/"teamcart-8c66dca5-db66-415f-9895-e322f9c58f54-v11"[0m
14:37:27.144  I  [38;5;8m14:37:27.144 (+0:30:03.593) 菅 [LoggingInterceptor] Request: GET https://yz.hnv1420.org/api/v1/team-carts/8c66dca5-db66-415f-9895-e322f9c58f54/rt [auth: yes][0m
14:37:27.544  I  [38;5;8m14:37:27.544 (+0:30:03.993) 菅 [LoggingInterceptor] Response: 200 GET /api/v1/team-carts/8c66dca5-db66-415f-9895-e322f9c58f54/rt[0m
14:37:39.670  I  [38;5;8m14:37:39.669 (+0:30:16.118) 菅 [TeamCartTracking] TeamCart polling tick: cartId=8c66dca5-db66-415f-9895-e322f9c58f54, etag=W/"teamcart-8c66dca5-db66-415f-9895-e322f9c58f54-v11"[0m
14:37:39.685  I  [38;5;8m14:37:39.684 (+0:30:16.133) 菅 [LoggingInterceptor] Request: GET https://yz.hnv1420.org/api/v1/team-carts/8c66dca5-db66-415f-9895-e322f9c58f54/rt [auth: yes][0m
14:37:39.981  I  [38;5;8m14:37:39.980 (+0:30:16.429) 菅 [LoggingInterceptor] Response: 200 GET /api/v1/team-carts/8c66dca5-db66-415f-9895-e322f9c58f54/rt[0m
14:37:49.167  I  [38;5;8m14:37:49.167 (+0:30:25.616) 菅 [LoggingInterceptor] Request: POST https://yz.hnv1420.org/api/v1/team-carts/8c66dca5-db66-415f-9895-e322f9c58f54/payments/online [auth: yes][0m
14:37:51.387  I  [38;5;8m14:37:51.387 (+0:30:27.836) 菅 [LoggingInterceptor] Response: 200 POST /api/v1/team-carts/8c66dca5-db66-415f-9895-e322f9c58f54/payments/online[0m
14:37:51.388  I  [38;5;12m14:37:51.388 (+0:30:27.837) 庁 [TeamCartRepository] Online payment intent created[0m
14:37:52.208  I  [38;5;8m14:37:52.208 (+0:30:28.657) 菅 [TeamCartTracking] TeamCart polling tick: cartId=8c66dca5-db66-415f-9895-e322f9c58f54, etag=W/"teamcart-8c66dca5-db66-415f-9895-e322f9c58f54-v12"[0m
14:37:52.680  I  [38;5;12m14:37:52.680 (+0:30:29.129) 庁 [StripePayments] Stripe PaymentSheet initialized[0m
14:37:52.859  I  [38;5;8m14:37:52.859 (+0:30:29.308) 菅 [LoggingInterceptor] Request: GET https://yz.hnv1420.org/api/v1/team-carts/8c66dca5-db66-415f-9895-e322f9c58f54/rt [auth: yes][0m
14:37:56.573  I  [38;5;8m14:37:56.568 (+0:30:33.017) 菅 [LoggingInterceptor] Response: 200 GET /api/v1/team-carts/8c66dca5-db66-415f-9895-e322f9c58f54/rt[0m