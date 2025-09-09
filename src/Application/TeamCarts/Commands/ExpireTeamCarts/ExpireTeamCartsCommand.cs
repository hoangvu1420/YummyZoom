using MediatR;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Common.Interfaces.IRepositories;

namespace YummyZoom.Application.TeamCarts.Commands.ExpireTeamCarts;

/// <summary>
/// Internal command to run a single expiration iteration with explicit parameters (test-only).
/// </summary>
public sealed record ExpireTeamCartsCommand(DateTime CutoffUtc, int BatchSize) : IRequest<int>;

public sealed class ExpireTeamCartsCommandHandler : IRequestHandler<ExpireTeamCartsCommand, int>
{
    private readonly ITeamCartRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public ExpireTeamCartsCommandHandler(
        ITeamCartRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(ExpireTeamCartsCommand request, CancellationToken cancellationToken)
    {
        var total = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            var candidates = await _repository.GetExpiringCartsAsync(request.CutoffUtc, request.BatchSize, cancellationToken);
            if (candidates.Count == 0)
            {
                break;
            }

            foreach (var cart in candidates)
            {
                var result = cart.MarkAsExpired();
                if (result.IsSuccess)
                {
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    total++;
                }
            }
        }

        return total;
    }
}


