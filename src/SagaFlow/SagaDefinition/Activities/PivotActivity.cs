namespace SagaFlow.SagaDefinition.Activities;

/// <summary>
/// Activity that marks a pivot point in the saga (point of no return).
/// After a pivot point, no compensations will be registered.
/// </summary>
/// <typeparam name="TSagaData">The type of saga data.</typeparam>
public sealed class PivotActivity<TSagaData> : IActivity<TSagaData> where TSagaData : class
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PivotActivity{TSagaData}"/> class.
    /// </summary>
    public PivotActivity()
    {
    }

    /// <inheritdoc/>
    public ValueTask ExecuteAsync(SagaExecutionContext<TSagaData> context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Mark that we've reached the pivot point
        // After this, compensation messages should not be registered
        context.IsPivotReached = true;

        // Clear any existing compensation messages since we can't rollback past this point
        context.CompensationMessages.Clear();

        return ValueTask.CompletedTask;
    }
}
