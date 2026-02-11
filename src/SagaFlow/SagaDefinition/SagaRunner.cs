using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SagaFlow.Messages;
using SagaFlow.Registration;

namespace SagaFlow.SagaDefinition;

/// <summary>
///     Default implementation of <see cref="ISagaRunner"/> that resolves typed
///     <see cref="ISagaExecutor"/> instances from the DI container and delegates execution.
/// </summary>
/// <remarks>
///     <para>
///     The runner uses <see cref="ISagaDescriptor.SagaInstanceType"/> to construct the
///     closed generic type <c>ISagaExecutor</c> at runtime and resolve it from DI.
///     Each saga type has its own executor registered at startup via
///     <see cref="Configuration.SagaFlowBuilder.AddSagaStateMachine{TSaga,TInstance}"/>.
///     </para>
/// </remarks>
internal sealed class SagaRunner(
    IServiceProvider serviceProvider,
    ILogger<SagaRunner> logger) : ISagaRunner
{
    /// <inheritdoc />
    public async ValueTask RunAsync(
        ISagaDescriptor descriptor,
        ISagaMessage message,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(message);

        var messageType = message.GetType();
        var isInitiator = descriptor.InitiatorTypes.Contains(messageType);

        logger.LogDebug(
            "SagaRunner dispatching message {MessageType} to saga {SagaType} (IsInitiator={IsInitiator})",
            messageType.Name,
            descriptor.SagaType.Name,
            isInitiator);

        var executor = ResolveExecutor(descriptor);

        await executor
            .ExecuteAsync(message, isInitiator, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    ///     Resolves the typed <see cref="ISagaExecutor"/> for the given saga descriptor.
    ///     The executor is registered as <c>SagaExecutor&lt;TSaga, TInstance&gt;</c> keyed
    ///     by the saga type at startup time.
    /// </summary>
    private ISagaExecutor ResolveExecutor(ISagaDescriptor descriptor)
    {
        // Resolve ISagaExecutor that was registered for this specific saga type at startup
        // via SagaFlowBuilder.AddSagaStateMachine<TSaga, TInstance>()
        var executors = serviceProvider.GetServices<ISagaExecutor>();

        foreach (var executor in executors)
        {
            // Match by checking the closed generic type arguments
            var executorType = executor.GetType();
            if (executorType.IsGenericType &&
                executorType.GetGenericTypeDefinition() == typeof(SagaExecutor<,>))
            {
                var typeArgs = executorType.GetGenericArguments();
                if (typeArgs[0] == descriptor.SagaType && typeArgs[1] == descriptor.SagaInstanceType)
                {
                    return executor;
                }
            }
        }

        throw new InvalidOperationException(
            $"No ISagaExecutor registered for saga type '{descriptor.SagaType.FullName}'. " +
            $"Ensure the saga is registered via AddSagaStateMachine<{descriptor.SagaType.Name}, {descriptor.SagaInstanceType.Name}>().");
    }
}
