namespace SagaFlow.Outbox;

/// <summary>
///     Configuration options for the outbox message processor.
///     Controls how messages are retrieved, processed, and retried.
/// </summary>
/// <remarks>
///     <para>
///     The outbox processor uses a polling pattern to retrieve pending messages
///     from the repository and publish them to the message broker.
///     </para>
///     <para>
///     Failed messages are retried with exponential backoff up to <see cref="MaxRetryCount"/> times
///     before being moved to the dead letter queue.
///     </para>
/// </remarks>
/// <example>
///     <code>
///     services.Configure&lt;OutboxProcessorOptions&gt;(options =>
///     {
///         options.BatchSize = 50;
///         options.PollingIntervalSeconds = 5;
///         options.MaxRetryCount = 5;
///     });
///     </code>
/// </example>
public sealed class OutboxProcessorOptions
{
    /// <summary>
    ///     Gets or sets the maximum number of messages to process in a single batch.
    ///     Default is 100.
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    ///     Gets or sets the interval in seconds between polling cycles for pending messages.
    ///     Default is 5 seconds.
    /// </summary>
    public int PollingIntervalSeconds { get; set; } = 5;

    /// <summary>
    ///     Gets or sets the maximum number of retry attempts for failed messages.
    ///     Messages exceeding this count are moved to dead letter.
    ///     Default is 3.
    /// </summary>
    public int MaxRetryCount { get; set; } = 3;

    /// <summary>
    ///     Gets or sets the base delay in seconds before retrying a failed message.
    ///     Actual delay uses exponential backoff: BaseRetryDelaySeconds * 2^(RetryCount-1).
    ///     Default is 30 seconds.
    /// </summary>
    public int BaseRetryDelaySeconds { get; set; } = 30;

    /// <summary>
    ///     Gets or sets a value indicating whether to process failed messages alongside pending messages.
    ///     Default is true.
    /// </summary>
    public bool ProcessFailedMessages { get; set; } = true;

    /// <summary>
    ///     Gets or sets the number of days to retain processed messages before cleanup.
    ///     Default is 7 days.
    /// </summary>
    public int ProcessedMessageRetentionDays { get; set; } = 7;

    /// <summary>
    ///     Gets or sets the interval in hours between cleanup runs for processed messages.
    ///     Default is 24 hours (once per day).
    /// </summary>
    public int CleanupIntervalHours { get; set; } = 24;

    /// <summary>
    ///     Gets or sets a value indicating whether the outbox processor is enabled.
    ///     Default is true.
    /// </summary>
    public bool Enabled { get; set; } = true;

}
