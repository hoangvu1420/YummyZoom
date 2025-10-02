using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.FunctionalTests.Infrastructure;

/// <summary>
/// Centralized configuration and constants for functional tests.
/// </summary>
public static class TestConfiguration
{
    /// <summary>
    /// Default test user credentials and settings.
    /// </summary>
    public static class DefaultUsers
    {
        /// <summary>
        /// Default test user credentials.
        /// </summary>
        public static class TestUser
        {
            public const string Email = "test@local";
            public const string Password = "Testing1234!";
        }

        /// <summary>
        /// Default administrator user credentials.
        /// </summary>
        public static class Administrator
        {
            public const string Email = "administrator@local";
            public const string Password = "Administrator1234!";
        }

        /// <summary>
        /// Common test password used across various test scenarios.
        /// </summary>
        public const string CommonTestPassword = "Password123!";
    }

    /// <summary>
    /// Test role definitions and constants.
    /// </summary>
    public static class TestRoles
    {
        /// <summary>
        /// All roles that should be available for testing.
        /// </summary>
        public static readonly string[] AllRoles =
        {
            Roles.Administrator,
            Roles.RestaurantOwner,
            Roles.RestaurantStaff,
            Roles.User
        };

        /// <summary>
        /// Basic roles required for most tests.
        /// </summary>
        public static readonly string[] BasicRoles =
        {
            Roles.User,
            Roles.Administrator
        };

        /// <summary>
        /// Restaurant-specific roles.
        /// </summary>
        public static readonly string[] RestaurantRoles =
        {
            Roles.RestaurantOwner,
            Roles.RestaurantStaff
        };

        /// <summary>
        /// Roles required for user registration tests.
        /// </summary>
        public static readonly string[] UserRegistrationRoles =
        {
            Roles.User,
            Roles.Administrator,
            Roles.RestaurantOwner
        };
    }

    /// <summary>
    /// Payment and Stripe test configuration.
    /// </summary>
    public static class Payment
    {
        /// <summary>
        /// Test Stripe configuration section name.
        /// </summary>
        public const string StripeSectionName = "Stripe";

        /// <summary>
        /// Test payment methods for different scenarios.
        /// </summary>
        public static class TestPaymentMethods
        {
            /// <summary>
            /// Visa card that will succeed.
            /// </summary>
            public const string VisaSuccess = "pm_card_visa";

            /// <summary>
            /// Card that will be declined.
            /// </summary>
            public const string VisaDeclined = "pm_card_visa_chargeDeclined";

            /// <summary>
            /// Card that requires authentication.
            /// </summary>
            public const string VisaAuthentication = "pm_card_visa_chargeDeclinedInsufficientFunds";

            /// <summary>
            /// Card that will be declined due to insufficient funds.
            /// </summary>
            public const string VisaInsufficientFunds = "pm_card_visa_chargeDeclinedInsufficientFunds";

            /// <summary>
            /// Card that will be declined due to expired card.
            /// </summary>
            public const string VisaExpired = "pm_card_visa_chargeDeclinedExpiredCard";

            /// <summary>
            /// Card that will be declined due to incorrect CVC.
            /// </summary>
            public const string VisaIncorrectCvc = "pm_card_visa_chargeDeclinedIncorrectCvc";
        }

        /// <summary>
        /// Test webhook event types.
        /// </summary>
        public static class WebhookEvents
        {
            /// <summary>
            /// Payment intent succeeded event.
            /// </summary>
            public const string PaymentIntentSucceeded = "payment_intent.succeeded";

            /// <summary>
            /// Payment intent payment failed event.
            /// </summary>
            public const string PaymentIntentPaymentFailed = "payment_intent.payment_failed";

            /// <summary>
            /// Payment intent canceled event.
            /// </summary>
            public const string PaymentIntentCanceled = "payment_intent.canceled";

            /// <summary>
            /// Payment intent requires action event.
            /// </summary>
            public const string PaymentIntentRequiresAction = "payment_intent.requires_action";

            /// <summary>
            /// Payment intent processing event.
            /// </summary>
            public const string PaymentIntentProcessing = "payment_intent.processing";
        }
    }
}
