using Microsoft.Extensions.Logging;
using SagaFlow.Outbox;
using SagaFlow.Utils;

namespace SagaFlow.Messages;

/// <summary>
///     Default implementation of <see cref="ISagaMessagePublisher"/> that stores messages
///     in the outbox for reliable delivery following the transactional outbox pattern.
/// </summary>
/// <remarks>
///     <para>
///     Messages published through this class are first persisted to the outbox repository.
///     A background processor (<see cref="IOutboxProcessor"/>) later retrieves these messages
///     and publishes them to the actual message broker.
///     </para>
///     <para>
///     This ensures at-least-once delivery semantics. To achieve exactly-once processing,
///     consumers should use the <see cref="ISagaMessage.IdempotencyKey"/> for deduplication.
///     </para>
/// </remarks>
internal sealed class SagaMessagePublisher : ISagaMessagePublisher
{
    private readonly IOutboxRepository _outboxRepository;
    private readonly ISerializer _serializer;
    private readonly ILogger<SagaMessagePublisher> _logger;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SagaMessagePublisher"/> class.
    /// </summary>
    /// <param name="outboxRepository">The outbox repository for message persistence.</param>
    /// <param name="serializer">The serializer for message payloads.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="timeProvider">The time provider for consistent time operations.</param>
    public SagaMessagePublisher(
        IOutboxRepository outboxRepository,
        ISerializer serializer,
        ILogger<SagaMessagePublisher> logger,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(outboxRepository);
        ArgumentNullException.ThrowIfNull(serializer);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _outboxRepository = outboxRepository;
        _serializer = serializer;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public Task PublishCommandAsync<TCommand>(
        TCommand command,
        CancellationToken cancellationToken = default)
        where TCommand : ISagaCommand
    {
        ArgumentNullException.ThrowIfNull(command);
        return PublishMessageInternalAsync(command, cancellationToken);
    }

    /// <inheritdoc />
    public Task PublishEventAsync<TEvent>(
        TEvent @event,
        CancellationToken cancellationToken = default)
        where TEvent : ISagaEvent
    {
        ArgumentNullException.ThrowIfNull(@event);
        return PublishMessageInternalAsync(@event, cancellationToken);
    }

    /// <summary>
    ///     Internal method to publish a single message to the outbox.
    /// </summary>
    private async Task PublishMessageInternalAsync<TMessage>(
        TMessage message,
        CancellationToken cancellationToken)
        where TMessage : ISagaMessage
    {
        var outboxMessage = CreateOutboxMessage(message);
        await _outboxRepository
            .AddAsync(outboxMessage, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    ///     Creates an outbox message from a saga message.
    /// </summary>
    private OutboxMessage CreateOutboxMessage<TMessage>(TMessage message)
        where TMessage : ISagaMessage
    {
        var messageType = message.GetType();

        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            CorrelationId = message.CorrelationId,
            IdempotencyKey = message.IdempotencyKey,
            MessageType = messageType.AssemblyQualifiedName ?? messageType.FullName ?? messageType.Name,
            Payload = _serializer.Serialize(message),
            OccurredOnUtc = _timeProvider.GetUtcNow(),
            Status = OutboxMessageStatus.Pending,
            RetryCount = 0
        };
    }
}
