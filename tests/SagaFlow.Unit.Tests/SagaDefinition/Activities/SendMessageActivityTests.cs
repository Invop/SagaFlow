using SagaFlow.Messages;
using SagaFlow.SagaDefinition.Activities;

namespace SagaFlow.Unit.Tests.SagaDefinition.Activities;

/// <summary>
/// Unit tests for <see cref="SendMessageActivity{TSagaData,TMessage,TSendMessage}"/> class.
/// Tests in-process message sending via ISagaMessageHandler.
/// </summary>
public sealed class SendMessageActivityTests
{
    private readonly ISagaMessageHandler<TestEvent> _messageHandler = Substitute.For<ISagaMessageHandler<TestEvent>>();

    [Fact]
    public void Constructor_WithValidMessageFactory_CreatesActivity()
    {
        Func<ISagaMessageContext<TestCommand>, TestSagaData, TestEvent> factory =
            (ctx, _) => CreateTestEvent(ctx.Message.CorrelationId);

        var activity = new SendMessageActivity<TestSagaData, TestCommand, TestEvent>(factory);

        activity.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_WithNullMessageFactory_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new SendMessageActivity<TestSagaData, TestCommand, TestEvent>(null!));
    }

    [Fact]
    public async Task ExecuteAsync_WithNullContext_ThrowsArgumentNullException()
    {
        var activity = CreateActivity();

        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await activity.ExecuteAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_InvokesMessageHandler()
    {
        var activity = CreateActivity();
        var context = CreateContext();

        await activity.ExecuteAsync(context, CancellationToken.None);

        await _messageHandler.Received(1).HandleAsync(
            Arg.Any<ISagaMessageContext<TestEvent>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_PassesCorrectMessageToHandler()
    {
        var activity = CreateActivity();
        var context = CreateContext();
        ISagaMessageContext<TestEvent>? capturedContext = null;
        _messageHandler.HandleAsync(Arg.Any<ISagaMessageContext<TestEvent>>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                capturedContext = ci.Arg<ISagaMessageContext<TestEvent>>();
                return ValueTask.CompletedTask;
            });

        await activity.ExecuteAsync(context, CancellationToken.None);

        capturedContext.ShouldNotBeNull();
        capturedContext.Message.ShouldNotBeNull();
        capturedContext.Message.ShouldBeOfType<TestEvent>();
    }

    [Fact]
    public async Task ExecuteAsync_PassesCancellationTokenToHandler()
    {
        var activity = CreateActivity();
        var context = CreateContext();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        await activity.ExecuteAsync(context, token);

        await _messageHandler.Received(1).HandleAsync(
            Arg.Any<ISagaMessageContext<TestEvent>>(),
            token);
    }

    [Fact]
    public async Task ExecuteAsync_WithNullMessageFromFactory_ThrowsArgumentNullException()
    {
        var activity = new SendMessageActivity<TestSagaData, TestCommand, TestEvent>((_, _) => null!);
        var context = CreateContext();

        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await activity.ExecuteAsync(context, CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_CreatesMessageContextWithSagaOrchestratorSender()
    {
        var activity = CreateActivity();
        var context = CreateContext();
        ISagaMessageContext<TestEvent>? capturedContext = null;
        _messageHandler.HandleAsync(Arg.Any<ISagaMessageContext<TestEvent>>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                capturedContext = ci.Arg<ISagaMessageContext<TestEvent>>();
                return ValueTask.CompletedTask;
            });

        await activity.ExecuteAsync(context, CancellationToken.None);

        capturedContext.ShouldNotBeNull();
        capturedContext.SenderId.ShouldBe("SagaOrchestrator");
    }

    [Fact]
    public async Task ExecuteAsync_UsesFactoryToCreateMessage()
    {
        var expectedCorrelationId = Guid.NewGuid();
        var wasFactoryCalled = false;
        var activity = new SendMessageActivity<TestSagaData, TestCommand, TestEvent>((ctx, _) =>
        {
            wasFactoryCalled = true;
            return CreateTestEvent(ctx.Message.CorrelationId);
        });
        var context = CreateContext();

        await activity.ExecuteAsync(context, CancellationToken.None);

        wasFactoryCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_PassesSagaDataToFactory()
    {
        TestSagaData? capturedData = null;
        var activity = new SendMessageActivity<TestSagaData, TestCommand, TestEvent>((ctx, data) =>
        {
            capturedData = data;
            return CreateTestEvent(ctx.Message.CorrelationId);
        });
        var context = CreateContext();

        await activity.ExecuteAsync(context, CancellationToken.None);

        capturedData.ShouldBe(context.SagaData);
    }

    private SendMessageActivity<TestSagaData, TestCommand, TestEvent> CreateActivity()
    {
        return new SendMessageActivity<TestSagaData, TestCommand, TestEvent>(
            (ctx, _) => CreateTestEvent(ctx.Message.CorrelationId));
    }

    private static TestEvent CreateTestEvent(Guid correlationId)
    {
        return new TestEvent
        {
            IdempotencyKey = Guid.NewGuid().ToString(),
            CorrelationId = correlationId,
            Timestamp = DateTimeOffset.UtcNow,
            StepId = "send-step",
            IsSuccess = true,
            EventData = "Sent message"
        };
    }

    private SagaExecutionContext<TestSagaData> CreateContext()
    {
        var sagaData = new TestSagaData();
        var message = TestDataGenerators.GenerateCommand();
        var messageContext = Substitute.For<ISagaMessageContext<TestCommand>>();
        messageContext.Message.Returns(message);
        messageContext.CorrelationId.Returns(message.CorrelationId);

        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(ISagaMessageHandler<TestEvent>)).Returns(_messageHandler);

        return new SagaExecutionContext<TestSagaData>(
            sagaData,
            message,
            messageContext,
            serviceProvider);
    }
}
