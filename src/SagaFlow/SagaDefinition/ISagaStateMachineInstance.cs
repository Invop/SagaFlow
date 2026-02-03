namespace SagaFlow.SagaDefinition;

/// <summary>
/// Represents a saga instance that is managed by a state machine.
/// Combines saga identity with state tracking.
/// </summary>
public interface ISagaStateMachineInstance : ISaga
{
    /// <summary>
    /// Gets or sets the current state of the saga instance.
    /// The state machine uses this property to track and manage state transitions.
    /// </summary>
    string CurrentState { get; set; }
}