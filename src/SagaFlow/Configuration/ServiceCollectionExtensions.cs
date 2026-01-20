using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SagaFlow.Messages;
using SagaFlow.Outbox;
using SagaFlow.Utils;

namespace SagaFlow.Configuration;

/// <summary>
///     Extension methods for configuring SagaFlow services in the dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Adds SagaFlow core services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>A builder for further configuration.</returns>
    /// <remarks>
    ///     <para>
    ///     This method registers the following services:
    ///     <list type="bullet">
    ///         <item><description><see cref="ISagaMessagePublisher"/> - For publishing messages to the outbox</description></item>
    ///         <item><description><see cref="IOutboxProcessor"/> - For processing outbox messages</description></item>
    ///         <item><description><see cref="ISerializer"/> - Default JSON serializer (can be overridden)</description></item>
    ///         <item><description>Background services for outbox processing and cleanup</description></item>
    ///     </list>
    ///     </para>
    ///     <para>
    ///     You must provide implementations for <see cref="IOutboxRepository"/> and <see cref="IPublisher"/>
    ///     either through the builder or by registering them separately.
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
    ///         builder.UsePublisher&lt;RabbitMqPublisher&gt;();
    ///     });
    ///     </code>
    /// </example>
    /// <param name="configure">Optional action to configure the SagaFlow services.</param>
    public static SagaFlowBuilder AddSagaFlow(
        this IServiceCollection services,
        Action<SagaFlowBuilder>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register core services
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<ISerializer, JsonSerializer>();

        // Register outbox processing services
        services.TryAddScoped<IOutboxProcessor, OutboxProcessor>();
        services.TryAddScoped<ISagaMessagePublisher, SagaMessagePublisher>();

        // Register default options
        services.AddOptions<OutboxProcessorOptions>();

        // Register background services
        services.AddHostedService<OutboxBackgroundService>();
        services.AddHostedService<OutboxCleanupService>();

        // Create builder and apply configuration
        var builder = new SagaFlowBuilder(services);
        configure?.Invoke(builder);

        return builder;
    }

    /// <summary>
    ///     Adds SagaFlow services with a custom time provider.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="timeProvider">The time provider to use.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>A builder for further configuration.</returns>
    public static SagaFlowBuilder AddSagaFlow(
        this IServiceCollection services,
        TimeProvider timeProvider,
        Action<SagaFlowBuilder>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(timeProvider);

        services.AddSingleton(timeProvider);

        return services.AddSagaFlow(configure);
    }
}

