using Microsoft.Extensions.DependencyInjection;
using SagaFlow.SagaDefinition;

namespace SagaFlow.Registration;

/// <summary>
/// Implementation of <see cref="ISagaRegistrationConfigurator{TInstance}"/> for configuring saga registrations.
/// </summary>
/// <typeparam name="TSaga">The saga state machine type.</typeparam>
/// <typeparam name="TInstance">The saga instance type.</typeparam>
internal sealed class SagaRegistrationConfigurator<TSaga, TInstance> : ISagaRegistrationConfigurator<TInstance>
    where TSaga : SagaStateMachine<TInstance>
    where TInstance : class, ISagaStateMachineInstance
{
    private readonly IServiceCollection _services;

    /// <inheritdoc />
    public ISagaDescriptor Descriptor { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SagaRegistrationConfigurator{TSaga, TInstance}"/> class.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="descriptor">The saga descriptor.</param>
    internal SagaRegistrationConfigurator(IServiceCollection services, ISagaDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(descriptor);

        _services = services;
        Descriptor = descriptor;
    }

    /// <inheritdoc />
    public ISagaRegistrationConfigurator<TInstance> UseRepository<TRepository>()
        where TRepository : class, ISagaRepository<TInstance>
    {
        // Remove any existing repository registration for this type
        var existingDescriptor = _services.FirstOrDefault(
            d => d.ServiceType == typeof(ISagaRepository<TInstance>));

        if (existingDescriptor is not null)
        {
            _services.Remove(existingDescriptor);
        }

        _services.AddScoped<ISagaRepository<TInstance>, TRepository>();
        return this;
    }

    /// <inheritdoc />
    public ISagaRegistrationConfigurator<TInstance> InMemoryRepository()
    {
        // Remove any existing repository registration for this type
        var existingDescriptor = _services.FirstOrDefault(
            d => d.ServiceType == typeof(ISagaRepository<TInstance>));

        if (existingDescriptor is not null)
        {
            _services.Remove(existingDescriptor);
        }

        _services.AddSingleton<ISagaRepository<TInstance>, InMemorySagaRepository<TInstance>>();
        return this;
    }
}

/// <summary>
/// Non-generic implementation of saga registration configurator.
/// </summary>
internal sealed class SagaRegistrationConfigurator : ISagaRegistrationConfigurator
{
    /// <inheritdoc />
    public ISagaDescriptor Descriptor { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SagaRegistrationConfigurator"/> class.
    /// </summary>
    /// <param name="descriptor">The saga descriptor.</param>
    internal SagaRegistrationConfigurator(ISagaDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        Descriptor = descriptor;
    }
}
