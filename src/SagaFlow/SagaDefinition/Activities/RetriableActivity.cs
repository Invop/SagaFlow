using SagaFlow.Messages;

namespace SagaFlow.SagaDefinition.Activities;

/// <summary>
/// Activity that marks a step as retriable.
/// Retriable steps can be automatically retried on failure according to the retry policy.
/// </summary>
/// <typeparam name="TSagaData">The type of saga data.</typeparam>
/// <typeparam name="TMessage">The type of message being processed.</typeparam>
public sealed class RetriableActivity<TSagaData, TMessage> : IActivity<TSagaData>
    where TSagaData : class
    where TMessage : ISagaMessage
{
    private readonly int _maxRetries;
    private readonly TimeSpan _retryDelay;
    private readonly Func<Exception, bool>? _retryPredicate;

    /// <summary>
    /// Initializes a new instance of the <see cref="RetriableActivity{TSagaData, TMessage}"/> class.
    /// </summary>
    /// <param name="maxRetries">Maximum number of retry attempts.</param>
    /// <param name="retryDelay">Delay between retry attempts.</param>
    /// <param name="retryPredicate">Optional predicate to determine if an exception should trigger a retry.</param>
    public RetriableActivity(
        int maxRetries = 3,
        TimeSpan? retryDelay = null,
        Func<Exception, bool>? retryPredicate = null)
    {
        if (maxRetries < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxRetries), "Maximum retries must be non-negative.");
        }

        _maxRetries = maxRetries;
        _retryDelay = retryDelay ?? TimeSpan.FromSeconds(5);
        _retryPredicate = retryPredicate;
    }

    /// <summary>
    /// Gets the maximum number of retry attempts.
    /// </summary>
    public int MaxRetries => _maxRetries;

    /// <summary>
    /// Gets the delay between retry attempts.
    /// </summary>
    public TimeSpan RetryDelay => _retryDelay;

    /// <summary>
    /// Determines whether the specified exception should trigger a retry.
    /// </summary>
    /// <param name="exception">The exception that occurred.</param>
    /// <returns>True if the operation should be retried; otherwise, false.</returns>
    public bool ShouldRetry(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return _retryPredicate?.Invoke(exception) ?? true;
    }

    /// <inheritdoc/>
    public ValueTask ExecuteAsync(SagaExecutionContext<TSagaData> context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        // This activity doesn't execute any logic during normal flow
        // It's metadata that's used by the orchestrator to configure retry behavior
        // The actual retry logic is handled by the orchestrator or activity executor

        return ValueTask.CompletedTask;
    }
}
