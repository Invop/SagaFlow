namespace SagaFlow.Messages;

/// <summary>
///     Publisher interface for sending messages through the outbox pattern.
///     Messages are first persisted to the outbox for reliable delivery before being sent to the message broker.
/// </summary>
public interface ISagaMessagePublisher
{
    /// <summary>
    ///     Publishes a saga command through the outbox for reliable delivery.
    /// </summary>
    /// <typeparam name="TCommand">The type of command to publish.</typeparam>
    /// <param name="command">The command to publish.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task PublishCommandAsync<TCommand>(
        TCommand command,
        CancellationToken cancellationToken = default)
        where TCommand : ISagaCommand;

    /// <summary>
    ///     Publishes a saga event through the outbox for reliable delivery.
    /// </summary>
    /// <typeparam name="TEvent">The type of event to publish.</typeparam>
    /// <param name="event">The event to publish.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task PublishEventAsync<TEvent>(
        TEvent @event,
        CancellationToken cancellationToken = default)
        where TEvent : ISagaEvent;
}
