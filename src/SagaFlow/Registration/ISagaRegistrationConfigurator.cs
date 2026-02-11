using SagaFlow.SagaDefinition;

namespace SagaFlow.Registration;

/// <summary>
/// Provides a fluent API for configuring a saga registration.
/// </summary>
/// <typeparam name="TInstance">The saga instance type.</typeparam>
/// <remarks>
/// <para>
/// This configurator allows additional configuration of a saga after registration, such as:
/// </para>
/// <list type="bullet">
///     <item><description>Configuring the saga repository</description></item>
///     <item><description>Setting up saga-specific options</description></item>
///     <item><description>Adding saga observers</description></item>
/// </list>
/// </remarks>
public interface ISagaRegistrationConfigurator<TInstance>
    where TInstance : class, ISagaStateMachineInstance
{
    /// <summary>
    /// Gets the saga descriptor for this registration.
    /// </summary>
    ISagaDescriptor Descriptor { get; }

    /// <summary>
    /// Configures the saga repository to use for persisting saga state.
    /// </summary>
    /// <typeparam name="TRepository">The repository implementation type.</typeparam>
    /// <returns>The configurator for method chaining.</returns>
    ISagaRegistrationConfigurator<TInstance> UseRepository<TRepository>()
        where TRepository : class, ISagaRepository<TInstance>;

    /// <summary>
    /// Configures the saga to use an in-memory repository.
    /// </summary>
    /// <remarks>
    /// The in-memory repository is useful for testing and development scenarios.
    /// For production use, consider a persistent repository implementation.
    /// </remarks>
    /// <returns>The configurator for method chaining.</returns>
    ISagaRegistrationConfigurator<TInstance> InMemoryRepository();
}

/// <summary>
/// Non-generic saga registration configurator for runtime type scenarios.
/// </summary>
public interface ISagaRegistrationConfigurator
{
    /// <summary>
    /// Gets the saga descriptor for this registration.
    /// </summary>
    ISagaDescriptor Descriptor { get; }
}
