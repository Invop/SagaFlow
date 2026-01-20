using SagaFlow.Messages;

namespace SagaFlow.Utils;

/// <summary>
///     Serializer interface for outbox message payloads.
///     Implementations handle serialization/deserialization of saga messages for outbox storage.
/// </summary>
public interface ISerializer
{
    /// <summary>
    ///     Serializes a saga message to a byte array for storage.
    /// </summary>
    /// <typeparam name="TMessage">The type of message to serialize.</typeparam>
    /// <param name="message">The message to serialize.</param>
    /// <returns>The serialized message as a byte array.</returns>
    byte[] Serialize<TMessage>(TMessage message) where TMessage : ISagaMessage;

    /// <summary>
    ///     Deserializes a message payload back to its original type.
    /// </summary>
    /// <param name="payload">The serialized payload.</param>
    /// <param name="messageType">The full type name of the message.</param>
    /// <returns>The deserialized message object.</returns>
    object Deserialize(byte[] payload, string messageType);

    /// <summary>
    ///     Deserializes a message payload to a specific type.
    /// </summary>
    /// <typeparam name="TMessage">The expected message type.</typeparam>
    /// <param name="payload">The serialized payload.</param>
    /// <returns>The deserialized message.</returns>
    TMessage Deserialize<TMessage>(byte[] payload) where TMessage : ISagaMessage;
}
