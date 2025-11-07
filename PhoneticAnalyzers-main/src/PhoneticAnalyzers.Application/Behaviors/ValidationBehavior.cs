using FluentValidation;
using MediatR;

namespace PhoneticAnalyzers.Application.Behaviors;

/// <summary>
/// MediatR pipeline behavior that runs all FluentValidation validators for a request before handling.
/// </summary>
/// <typeparam name="TRequest">Request type</typeparam>
/// <typeparam name="TResponse">Response type</typeparam>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    /// <summary>
    /// Creates a new validation pipeline behavior.
    /// </summary>
    /// <param name="validators">Collection of validators applicable to the request type.</param>
    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    /// <summary>
    /// Executes validation for the request and proceeds to the next handler if valid.
    /// </summary>
    /// <param name="request">The request instance.</param>
    /// <param name="next">Delegate to invoke the next handler in the pipeline.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response from the next handler if validation succeeds.</returns>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);
            var validationTasks = _validators.Select(v => v.ValidateAsync(context, cancellationToken));
            var results = await Task.WhenAll(validationTasks);

            var failures = results.SelectMany(r => r.Errors)
                                   .Where(f => f is not null)
                                   .ToList();

            if (failures.Count != 0)
            {
                throw new ValidationException(failures);
            }
        }

        return await next();
    }
}
