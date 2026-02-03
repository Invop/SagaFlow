namespace SagaFlow.SagaDefinition;

/// <summary>
/// Defines the contract for a saga state machine.
/// Provides access to states and completion status for saga instance management.
/// </summary>
/// <typeparam name="TSagaInstance">The type of saga instance managed by this state machine.</typeparam>
public interface ISagaStateMachine<TSagaInstance> where TSagaInstance : class, ISagaStateMachineInstance
{
    /// <summary>
    /// Gets all states defined in this state machine.
    /// </summary>
    IEnumerable<State> States { get; }

    /// <summary>
    /// Gets the initial state where new saga instances start.
    /// </summary>
    State Initial { get; }

    /// <summary>
    /// Gets the final state indicating saga completion.
    /// </summary>
    State Final { get; }

    /// <summary>
    /// Determines whether the saga instance has completed and can be removed from the repository.
    /// </summary>
    /// <param name="instance">The saga instance to check.</param>
    /// <returns>True if the saga is complete; otherwise, false.</returns>
    Task<bool> IsCompleted(TSagaInstance instance);
}