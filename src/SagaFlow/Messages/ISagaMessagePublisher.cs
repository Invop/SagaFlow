namespace SagaFlow.Messages;

/// <summary>
///     Publisher interface for sending messages through the outbox pattern.
///     Messages are first persisted to the outbox for reliable delivery before being sent to the message broker.
/// </summary>
public interface ISagaMessagePublisher
{
    /// <summary>
    ///     Publishes a saga message through the outbox for reliable delivery.
    /// </summary>
    /// <typeparam name="TMessage">The type of message to publish.</typeparam>
    /// <param name="message">The message to publish.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task PublishAsync<TMessage>(
        TMessage message,
        CancellationToken cancellationToken = default)
        where TMessage : ISagaMessage;
}
