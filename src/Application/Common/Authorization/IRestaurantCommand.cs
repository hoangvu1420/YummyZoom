using YummyZoom.Domain.RoleAssignmentAggregate.ValueObjects;

namespace YummyZoom.Application.Common.Authorization;

public interface IRestaurantCommand : IContextualCommand
{
    RestaurantId RestaurantId { get; }
    
    // Default implementations
    string IContextualCommand.ResourceType => "Restaurant";
    string IContextualCommand.ResourceId => RestaurantId.Value.ToString();
} 
