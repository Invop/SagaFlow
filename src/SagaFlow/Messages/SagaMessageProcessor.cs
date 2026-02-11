using Microsoft.Extensions.Logging;
using SagaFlow.Outbox;
using SagaFlow.Registration;
using SagaFlow.SagaDefinition;
using SagaFlow.Utils;

namespace SagaFlow.Messages;

/// <summary>
///     Entry point for processing messages received from the message bus.
///     Deserializes the <see cref="OutboxMessage"/> payload, resolves matching sagas
///     from the <see cref="ISagaRegistry"/>, and delegates execution to <see cref="ISagaRunner"/>.
/// </summary>
/// <remarks>
///     <para>
///     This class is the first layer of the orchestration pipeline:
///     <c>ISagaMessageProcessor → ISagaRunner → ISagaExecutor</c>.
///     </para>
///     <para>
///     A single message may match multiple sagas (fan-out). Each matching saga
///     is processed independently. Failures in one saga do not prevent
///     processing of the others.
///     </para>
/// </remarks>
internal sealed class SagaMessageProcessor(
    ISagaRegistry registry,
    ISagaRunner runner,
    ISerializer serializer,
    ILogger<SagaMessageProcessor> logger) : ISagaMessageProcessor
{
    /// <inheritdoc />
    public async ValueTask ProcessAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var messageType = ResolveMessageType(message.MessageType);
        if (messageType is null)
        {
            logger.LogError(
                "Cannot resolve message type '{MessageType}' for OutboxMessage {MessageId}. Ensure the assembly is loaded",
                message.MessageType,
                message.Id);

            return;
        }

        var sagaMessage = DeserializePayload(message, messageType);
        if (sagaMessage is null)
        {
            return;
        }

        var descriptors = registry.ResolveByMessageType(messageType).ToList();
        if (descriptors.Count == 0)
        {
            logger.LogWarning(
                "No saga registered for message type '{MessageType}' (OutboxMessage {MessageId})",
                messageType.Name,
                message.Id);

            return;
        }

        logger.LogDebug(
            "OutboxMessage {MessageId} matched {SagaCount} saga(s) for message type {MessageType}",
            message.Id,
            descriptors.Count,
            messageType.Name);

        foreach (var descriptor in descriptors)
        {
            await runner
                .RunAsync(descriptor, sagaMessage, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     Resolves the CLR <see cref="Type"/> from the stored assembly-qualified type name.
    /// </summary>
    private static Type? ResolveMessageType(string messageTypeName)
    {
        if (string.IsNullOrWhiteSpace(messageTypeName))
        {
            return null;
        }

        return Type.GetType(messageTypeName, throwOnError: false);
    }

    /// <summary>
    ///     Deserializes the outbox message payload into the concrete <see cref="ISagaMessage"/> type.
    /// </summary>
    private ISagaMessage? DeserializePayload(OutboxMessage message, Type messageType)
    {
        try
        {
            var deserialized = serializer.Deserialize(message.Payload, message.MessageType);

            if (deserialized is not ISagaMessage sagaMessage)
            {
                logger.LogError(
                    "Deserialized message for OutboxMessage {MessageId} does not implement ISagaMessage. Actual type: {ActualType}",
                    message.Id,
                    deserialized.GetType().FullName);

                return null;
            }

            return sagaMessage;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to deserialize OutboxMessage {MessageId} of type '{MessageType}'",
                message.Id,
                messageType.FullName);

            return null;
        }
    }
}
