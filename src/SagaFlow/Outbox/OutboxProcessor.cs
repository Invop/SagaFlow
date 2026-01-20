using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SagaFlow.Outbox;

/// <summary>
///     Default implementation of <see cref="IOutboxProcessor"/> that processes outbox messages
///     by retrieving them from the repository and publishing to the message broker.
/// </summary>
/// <remarks>
///     <para>
///     The processor follows the transactional outbox pattern:
///     1. Retrieve pending messages from the outbox repository
///     2. Publish each message to the message broker via <see cref="IPublisher"/>
///     3. Mark messages as processed or failed based on the result
///     </para>
///     <para>
///     Messages are processed with at-least-once semantics. Consumers should implement
///     idempotency using the <see cref="OutboxMessage.IdempotencyKey"/>.
///     </para>
/// </remarks>
internal sealed class OutboxProcessor : IOutboxProcessor
{
    private readonly IOutboxRepository _repository;
    private readonly IPublisher _publisher;
    private readonly OutboxProcessorOptions _options;
    private readonly ILogger<OutboxProcessor> _logger;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    ///     Initializes a new instance of the <see cref="OutboxProcessor"/> class.
    /// </summary>
    /// <param name="repository">The outbox repository for message persistence.</param>
    /// <param name="publisher">The publisher for sending messages to the message broker.</param>
    /// <param name="options">The processor configuration options.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="timeProvider">The time provider for consistent time operations.</param>
    public OutboxProcessor(
        IOutboxRepository repository,
        IPublisher publisher,
        IOptions<OutboxProcessorOptions> options,
        ILogger<OutboxProcessor> logger,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(publisher);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _repository = repository;
        _publisher = publisher;
        _options = options.Value;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async ValueTask ProcessPendingMessagesAsync(CancellationToken cancellationToken = default)
    {
        await ProcessNewMessagesAsync(cancellationToken).ConfigureAwait(false);

        if (_options.ProcessFailedMessages)
        {
            await ProcessFailedMessagesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     Processes new pending messages that have not been attempted yet.
    /// </summary>
    private async Task ProcessNewMessagesAsync(CancellationToken cancellationToken)
    {
        var pendingMessages = await _repository
            .GetPendingMessagesAsync(_options.BatchSize, cancellationToken)
            .ConfigureAwait(false);

        if (pendingMessages.Count == 0)
        {
            _logger.LogDebug("No pending messages to process");
            return;
        }

        _logger.LogInformation("Processing {MessageCount} pending outbox messages", pendingMessages.Count);

        await ProcessMessagesSequentiallyAsync(pendingMessages, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Processes failed messages that are eligible for retry based on configuration.
    /// </summary>
    private async Task ProcessFailedMessagesAsync(CancellationToken cancellationToken)
    {
        var retryDelaySeconds = CalculateRetryDelaySeconds();
        var failedMessages = await _repository
            .GetFailedMessagesForRetryAsync(
                _options.BatchSize,
                _options.MaxRetryCount,
                retryDelaySeconds,
                cancellationToken)
            .ConfigureAwait(false);

        if (failedMessages.Count == 0)
        {
            _logger.LogDebug("No failed messages eligible for retry");
            return;
        }

        _logger.LogInformation("Retrying {MessageCount} failed outbox messages", failedMessages.Count);

        // Failed messages are always processed sequentially to avoid overwhelming the system
        await ProcessMessagesSequentiallyAsync(failedMessages, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Processes messages sequentially, preserving order within the batch.
    /// </summary>
    private async Task ProcessMessagesSequentiallyAsync(
        IReadOnlyList<OutboxMessage> messages,
        CancellationToken cancellationToken)
    {
        foreach (var message in messages)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await ProcessSingleMessageAsync(message, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     Processes a single outbox message by publishing it and updating its status.
    /// </summary>
    private async Task ProcessSingleMessageAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug(
                "Publishing outbox message {MessageId} of type {MessageType}",
                message.Id,
                message.MessageType);

            await _publisher.PublishAsync(message, cancellationToken).ConfigureAwait(false);

            var processedOnUtc = _timeProvider.GetUtcNow();
            await _repository
                .MarkAsProcessedAsync(message.Id, processedOnUtc, cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation(
                "Successfully published outbox message {MessageId}",
                message.Id);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning(
                "Processing of message {MessageId} was cancelled",
                message.Id);
            throw;
        }
        catch (Exception ex)
        {
            await HandleMessageFailureAsync(message, ex, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     Handles a message processing failure by updating its status and potentially moving to dead letter.
    /// </summary>
    private async Task HandleMessageFailureAsync(
        OutboxMessage message,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var newRetryCount = message.RetryCount + 1;

        if (newRetryCount >= _options.MaxRetryCount)
        {
            _logger.LogError(
                exception,
                "Outbox message {MessageId} exceeded max retry count ({MaxRetryCount}). Moving to dead letter",
                message.Id,
                _options.MaxRetryCount);

            await _repository
                .MarkAsDeadLetterAsync(message.Id, cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            _logger.LogWarning(
                exception,
                "Failed to publish outbox message {MessageId}. Retry attempt {RetryCount} of {MaxRetryCount}",
                message.Id,
                newRetryCount,
                _options.MaxRetryCount);

            await _repository
                .MarkAsFailedAsync(message.Id, exception.Message, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     Calculates the minimum retry delay in seconds based on the base delay.
    ///     Uses the base delay for simplicity; the repository will handle exponential backoff.
    /// </summary>
    private int CalculateRetryDelaySeconds() => _options.BaseRetryDelaySeconds;
}
