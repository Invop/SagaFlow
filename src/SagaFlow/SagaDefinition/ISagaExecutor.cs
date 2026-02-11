using SagaFlow.Messages;

namespace SagaFlow.SagaDefinition;

/// <summary>
///     Executes saga activity chains for a specific saga instance type.
///     Responsible for loading/creating instances, matching event activities
///     to the current state, executing them, handling compensation on failure,
///     and persisting the updated instance.
/// </summary>
/// <remarks>
///     <para>
///     Each registered saga type gets its own <see cref="ISagaExecutor"/> resolved from DI.
///     The <see cref="ISagaRunner"/> bridges the non-generic message processing pipeline
///     to the typed executor using the saga descriptor metadata.
///     </para>
///     <para>
///     The executor follows a strict lifecycle per message:
///     <list type="number">
///         <item><description>Load existing instance (by CorrelationId) or create a new one (for initiator messages)</description></item>
///         <item><description>Find matching <c>EventActivity</c> entries for the current state + message type</description></item>
///         <item><description>Build a <c>SagaExecutionContext</c> and execute activities sequentially</description></item>
///         <item><description>On failure: execute registered compensation messages in LIFO order (if before pivot)</description></item>
///         <item><description>Save the updated instance; delete if saga reached Final state</description></item>
///     </list>
///     </para>
/// </remarks>
public interface ISagaExecutor
{
    /// <summary>
    ///     Executes the saga for the given message.
    /// </summary>
    /// <param name="message">The deserialized saga message.</param>
    /// <param name="isInitiator">
    ///     Whether this message can initiate a new saga instance.
    ///     When <see langword="true"/> and no existing instance is found,
    ///     a new instance is created with the message's CorrelationId.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous execution.</returns>
    ValueTask ExecuteAsync(ISagaMessage message, bool isInitiator, CancellationToken cancellationToken);
}
