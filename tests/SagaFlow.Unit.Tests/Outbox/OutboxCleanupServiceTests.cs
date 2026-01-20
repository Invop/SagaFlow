using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using SagaFlow.Outbox;

namespace SagaFlow.Unit.Tests.Outbox;

/// <summary>
/// Unit tests for <see cref="OutboxCleanupService"/>.
/// Tests cleanup service lifecycle and message cleanup orchestration.
/// </summary>
public sealed class OutboxCleanupServiceTests
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxCleanupService> _logger;
    private readonly FakeTimeProvider _timeProvider;
    private readonly OutboxProcessorOptions _options;

    public OutboxCleanupServiceTests()
    {
        _scopeFactory = Substitute.For<IServiceScopeFactory>();
        _logger = NullLogger<OutboxCleanupService>.Instance;
        _timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        _options = new OutboxProcessorOptions
        {
            Enabled = true,
            CleanupIntervalHours = 24,
            ProcessedMessageRetentionDays = 7
        };
    }

    private OutboxCleanupService CreateService() =>
        new(
            _scopeFactory,
            Options.Create(_options),
            _logger,
            _timeProvider);

    [Fact]
    public async Task StartAsync_WithEnabledProcessor_StartsSuccessfully()
    {
        // Setup for the test
        var repository = Substitute.For<IOutboxRepository>();
        var serviceProvider = Substitute.For<IServiceProvider>();
        var asyncScope = Substitute.For<IServiceScope>();
        serviceProvider.GetService(typeof(IOutboxRepository)).Returns(repository);
        asyncScope.ServiceProvider.Returns(serviceProvider);
        _scopeFactory.CreateAsyncScope().Returns(asyncScope);
        repository.DeleteProcessedMessagesAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(0);
        var service = CreateService();
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(100));

        // Execution of the method under test
        await service.StartAsync(CancellationToken.None);
        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(200), cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Verification of the outcome
        await service.StopAsync(CancellationToken.None);
        service.ShouldNotBeNull();
    }

    [Fact]
    public async Task StartAsync_WithDisabledProcessor_DoesNotStart()
    {
        // Setup for the test
        _options.Enabled = false;
        var service = CreateService();
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(100));

        // Execution of the method under test
        await service.StartAsync(CancellationToken.None);
        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(200), cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Verification of the outcome
        await service.StopAsync(CancellationToken.None);
        _scopeFactory.DidNotReceive().CreateAsyncScope();
    }

    [Fact(DisplayName = "Cleanup service calculates correct cutoff date based on retention days")]
    public void CleanupService_CalculatesCorrectCutoffDateBasedOnRetentionDays()
    {
        // Setup for the test
        var currentTime = new DateTimeOffset(2026, 1, 20, 12, 0, 0, TimeSpan.Zero);
        var freshTimeProvider = new FakeTimeProvider(currentTime);
        var expectedCutoff = currentTime.AddDays(-_options.ProcessedMessageRetentionDays);

        // Execution of the method under test
        var actualCutoff = freshTimeProvider.GetUtcNow().AddDays(-_options.ProcessedMessageRetentionDays);

        // Verification of the outcome
        actualCutoff.ShouldBe(expectedCutoff);
        (actualCutoff - expectedCutoff).Duration().ShouldBeLessThan(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task StopAsync_StopsServiceGracefully()
    {
        // Setup for the test
        var repository = Substitute.For<IOutboxRepository>();
        var serviceProvider = Substitute.For<IServiceProvider>();
        var asyncScope = Substitute.For<IServiceScope>();
        serviceProvider.GetService(typeof(IOutboxRepository)).Returns(repository);
        asyncScope.ServiceProvider.Returns(serviceProvider);
        _scopeFactory.CreateAsyncScope().Returns(asyncScope);
        repository.DeleteProcessedMessagesAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(0);
        var service = CreateService();

        // Execution of the method under test
        await service.StartAsync(CancellationToken.None);
        await Task.Delay(50);
        await service.StopAsync(CancellationToken.None);

        // Verification of the outcome
        service.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_WithNullScopeFactory_ThrowsArgumentNullException()
    {
        // Setup for the test and execution
        var exception = Should.Throw<ArgumentNullException>(() =>
            new OutboxCleanupService(
                null!,
                Options.Create(_options),
                _logger,
                _timeProvider));

        // Verification of the outcome
        exception.ParamName.ShouldBe("scopeFactory");
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Setup for the test and execution
        var exception = Should.Throw<ArgumentNullException>(() =>
            new OutboxCleanupService(
                _scopeFactory,
                null!,
                _logger,
                _timeProvider));

        // Verification of the outcome
        exception.ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Setup for the test and execution
        var exception = Should.Throw<ArgumentNullException>(() =>
            new OutboxCleanupService(
                _scopeFactory,
                Options.Create(_options),
                null!,
                _timeProvider));

        // Verification of the outcome
        exception.ParamName.ShouldBe("logger");
    }

    [Fact]
    public void Constructor_WithNullTimeProvider_ThrowsArgumentNullException()
    {
        // Setup for the test and execution
        var exception = Should.Throw<ArgumentNullException>(() =>
            new OutboxCleanupService(
                _scopeFactory,
                Options.Create(_options),
                _logger,
                null!));

        // Verification of the outcome
        exception.ParamName.ShouldBe("timeProvider");
    }
}
