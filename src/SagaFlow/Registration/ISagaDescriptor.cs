namespace SagaFlow.Registration;

/// <summary>
/// Describes a registered saga including its type information and message handling capabilities.
/// </summary>
/// <remarks>
/// <para>
/// A saga descriptor provides metadata about a saga that is used by the orchestrator
/// to route messages and manage saga instances. It contains information about:
/// </para>
/// <list type="bullet">
///     <item>
///         <description>The saga type (state machine implementation)</description>
///     </item>
///     <item>
///         <description>The saga instance/state type that maintains saga state</description>
///     </item>
///     <item>
///         <description>Message types that can initiate new saga instances</description>
///     </item>
///     <item>
///         <description>All message types that the saga handles</description>
///     </item>
/// </list>
/// </remarks>
public interface ISagaDescriptor
{
    /// <summary>
    /// Gets the type of the saga state machine.
    /// </summary>
    /// <remarks>
    /// This is the type that derives from <see cref="SagaDefinition.SagaStateMachine{TSagaData}"/>
    /// and contains the saga orchestration logic.
    /// </remarks>
    Type SagaType { get; }

    /// <summary>
    /// Gets the type of the saga instance/state.
    /// </summary>
    /// <remarks>
    /// This is the type that implements <see cref="SagaDefinition.ISagaStateMachineInstance"/>
    /// and maintains the persistent state of a saga instance.
    /// </remarks>
    Type SagaInstanceType { get; }

    /// <summary>
    /// Gets the types of messages that can initiate a new saga instance.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Initiator messages are handled in the <c>Initially</c> block of the saga state machine.
    /// When an initiator message is received and no existing saga instance is found
    /// with the matching correlation ID, a new instance is created.
    /// </para>
    /// </remarks>
    IReadOnlySet<Type> InitiatorTypes { get; }

    /// <summary>
    /// Gets all message types handled by this saga.
    /// </summary>
    /// <remarks>
    /// This includes both initiator messages and messages handled in subsequent states.
    /// Used for message routing and subscription configuration.
    /// </remarks>
    IReadOnlySet<Type> HandledMessageTypes { get; }
}
