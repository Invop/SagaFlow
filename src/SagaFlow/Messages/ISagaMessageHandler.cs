namespace SagaFlow.Messages;

public interface ISagaMessageHandler<in TMessage> where TMessage : ISagaMessage
{
    ValueTask HandleAsync(
        ISagaMessageContext<TMessage> context,
        CancellationToken cancellationToken = default);
}