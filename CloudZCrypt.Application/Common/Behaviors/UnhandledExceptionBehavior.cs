using CloudZCrypt.Application.Common.Models;
using MediatR;

namespace CloudZCrypt.Application.Common.Behaviors;

/// <summary>
/// Pipeline behavior for handling unhandled exceptions
/// Follows CQRS best practices for cross-cutting concerns
/// </summary>
/// <typeparam name="TRequest">The request type</typeparam>
/// <typeparam name="TResponse">The response type</typeparam>
public class UnhandledExceptionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        try
        {
            return await next();
        }
        catch (Exception ex)
        {
            // If the response type is a Result<T>, return a failure result
            if (typeof(TResponse).IsGenericType && typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
            {
                Type resultType = typeof(TResponse).GetGenericArguments()[0];
                System.Reflection.MethodInfo? failureMethod = typeof(Result<>).MakeGenericType(resultType).GetMethod("Failure", [typeof(string[])]);
                string[] errors = [$"An unexpected error occurred: {ex.Message}"];
                return (TResponse)failureMethod!.Invoke(null, [errors])!;
            }

            // If the response type is Result, return a failure result
            if (typeof(TResponse) == typeof(Result))
            {
                string[] errors = [$"An unexpected error occurred: {ex.Message}"];
                return (TResponse)(object)Result.Failure(errors);
            }

            // For other response types, rethrow the exception
            throw;
        }
    }
}