using MediatR;

namespace CloudZCrypt.Application.Common.Abstractions;
public interface IQuery<out TResponse> : IRequest<TResponse>
{
}
