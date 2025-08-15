using MediatR;

namespace CloudZCrypt.Application.Common.Abstractions;

/// <summary>
/// Represents a query handler that returns a value
/// </summary>
/// <typeparam name="TQuery">The query type</typeparam>
/// <typeparam name="TResponse">The response type</typeparam>
public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
}