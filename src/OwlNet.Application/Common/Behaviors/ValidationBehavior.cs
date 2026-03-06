using System.Reflection;
using DispatchR.Abstractions.Send;
using FluentValidation;
using OwlNet.Application.Common.Models;

namespace OwlNet.Application.Common.Behaviors;

/// <summary>
/// DispatchR pipeline behavior that runs FluentValidation validators before the handler.
/// If validation fails, returns a <see cref="Result"/> or <see cref="Result{T}"/> failure
/// with the first validation error message instead of calling the next handler.
/// </summary>
/// <typeparam name="TRequest">The request type being validated.</typeparam>
/// <typeparam name="TResponse">The response type (must be <see cref="Result"/> or <see cref="Result{T}"/>).</typeparam>
public sealed class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, ValueTask<TResponse>>
    where TRequest : class, IRequest<TRequest, ValueTask<TResponse>>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="validators">The collection of validators registered for <typeparamref name="TRequest"/>.</param>
    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    /// <summary>
    /// The next handler in the pipeline chain. Set by DispatchR during pipeline construction.
    /// </summary>
    public required IRequestHandler<TRequest, ValueTask<TResponse>> NextPipeline { get; set; }

    /// <inheritdoc />
    public async ValueTask<TResponse> Handle(TRequest request, CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await NextPipeline.Handle(request, cancellationToken);
        }

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count == 0)
        {
            return await NextPipeline.Handle(request, cancellationToken);
        }

        return CreateFailureResult(failures[0].ErrorMessage);
    }

    /// <summary>
    /// Creates a failure result for both <see cref="Result"/> and <see cref="Result{T}"/> types.
    /// </summary>
    private static TResponse CreateFailureResult(string errorMessage)
    {
        if (typeof(TResponse) == typeof(Result))
        {
            return (TResponse)(object)Result.Failure(errorMessage);
        }

        if (typeof(TResponse).IsGenericType &&
            typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
        {
            var failureMethod = typeof(TResponse).GetMethod(
                nameof(Result.Failure),
                BindingFlags.Public | BindingFlags.Static)!;

            return (TResponse)failureMethod.Invoke(null, [errorMessage])!;
        }

        throw new InvalidOperationException(
            $"ValidationBehavior does not support response type '{typeof(TResponse).Name}'. " +
            $"Use Result or Result<T> as the response type.");
    }
}
