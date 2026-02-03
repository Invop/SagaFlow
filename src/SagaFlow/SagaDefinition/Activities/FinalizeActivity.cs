namespace SagaFlow.SagaDefinition.Activities;

/// <summary>
/// Activity that marks the saga as finalized (completed).
/// </summary>
/// <typeparam name="TSagaData">The type of saga data.</typeparam>
public sealed class FinalizeActivity<TSagaData> : IActivity<TSagaData> where TSagaData : class
{
    private readonly Func<TSagaData, string> _stateAccessor;
    private readonly Action<TSagaData, string> _stateMutator;

    /// <summary>
    /// The name of the final state.
    /// </summary>
    public const string FinalStateName = "Final";

    /// <summary>
    /// Initializes a new instance of the <see cref="FinalizeActivity{TSagaData}"/> class.
    /// </summary>
    /// <param name="stateAccessor">Function to get the current state from saga data.</param>
    /// <param name="stateMutator">Action to set the current state in saga data.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public FinalizeActivity(
        Func<TSagaData, string> stateAccessor,
        Action<TSagaData, string> stateMutator)
    {
        ArgumentNullException.ThrowIfNull(stateAccessor);
        ArgumentNullException.ThrowIfNull(stateMutator);

        _stateAccessor = stateAccessor;
        _stateMutator = stateMutator;
    }

    /// <inheritdoc/>
    public ValueTask ExecuteAsync(SagaExecutionContext<TSagaData> context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Transition to final state
        _stateMutator(context.SagaData, FinalStateName);

        return ValueTask.CompletedTask;
    }
}
