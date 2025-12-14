using Dapper;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Domain.ReviewAggregate.Errors;
using YummyZoom.SharedKernel;
using Result = YummyZoom.SharedKernel.Result;

namespace YummyZoom.Application.Reviews.Queries.GetOrderReview;

public sealed class GetOrderReviewQueryHandler : IRequestHandler<GetOrderReviewQuery, Result<OrderReviewDto>>
{
    private readonly IDbConnectionFactory _dbConnectionFactory;
    private readonly IUser _currentUser;

    public GetOrderReviewQueryHandler(IDbConnectionFactory dbConnectionFactory, IUser currentUser)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _currentUser = currentUser;
    }

    public async Task<Result<OrderReviewDto>> Handle(GetOrderReviewQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.Id;
        if (userId == null) throw new UnauthorizedAccessException();

        using var connection = _dbConnectionFactory.CreateConnection();

        const string sql = """
            SELECT 
                "Id" AS ReviewId,
                "OrderId",
                "RestaurantId",
                "Rating",
                NULL AS Title,
                "Comment",
                "Created" AS CreatedAt
            FROM "Reviews"
            WHERE "OrderId" = @OrderId AND "CustomerId" = @CustomerId
            LIMIT 1
            """;

        var review = await connection.QuerySingleOrDefaultAsync<OrderReviewDto>(
            sql, 
            new { request.OrderId, CustomerId = Guid.Parse(userId) });

        if (review is null)
        {
            return Result.Failure<OrderReviewDto>(ReviewErrors.NotFound);
        }

        return Result.Success(review);
    }
}
