using System.Collections.Concurrent;
using SagaFlow.Messages;

namespace SagaFlow.Registration;

/// <summary>
/// Thread-safe implementation of <see cref="ISagaRegistry"/> for managing saga registrations.
/// </summary>
/// <remarks>
/// <para>
/// This registry maintains efficient lookups for:
/// </para>
/// <list type="bullet">
///     <item><description>Message type to saga descriptors (for routing)</description></item>
///     <item><description>Saga type to descriptor (for direct lookup)</description></item>
///     <item><description>Initiator message types (for new instance creation)</description></item>
/// </list>
/// <para>
/// The registry is designed to be populated at application startup during DI configuration
/// and queried at runtime during message processing. All operations are thread-safe.
/// </para>
/// </remarks>
public sealed class SagaRegistry : ISagaRegistry
{
    private readonly ConcurrentDictionary<Type, ISagaDescriptor> _descriptorsBySagaType = new();
    private readonly ConcurrentDictionary<Type, ConcurrentBag<ISagaDescriptor>> _descriptorsByMessageType = new();
    private readonly ConcurrentDictionary<Type, ConcurrentBag<ISagaDescriptor>> _initiatorsByMessageType = new();

    /// <inheritdoc />
    public IEnumerable<ISagaDescriptor> Descriptors => _descriptorsBySagaType.Values;

    /// <inheritdoc />
    public IEnumerable<Type> RegisteredMessageTypes => _descriptorsByMessageType.Keys;

    /// <summary>
    /// Registers a saga descriptor in the registry.
    /// </summary>
    /// <param name="descriptor">The saga descriptor to register.</param>
    /// <exception cref="ArgumentNullException">Thrown when descriptor is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a saga with the same type is already registered.</exception>
    public void Register(ISagaDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        // Attempt to add the descriptor by saga type
        if (!_descriptorsBySagaType.TryAdd(descriptor.SagaType, descriptor))
        {
            throw new InvalidOperationException(
                $"Saga type '{descriptor.SagaType.FullName}' is already registered. " +
                "Each saga type can only be registered once.");
        }

        // Index by handled message types
        foreach (var messageType in descriptor.HandledMessageTypes)
        {
            var bag = _descriptorsByMessageType.GetOrAdd(messageType, _ => []);
            bag.Add(descriptor);
        }

        // Index initiator types
        foreach (var initiatorType in descriptor.InitiatorTypes)
        {
            var bag = _initiatorsByMessageType.GetOrAdd(initiatorType, _ => []);
            bag.Add(descriptor);
        }
    }

    /// <summary>
    /// Registers a saga by its type.
    /// </summary>
    /// <typeparam name="TSaga">The saga state machine type.</typeparam>
    /// <typeparam name="TInstance">The saga instance type.</typeparam>
    public void Register<TSaga, TInstance>()
        where TSaga : SagaDefinition.SagaStateMachine<TInstance>
        where TInstance : class, SagaDefinition.ISagaStateMachineInstance
    {
        var descriptor = SagaDescriptor.Create<TSaga, TInstance>();
        Register(descriptor);
    }

    /// <summary>
    /// Registers a saga by its types (runtime discovery).
    /// </summary>
    /// <param name="sagaType">The saga state machine type.</param>
    /// <param name="instanceType">The saga instance type.</param>
    public void Register(Type sagaType, Type instanceType)
    {
        var descriptor = SagaDescriptor.Create(sagaType, instanceType);
        Register(descriptor);
    }

    /// <summary>
    /// Registers a saga by discovering its instance type from the saga type.
    /// </summary>
    /// <param name="sagaType">The saga state machine type.</param>
    public void Register(Type sagaType)
    {
        var descriptor = SagaDescriptor.Create(sagaType);
        Register(descriptor);
    }

    /// <inheritdoc />
    public IEnumerable<ISagaDescriptor> ResolveByMessageType(Type messageType)
    {
        ArgumentNullException.ThrowIfNull(messageType);

        return _descriptorsByMessageType.TryGetValue(messageType, out var descriptors)
            ? descriptors.ToArray()
            : [];
    }

    /// <inheritdoc />
    public IEnumerable<ISagaDescriptor> ResolveByMessageType<TMessage>() where TMessage : ISagaMessage
    {
        return ResolveByMessageType(typeof(TMessage));
    }

    /// <inheritdoc />
    public bool IsInitiator(Type messageType)
    {
        ArgumentNullException.ThrowIfNull(messageType);
        return _initiatorsByMessageType.ContainsKey(messageType);
    }

    /// <inheritdoc />
    public IEnumerable<ISagaDescriptor> ResolveInitiators(Type messageType)
    {
        ArgumentNullException.ThrowIfNull(messageType);

        return _initiatorsByMessageType.TryGetValue(messageType, out var descriptors)
            ? descriptors.ToArray()
            : [];
    }

    /// <inheritdoc />
    public ISagaDescriptor? GetDescriptor(Type sagaType)
    {
        ArgumentNullException.ThrowIfNull(sagaType);

        _descriptorsBySagaType.TryGetValue(sagaType, out var descriptor);
        return descriptor;
    }

    /// <inheritdoc />
    public ISagaDescriptor? GetDescriptor<TSaga>()
    {
        return GetDescriptor(typeof(TSaga));
    }

    /// <summary>
    /// Checks if a saga type is already registered.
    /// </summary>
    /// <param name="sagaType">The saga type to check.</param>
    /// <returns>True if the saga is registered; otherwise, false.</returns>
    public bool IsRegistered(Type sagaType)
    {
        ArgumentNullException.ThrowIfNull(sagaType);
        return _descriptorsBySagaType.ContainsKey(sagaType);
    }

    /// <summary>
    /// Checks if a saga type is already registered.
    /// </summary>
    /// <typeparam name="TSaga">The saga type to check.</typeparam>
    /// <returns>True if the saga is registered; otherwise, false.</returns>
    public bool IsRegistered<TSaga>()
    {
        return IsRegistered(typeof(TSaga));
    }

    /// <summary>
    /// Gets the total number of registered sagas.
    /// </summary>
    public int Count => _descriptorsBySagaType.Count;

    /// <summary>
    /// Clears all registered sagas from the registry.
    /// </summary>
    /// <remarks>
    /// This method is primarily intended for testing scenarios.
    /// In production, sagas should be registered once at startup.
    /// </remarks>
    public void Clear()
    {
        _descriptorsBySagaType.Clear();
        _descriptorsByMessageType.Clear();
        _initiatorsByMessageType.Clear();
    }
}
