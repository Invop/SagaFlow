using SagaFlow.Messages;
using SagaFlow.Outbox;
using SagaFlow.SagaDefinition.Activities;

namespace SagaFlow.Unit.Tests.SagaDefinition.Activities;

/// <summary>
/// Unit tests for <see cref="PublishMessageActivity{TSagaData,TMessage,TPublishMessage}"/> class.
/// Tests outbox message creation through transactional outbox pattern.
/// </summary>
public sealed class PublishMessageActivityTests
{
    private readonly IOutboxRepository _outboxRepository = Substitute.For<IOutboxRepository>();

    [Fact]
    public void Constructor_WithValidMessageFactory_CreatesActivity()
    {
        Func<ISagaMessageContext<TestCommand>, TestSagaData, TestEvent> factory =
            (ctx, _) => CreateTestEvent(ctx.Message.CorrelationId);

        var activity = new PublishMessageActivity<TestSagaData, TestCommand, TestEvent>(factory);

        activity.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_WithNullMessageFactory_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new PublishMessageActivity<TestSagaData, TestCommand, TestEvent>(null!));
    }

    [Fact]
    public async Task ExecuteAsync_WithNullContext_ThrowsArgumentNullException()
    {
        var activity = CreateActivity();

        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await activity.ExecuteAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_CreatesOutboxMessageAndAddsToRepository()
    {
        var activity = CreateActivity();
        var context = CreateContext();
        OutboxMessage? capturedMessage = null;
        _outboxRepository.AddAsync(Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                capturedMessage = ci.Arg<OutboxMessage>();
                return Task.CompletedTask;
            });

        await activity.ExecuteAsync(context, CancellationToken.None);

        capturedMessage.ShouldNotBeNull();
        capturedMessage.Status.ShouldBe(OutboxMessageStatus.Pending);
        capturedMessage.RetryCount.ShouldBe(0);
    }

    [Fact]
    public async Task ExecuteAsync_SetsCorrectMessageType()
    {
        var activity = CreateActivity();
        var context = CreateContext();
        OutboxMessage? capturedMessage = null;
        _outboxRepository.AddAsync(Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                capturedMessage = ci.Arg<OutboxMessage>();
                return Task.CompletedTask;
            });

        await activity.ExecuteAsync(context, CancellationToken.None);

        capturedMessage.ShouldNotBeNull();
        capturedMessage.MessageType.ShouldContain(nameof(TestEvent));
    }

    [Fact]
    public async Task ExecuteAsync_SetsCorrectCorrelationId()
    {
        var activity = CreateActivity();
        var (context, messageContext) = CreateContextWithDetails();
        OutboxMessage? capturedMessage = null;
        _outboxRepository.AddAsync(Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                capturedMessage = ci.Arg<OutboxMessage>();
                return Task.CompletedTask;
            });

        await activity.ExecuteAsync(context, CancellationToken.None);

        capturedMessage.ShouldNotBeNull();
        capturedMessage.CorrelationId.ShouldBe(messageContext.Message.CorrelationId);
    }

    [Fact]
    public async Task ExecuteAsync_WithNullMessageFromFactory_ThrowsArgumentNullException()
    {
        var activity = new PublishMessageActivity<TestSagaData, TestCommand, TestEvent>((_, _) => null!);
        var context = CreateContext();

        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await activity.ExecuteAsync(context, CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_PassesCancellationTokenToRepository()
    {
        var activity = CreateActivity();
        var context = CreateContext();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        await activity.ExecuteAsync(context, token);

        await _outboxRepository.Received(1).AddAsync(Arg.Any<OutboxMessage>(), token);
    }

    [Fact]
    public async Task ExecuteAsync_SerializesMessageToPayload()
    {
        var activity = CreateActivity();
        var context = CreateContext();
        OutboxMessage? capturedMessage = null;
        _outboxRepository.AddAsync(Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                capturedMessage = ci.Arg<OutboxMessage>();
                return Task.CompletedTask;
            });

        await activity.ExecuteAsync(context, CancellationToken.None);

        capturedMessage.ShouldNotBeNull();
        capturedMessage.Payload.ShouldNotBeNull();
        capturedMessage.Payload.Length.ShouldBeGreaterThan(0);
    }

    private PublishMessageActivity<TestSagaData, TestCommand, TestEvent> CreateActivity()
    {
        return new PublishMessageActivity<TestSagaData, TestCommand, TestEvent>(
            (ctx, _) => CreateTestEvent(ctx.Message.CorrelationId));
    }

    private static TestEvent CreateTestEvent(Guid correlationId)
    {
        return new TestEvent
        {
            IdempotencyKey = Guid.NewGuid().ToString(),
            CorrelationId = correlationId,
            Timestamp = DateTimeOffset.UtcNow,
            StepId = "publish-step",
            IsSuccess = true,
            EventData = "Published message"
        };
    }

    private SagaExecutionContext<TestSagaData> CreateContext()
    {
        var (context, _) = CreateContextWithDetails();
        return context;
    }

    private (SagaExecutionContext<TestSagaData> Context, ISagaMessageContext<TestCommand> MessageContext) CreateContextWithDetails()
    {
        var sagaData = new TestSagaData();
        var message = TestDataGenerators.GenerateCommand();
        var messageContext = Substitute.For<ISagaMessageContext<TestCommand>>();
        messageContext.Message.Returns(message);
        messageContext.CorrelationId.Returns(message.CorrelationId);

        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IOutboxRepository)).Returns(_outboxRepository);

        var context = new SagaExecutionContext<TestSagaData>(
            sagaData,
            message,
            messageContext,
            serviceProvider);

        return (context, messageContext);
    }
}
