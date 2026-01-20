namespace SagaFlow.Outbox;

/// <summary>
///     Repository interface for outbox message persistence.
///     Implementations should ensure transactional consistency with business data.
/// </summary>
public interface IOutboxRepository
{
    /// <summary>
    ///     Adds a message to the outbox.
    ///     This should be called within the same transaction as business data changes.
    /// </summary>
    /// <param name="message">The outbox message to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Adds multiple messages to the outbox in a single operation.
    ///     This should be called within the same transaction as business data changes.
    /// </summary>
    /// <param name="messages">The outbox messages to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddRangeAsync(IEnumerable<OutboxMessage> messages, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Retrieves pending messages that are ready for processing.
    /// </summary>
    /// <param name="batchSize">Maximum number of messages to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of pending outbox messages.</returns>
    Task<IReadOnlyList<OutboxMessage>> GetPendingMessagesAsync(int batchSize, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Retrieves failed messages that are eligible for retry.
    /// </summary>
    /// <param name="batchSize">Maximum number of messages to retrieve.</param>
    /// <param name="maxRetryCount">Maximum retry attempts before considering a message dead.</param>
    /// <param name="retryDelaySeconds">Minimum seconds since last attempt before retry.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of failed outbox messages ready for retry.</returns>
    Task<IReadOnlyList<OutboxMessage>> GetFailedMessagesForRetryAsync(
        int batchSize,
        int maxRetryCount,
        int retryDelaySeconds,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Marks a message as successfully processed.
    /// </summary>
    /// <param name="messageId">The ID of the message to mark as processed.</param>
    /// <param name="processedOnUtc">The timestamp when processing completed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task MarkAsProcessedAsync(Guid messageId, DateTimeOffset processedOnUtc, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Marks a message as failed with an error message.
    /// </summary>
    /// <param name="messageId">The ID of the message that failed.</param>
    /// <param name="error">The error message describing the failure.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task MarkAsFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Marks a message as dead letter when maximum retries are exceeded.
    /// </summary>
    /// <param name="messageId">The ID of the message to mark as dead letter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task MarkAsDeadLetterAsync(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Deletes processed messages older than the specified age.
    ///     Used for periodic cleanup of successfully processed messages.
    /// </summary>
    /// <param name="olderThan">Delete messages processed before this time.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of messages deleted.</returns>
    Task<int> DeleteProcessedMessagesAsync(DateTimeOffset olderThan, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets a message by its unique identifier.
    /// </summary>
    /// <param name="messageId">The message ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The outbox message if found; otherwise, null.</returns>
    Task<OutboxMessage?> GetByIdAsync(Guid messageId, CancellationToken cancellationToken = default);
}
