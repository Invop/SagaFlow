namespace SagaFlow.Messages;

/// <summary>
///     Base interface for all saga messages.
///     Provides a correlation identifier for message tracking across saga steps.
/// </summary>
public interface ISagaMessage
{
    /// <summary>
    ///     Gets the idempotency key to ensure the command is processed only once.
    /// </summary>
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