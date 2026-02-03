using SagaFlow.Messages;
using SagaFlow.SagaDefinition.Activities;

namespace SagaFlow.Unit.Tests.SagaDefinition.Activities;

/// <summary>
/// Unit tests for <see cref="ThenAsyncActivity{TSagaData,TMessage}"/> class.
/// Tests asynchronous action execution within saga workflow.
/// </summary>
public sealed class ThenAsyncActivityTests
{
    [Fact]
    public void Constructor_WithValidAsyncAction_CreatesActivity()
    {
        Func<ISagaMessageContext<TestCommand>, TestSagaData, CancellationToken, ValueTask> asyncAction =
            (_, _, _) => ValueTask.CompletedTask;

        var activity = new ThenAsyncActivity<TestSagaData, TestCommand>(asyncAction);

        activity.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_WithNullAsyncAction_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ThenAsyncActivity<TestSagaData, TestCommand>(null!));
    }

    [Fact]
    public async Task ExecuteAsync_WithNullContext_ThrowsArgumentNullException()
    {
        var activity = new ThenAsyncActivity<TestSagaData, TestCommand>((_, _, _) => ValueTask.CompletedTask);

        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await activity.ExecuteAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_ExecutesProvidedAsyncAction()
    {
        var wasExecuted = false;
        var activity = new ThenAsyncActivity<TestSagaData, TestCommand>(async (_, _, _) =>
        {
            await Task.Yield();
            wasExecuted = true;
        });
        var context = CreateContext();

        await activity.ExecuteAsync(context, CancellationToken.None);

        wasExecuted.ShouldBeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_PassesMessageContextToAsyncAction()
    {
        ISagaMessageContext<TestCommand>? capturedContext = null;
        var activity = new ThenAsyncActivity<TestSagaData, TestCommand>((ctx, _, _) =>
        {
            capturedContext = ctx;
            return ValueTask.CompletedTask;
        });
        var (context, messageContext) = CreateContextWithMessageContext();

        await activity.ExecuteAsync(context, CancellationToken.None);

        capturedContext.ShouldBe(messageContext);
    }

    [Fact]
    public async Task ExecuteAsync_PassesSagaDataToAsyncAction()
    {
        TestSagaData? capturedSagaData = null;
        var activity = new ThenAsyncActivity<TestSagaData, TestCommand>((_, data, _) =>
        {
            capturedSagaData = data;
            return ValueTask.CompletedTask;
        });
        var context = CreateContext();

        await activity.ExecuteAsync(context, CancellationToken.None);

        capturedSagaData.ShouldBe(context.SagaData);
    }

    [Fact]
    public async Task ExecuteAsync_PassesCancellationTokenToAsyncAction()
    {
        CancellationToken capturedToken = default;
        var activity = new ThenAsyncActivity<TestSagaData, TestCommand>((_, _, ct) =>
        {
            capturedToken = ct;
            return ValueTask.CompletedTask;
        });
        var context = CreateContext();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        await activity.ExecuteAsync(context, token);

        capturedToken.ShouldBe(token);
    }

    [Fact]
    public async Task ExecuteAsync_CanModifySagaData()
    {
        var activity = new ThenAsyncActivity<TestSagaData, TestCommand>(async (_, data, _) =>
        {
            await Task.Delay(1);
            data.IsProcessed = true;
            data.Amount = 199.99m;
        });
        var context = CreateContext();

        await activity.ExecuteAsync(context, CancellationToken.None);

        context.SagaData.IsProcessed.ShouldBeTrue();
        context.SagaData.Amount.ShouldBe(199.99m);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAsyncActionThrows_PropagatesException()
    {
        var expectedException = new InvalidOperationException("Async test exception");
        var activity = new ThenAsyncActivity<TestSagaData, TestCommand>(async (_, _, _) =>
        {
            await Task.Yield();
            throw expectedException;
        });
        var context = CreateContext();

        var exception = await Should.ThrowAsync<InvalidOperationException>(async () =>
            await activity.ExecuteAsync(context, CancellationToken.None));

        exception.ShouldBe(expectedException);
    }

    [Fact]
    public async Task ExecuteAsync_WithCancellation_RespectsCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var activity = new ThenAsyncActivity<TestSagaData, TestCommand>(async (_, _, ct) =>
        {
            ct.ThrowIfCancellationRequested();
            await Task.Delay(1000, ct);
        });
        var context = CreateContext();

        await Should.ThrowAsync<OperationCanceledException>(async () =>
            await activity.ExecuteAsync(context, cts.Token));
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
