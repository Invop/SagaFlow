namespace SagaFlow.Outbox;

/// <summary>
///     Represents the processing status of an outbox message.
/// </summary>
public enum OutboxMessageStatus
{
    /// <summary>
    ///     Message is pending and waiting to be processed.
    /// </summary>
    Pending = 0,

    /// <summary>
    ///     Message is currently being processed.
    /// </summary>
    Processing = 1,

    /// <summary>
    ///     Message has been successfully processed and delivered.
    /// </summary>
    Processed = 2,

    /// <summary>
    ///     Message processing failed and will be retried.
    /// </summary>
    Failed = 3,

    /// <summary>
    ///     Message has exceeded maximum retry attempts and is considered dead.
    /// </summary>
    DeadLetter = 4
}
