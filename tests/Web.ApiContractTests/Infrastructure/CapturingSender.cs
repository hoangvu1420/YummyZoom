using System.Linq; // For AsyncEnumerable.Empty
using MediatR;

namespace YummyZoom.Web.ApiContractTests.Infrastructure;

public sealed class CapturingSender : ISender
{
    public object? LastRequest { get; private set; }
    private Func<object, object?>? _responder;

    public void RespondWith(Func<object, object?> responder) => _responder = responder;

    // IRequest<TResponse>
    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default)
    {
        LastRequest = request;
        var value = _responder is null ? default : _responder(request);
        return Task.FromResult((TResponse?)value!);
    }

    // IRequest (non-generic) overload (MediatR 12)
    public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
    {
        LastRequest = request!;
        _responder?.Invoke(request!);
        return Task.CompletedTask;
    }

    // Object based
    public Task<object?> Send(object request, CancellationToken ct = default)
    {
        LastRequest = request;
        var value = _responder?.Invoke(request);
        return Task.FromResult(value);
    }

    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken ct = default)
    {
        LastRequest = request;
        return EmptyAsync<TResponse>();
    }

    public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken ct = default)
    {
        LastRequest = request;
        return EmptyAsync<object?>();
    }

    private static IAsyncEnumerable<T> EmptyAsync<T>()
    {
        return Enumerate();

#pragma warning disable CS1998
        static async IAsyncEnumerable<T> Enumerate()
        {
            yield break; // empty sequence
        }
#pragma warning restore CS1998
    }
}
