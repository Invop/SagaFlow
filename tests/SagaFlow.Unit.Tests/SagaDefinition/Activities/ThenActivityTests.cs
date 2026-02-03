using SagaFlow.Messages;
using SagaFlow.SagaDefinition.Activities;

namespace SagaFlow.Unit.Tests.SagaDefinition.Activities;

/// <summary>
/// Unit tests for <see cref="ThenActivity{TSagaData,TMessage}"/> class.
/// Tests synchronous action execution within saga workflow.
/// </summary>
public sealed class ThenActivityTests
{
    [Fact]
    public void Constructor_WithValidAction_CreatesActivity()
    {
        Action<ISagaMessageContext<TestCommand>, TestSagaData> action = (_, _) => { };

        var activity = new ThenActivity<TestSagaData, TestCommand>(action);

        activity.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_WithNullAction_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ThenActivity<TestSagaData, TestCommand>(null!));
    }

    [Fact]
    public async Task ExecuteAsync_WithNullContext_ThrowsArgumentNullException()
    {
        var activity = new ThenActivity<TestSagaData, TestCommand>((_, _) => { });

        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await activity.ExecuteAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_ExecutesProvidedAction()
    {
        var wasExecuted = false;
        var activity = new ThenActivity<TestSagaData, TestCommand>((_, _) =>
        {
            wasExecuted = true;
        });
        var context = CreateContext();

        await activity.ExecuteAsync(context, CancellationToken.None);

        wasExecuted.ShouldBeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_PassesMessageContextToAction()
    {
        ISagaMessageContext<TestCommand>? capturedContext = null;
        var activity = new ThenActivity<TestSagaData, TestCommand>((ctx, _) =>
        {
            capturedContext = ctx;
        });
        var (context, messageContext) = CreateContextWithMessageContext();

        await activity.ExecuteAsync(context, CancellationToken.None);

        capturedContext.ShouldBe(messageContext);
    }

    [Fact]
    public async Task ExecuteAsync_PassesSagaDataToAction()
    {
        TestSagaData? capturedSagaData = null;
        var activity = new ThenActivity<TestSagaData, TestCommand>((_, data) =>
        {
            capturedSagaData = data;
        });
        var context = CreateContext();

        await activity.ExecuteAsync(context, CancellationToken.None);

        capturedSagaData.ShouldBe(context.SagaData);
    }

    [Fact]
    public async Task ExecuteAsync_CanModifySagaData()
    {
        var activity = new ThenActivity<TestSagaData, TestCommand>((_, data) =>
        {
            data.IsProcessed = true;
            data.OrderId = "ORD-12345";
        });
        var context = CreateContext();

        await activity.ExecuteAsync(context, CancellationToken.None);

        context.SagaData.IsProcessed.ShouldBeTrue();
        context.SagaData.OrderId.ShouldBe("ORD-12345");
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsCompletedValueTask()
    {
        var activity = new ThenActivity<TestSagaData, TestCommand>((_, _) => { });
        var context = CreateContext();

        var result = activity.ExecuteAsync(context, CancellationToken.None);

        result.IsCompleted.ShouldBeTrue();
        await result;
    }

    [Fact]
    public async Task ExecuteAsync_WhenActionThrows_PropagatesException()
    {
        var expectedException = new InvalidOperationException("Test exception");
        var activity = new ThenActivity<TestSagaData, TestCommand>((_, _) =>
            throw expectedException);
        var context = CreateContext();

        var exception = await Should.ThrowAsync<InvalidOperationException>(async () =>
            await activity.ExecuteAsync(context, CancellationToken.None));

        exception.ShouldBe(expectedException);
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
