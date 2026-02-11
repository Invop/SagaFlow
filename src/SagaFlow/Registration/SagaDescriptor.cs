using System.Reflection;
using SagaFlow.Messages;
using SagaFlow.SagaDefinition;

namespace SagaFlow.Registration;

/// <summary>
/// Default implementation of <see cref="ISagaDescriptor"/> that describes a saga state machine.
/// </summary>
/// <remarks>
/// <para>
/// This record captures metadata about a saga at registration time, including:
/// </para>
/// <list type="bullet">
///     <item><description>The saga state machine type</description></item>
///     <item><description>The saga instance/state type</description></item>
///     <item><description>Message types that can initiate new instances</description></item>
///     <item><description>All message types the saga handles</description></item>
/// </list>
/// </remarks>
public sealed record SagaDescriptor : ISagaDescriptor
{
    /// <inheritdoc />
    public Type SagaType { get; }

    /// <inheritdoc />
    public Type SagaInstanceType { get; }

    /// <inheritdoc />
    public IReadOnlySet<Type> InitiatorTypes { get; }

    /// <inheritdoc />
    public IReadOnlySet<Type> HandledMessageTypes { get; }

    private SagaDescriptor(
        Type sagaType,
        Type sagaInstanceType,
        IReadOnlySet<Type> initiatorTypes,
        IReadOnlySet<Type> handledMessageTypes)
    {
        SagaType = sagaType;
        SagaInstanceType = sagaInstanceType;
        InitiatorTypes = initiatorTypes;
        HandledMessageTypes = handledMessageTypes;
    }

    /// <summary>
    /// Creates a saga descriptor for a saga state machine with custom state.
    /// </summary>
    /// <typeparam name="TSaga">The saga state machine type.</typeparam>
    /// <typeparam name="TInstance">The saga instance type.</typeparam>
    /// <returns>A new saga descriptor.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the saga type doesn't derive from <see cref="SagaStateMachine{TSagaData}"/>
    /// or the instance type doesn't implement <see cref="ISagaStateMachineInstance"/>.
    /// </exception>
    public static SagaDescriptor Create<TSaga, TInstance>()
        where TSaga : SagaStateMachine<TInstance>
        where TInstance : class, ISagaStateMachineInstance
    {
        return Create(typeof(TSaga), typeof(TInstance));
    }

    /// <summary>
    /// Creates a saga descriptor from types discovered at runtime.
    /// </summary>
    /// <param name="sagaType">The saga state machine type.</param>
    /// <param name="sagaInstanceType">The saga instance type.</param>
    /// <returns>A new saga descriptor.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the types don't satisfy the saga constraints.
    /// </exception>
    public static SagaDescriptor Create(Type sagaType, Type sagaInstanceType)
    {
        ArgumentNullException.ThrowIfNull(sagaType);
        ArgumentNullException.ThrowIfNull(sagaInstanceType);

        ValidateSagaType(sagaType, sagaInstanceType);
        ValidateSagaInstanceType(sagaInstanceType);

        var handledMessageTypes = DiscoverHandledMessageTypes(sagaType);
        var initiatorTypes = DiscoverInitiatorTypes(sagaType, handledMessageTypes);

        if (initiatorTypes.Count == 0)
        {
            throw new ArgumentException(
                $"Saga type '{sagaType.FullName}' does not define any initiator messages. " +
                "At least one message must be handled in the Initially block.",
                nameof(sagaType));
        }

        return new SagaDescriptor(
            sagaType,
            sagaInstanceType,
            initiatorTypes,
            handledMessageTypes);
    }

    /// <summary>
    /// Creates a saga descriptor by discovering the instance type from the saga.
    /// </summary>
    /// <param name="sagaType">The saga state machine type.</param>
    /// <returns>A new saga descriptor.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the saga type is invalid or instance type cannot be determined.
    /// </exception>
    public static SagaDescriptor Create(Type sagaType)
    {
        ArgumentNullException.ThrowIfNull(sagaType);

        var instanceType = GetSagaInstanceType(sagaType);
        return Create(sagaType, instanceType);
    }

    /// <summary>
    /// Gets the saga instance type from a saga state machine type.
    /// </summary>
    /// <param name="sagaType">The saga type to inspect.</param>
    /// <returns>The saga instance type.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the type is not a valid saga state machine.
    /// </exception>
    internal static Type GetSagaInstanceType(Type sagaType)
    {
        var baseType = sagaType.BaseType;
        while (baseType is not null)
        {
            if (baseType.IsGenericType &&
                baseType.GetGenericTypeDefinition() == typeof(SagaStateMachine<>))
            {
                return baseType.GetGenericArguments()[0];
            }

            baseType = baseType.BaseType;
        }

        throw new ArgumentException(
            $"Type '{sagaType.FullName}' does not derive from SagaStateMachine<T>.",
            nameof(sagaType));
    }

    private static void ValidateSagaType(Type sagaType, Type sagaInstanceType)
    {
        // Check that sagaType derives from SagaStateMachine<sagaInstanceType>
        var expectedBaseType = typeof(SagaStateMachine<>).MakeGenericType(sagaInstanceType);

        if (!expectedBaseType.IsAssignableFrom(sagaType))
        {
            throw new ArgumentException(
                $"Saga type '{sagaType.FullName}' must derive from SagaStateMachine<{sagaInstanceType.Name}>.",
                nameof(sagaType));
        }

        if (sagaType.IsAbstract)
        {
            throw new ArgumentException(
                $"Saga type '{sagaType.FullName}' cannot be abstract.",
                nameof(sagaType));
        }
    }

    private static void ValidateSagaInstanceType(Type sagaInstanceType)
    {
        if (!typeof(ISagaStateMachineInstance).IsAssignableFrom(sagaInstanceType))
        {
            throw new ArgumentException(
                $"Saga instance type '{sagaInstanceType.FullName}' must implement {nameof(ISagaStateMachineInstance)}.",
                nameof(sagaInstanceType));
        }

        if (sagaInstanceType.IsAbstract || sagaInstanceType.IsInterface)
        {
            throw new ArgumentException(
                $"Saga instance type '{sagaInstanceType.FullName}' must be a concrete class.",
                nameof(sagaInstanceType));
        }

        // Check for parameterless constructor
        var constructor = sagaInstanceType.GetConstructor(
            BindingFlags.Public | BindingFlags.Instance,
            null,
            Type.EmptyTypes,
            null);

        if (constructor is null)
        {
            throw new ArgumentException(
                $"Saga instance type '{sagaInstanceType.FullName}' must have a public parameterless constructor.",
                nameof(sagaInstanceType));
        }
    }

    /// <summary>
    /// Discovers all message types handled by a saga by analyzing its event handlers.
    /// </summary>
    /// <remarks>
    /// This method uses reflection to find all <c>When&lt;TMessage&gt;()</c> calls in the saga.
    /// The actual message types are extracted from the generic type parameters.
    /// </remarks>
    private static HashSet<Type> DiscoverHandledMessageTypes(Type sagaType)
    {
        var messageTypes = new HashSet<Type>();

        // Look for IEventActivityBinder<TSagaData, TMessage> usages via method return types
        // and ISagaMessage implementations in the assembly
        var sagaInstanceType = GetSagaInstanceType(sagaType);
        var assembly = sagaType.Assembly;

        // Strategy 1: Look for ISagaMessage implementations in the same assembly
        // that might be used by this saga
        var potentialMessageTypes = assembly.GetTypes()
            .Where(t => typeof(ISagaMessage).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
            .ToList();

        // Strategy 2: Look for generic method invocations in the saga type via fields
        // This checks for any fields that reference message types
        foreach (var field in sagaType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
        {
            var fieldType = field.FieldType;
            if (fieldType.IsGenericType)
            {
                foreach (var genericArg in fieldType.GetGenericArguments())
                {
                    if (typeof(ISagaMessage).IsAssignableFrom(genericArg))
                    {
                        messageTypes.Add(genericArg);
                    }
                }
            }
        }

        // Strategy 3: Instantiate the saga and query its registered handlers
        // This is the most reliable way to discover message types
        try
        {
            var saga = Activator.CreateInstance(sagaType);
            if (saga is not null)
            {
                var getEventActivitiesMethod = sagaType.GetMethod(
                    "GetEventActivitiesForMessage",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                if (getEventActivitiesMethod is not null)
                {
                    // Try each potential message type to see if it has handlers
                    foreach (var messageType in potentialMessageTypes)
                    {
                        try
                        {
                            var activities = getEventActivitiesMethod.Invoke(saga, [messageType]);
                            if (activities is System.Collections.ICollection collection && collection.Count > 0)
                            {
                                messageTypes.Add(messageType);
                            }
                        }
                        catch
                        {
                            // Ignore errors for this message type
                        }
                    }
                }
            }
        }
        catch
        {
            // If instantiation fails, we rely on the field analysis
        }

        return messageTypes;
    }

    /// <summary>
    /// Discovers message types that can initiate a new saga instance.
    /// </summary>
    /// <remarks>
    /// Initiator messages are those handled in the Initial state (via Initially block).
    /// </remarks>
    private static HashSet<Type> DiscoverInitiatorTypes(Type sagaType, IReadOnlySet<Type> handledMessageTypes)
    {
        var initiatorTypes = new HashSet<Type>();

        try
        {
            var saga = Activator.CreateInstance(sagaType);
            if (saga is null)
            {
                return initiatorTypes;
            }

            var getEventActivitiesMethod = sagaType.GetMethod(
                "GetEventActivitiesForMessage",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (getEventActivitiesMethod is null)
            {
                return initiatorTypes;
            }

            foreach (var messageType in handledMessageTypes)
            {
                try
                {
                    var activities = getEventActivitiesMethod.Invoke(saga, [messageType]);
                    if (activities is System.Collections.IEnumerable enumerable)
                    {
                        foreach (var activity in enumerable)
                        {
                            // Check if the activity's State is null (meaning Initially block)
                            // or if the state name is "Initial"
                            var stateProperty = activity?.GetType().GetProperty("State");
                            var state = stateProperty?.GetValue(activity);

                            if (state is null)
                            {
                                // Null state means it's in Initially block
                                initiatorTypes.Add(messageType);
                            }
                            else if (state is State sagaState && sagaState.Name == "Initial")
                            {
                                initiatorTypes.Add(messageType);
                            }
                        }
                    }
                }
                catch
                {
                    // Ignore errors for this message type
                }
            }
        }
        catch
        {
            // If we can't determine initiators, return empty
        }

        return initiatorTypes;
    }
}
