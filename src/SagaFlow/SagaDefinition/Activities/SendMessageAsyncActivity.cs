using SagaFlow.Messages;
using Microsoft.Extensions.DependencyInjection;

namespace SagaFlow.SagaDefinition.Activities;

/// <summary>
/// Activity that sends a message asynchronously locally (in-process) via ISagaMessageHandler.
/// This bypasses the Outbox and directly invokes the handler.
/// </summary>
/// <typeparam name="TSagaData">The type of saga data.</typeparam>
/// <typeparam name="TMessage">The type of message being processed.</typeparam>
/// <typeparam name="TSendMessage">The type of message to send.</typeparam>
public sealed class SendMessageAsyncActivity<TSagaData, TMessage, TSendMessage> : IActivity<TSagaData>
    where TSagaData : class
    where TMessage : ISagaMessage
    where TSendMessage : ISagaMessage
{
    private readonly Func<ISagaMessageContext<TMessage>, TSagaData, CancellationToken, ValueTask<TSendMessage>> _asyncMessageFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="SendMessageAsyncActivity{TSagaData, TMessage, TSendMessage}"/> class.
    /// </summary>
    /// <param name="asyncMessageFactory">Async factory function to create the message to send.</param>
    /// <exception cref="ArgumentNullException">Thrown when asyncMessageFactory is null.</exception>
    public SendMessageAsyncActivity(
        Func<ISagaMessageContext<TMessage>, TSagaData, CancellationToken, ValueTask<TSendMessage>> asyncMessageFactory)
    {
        ArgumentNullException.ThrowIfNull(asyncMessageFactory);
        _asyncMessageFactory = asyncMessageFactory;
    }

    /// <inheritdoc/>
    public async ValueTask ExecuteAsync(SagaExecutionContext<TSagaData> context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        var typedContext = (ISagaMessageContext<TMessage>)context.MessageContext;
        var messageToSend = await _asyncMessageFactory(typedContext, context.SagaData, cancellationToken).ConfigureAwait(false);

        ArgumentNullException.ThrowIfNull(messageToSend, nameof(messageToSend));

        // Get the message handler from DI
        var handler = context.ServiceProvider.GetRequiredService<ISagaMessageHandler<TSendMessage>>();

        // Create message context
        var messageContext = new SagaMessageContext<TSendMessage>(messageToSend);

        // Invoke handler directly (in-process)
        await handler.HandleAsync(messageContext, cancellationToken).ConfigureAwait(false);
    }
}
