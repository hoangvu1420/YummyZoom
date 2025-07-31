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
    /// Common test data constants.
    /// </summary>
    public static class TestData
    {
        /// <summary>
        /// Common test email domains and patterns.
        /// </summary>
        public static class EmailPatterns
        {
            public const string TestDomain = "@example.com";
            public const string LocalDomain = "@local";
            public const string WorkflowDomain = "@test.com";
        }

        /// <summary>
        /// Default timeout values for test operations.
        /// </summary>
        public static class Timeouts
        {
            /// <summary>
            /// Default timeout for database operations in milliseconds.
            /// </summary>
            public const int DatabaseOperationTimeoutMs = 30000;

            /// <summary>
            /// Default timeout for HTTP requests in milliseconds.
            /// </summary>
            public const int HttpRequestTimeoutMs = 10000;

            /// <summary>
            /// Default timeout for async operations in milliseconds.
            /// </summary>
            public const int AsyncOperationTimeoutMs = 5000;
        }

        /// <summary>
        /// Test environment settings.
        /// </summary>
        public static class Environment
        {
            /// <summary>
            /// Whether to use in-memory database for tests.
            /// </summary>
            public const bool UseInMemoryDatabase = false;

            /// <summary>
            /// Whether to reset database state between tests.
            /// </summary>
            public const bool ResetDatabaseBetweenTests = true;

            /// <summary>
            /// Whether to enable detailed logging during tests.
            /// </summary>
            public const bool EnableDetailedLogging = false;
        }
    }

    /// <summary>
    /// Test database configuration.
    /// </summary>
    public static class Database
    {
        /// <summary>
        /// Connection string key for test database configuration.
        /// </summary>
        public const string ConnectionStringKey = "ConnectionStrings:YummyZoomDb";

        /// <summary>
        /// Default connection string template for test database.
        /// </summary>
        public const string ConnectionStringTemplate = "Host=localhost;Database=YummyZoom_Test_{0};Username=postgres;Password=postgres";

        /// <summary>
        /// Test database name prefix.
        /// </summary>
        public const string TestDatabasePrefix = "YummyZoom_Test_";

        /// <summary>
        /// Maximum number of retry attempts for database operations.
        /// </summary>
        public const int MaxRetryAttempts = 3;

        /// <summary>
        /// Delay between retry attempts in milliseconds.
        /// </summary>
        public const int RetryDelayMs = 1000;
    }

    /// <summary>
    /// Test service configuration.
    /// </summary>
    public static class Services
    {
        /// <summary>
        /// Whether to use mock services in tests.
        /// </summary>
        public const bool UseMockServices = true;

        /// <summary>
        /// Whether to enable service validation in tests.
        /// </summary>
        public const bool EnableServiceValidation = true;

        /// <summary>
        /// Default service scope lifetime for tests.
        /// </summary>
        public const string DefaultScopeLifetime = "Scoped";
    }
}
