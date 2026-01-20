using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SagaFlow.Outbox;

/// <summary>
///     Background service that periodically cleans up old processed outbox messages.
///     This helps maintain database performance and storage efficiency.
/// </summary>
/// <remarks>
///     <para>
///     The cleanup service runs at configurable intervals and removes messages
///     that have been successfully processed beyond the retention period.
///     </para>
///     <para>
///     Messages in pending, failed, or dead letter states are not affected by cleanup.
///     </para>
/// </remarks>
public sealed class OutboxCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly OutboxProcessorOptions _options;
    private readonly ILogger<OutboxCleanupService> _logger;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    ///     Initializes a new instance of the <see cref="OutboxCleanupService"/> class.
    /// </summary>
    /// <param name="scopeFactory">The service scope factory for creating scoped instances.</param>
    /// <param name="options">The processor configuration options.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="timeProvider">The time provider for consistent time operations.</param>
    public OutboxCleanupService(
        IServiceScopeFactory scopeFactory,
        IOptions<OutboxProcessorOptions> options,
        ILogger<OutboxCleanupService> logger,
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
            _logger.LogInformation("Outbox processor is disabled. Cleanup service will not start");
            return;
        }

        _logger.LogInformation(
            "Outbox cleanup service starting. Cleanup interval: {CleanupInterval} hours, Retention: {RetentionDays} days",
            _options.CleanupIntervalHours,
            _options.ProcessedMessageRetentionDays);

        // Wait before first cleanup to allow system to stabilize
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken).ConfigureAwait(false);

        using var timer = new PeriodicTimer(
            TimeSpan.FromHours(_options.CleanupIntervalHours),
            _timeProvider);

        do
        {
            try
            {
                await CleanupProcessedMessagesAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Service is shutting down, exit gracefully
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error occurred during outbox cleanup. Will retry in {Interval} hours",
                    _options.CleanupIntervalHours);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));

        _logger.LogInformation("Outbox cleanup service stopped");
    }

    /// <summary>
    ///     Performs the cleanup of processed messages older than the retention period.
    /// </summary>
    private async Task CleanupProcessedMessagesAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();

        var cutoffDate = _timeProvider.GetUtcNow().AddDays(-_options.ProcessedMessageRetentionDays);

        _logger.LogDebug(
            "Starting cleanup of processed messages older than {CutoffDate}",
            cutoffDate);

        var deletedCount = await repository
            .DeleteProcessedMessagesAsync(cutoffDate, cancellationToken)
            .ConfigureAwait(false);

        if (deletedCount > 0)
        {
            _logger.LogInformation(
                "Cleaned up {DeletedCount} processed outbox messages older than {CutoffDate}",
                deletedCount,
                cutoffDate);
        }
        else
        {
            _logger.LogDebug("No processed messages found for cleanup");
        }
    }

    /// <inheritdoc />
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Outbox cleanup service is stopping...");
        await base.StopAsync(cancellationToken).ConfigureAwait(false);
    }
}
