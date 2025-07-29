### Phase 1: Setup & Configuration ⚙️

This phase prepares your project to communicate with Stripe securely.

1. **Install NuGet Package:**
    In your terminal, add the Stripe.net package to the `Infrastructure` project.

    ```bash
    dotnet add src/Infrastructure/Infrastructure.csproj package Stripe.net
    ```

2. **Define Options Class:**
    Create the `StripeOptions.cs` file to hold your configuration keys.

      * **File:** `src/Infrastructure/Payments/Stripe/StripeOptions.cs`
      * **Content:**

        ```csharp
        namespace YummyZoom.Infrastructure.Payments.Stripe;

        public class StripeOptions
        {
            public const string SectionName = "Stripe";
            public string SecretKey { get; init; } = string.Empty;
            public string PublishableKey { get; init; } = string.Empty;
            public string WebhookSecret { get; init; } = string.Empty;
        }
        ```

3. **Configure Secrets:**
    For local development, store your Stripe keys in the `secrets.json` file for the `AppHost` project.

      * **In your terminal:**

        ```bash
        dotnet user-secrets init --project src/AppHost/YummyZoom.AppHost.csproj
        dotnet user-secrets set "Stripe:SecretKey" "sk_test_..." --project src/AppHost/YummyZoom.AppHost.csproj
        dotnet user-secrets set "Stripe:WebhookSecret" "whsec_..." --project src/AppHost/YummyZoom.AppHost.csproj
        ```

    *Note: The `PublishableKey` can go in `appsettings.Development.json` as it's not a secret.*

4. **Register Services in `Program.cs`:**
    In your `AppHost` project, configure dependency injection to register the options and the `StripeService`.

      * **File:** `src/AppHost/Program.cs`
      * **Action:** Add the following lines before `builder.Build()`.

        ```csharp
        // Add services to the container.
        builder.Services.Configure<StripeOptions>(builder.Configuration.GetSection(StripeOptions.SectionName));

        var stripeOptions = builder.Configuration.GetSection(StripeOptions.SectionName).Get<StripeOptions>();
        StripeConfiguration.ApiKey = stripeOptions.SecretKey;

        builder.Services.AddScoped<IPaymentGatewayService, StripeService>();
        ```

-----

### Phase 2: Infrastructure Layer Implementation 🏗️

Now, let's build the core logic that talks to Stripe and the database.

1. **Implement `StripeService`:**
    Create the `StripeService.cs` file and populate it with the logic we discussed. This class translates your application's needs into Stripe API calls.

      * **File:** `src/Infrastructure/Payments/Stripe/StripeService.cs`
      * **Action:** Implement the class skeleton provided in our previous discussion, including the `CreatePaymentIntentAsync` and `ConstructWebhookEvent` methods.

2. **Create Idempotency Table:**
    To prevent processing the same webhook twice, define an entity for tracking processed events.

      * **File:** `src/Infrastructure/Data/Persistence/Entities/ProcessedWebhookEvent.cs`
      * **Content:**

        ```csharp
        namespace YummyZoom.Infrastructure.Data.Persistence.Entities;

        public class ProcessedWebhookEvent
        {
            public string Id { get; set; } // Stripe Event ID (evt_...)
            public DateTime ProcessedAt { get; set; }
        }
        ```

      * **Action:** Add a `DbSet<ProcessedWebhookEvent>` to your `ApplicationDbContext` and configure it using `modelBuilder.Entity<ProcessedWebhookEvent>().HasKey(e => e.Id);`.

-----

### Phase 3: Application Layer Implementation 💼

Here, you'll create the commands that orchestrate the payment flow.

1. **Implement `InitiateOrderCommand`:**
    This handler will be called when a user clicks "Place Order".

      * **File:** `src/Application/Features/Orders/Commands/InitiateOrder/InitiateOrderCommand.cs` (and its Handler/Validator)
      * **Handler Logic:**
        1. Validate the command input.
        2. Fetch restaurant, menu items, and calculate totals.
        3. If payment method is online:
            a.  Call `_paymentGatewayService.CreatePaymentIntentAsync(...)`. Pass the calculated total and metadata (like `order_id`).
            b.  Call `Order.Create(...)`, providing the `paymentIntent.Id` as the `paymentGatewayReferenceId`. The order status will be `AwaitingPayment`.
        4. If payment is COD, call `Order.Create(...)` directly. The status will be `Placed`.
        5. Add the new `Order` to the repository and save changes.
        6. Return the `OrderId` and the `client_secret` (if online payment) to the caller.

2. **Implement `HandleStripeWebhookCommand`:**
    This handler processes the events sent by Stripe.

      * **File:** `src/Application/Features/Orders/Commands/HandleStripeWebhook/HandleStripeWebhookCommand.cs` (and Handler)
      * **Handler Logic:**
        1. Call `_paymentGatewayService.ConstructWebhookEvent(...)` to verify the signature and deserialize the event. Fail if invalid.
        2. **Idempotency Check:** Attempt to insert the `stripeEvent.Id` into the `ProcessedWebhookEvents` table within a new transaction scope. If it fails (duplicate key), return success immediately.
        3. Extract the `PaymentIntent` ID from the event data.
        4. Fetch the `Order` from the repository using the Payment Intent ID. If not found, log it and return success (the event might not be for an order).
        5. Use a `switch` on `stripeEvent.Type`:
              * `case "payment_intent.succeeded":` Call `order.RecordPaymentSuccess(...)`.
              * `case "payment_intent.payment_failed":` Call `order.RecordPaymentFailure(...)`.
        6. Update the `Order` in the repository and save changes.

-----

### Phase 4: Presentation (Web API) Layer 🌐

Finally, expose the functionality through API endpoints.

1. **Create Order Endpoint:**
    In your `Web` project, create an endpoint for placing an order.

      * **File:** `src/Web/Endpoints/Orders.cs` (or `OrdersController.cs`)
      * **Action:** Create a `POST /api/orders` endpoint that accepts the order details, builds an `InitiateOrderCommand`, sends it via MediatR, and returns the result (`InitiateOrderResponse`).

2. **Create Stripe Webhook Endpoint:**
    This special endpoint receives notifications from Stripe. It needs to be public and handle raw request data.

      * **File:** `src/Web/Endpoints/StripeWebhooks.cs` (or `StripeWebhookController.cs`)
      * **Action:**
        1. Create a `POST /api/webhooks/stripe` endpoint.
        2. **Crucially**, read the raw JSON body of the request into a string.
        3. Get the `Stripe-Signature` header value.
        4. Create a `HandleStripeWebhookCommand` with the raw JSON and the signature header.
        5. Send the command via MediatR.
        6. Return a `200 OK` response to Stripe.

After completing these steps, you'll have a fully functional, secure, and robust Stripe payment integration that follows the architecture of your project.
