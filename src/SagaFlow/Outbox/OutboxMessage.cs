namespace SagaFlow.Outbox;

/// <summary>
///     Represents a message stored in the outbox for reliable delivery.
///     Messages are persisted in the same transaction as business data to ensure atomicity.
/// </summary>
public sealed class OutboxMessage
{
    /// <summary>
    ///     Gets the unique identifier for this outbox message.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    ///     Gets the correlation ID linking this message to a saga instance.
    /// </summary>
    public required Guid CorrelationId { get; init; }

    /// <summary>
    ///     Gets the idempotency key to ensure the message is processed only once.
    ///     Consumers should use this key to detect and skip duplicate processing.
    /// </summary>
    public required string IdempotencyKey { get; init; }

    /// <summary>
    ///     Gets the fully qualified type name of the message payload.
    /// </summary>
    public required string MessageType { get; init; }

    /// <summary>
    ///     Gets the serialized message payload.
    /// </summary>
    public required byte[] Payload { get; init; }

    /// <summary>
    ///     Gets the timestamp when the message was created.
    /// </summary>
    public required DateTimeOffset OccurredOnUtc { get; init; }

    /// <summary>
    ///     Gets or sets the timestamp when the message was successfully processed.
    /// </summary>
    public DateTimeOffset? ProcessedOnUtc { get; set; }

    /// <summary>
    ///     Gets or sets the current processing status of the message.
    /// </summary>
    public OutboxMessageStatus Status { get; set; } = OutboxMessageStatus.Pending;

    /// <summary>
    ///     Gets or sets the number of retry attempts that have been made.
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    ///     Gets or sets the error message from the last failed processing attempt.
    /// </summary>
    public string? LastError { get; set; }

    /// <summary>
    ///     Gets or sets the timestamp of the last processing attempt.
    /// </summary>
    public DateTimeOffset? LastAttemptOnUtc { get; set; }
}
