namespace SagaFlow.Sagas;

/// <summary>
///     Provides read-only access to the saga state during event handling.
///     This context is available to saga event handlers to make decisions based on current saga state.
/// </summary>
/// <typeparam name="TState">The type of the saga state</typeparam>
public interface ISagaContext<out TState>
{
    /// <summary>
    ///     Gets the correlation identifier of the saga instance.
    /// </summary>
    Guid CorrelationId { get; }

    /// <summary>
    ///     Gets the current state of the saga (read-only).
    /// </summary>
    TState State { get; }
}