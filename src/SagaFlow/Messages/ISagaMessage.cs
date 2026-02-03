using SagaFlow.Attributes;

namespace SagaFlow.Messages;

/// <summary>
///     Base interface for all saga messages.
///     Provides a correlation identifier for message tracking across saga steps.
/// </summary>
public interface ISagaMessage
{
    /// <summary>
    /// Gets the idempotency key associated with the request.
    /// </summary>
    /// <remarks>
    /// This key is used to ensure that the message is processed only once. 
    /// <para>
    /// The value is automatically calculated based on properties marked with 
    /// <see cref="IdempotencyKeyAttribute"/>. If no properties are marked, a random 
    /// unique key is generated to ensure safe processing.
    /// </para>
    /// </remarks>
    string IdempotencyKey { get; }

    /// <summary>
    ///     Gets the correlation identifier that links all messages belonging to the same saga instance.
    /// </summary>
    Guid CorrelationId { get; }

    /// <summary>
    ///     Gets the timestamp when the message was created.
    /// </summary>
    DateTimeOffset Timestamp { get; }
}