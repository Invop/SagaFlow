namespace SagaFlow.Outbox;
/// <summary>
/// Sends outbox messages to messageBus.
/// </summary>
public interface IPublisher
{
    ValueTask PublishAsync(OutboxMessage message, CancellationToken cancellationToken = default);
}