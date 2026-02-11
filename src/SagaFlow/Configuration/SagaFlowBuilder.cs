using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SagaFlow.Outbox;
using SagaFlow.Registration;
using SagaFlow.SagaDefinition;

namespace SagaFlow.Configuration;

/// <summary>
///     Builder for configuring SagaFlow outbox services.
///     Provides a fluent API for customizing outbox behavior.
/// </summary>
/// <remarks>
///     <para>
///     Use this builder to configure which implementations to use for
///     the outbox repository and message publisher.
///     </para>
///     <para>
///     The <see cref="IPublisher"/> must be registered by a transport-specific library
///     (e.g., SagaFlow.RabbitMQ, SagaFlow.Kafka, SagaFlow.InMemory).
///     </para>
/// </remarks>
/// <example>
///     <code>
///     services.AddSagaFlow(builder =>
///     {
///         builder.ConfigureOutbox(options =>
///         {
///             options.BatchSize = 50;
///             options.MaxRetryCount = 5;
///         });
///         builder.UseOutboxRepository&lt;SqlServerOutboxRepository&gt;();
///     });
///     </code>
/// </example>
public sealed class SagaFlowBuilder
{
    /// <summary>
    ///     Gets the service collection being configured.
    /// </summary>
    public IServiceCollection Services { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SagaFlowBuilder"/> class.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    internal SagaFlowBuilder(IServiceCollection services)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
    }

    /// <summary>
    ///     Configures the outbox processor options.
    /// </summary>
    /// <param name="configure">The action to configure the options.</param>
    /// <returns>The builder for method chaining.</returns>
    public SagaFlowBuilder ConfigureOutbox(Action<OutboxProcessorOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        Services.Configure(configure);
        return this;
    }

    /// <summary>
    ///     Registers a custom outbox repository implementation.
    /// </summary>
    /// <typeparam name="TRepository">The repository implementation type.</typeparam>
    /// <returns>The builder for method chaining.</returns>
    public SagaFlowBuilder UseOutboxRepository<TRepository>()
        where TRepository : class, IOutboxRepository
    {
        Services.RemoveAll<IOutboxRepository>();
        Services.AddScoped<IOutboxRepository, TRepository>();
        return this;
    }

    /// <summary>
    ///     Registers a custom outbox repository implementation using a factory.
    /// </summary>
    /// <typeparam name="TRepository">The repository implementation type.</typeparam>
    /// <param name="factory">The factory function to create the repository.</param>
    /// <returns>The builder for method chaining.</returns>
    public SagaFlowBuilder UseOutboxRepository<TRepository>(
        Func<IServiceProvider, TRepository> factory)
        where TRepository : class, IOutboxRepository
    {
        ArgumentNullException.ThrowIfNull(factory);
        Services.RemoveAll<IOutboxRepository>();
        Services.AddScoped<IOutboxRepository>(factory);
        return this;
    }

    /// <summary>
    ///     Registers a custom message publisher implementation.
    /// </summary>
    /// <typeparam name="TPublisher">The publisher implementation type.</typeparam>
    /// <returns>The builder for method chaining.</returns>
    /// <remarks>
    ///     The IPublisher sends messages to the actual message broker (RabbitMQ, Kafka, etc.).
    ///     This must be implemented by a transport-specific library.
    /// </remarks>
    public SagaFlowBuilder UsePublisher<TPublisher>()
        where TPublisher : class, IPublisher
    {
        Services.RemoveAll<IPublisher>();
        Services.AddSingleton<IPublisher, TPublisher>();
        return this;
    }

    /// <summary>
    ///     Registers a custom message publisher implementation using a factory.
    /// </summary>
    /// <typeparam name="TPublisher">The publisher implementation type.</typeparam>
    /// <param name="factory">The factory function to create the publisher.</param>
    /// <returns>The builder for method chaining.</returns>
    public SagaFlowBuilder UsePublisher<TPublisher>(
        Func<IServiceProvider, TPublisher> factory)
        where TPublisher : class, IPublisher
    {
        ArgumentNullException.ThrowIfNull(factory);
        Services.RemoveAll<IPublisher>();
        Services.AddSingleton<IPublisher>(factory);
        return this;
    }

    /// <summary>
    ///     Disables the outbox background processor.
    ///     Useful for testing or when using an external processor.
    /// </summary>
    /// <returns>The builder for method chaining.</returns>
    public SagaFlowBuilder DisableOutboxProcessor()
    {
        Services.Configure<OutboxProcessorOptions>(options => options.Enabled = false);
        return this;
    }

    /// <summary>
    ///     Registers a saga state machine with the SagaFlow orchestrator.
    /// </summary>
    /// <typeparam name="TSaga">The saga state machine type.</typeparam>
    /// <typeparam name="TInstance">The saga instance type that maintains saga state.</typeparam>
    /// <returns>A configurator for additional saga configuration.</returns>
    /// <remarks>
    ///     <para>
    ///     Use this method to register orchestration-based sagas that coordinate distributed transactions.
    ///     The saga must derive from <see cref="SagaStateMachine{TSagaData}"/> and define:
    ///     </para>
    ///     <list type="bullet">
    ///         <item><description>States using the <c>State()</c> method</description></item>
    ///         <item><description>Initiator messages in the <c>Initially()</c> block</description></item>
    ///         <item><description>State transitions using <c>During()</c> and <c>When()</c></description></item>
    ///         <item><description>Compensation logic using <c>CompensateWith()</c></description></item>
    ///     </list>
    /// </remarks>
    /// <example>
    ///     <code>
    ///     services.AddSagaFlow(builder =>
    ///     {
    ///         builder.AddSagaStateMachine&lt;OrderSaga, OrderSagaState&gt;()
    ///             .InMemoryRepository();
    ///     });
    ///     </code>
    /// </example>
    public ISagaRegistrationConfigurator<TInstance> AddSagaStateMachine<TSaga, TInstance>()
        where TSaga : SagaStateMachine<TInstance>
        where TInstance : class, ISagaStateMachineInstance
    {
        // Register the saga state machine as a singleton
        Services.TryAddSingleton<TSaga>();
        Services.TryAddSingleton<SagaStateMachine<TInstance>>(sp => sp.GetRequiredService<TSaga>());
        Services.TryAddSingleton<ISagaStateMachine<TInstance>>(sp => sp.GetRequiredService<TSaga>());

        // Register the typed executor so ISagaRunner can resolve it at runtime
        Services.AddScoped<ISagaExecutor, SagaExecutor<TSaga, TInstance>>();

        // Create and register the descriptor
        var descriptor = SagaDescriptor.Create<TSaga, TInstance>();

        // Get or create the registry and register this saga
        EnsureSagaRegistry();
        var registry = GetSagaRegistry();
        if (!registry.IsRegistered(typeof(TSaga)))
        {
            registry.Register(descriptor);
        }

        return new SagaRegistrationConfigurator<TSaga, TInstance>(Services, descriptor);
    }

    /// <summary>
    ///     Registers a saga state machine by type (runtime discovery).
    /// </summary>
    /// <param name="sagaType">The saga state machine type.</param>
    /// <returns>A non-generic configurator for the saga.</returns>
    /// <exception cref="ArgumentNullException">Thrown when sagaType is null.</exception>
    /// <exception cref="ArgumentException">Thrown when sagaType is not a valid saga state machine.</exception>
    public ISagaRegistrationConfigurator AddSagaStateMachine(Type sagaType)
    {
        ArgumentNullException.ThrowIfNull(sagaType);

        var instanceType = SagaDescriptor.GetSagaInstanceType(sagaType);
        return AddSagaStateMachine(sagaType, instanceType);
    }

    /// <summary>
    ///     Registers a saga state machine by type with explicit instance type.
    /// </summary>
    /// <param name="sagaType">The saga state machine type.</param>
    /// <param name="instanceType">The saga instance type.</param>
    /// <returns>A non-generic configurator for the saga.</returns>
    public ISagaRegistrationConfigurator AddSagaStateMachine(Type sagaType, Type instanceType)
    {
        ArgumentNullException.ThrowIfNull(sagaType);
        ArgumentNullException.ThrowIfNull(instanceType);

        // Register the saga using open generics
        var sagaStateMachineType = typeof(SagaStateMachine<>).MakeGenericType(instanceType);
        var interfaceType = typeof(ISagaStateMachine<>).MakeGenericType(instanceType);

        Services.TryAddSingleton(sagaType);
        Services.TryAddSingleton(sagaStateMachineType, sp => sp.GetRequiredService(sagaType));
        Services.TryAddSingleton(interfaceType, sp => sp.GetRequiredService(sagaType));

        // Register the typed executor so ISagaRunner can resolve it at runtime
        var executorType = typeof(SagaExecutor<,>).MakeGenericType(sagaType, instanceType);
        Services.AddScoped(typeof(ISagaExecutor), executorType);

        // Create and register the descriptor
        var descriptor = SagaDescriptor.Create(sagaType, instanceType);

        EnsureSagaRegistry();
        var registry = GetSagaRegistry();
        if (!registry.IsRegistered(sagaType))
        {
            registry.Register(descriptor);
        }

        return new SagaRegistrationConfigurator(descriptor);
    }

    /// <summary>
    ///     Scans assemblies for saga state machines and registers them automatically.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan. If empty, scans the calling assembly.</param>
    /// <returns>The builder for method chaining.</returns>
    /// <remarks>
    ///     <para>
    ///     This method discovers all concrete classes that derive from <see cref="SagaStateMachine{TSagaData}"/>
    ///     in the specified assemblies and registers them with the orchestrator.
    ///     </para>
    ///     <para>
    ///     Sagas discovered this way will use the default in-memory repository unless
    ///     a custom repository is configured separately.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    ///     services.AddSagaFlow(builder =>
    ///     {
    ///         builder.AddSagaStateMachinesFromAssembly(typeof(OrderSaga).Assembly);
    ///     });
    ///     </code>
    /// </example>
    public SagaFlowBuilder AddSagaStateMachinesFromAssembly(params Assembly[] assemblies)
    {
        if (assemblies.Length == 0)
        {
            assemblies = [Assembly.GetCallingAssembly()];
        }

        foreach (var assembly in assemblies)
        {
            var sagaTypes = assembly.GetTypes()
                .Where(IsSagaStateMachineType)
                .ToList();

            foreach (var sagaType in sagaTypes)
            {
                AddSagaStateMachine(sagaType);
            }
        }

        return this;
    }

    /// <summary>
    ///     Scans assemblies for saga state machines in the specified namespace.
    /// </summary>
    /// <typeparam name="TMarker">A type in the namespace to scan.</typeparam>
    /// <returns>The builder for method chaining.</returns>
    public SagaFlowBuilder AddSagaStateMachinesFromNamespaceContaining<TMarker>()
    {
        return AddSagaStateMachinesFromNamespaceContaining(typeof(TMarker));
    }

    /// <summary>
    ///     Scans assemblies for saga state machines in the specified namespace.
    /// </summary>
    /// <param name="markerType">A type in the namespace to scan.</param>
    /// <returns>The builder for method chaining.</returns>
    public SagaFlowBuilder AddSagaStateMachinesFromNamespaceContaining(Type markerType)
    {
        ArgumentNullException.ThrowIfNull(markerType);

        var targetNamespace = markerType.Namespace;
        if (string.IsNullOrEmpty(targetNamespace))
        {
            throw new ArgumentException(
                $"Type '{markerType.FullName}' does not have a namespace.",
                nameof(markerType));
        }

        var sagaTypes = markerType.Assembly.GetTypes()
            .Where(t => IsSagaStateMachineType(t) &&
                        t.Namespace is not null &&
                        t.Namespace.StartsWith(targetNamespace, StringComparison.Ordinal))
            .ToList();

        foreach (var sagaType in sagaTypes)
        {
            AddSagaStateMachine(sagaType);
        }

        return this;
    }

    private static bool IsSagaStateMachineType(Type type)
    {
        if (type.IsAbstract || type.IsInterface || !type.IsClass)
        {
            return false;
        }

        // Check if the type derives from SagaStateMachine<>
        var baseType = type.BaseType;
        while (baseType is not null)
        {
            if (baseType.IsGenericType &&
                baseType.GetGenericTypeDefinition() == typeof(SagaStateMachine<>))
            {
                return true;
            }

            baseType = baseType.BaseType;
        }

        return false;
    }

    private void EnsureSagaRegistry()
    {
        Services.TryAddSingleton<SagaRegistry>();
        Services.TryAddSingleton<ISagaRegistry>(sp => sp.GetRequiredService<SagaRegistry>());
    }

    private SagaRegistry GetSagaRegistry()
    {
        // Try to get existing registry from services
        var existingDescriptor = Services.FirstOrDefault(
            d => d.ServiceType == typeof(SagaRegistry) &&
                 d.ImplementationInstance is not null);

        if (existingDescriptor?.ImplementationInstance is SagaRegistry existingRegistry)
        {
            return existingRegistry;
        }

        // Create a new registry instance and register it
        var registry = new SagaRegistry();

        // Remove existing registration if any
        var toRemove = Services.Where(d => d.ServiceType == typeof(SagaRegistry)).ToList();
        foreach (var descriptor in toRemove)
        {
            Services.Remove(descriptor);
        }

        Services.AddSingleton(registry);
        Services.AddSingleton<ISagaRegistry>(registry);

        return registry;
    }
}
