using Microsoft.Extensions.Logging;
using SagaFlow.Messages;
using SagaFlow.Registration;
using SagaFlow.SagaDefinition.Activities;

namespace SagaFlow.SagaDefinition;

/// <summary>
///     Typed saga executor that orchestrates the full lifecycle of processing a message
///     against a specific saga state machine and instance type.
/// </summary>
/// <typeparam name="TSaga">The saga state machine type.</typeparam>
/// <typeparam name="TInstance">The saga instance type that maintains persistent state.</typeparam>
/// <remarks>
///     <para>
///     This is the core execution engine for a saga. For each incoming message it:
///     </para>
///     <list type="number">
///         <item><description>Loads an existing instance or creates a new one (for initiator messages)</description></item>
///         <item><description>Matches event activities registered for the current state and message type</description></item>
///         <item><description>Executes the activity chain within a <see cref="SagaExecutionContext{TSagaData}"/></description></item>
///         <item><description>On failure before pivot: publishes compensation messages in LIFO order</description></item>
///         <item><description>Persists the updated instance; removes it if the saga is completed</description></item>
///     </list>
/// </remarks>
internal sealed class SagaExecutor<TSaga, TInstance> : ISagaExecutor
    where TSaga : SagaStateMachine<TInstance>
    where TInstance : class, ISagaStateMachineInstance
{
    private readonly TSaga _stateMachine;
    private readonly ISagaRepository<TInstance> _repository;
    private readonly ISagaMessagePublisher _messagePublisher;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SagaExecutor<TSaga, TInstance>> _logger;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SagaExecutor{TSaga, TInstance}"/> class.
    /// </summary>
    /// <param name="stateMachine">The saga state machine definition.</param>
    /// <param name="repository">The repository for persisting saga instances.</param>
    /// <param name="messagePublisher">The publisher for compensation messages.</param>
    /// <param name="serviceProvider">The service provider for activity dependency resolution.</param>
    /// <param name="logger">The logger instance.</param>
    public SagaExecutor(
        TSaga stateMachine,
        ISagaRepository<TInstance> repository,
        ISagaMessagePublisher messagePublisher,
        IServiceProvider serviceProvider,
        ILogger<SagaExecutor<TSaga, TInstance>> logger)
    {
        ArgumentNullException.ThrowIfNull(stateMachine);
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(messagePublisher);
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _stateMachine = stateMachine;
        _repository = repository;
        _messagePublisher = messagePublisher;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask ExecuteAsync(
        ISagaMessage message,
        bool isInitiator,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        var correlationId = message.CorrelationId;
        var messageType = message.GetType();

        _logger.LogDebug(
            "Executing saga {SagaType} for message {MessageType} with CorrelationId {CorrelationId}",
            typeof(TSaga).Name,
            messageType.Name,
            correlationId);

        var instance = await LoadOrCreateInstanceAsync(correlationId, isInitiator, cancellationToken)
            .ConfigureAwait(false);

        if (instance is null)
        {
            _logger.LogWarning(
                "No saga instance found for CorrelationId {CorrelationId} and message {MessageType} is not an initiator. Skipping",
                correlationId,
                messageType.Name);

            return;
        }

        var matchingActivities = FindMatchingActivities(instance, messageType);

        if (matchingActivities.Count == 0)
        {
            _logger.LogWarning(
                "No event activities matched for saga {SagaType} in state '{CurrentState}' for message {MessageType}",
                typeof(TSaga).Name,
                instance.CurrentState,
                messageType.Name);

            return;
        }

        var context = CreateExecutionContext(instance, message);

        try
        {
            await ExecuteActivitiesAsync(matchingActivities, context, cancellationToken)
                .ConfigureAwait(false);

            await PersistInstanceAsync(instance, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "Saga {SagaType} with CorrelationId {CorrelationId} transitioned to state '{CurrentState}'",
                typeof(TSaga).Name,
                correlationId,
                instance.CurrentState);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Saga {SagaType} with CorrelationId {CorrelationId} failed during execution in state '{CurrentState}'",
                typeof(TSaga).Name,
                correlationId,
                instance.CurrentState);

            await ExecuteCompensationsAsync(context, cancellationToken).ConfigureAwait(false);

            // Re-throw so the caller (outbox processor) can track the failure
            throw;
        }
    }

    /// <summary>
    ///     Loads an existing saga instance or creates a new one for initiator messages.
    /// </summary>
    private async ValueTask<TInstance?> LoadOrCreateInstanceAsync(
        Guid correlationId,
        bool isInitiator,
        CancellationToken cancellationToken)
    {
        var existing = await _repository
            .LoadAsync(correlationId, cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            _logger.LogDebug(
                "Loaded existing saga instance {CorrelationId} in state '{CurrentState}'",
                correlationId,
                existing.CurrentState);

            return existing;
        }

        if (!isInitiator)
        {
            return null;
        }

        // Create a new instance using parameterless constructor
        var instance = Activator.CreateInstance<TInstance>();
        instance.CorrelationId = correlationId;

        var stateMachine = (ISagaStateMachine<TInstance>)_stateMachine;
        instance.CurrentState = stateMachine.Initial.Name;

        _logger.LogDebug(
            "Created new saga instance {CorrelationId} in state '{CurrentState}'",
            correlationId,
            instance.CurrentState);

        return instance;
    }

    /// <summary>
    ///     Finds event activities that match the current instance state and message type.
    /// </summary>
    /// <remarks>
    ///     An <see cref="EventActivity{TSagaData}"/> matches when:
    ///     <list type="bullet">
    ///         <item><description>Its <c>MessageType</c> equals the incoming message type</description></item>
    ///         <item><description>Its <c>State</c> is <see langword="null"/> (Initially block)
    ///             and the instance is in the Initial state,
    ///             OR its <c>State.Name</c> matches the instance's current state</description></item>
    ///     </list>
    /// </remarks>
    private IReadOnlyList<EventActivity<TInstance>> FindMatchingActivities(
        TInstance instance,
        Type messageType)
    {
        var allActivities = _stateMachine.GetEventActivitiesForMessage(messageType);
        var currentState = instance.CurrentState;
        var stateMachine = (ISagaStateMachine<TInstance>)_stateMachine;
        var initialStateName = stateMachine.Initial.Name;

        var matching = new List<EventActivity<TInstance>>();
        foreach (var activity in allActivities)
        {
            // Null state means Initially block — matches when instance is in Initial state
            if (activity.State is null && currentState == initialStateName)
            {
                matching.Add(activity);
            }
            else if (activity.State is not null && activity.State.Name == currentState)
            {
                matching.Add(activity);
            }
        }

        return matching;
    }

    /// <summary>
    ///     Creates a <see cref="SagaExecutionContext{TSagaData}"/> for the current message processing.
    /// </summary>
    private SagaExecutionContext<TInstance> CreateExecutionContext(
        TInstance instance,
        ISagaMessage message)
    {
        // Wrap the message in a typed context for activity consumption
        var messageContextType = typeof(SagaMessageContext<>).MakeGenericType(message.GetType());
        var messageContext = Activator.CreateInstance(messageContextType, message, "SagaExecutor")
            ?? throw new InvalidOperationException(
                $"Failed to create message context for type '{message.GetType().Name}'.");

        return new SagaExecutionContext<TInstance>(
            instance,
            message,
            messageContext,
            _serviceProvider);
    }

    /// <summary>
    ///     Executes all matching event activities sequentially.
    /// </summary>
    private static async ValueTask ExecuteActivitiesAsync(
        IReadOnlyList<EventActivity<TInstance>> activities,
        SagaExecutionContext<TInstance> context,
        CancellationToken cancellationToken)
    {
        foreach (var eventActivity in activities)
        {
            await eventActivity.ExecuteAsync(context, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     Persists the saga instance after successful execution.
    ///     If the saga has completed (reached Final state), the instance is removed.
    /// </summary>
    private async ValueTask PersistInstanceAsync(
        TInstance instance,
        CancellationToken cancellationToken)
    {
        var stateMachine = (ISagaStateMachine<TInstance>)_stateMachine;
        var isCompleted = await stateMachine.IsCompleted(instance).ConfigureAwait(false);

        if (isCompleted)
        {
            _logger.LogInformation(
                "Saga {SagaType} with CorrelationId {CorrelationId} completed. Removing instance",
                typeof(TSaga).Name,
                instance.CorrelationId);

            await _repository
                .DeleteAsync(instance.CorrelationId, cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            await _repository
                .SaveAsync(instance, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     Executes compensation messages in LIFO order when a saga step fails before the pivot point.
    /// </summary>
    /// <remarks>
    ///     Compensation messages are published through the outbox to guarantee delivery.
    ///     If compensation itself fails, the error is logged but not re-thrown
    ///     to avoid masking the original exception.
    /// </remarks>
    private async ValueTask ExecuteCompensationsAsync(
        SagaExecutionContext<TInstance> context,
        CancellationToken cancellationToken)
    {
        if (context.IsPivotReached)
        {
            _logger.LogWarning(
                "Saga {SagaType} with CorrelationId {CorrelationId} failed after pivot point. No compensations will be executed",
                typeof(TSaga).Name,
                context.SagaData.CorrelationId);

            return;
        }

        if (context.CompensationMessages.Count == 0)
        {
            _logger.LogDebug(
                "No compensation messages registered for saga {SagaType} with CorrelationId {CorrelationId}",
                typeof(TSaga).Name,
                context.SagaData.CorrelationId);

            return;
        }

        _logger.LogInformation(
            "Executing {CompensationCount} compensation(s) for saga {SagaType} with CorrelationId {CorrelationId}",
            context.CompensationMessages.Count,
            typeof(TSaga).Name,
            context.SagaData.CorrelationId);

        // CompensationMessages are already in LIFO order (inserted at index 0 by CompensateWithActivity)
        foreach (var compensationMessage in context.CompensationMessages)
        {
            try
            {
                await _messagePublisher
                    .PublishAsync(compensationMessage, cancellationToken)
                    .ConfigureAwait(false);

                _logger.LogDebug(
                    "Published compensation message {CompensationType} for CorrelationId {CorrelationId}",
                    compensationMessage.GetType().Name,
                    context.SagaData.CorrelationId);
            }
            catch (Exception ex)
            {
                // Log but do not re-throw — compensation failure should not mask the original error
                _logger.LogError(
                    ex,
                    "Failed to publish compensation message {CompensationType} for CorrelationId {CorrelationId}",
                    compensationMessage.GetType().Name,
                    context.SagaData.CorrelationId);
            }
        }
    }
}
