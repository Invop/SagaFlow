namespace SagaFlow.Sagas;

/// <summary>
///     Defines a saga - a long-running business process that coordinates multiple steps and maintains state.
///     Each saga instance is uniquely identified by a correlation ID for message routing and state tracking.
/// </summary>
public interface ISaga
{
    /// <summary>
    ///     Identifies the saga instance uniquely, and is the primary correlation
    ///     for the instance. While the setter is not typically called, it is there
    ///     to support persistence consistently across implementations.
    /// </summary>
    Guid CorrelationId { get; set; }
}

/// <summary>
///     Defines a saga with strongly-typed state management.
///     Extends <see cref="ISaga" /> to provide state persistence and tracking capabilities for saga orchestrators.
/// </summary>
/// <typeparam name="TState">
///     The type representing the saga's state. This should be a serializable type (typically a record or class)
///     that captures all the data needed throughout the saga's lifecycle.
/// </typeparam>
/// <remarks>
///     <para>
///         This interface is designed specifically for saga orchestrators that need to maintain state across
///         multiple message exchanges. The state is persisted between message processing steps and can be
///         accessed by saga step handlers through <see cref="ISagaContext{TState}" />.
///     </para>
///     <para>
///         Important: This interface should ONLY be used in saga orchestrator services.
///         Participant services (microservices that react to saga commands/events) should use
///         <see cref="Messages.ISagaMessageHandler{TSagaMessage}" /> without any saga state access,
///         as they are stateless from the saga's perspective.
///     </para>
/// </remarks>
public interface ISaga<TState> : ISaga
{
    /// <summary>
    ///     Gets or sets the current state of the saga instance.
    /// </summary>
    /// <value>The strongly-typed state object containing all saga data</value>
    /// <remarks>
    ///     The state is automatically persisted by the saga framework after each message processing step.
    ///     Implementations should ensure that the state type is serializable for persistence.
    ///     The setter exists to support state rehydration during saga instance recovery.
    /// </remarks>
    TState State { get; set; }
}