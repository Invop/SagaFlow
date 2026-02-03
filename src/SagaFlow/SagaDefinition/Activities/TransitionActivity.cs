namespace SagaFlow.SagaDefinition.Activities;

/// <summary>
/// Activity that transitions the saga to a new state.
/// </summary>
/// <typeparam name="TSagaData">The type of saga data.</typeparam>
public sealed class TransitionActivity<TSagaData> : IActivity<TSagaData> where TSagaData : class
{
    private readonly State<TSagaData> _targetState;
    private readonly Func<TSagaData, string> _stateAccessor;
    private readonly Action<TSagaData, string> _stateMutator;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransitionActivity{TSagaData}"/> class.
    /// </summary>
    /// <param name="targetState">The target state to transition to.</param>
    /// <param name="stateAccessor">Function to get the current state from saga data.</param>
    /// <param name="stateMutator">Action to set the current state in saga data.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public TransitionActivity(
        State<TSagaData> targetState,
        Func<TSagaData, string> stateAccessor,
        Action<TSagaData, string> stateMutator)
    {
        ArgumentNullException.ThrowIfNull(targetState);
        ArgumentNullException.ThrowIfNull(stateAccessor);
        ArgumentNullException.ThrowIfNull(stateMutator);

        _targetState = targetState;
        _stateAccessor = stateAccessor;
        _stateMutator = stateMutator;
    }

    /// <inheritdoc/>
    public async ValueTask ExecuteAsync(SagaExecutionContext<TSagaData> context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Execute exit action for current state
        var currentStateName = _stateAccessor(context.SagaData);
        if (!string.IsNullOrEmpty(currentStateName))
        {
            // Note: In full implementation, we would look up the State<TSagaData> and call OnExit
            // For now, we assume the orchestrator handles this
        }

        // Transition to new state
        _stateMutator(context.SagaData, _targetState.Name);

        // Execute entry action for new state
        await _targetState.ExecuteOnEntryAsync(context.SagaData, cancellationToken).ConfigureAwait(false);
    }
}
