using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.FunctionalTests.TestData;

/// <summary>
/// Static configuration for default test entities.
/// Provides constants and default values for creating test data without containing the creation logic.
/// </summary>
public static class DefaultTestData
{
    /// <summary>
    /// Default test user configuration.
    /// </summary>
    public static class User
    {
        public const string Email = "default-customer@yummyzoom.test";
        public const string Password = "TestUser123!";
        public static readonly string[] UserRoles = [Roles.User];
        public static readonly string[] AllRoles =
        [
            Roles.Administrator,
            Roles.RestaurantOwner,
            Roles.RestaurantStaff,
            Roles.User
        ];
    }

    /// <summary>
    /// Default test restaurant configuration.
    /// </summary>
    public static class Restaurant
    {
        public const string Name = "YummyZoom Test Kitchen";
        public const string LogoUrl = "https://test.yummyzoom.com/logo.png";
        public const string Description = "A test restaurant for functional testing with authentic flavors and fresh ingredients.";
        public const string CuisineType = "International Fusion";
        
        /// <summary>
        /// Restaurant address information.
        /// </summary>
        public static class Address
        {
            public const string Street = "123 Test Kitchen Boulevard";
            public const string City = "Test City";
            public const string State = "Test State";
            public const string ZipCode = "12345";
            public const string Country = "Test Country";
        }
        
        /// <summary>
        /// Restaurant contact information.
        /// </summary>
        public static class Contact
        {
            public const string Phone = "+1-555-123-4567";
            public const string Email = "contact@yummyzoom-test.com";
        }
        
        /// <summary>
        /// Restaurant business hours.
        /// </summary>
        public static class Hours
        {
            public const string BusinessHours = "Monday-Sunday: 9:00 AM - 10:00 PM";
        }
    }

    /// <summary>
    /// Default test menu configuration.
    /// </summary>
    public static class Menu
    {
        public const string Name = "Test Menu";
        public const string Description = "A comprehensive test menu with various food categories and items.";
        
        /// <summary>
        /// Menu categories configuration.
        /// </summary>
        public static class Categories
        {
            public static readonly (string Name, int SortOrder) MainDishes = ("Main Dishes", 1);
            public static readonly (string Name, int SortOrder) Appetizers = ("Appetizers", 2);
            public static readonly (string Name, int SortOrder) Desserts = ("Desserts", 3);
            public static readonly (string Name, int SortOrder) Beverages = ("Beverages", 4);
        }
    }

    /// <summary>
    /// Default test menu items configuration.
    /// </summary>
    public static class MenuItems
    {
        /// <summary>
        /// Main dish menu items.
        /// </summary>
        public static class MainDishes
        {
            public static readonly (string Name, string Description, decimal Price) ClassicBurger = 
                ("Classic Beef Burger", "Juicy beef patty with lettuce, tomato, pickles, and our special sauce on a brioche bun", 15.99m);
            
            public static readonly (string Name, string Description, decimal Price) MargheritaPizza = 
                ("Margherita Pizza", "Traditional pizza with fresh mozzarella, tomatoes, and basil on hand-tossed dough", 18.50m);
            
            public static readonly (string Name, string Description, decimal Price) GrilledSalmon = 
                ("Grilled Atlantic Salmon", "Fresh salmon fillet grilled to perfection with lemon herb seasoning and seasonal vegetables", 24.99m);
        }

        /// <summary>
        /// Appetizer menu items.
        /// </summary>
        public static class Appetizers
        {
            public static readonly (string Name, string Description, decimal Price) BuffaloWings = 
                ("Buffalo Chicken Wings", "Crispy chicken wings tossed in our signature buffalo sauce, served with celery and ranch", 12.99m);
            
            public static readonly (string Name, string Description, decimal Price) CaesarSalad = 
                ("Caesar Salad", "Crisp romaine lettuce with parmesan cheese, croutons, and our house-made Caesar dressing", 9.99m);
        }

        /// <summary>
        /// Dessert menu items.
        /// </summary>
        public static class Desserts
        {
            public static readonly (string Name, string Description, decimal Price) ChocolateCake = 
                ("Chocolate Fudge Cake", "Rich chocolate cake with layers of chocolate fudge and fresh berries", 8.99m);
        }

        /// <summary>
        /// Beverage menu items.
        /// </summary>
        public static class Beverages
        {
            public static readonly (string Name, string Description, decimal Price) CraftBeer = 
                ("Local Craft Beer", "Rotating selection of local craft beers on tap", 6.99m);
            
            public static readonly (string Name, string Description, decimal Price) FreshJuice = 
                ("Fresh Orange Juice", "Freshly squeezed orange juice made to order", 4.99m);
        }
    }

    /// <summary>
    /// Default test coupon configuration.
    /// </summary>
    public static class Coupon
    {
        public const string Code = "SAVE15TEST";
        public const string Description = "Save 15% on your test order - Valid for functional testing";
        public const decimal DiscountPercentage = 15m;
        public const decimal MinimumOrderAmount = 20.00m;
        public const int TotalUsageLimit = 100;
        public const int UsageLimitPerUser = 5;
        public const int ValidDaysFromNow = 30;
    }

    /// <summary>
    /// Currency configuration for test data.
    /// </summary>
    public static class Currency
    {
        public const string Default = "USD";
    }
}
