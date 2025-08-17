using MediatR;
using System.Diagnostics;

namespace CloudZCrypt.Application.Common.Behaviors;

/// <summary>
/// Pipeline behavior for monitoring performance of requests
/// Follows CQRS best practices for cross-cutting concerns
/// </summary>
/// <typeparam name="TRequest">The request type</typeparam>
/// <typeparam name="TResponse">The response type</typeparam>
public class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const int SlowRequestThreshold = 500; // ms

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        TResponse response = await next();

        stopwatch.Stop();

        long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;

        return response;
    }
}