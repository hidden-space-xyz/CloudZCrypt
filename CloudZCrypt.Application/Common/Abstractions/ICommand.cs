using MediatR;

namespace CloudZCrypt.Application.Common.Abstractions;
public interface ICommand : IRequest
{
}
public interface ICommand<out TResponse> : IRequest<TResponse>
{
}
