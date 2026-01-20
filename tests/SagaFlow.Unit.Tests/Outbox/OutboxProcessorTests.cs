using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using SagaFlow.Outbox;

namespace SagaFlow.Unit.Tests.Outbox;

/// <summary>
/// Unit tests for <see cref="OutboxProcessor"/> application service.
/// Tests message processing orchestration and failure handling.
/// </summary>
public sealed class OutboxProcessorTests
{
    private readonly IOutboxRepository _repository;
    private readonly IPublisher _publisher;
    private readonly ILogger<OutboxProcessor> _logger;
    private readonly FakeTimeProvider _timeProvider;
    private readonly OutboxProcessorOptions _options;

    public OutboxProcessorTests()
    {
        _repository = Substitute.For<IOutboxRepository>();
        _publisher = Substitute.For<IPublisher>();
        _logger = NullLogger<OutboxProcessor>.Instance;
        _timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        _options = new OutboxProcessorOptions
        {
            BatchSize = 10,
            MaxRetryCount = 3,
            BaseRetryDelaySeconds = 30,
            ProcessFailedMessages = true
        };
    }

    private OutboxProcessor CreateProcessor() =>
        new(
            _repository,
            _publisher,
            Options.Create(_options),
            _logger,
            _timeProvider);

    [Fact]
    public async Task ProcessPendingMessagesAsync_WithNoMessages_CompletesSuccessfully()
    {
        // Setup for the test
        _repository.GetPendingMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<OutboxMessage>().ToList());
        var processor = CreateProcessor();

        // Execution of the method under test
        await processor.ProcessPendingMessagesAsync();

        // Verification of the outcome
        await _repository.Received(1).GetPendingMessagesAsync(
            _options.BatchSize,
            Arg.Any<CancellationToken>());
        await _publisher.DidNotReceive().PublishAsync(
            Arg.Any<OutboxMessage>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessPendingMessagesAsync_WithPendingMessages_PublishesThem()
    {
        // Setup for the test
        var messages = TestDataGenerators.GenerateOutboxMessages(3);
        _repository.GetPendingMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(messages);
        _repository.GetFailedMessagesForRetryAsync(
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(Array.Empty<OutboxMessage>().ToList());
        var processor = CreateProcessor();

        // Execution of the method under test
        await processor.ProcessPendingMessagesAsync();

        // Verification of the outcome
        await _publisher.Received(3).PublishAsync(
            Arg.Any<OutboxMessage>(),
            Arg.Any<CancellationToken>());
        await _repository.Received(3).MarkAsProcessedAsync(
            Arg.Any<Guid>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessPendingMessagesAsync_AfterSuccessfulPublish_MarksMessageAsProcessed()
    {
        // Setup for the test
        var message = TestDataGenerators.GenerateOutboxMessage();
        _repository.GetPendingMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new[] { message }.ToList());
        _repository.GetFailedMessagesForRetryAsync(
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(Array.Empty<OutboxMessage>().ToList());
        var processor = CreateProcessor();

        // Execution of the method under test
        await processor.ProcessPendingMessagesAsync();

        // Verification of the outcome
        await _repository.Received(1).MarkAsProcessedAsync(
            message.Id,
            Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessPendingMessagesAsync_WhenPublishThrows_MarksMessageAsFailed()
    {
        // Setup for the test
        var message = TestDataGenerators.GenerateOutboxMessage();
        _repository.GetPendingMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new[] { message }.ToList());
        _repository.GetFailedMessagesForRetryAsync(
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(Array.Empty<OutboxMessage>().ToList());
        _publisher.PublishAsync(Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromException(new InvalidOperationException("Publisher unavailable")));
        var processor = CreateProcessor();

        // Execution of the method under test
        await processor.ProcessPendingMessagesAsync();

        // Verification of the outcome
        await _repository.Received(1).MarkAsFailedAsync(
            message.Id,
            "Publisher unavailable",
            Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().MarkAsProcessedAsync(
            Arg.Any<Guid>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessPendingMessagesAsync_AfterMaxRetries_MovesMessageToDeadLetter()
    {
        // Setup for the test
        var message = TestDataGenerators.GenerateFailedMessage(retryCount: 2);
        _repository.GetPendingMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new[] { message }.ToList());
        _repository.GetFailedMessagesForRetryAsync(
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(Array.Empty<OutboxMessage>().ToList());
        _publisher.PublishAsync(Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromException(new TimeoutException("Timeout occurred")));
        var processor = CreateProcessor();

        // Execution of the method under test
        await processor.ProcessPendingMessagesAsync();

        // Verification of the outcome
        await _repository.Received(1).MarkAsDeadLetterAsync(
            message.Id,
            Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().MarkAsFailedAsync(
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessPendingMessagesAsync_WithProcessFailedMessagesEnabled_ProcessesFailedMessages()
    {
        // Setup for the test
        var failedMessages = TestDataGenerators.GenerateOutboxMessages(2)
            .Select(m => { m.Status = OutboxMessageStatus.Failed; m.RetryCount = 1; return m; })
            .ToList();
        _repository.GetPendingMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<OutboxMessage>().ToList());
        _repository.GetFailedMessagesForRetryAsync(
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(failedMessages);
        var processor = CreateProcessor();

        // Execution of the method under test
        await processor.ProcessPendingMessagesAsync();

        // Verification of the outcome
        await _repository.Received(1).GetFailedMessagesForRetryAsync(
            _options.BatchSize,
            _options.MaxRetryCount,
            _options.BaseRetryDelaySeconds,
            Arg.Any<CancellationToken>());
        await _publisher.Received(2).PublishAsync(
            Arg.Any<OutboxMessage>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessPendingMessagesAsync_WithProcessFailedMessagesDisabled_SkipsFailedMessages()
    {
        // Setup for the test
        _options.ProcessFailedMessages = false;
        _repository.GetPendingMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<OutboxMessage>().ToList());
        var processor = CreateProcessor();

        // Execution of the method under test
        await processor.ProcessPendingMessagesAsync();

        // Verification of the outcome
        await _repository.DidNotReceive().GetFailedMessagesForRetryAsync(
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessPendingMessagesAsync_ProcessesMessagesSequentially()
    {
        // Setup for the test
        var messages = TestDataGenerators.GenerateOutboxMessages(5);
        var processedOrder = new List<Guid>();
        _repository.GetPendingMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(messages);
        _repository.GetFailedMessagesForRetryAsync(
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(Array.Empty<OutboxMessage>().ToList());
        _publisher.PublishAsync(Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var msg = callInfo.Arg<OutboxMessage>();
                processedOrder.Add(msg.Id);
                return ValueTask.CompletedTask;
            });
        var processor = CreateProcessor();

        // Execution of the method under test
        await processor.ProcessPendingMessagesAsync();

        // Verification of the outcome
        processedOrder.ShouldBe(messages.Select(m => m.Id).ToList());
    }

    [Fact]
    public async Task ProcessPendingMessagesAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Setup for the test
        var messages = TestDataGenerators.GenerateOutboxMessages(3);
        _repository.GetPendingMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(messages);
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var processor = CreateProcessor();

        // Execution of the method under test and verification
        await Should.ThrowAsync<OperationCanceledException>(
            async () => await processor.ProcessPendingMessagesAsync(cts.Token));
    }

    [Fact]
    public async Task ProcessPendingMessagesAsync_UsesTimeProviderForTimestamp()
    {
        // Setup for the test
        var fixedTime = new DateTimeOffset(2026, 1, 15, 10, 30, 0, TimeSpan.Zero);
        var freshTimeProvider = new FakeTimeProvider(fixedTime);
        var message = TestDataGenerators.GenerateOutboxMessage();
        _repository.GetPendingMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new[] { message }.ToList());
        _repository.GetFailedMessagesForRetryAsync(
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(Array.Empty<OutboxMessage>().ToList());
        var processor = new OutboxProcessor(
            _repository,
            _publisher,
            Options.Create(_options),
            _logger,
            freshTimeProvider);

        // Execution of the method under test
        await processor.ProcessPendingMessagesAsync();

        // Verification of the outcome
        await _repository.Received(1).MarkAsProcessedAsync(
            message.Id,
            fixedTime,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Constructor_WithNullRepository_ThrowsArgumentNullException()
    {
        // Setup for the test and execution
        var exception = Should.Throw<ArgumentNullException>(() =>
            new OutboxProcessor(
                null!,
                _publisher,
                Options.Create(_options),
                _logger,
                _timeProvider));

        // Verification of the outcome
        exception.ParamName.ShouldBe("repository");
    }

    [Fact]
    public void Constructor_WithNullPublisher_ThrowsArgumentNullException()
    {
        // Setup for the test and execution
        var exception = Should.Throw<ArgumentNullException>(() =>
            new OutboxProcessor(
                _repository,
                null!,
                Options.Create(_options),
                _logger,
                _timeProvider));

        // Verification of the outcome
        exception.ParamName.ShouldBe("publisher");
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Setup for the test and execution
        var exception = Should.Throw<ArgumentNullException>(() =>
            new OutboxProcessor(
                _repository,
                _publisher,
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
            new OutboxProcessor(
                _repository,
                _publisher,
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
            new OutboxProcessor(
                _repository,
                _publisher,
                Options.Create(_options),
                _logger,
                null!));

        // Verification of the outcome
        exception.ParamName.ShouldBe("timeProvider");
    }
}
