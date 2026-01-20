namespace SagaFlow.Outbox;

/// <summary>
///     Defines the contract for processing outbox messages.
/// </summary>
public interface IOutboxProcessor
{
    /// <summary>
    ///     Processes pending outbox messages by publishing them to the message broker.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    ValueTask ProcessPendingMessagesAsync(CancellationToken cancellationToken = default);
}