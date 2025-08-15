using MediatR;

namespace CloudZCrypt.Application.Common.Abstractions;

/// <summary>
/// Represents a query that returns a value
/// </summary>
/// <typeparam name="TResponse">The response type</typeparam>
public interface IQuery<out TResponse> : IRequest<TResponse>
{
}