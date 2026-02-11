namespace SagaFlow.Registration;

/// <summary>
/// Registry for managing saga descriptors and resolving sagas by message type.
/// </summary>
/// <remarks>
/// <para>
/// The saga registry maintains a mapping between message types and the sagas that handle them.
/// This is used by the saga orchestrator to:
/// </para>
/// <list type="bullet">
///     <item><description>Route incoming messages to the appropriate saga</description></item>
///     <item><description>Determine if a message should create a new saga instance</description></item>
///     <item><description>Configure message subscriptions with the transport layer</description></item>
/// </list>
/// </remarks>
public interface ISagaRegistry
{
    /// <summary>
    /// Gets all registered saga descriptors.
    /// </summary>
    IEnumerable<ISagaDescriptor> Descriptors { get; }

    /// <summary>
    /// Gets all message types that have registered handlers across all sagas.
    /// </summary>
    /// <remarks>
    /// This is useful for configuring message broker subscriptions.
    /// </remarks>
    IEnumerable<Type> RegisteredMessageTypes { get; }

    /// <summary>
    /// Resolves all sagas that can handle the specified message type.
    /// </summary>
    /// <param name="messageType">The message type to resolve handlers for.</param>
    /// <returns>Collection of saga descriptors that handle the message type.</returns>
    IEnumerable<ISagaDescriptor> ResolveByMessageType(Type messageType);

    /// <summary>
    /// Resolves all sagas that can handle the specified message.
    /// </summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    /// <returns>Collection of saga descriptors that handle the message type.</returns>
    IEnumerable<ISagaDescriptor> ResolveByMessageType<TMessage>() where TMessage : Messages.ISagaMessage;

    /// <summary>
    /// Checks if the specified message type can initiate a new saga instance.
    /// </summary>
    /// <param name="messageType">The message type to check.</param>
    /// <returns>True if the message type is an initiator for any registered saga.</returns>
    bool IsInitiator(Type messageType);

    /// <summary>
    /// Resolves sagas that can be initiated by the specified message type.
    /// </summary>
    /// <param name="messageType">The initiator message type.</param>
    /// <returns>Collection of saga descriptors that can be initiated by the message.</returns>
    IEnumerable<ISagaDescriptor> ResolveInitiators(Type messageType);

    /// <summary>
    /// Gets the descriptor for a specific saga type.
    /// </summary>
    /// <param name="sagaType">The saga type to find.</param>
    /// <returns>The saga descriptor, or null if not registered.</returns>
    ISagaDescriptor? GetDescriptor(Type sagaType);

    /// <summary>
    /// Gets the descriptor for a specific saga type.
    /// </summary>
    /// <typeparam name="TSaga">The saga type to find.</typeparam>
    /// <returns>The saga descriptor, or null if not registered.</returns>
    ISagaDescriptor? GetDescriptor<TSaga>();
}
