namespace SagaFlow.Messages;

/// <summary>
///     Provides context information for saga message processing, encapsulating the message along with its metadata.
///     This interface is used to pass both the message content and associated tracking information to message handlers.
/// </summary>
/// <typeparam name="TMessage">The type of saga message, must implement <see cref="ISagaMessage" /></typeparam>
/// <remarks>
///     The message context serves as a container that includes not only the business message but also
///     essential metadata for message correlation, identification, and routing within the saga framework.
/// </remarks>
public interface ISagaMessageContext<out TMessage> where TMessage : ISagaMessage
{
    /// <summary>
    ///     Gets the saga message instance containing the business data.
    /// </summary>
    /// <value>The strongly-typed message object</value>
    TMessage Message { get; }

    /// <summary>
    ///     Gets the correlation identifier used to associate this message with a specific saga instance.
    ///     This ID enables message routing to the correct saga and maintains process continuity.
    /// </summary>
    /// <value>A unique string identifier for saga correlation</value>
    Guid CorrelationId { get; }

    /// <summary>
    ///     Gets the unique identifier for this specific message instance.
    ///     Used for message deduplication, logging, and tracking purposes.
    /// </summary>
    /// <value>A unique string identifier for the message</value>
    string MessageId { get; }

    /// <summary>
    ///     Gets the identifier of the component or service that sent this message.
    ///     Useful for auditing, debugging, and understanding message flow.
    /// </summary>
    /// <value>A string identifier of the message sender</value>
    string SenderId { get; }
}