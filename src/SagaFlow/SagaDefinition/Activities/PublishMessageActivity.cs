using System.Text.Json;
using SagaFlow.Messages;
using SagaFlow.Outbox;
using Microsoft.Extensions.DependencyInjection;

namespace SagaFlow.SagaDefinition.Activities;

/// <summary>
/// Activity that publishes a message through the Transactional Outbox pattern.
/// </summary>
/// <typeparam name="TSagaData">The type of saga data.</typeparam>
/// <typeparam name="TMessage">The type of message being processed.</typeparam>
/// <typeparam name="TPublishMessage">The type of message to publish.</typeparam>
public sealed class PublishMessageActivity<TSagaData, TMessage, TPublishMessage> : IActivity<TSagaData>
    where TSagaData : class
    where TMessage : ISagaMessage
    where TPublishMessage : ISagaMessage
{
    private readonly Func<ISagaMessageContext<TMessage>, TSagaData, TPublishMessage> _messageFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="PublishMessageActivity{TSagaData, TMessage, TPublishMessage}"/> class.
    /// </summary>
    /// <param name="messageFactory">Factory function to create the message to publish.</param>
    /// <exception cref="ArgumentNullException">Thrown when messageFactory is null.</exception>
    public PublishMessageActivity(Func<ISagaMessageContext<TMessage>, TSagaData, TPublishMessage> messageFactory)
    {
        ArgumentNullException.ThrowIfNull(messageFactory);
        _messageFactory = messageFactory;
    }

    /// <inheritdoc/>
    public async ValueTask ExecuteAsync(SagaExecutionContext<TSagaData> context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        var typedContext = (ISagaMessageContext<TMessage>)context.MessageContext;
        var messageToPublish = _messageFactory(typedContext, context.SagaData);

        ArgumentNullException.ThrowIfNull(messageToPublish, nameof(messageToPublish));

        // Get the outbox repository from DI
        var outboxRepository = context.ServiceProvider.GetRequiredService<IOutboxRepository>();

        // Serialize message to bytes
        var payloadJson = JsonSerializer.Serialize(messageToPublish);
        var payloadBytes = System.Text.Encoding.UTF8.GetBytes(payloadJson);

        // Create outbox message
        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            MessageType = typeof(TPublishMessage).FullName ?? typeof(TPublishMessage).Name,
            Payload = payloadBytes,
            IdempotencyKey = "",//TODO
            CorrelationId = messageToPublish.CorrelationId,
            OccurredOnUtc = DateTimeOffset.UtcNow,
            Status = OutboxMessageStatus.Pending,
            RetryCount = 0
        };

        // Add to outbox (will be committed with saga data transaction)
        await outboxRepository.AddAsync(outboxMessage, cancellationToken).ConfigureAwait(false);
    }
}
