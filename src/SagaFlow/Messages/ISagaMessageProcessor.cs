using SagaFlow.Outbox;

namespace SagaFlow.Messages;

public interface ISagaMessageProcessor
{
    ValueTask ProcessAsync(OutboxMessage message, CancellationToken cancellationToken = default);
}
