using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SagaFlow.Outbox;

/// <summary>
///     Background service that periodically processes outbox messages.
///     This service implements the message relay component of the transactional outbox pattern.
/// </summary>
/// <remarks>
///     <para>
///     The service runs continuously in the background, polling the outbox repository
///     at configured intervals to retrieve and publish pending messages.
///     </para>
///     <para>
///     This service is designed to be resilient to failures. If an error occurs during
///     processing, the service logs the error and continues polling in the next interval.
///     </para>
/// </remarks>
public sealed class OutboxBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly OutboxProcessorOptions _options;
    private readonly ILogger<OutboxBackgroundService> _logger;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    ///     Initializes a new instance of the <see cref="OutboxBackgroundService"/> class.
    /// </summary>
    /// <param name="scopeFactory">The service scope factory for creating scoped instances.</param>
    /// <param name="options">The processor configuration options.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="timeProvider">The time provider for consistent time operations.</param>
    public OutboxBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<OutboxProcessorOptions> options,
        ILogger<OutboxBackgroundService> logger,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Outbox processor is disabled. Background service will not start");
            return;
        }

        _logger.LogInformation(
            "Outbox background service starting. Polling interval: {PollingInterval} seconds, Batch size: {BatchSize}",
            _options.PollingIntervalSeconds,
            _options.BatchSize);

        // Wait briefly before first poll to allow other services to initialize
        await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken).ConfigureAwait(false);

        using var timer = new PeriodicTimer(
            TimeSpan.FromSeconds(_options.PollingIntervalSeconds),
            _timeProvider);

        do
        {
            try
            {
                await ProcessOutboxMessagesAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Service is shutting down, exit gracefully
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing outbox messages. Will retry in {Interval} seconds",
                    _options.PollingIntervalSeconds);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));

        _logger.LogInformation("Outbox background service stopped");
    }

    /// <summary>
    ///     Processes outbox messages using a scoped service instance.
    /// </summary>
    private async Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var processor = scope.ServiceProvider.GetRequiredService<IOutboxProcessor>();

        await processor.ProcessPendingMessagesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Outbox background service is stopping...");
        await base.StopAsync(cancellationToken).ConfigureAwait(false);
    }
}