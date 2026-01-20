using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SagaFlow.Outbox;

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
}
