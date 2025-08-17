using MediatR;

namespace CloudZCrypt.Application.Common.Abstractions;
public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
}
