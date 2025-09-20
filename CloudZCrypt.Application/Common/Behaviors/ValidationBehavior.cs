using CloudZCrypt.Application.Common.Models;
using FluentValidation;
using MediatR;

namespace CloudZCrypt.Application.Common.Behaviors;
public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return await next();
        }

        ValidationContext<TRequest> context = new(request);

        FluentValidation.Results.ValidationResult[] validationResults = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        List<FluentValidation.Results.ValidationFailure> failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Count != 0)
        {
            if (typeof(TResponse).IsGenericType && typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
            {
                Type resultType = typeof(TResponse).GetGenericArguments()[0];
                System.Reflection.MethodInfo? failureMethod = typeof(Result<>).MakeGenericType(resultType).GetMethod("Failure", [typeof(string[])]);
                string[] errors = failures.Select(f => f.ErrorMessage).ToArray();
                return (TResponse)failureMethod!.Invoke(null, [errors])!;
            }

            if (typeof(TResponse) == typeof(Result))
            {
                string[] errors = failures.Select(f => f.ErrorMessage).ToArray();
                return (TResponse)(object)Result.Failure(errors);
            }

            throw new ValidationException(failures);
        }

        return await next();
    }
}
