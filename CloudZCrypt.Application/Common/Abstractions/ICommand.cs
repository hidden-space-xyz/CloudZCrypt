using MediatR;

namespace CloudZCrypt.Application.Common.Abstractions;

/// <summary>
/// Represents a command that doesn't return a value
/// </summary>
public interface ICommand : IRequest
{
}

/// <summary>
/// Represents a command that returns a value
/// </summary>
/// <typeparam name="TResponse">The response type</typeparam>
public interface ICommand<out TResponse> : IRequest<TResponse>
{
}