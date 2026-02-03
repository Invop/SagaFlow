using SagaFlow.Messages;
using SagaFlow.SagaDefinition.Activities;

namespace SagaFlow.Unit.Tests.SagaDefinition.Activities;

/// <summary>
/// Unit tests for <see cref="CompensateWithActivity{TSagaData,TMessage,TCompensationMessage}"/> class.
/// Tests compensation message registration and pivot point behavior.
/// </summary>
public sealed class CompensateWithActivityTests
{
    [Fact]
    public void Constructor_WithValidCompensationFactory_CreatesActivity()
    {
        Func<ISagaMessageContext<TestCommand>, TestSagaData, TestEvent> factory =
            (ctx, _) => CreateCompensationEvent(ctx.Message.CorrelationId);

        var activity = new CompensateWithActivity<TestSagaData, TestCommand, TestEvent>(factory);

        activity.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_WithNullCompensationFactory_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new CompensateWithActivity<TestSagaData, TestCommand, TestEvent>(null!));
    }

    [Fact]
    public async Task ExecuteAsync_WithNullContext_ThrowsArgumentNullException()
    {
        var activity = CreateActivity();

        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await activity.ExecuteAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_AddsCompensationMessageToContext()
    {
        var activity = CreateActivity();
        var context = CreateContext();

        await activity.ExecuteAsync(context, CancellationToken.None);

        context.CompensationMessages.Count.ShouldBe(1);
        context.CompensationMessages[0].ShouldBeOfType<TestEvent>();
    }

    [Fact]
    public async Task ExecuteAsync_InsertsCompensationMessageAtBeginning()
    {
        var activity = CreateActivity();
        var context = CreateContext();
        var existingMessage = TestDataGenerators.GenerateEvent();
        context.CompensationMessages.Add(existingMessage);

        await activity.ExecuteAsync(context, CancellationToken.None);

        context.CompensationMessages.Count.ShouldBe(2);
        context.CompensationMessages[0].ShouldBeOfType<TestEvent>();
        context.CompensationMessages[1].ShouldBe(existingMessage);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPivotReached_DoesNotAddCompensationMessage()
    {
        var activity = CreateActivity();
        var context = CreateContext();
        context.IsPivotReached = true;

        await activity.ExecuteAsync(context, CancellationToken.None);

        context.CompensationMessages.ShouldBeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_PassesCorrectContextToFactory()
    {
        ISagaMessageContext<TestCommand>? capturedContext = null;
        TestSagaData? capturedData = null;

        var activity = new CompensateWithActivity<TestSagaData, TestCommand, TestEvent>(
            (ctx, data) =>
            {
                capturedContext = ctx;
                capturedData = data;
                return CreateCompensationEvent(ctx.Message.CorrelationId);
            });

        var (context, messageContext) = CreateContextWithMessageContext();

        await activity.ExecuteAsync(context, CancellationToken.None);

        capturedContext.ShouldBe(messageContext);
        capturedData.ShouldBe(context.SagaData);
    }

    [Fact]
    public async Task ExecuteAsync_WithNullCompensationMessage_ThrowsArgumentNullException()
    {
        var activity = new CompensateWithActivity<TestSagaData, TestCommand, TestEvent>(
            (_, _) => null!);
        var context = CreateContext();

        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await activity.ExecuteAsync(context, CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_PreservesLIFOOrderForCompensations()
    {
        var context = CreateContext();

        var activity1 = new CompensateWithActivity<TestSagaData, TestCommand, TestEvent>(
            (ctx, _) => CreateCompensationEvent(ctx.Message.CorrelationId, "step1"));
        var activity2 = new CompensateWithActivity<TestSagaData, TestCommand, TestEvent>(
            (ctx, _) => CreateCompensationEvent(ctx.Message.CorrelationId, "step2"));
        var activity3 = new CompensateWithActivity<TestSagaData, TestCommand, TestEvent>(
            (ctx, _) => CreateCompensationEvent(ctx.Message.CorrelationId, "step3"));

        await activity1.ExecuteAsync(context, CancellationToken.None);
        await activity2.ExecuteAsync(context, CancellationToken.None);
        await activity3.ExecuteAsync(context, CancellationToken.None);

        context.CompensationMessages.Count.ShouldBe(3);
        ((TestEvent)context.CompensationMessages[0]).StepId.ShouldBe("step3");
        ((TestEvent)context.CompensationMessages[1]).StepId.ShouldBe("step2");
        ((TestEvent)context.CompensationMessages[2]).StepId.ShouldBe("step1");
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsCompletedValueTask()
    {
        var activity = CreateActivity();
        var context = CreateContext();

        var result = activity.ExecuteAsync(context, CancellationToken.None);

        result.IsCompleted.ShouldBeTrue();
        await result;
    }

    private static CompensateWithActivity<TestSagaData, TestCommand, TestEvent> CreateActivity()
    {
        return new CompensateWithActivity<TestSagaData, TestCommand, TestEvent>(
            (ctx, _) => CreateCompensationEvent(ctx.Message.CorrelationId));
    }

    private static TestEvent CreateCompensationEvent(Guid correlationId, string stepId = "compensation")
    {
        return new TestEvent
        {
            IdempotencyKey = Guid.NewGuid().ToString(),
            CorrelationId = correlationId,
            Timestamp = DateTimeOffset.UtcNow,
            StepId = stepId,
            IsSuccess = false,
            EventData = "Compensation triggered"
        };
    }

    private static SagaExecutionContext<TestSagaData> CreateContext()
    {
        var (context, _) = CreateContextWithMessageContext();
        return context;
    }

    private static (SagaExecutionContext<TestSagaData> Context, ISagaMessageContext<TestCommand> MessageContext) CreateContextWithMessageContext()
    {
        var sagaData = new TestSagaData();
        var message = TestDataGenerators.GenerateCommand();
        var messageContext = Substitute.For<ISagaMessageContext<TestCommand>>();
        messageContext.Message.Returns(message);
        messageContext.CorrelationId.Returns(message.CorrelationId);
        var serviceProvider = Substitute.For<IServiceProvider>();

        var context = new SagaExecutionContext<TestSagaData>(
            sagaData,
            message,
            messageContext,
            serviceProvider);

        return (context, messageContext);
    }
}
