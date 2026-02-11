using SagaFlow.Messages;
using SagaFlow.Registration;

namespace SagaFlow.SagaDefinition;

/// <summary>
///     Orchestrates saga execution by resolving the correct typed <see cref="ISagaExecutor"/>
///     for a given saga descriptor and delegating message processing to it.
/// </summary>
/// <remarks>
///     <para>
///     The runner is the bridge between the non-generic <see cref="ISagaMessageProcessor"/>
///     (which works with raw <c>OutboxMessage</c> payloads) and the generic saga execution pipeline.
///     </para>
///     <para>
///     For each message it:
///     <list type="number">
///         <item><description>Determines whether the message is an initiator for the saga</description></item>
///         <item><description>Resolves the typed <see cref="ISagaExecutor"/> from the DI container</description></item>
///         <item><description>Delegates execution to the executor</description></item>
///     </list>
///     </para>
/// </remarks>
public interface ISagaRunner
{
    /// <summary>
    ///     Runs the saga described by <paramref name="descriptor"/> for the given <paramref name="message"/>.
    /// </summary>
    /// <param name="descriptor">Metadata about the target saga (types, initiators, handled messages).</param>
    /// <param name="message">The deserialized saga message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous execution.</returns>
    ValueTask RunAsync(
        ISagaDescriptor descriptor,
        ISagaMessage message,
        CancellationToken cancellationToken);
}
