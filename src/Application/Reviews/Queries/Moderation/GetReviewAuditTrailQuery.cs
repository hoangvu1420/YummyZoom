using System.Data;
using Dapper;
using YummyZoom.Application.Common.Interfaces;

namespace YummyZoom.Application.Reviews.Queries.Moderation;

public sealed record GetReviewAuditTrailQuery(Guid ReviewId) : IRequest<YummyZoom.SharedKernel.Result<IReadOnlyList<ReviewModerationAuditDto>>>;

public sealed class GetReviewAuditTrailQueryHandler : IRequestHandler<GetReviewAuditTrailQuery, YummyZoom.SharedKernel.Result<IReadOnlyList<ReviewModerationAuditDto>>>
{
    private readonly IDbConnectionFactory _db;

    public GetReviewAuditTrailQueryHandler(IDbConnectionFactory db)
    {
        _db = db;
    }

    public Task<YummyZoom.SharedKernel.Result<IReadOnlyList<ReviewModerationAuditDto>>> Handle(GetReviewAuditTrailQuery request, CancellationToken cancellationToken)
    {
        // MVP: audit table not present; return empty list to avoid schema dependency.
        var empty = Array.Empty<ReviewModerationAuditDto>() as IReadOnlyList<ReviewModerationAuditDto>;
        return Task.FromResult(YummyZoom.SharedKernel.Result.Success(empty));
    }
}


