using SagaFlow.Messages;
using Microsoft.Extensions.DependencyInjection;

namespace SagaFlow.SagaDefinition.Activities;

/// <summary>
/// Activity that sends a message locally (in-process) via ISagaMessageHandler.
/// This bypasses the Outbox and directly invokes the handler.
/// </summary>
/// <typeparam name="TSagaData">The type of saga data.</typeparam>
/// <typeparam name="TMessage">The type of message being processed.</typeparam>
/// <typeparam name="TSendMessage">The type of message to send.</typeparam>
public sealed class SendMessageActivity<TSagaData, TMessage, TSendMessage> : IActivity<TSagaData>
    where TSagaData : class
    where TMessage : ISagaMessage
    where TSendMessage : ISagaMessage
{
    private readonly Func<ISagaMessageContext<TMessage>, TSagaData, TSendMessage> _messageFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="SendMessageActivity{TSagaData, TMessage, TSendMessage}"/> class.
    /// </summary>
    /// <param name="messageFactory">Factory function to create the message to send.</param>
    /// <exception cref="ArgumentNullException">Thrown when messageFactory is null.</exception>
    public SendMessageActivity(Func<ISagaMessageContext<TMessage>, TSagaData, TSendMessage> messageFactory)
    {
        ArgumentNullException.ThrowIfNull(messageFactory);
        _messageFactory = messageFactory;
    }

    /// <inheritdoc/>
    public async ValueTask ExecuteAsync(SagaExecutionContext<TSagaData> context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        var typedContext = (ISagaMessageContext<TMessage>)context.MessageContext;
        var messageToSend = _messageFactory(typedContext, context.SagaData);

        ArgumentNullException.ThrowIfNull(messageToSend, nameof(messageToSend));

        // Get the message handler from DI
        var handler = context.ServiceProvider.GetRequiredService<ISagaMessageHandler<TSendMessage>>();

        // Create message context
        var messageContext = new SagaMessageContext<TSendMessage>(messageToSend);

        // Invoke handler directly (in-process)
        await handler.HandleAsync(messageContext, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// Simple implementation of ISagaMessageContext for in-process message sending.
/// </summary>
internal sealed class SagaMessageContext<TMessage> : ISagaMessageContext<TMessage>
    where TMessage : ISagaMessage
{
    public TMessage Message { get; }

    public Guid CorrelationId => Message.CorrelationId;

    public string MessageId { get; }

    public string SenderId { get; }

    public SagaMessageContext(TMessage message, string? senderId = null)
    {
        Message = message;
        MessageId = Guid.NewGuid().ToString();
        SenderId = senderId ?? "SagaOrchestrator";
    }
}
