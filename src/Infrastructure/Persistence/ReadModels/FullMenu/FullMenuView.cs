namespace YummyZoom.Infrastructure.Persistence.ReadModels.FullMenu;

public class FullMenuView
{
    public Guid RestaurantId { get; set; }

    // Denormalized, pre-computed menu document for customer app consumption
    public string MenuJson { get; set; } = string.Empty;

    public DateTimeOffset LastRebuiltAt { get; set; }
}
