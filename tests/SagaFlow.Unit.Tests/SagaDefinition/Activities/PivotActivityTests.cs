using SagaFlow.Messages;
using SagaFlow.SagaDefinition.Activities;

namespace SagaFlow.Unit.Tests.SagaDefinition.Activities;

/// <summary>
/// Unit tests for <see cref="PivotActivity{TSagaData}"/> class.
/// Tests pivot point marking and compensation message clearing.
/// </summary>
public sealed class PivotActivityTests
{
    [Fact]
    public void Constructor_CreatesActivity()
    {
        var activity = new PivotActivity<TestSagaData>();

        activity.ShouldNotBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_WithNullContext_ThrowsArgumentNullException()
    {
        var activity = new PivotActivity<TestSagaData>();

        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await activity.ExecuteAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_SetsIsPivotReachedToTrue()
    {
        var activity = new PivotActivity<TestSagaData>();
        var context = CreateContext();
        context.IsPivotReached.ShouldBeFalse();

        await activity.ExecuteAsync(context, CancellationToken.None);

        context.IsPivotReached.ShouldBeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_ClearsCompensationMessages()
    {
        var activity = new PivotActivity<TestSagaData>();
        var context = CreateContext();
        context.CompensationMessages.Add(TestDataGenerators.GenerateEvent());
        context.CompensationMessages.Add(TestDataGenerators.GenerateCommand());
        context.CompensationMessages.Count.ShouldBe(2);

        await activity.ExecuteAsync(context, CancellationToken.None);

        context.CompensationMessages.ShouldBeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_WithNoCompensationMessages_DoesNotThrow()
    {
        var activity = new PivotActivity<TestSagaData>();
        var context = CreateContext();

        await Should.NotThrowAsync(async () =>
            await activity.ExecuteAsync(context, CancellationToken.None));

        context.IsPivotReached.ShouldBeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsCompletedValueTask()
    {
        var activity = new PivotActivity<TestSagaData>();
        var context = CreateContext();

        var result = activity.ExecuteAsync(context, CancellationToken.None);

        result.IsCompleted.ShouldBeTrue();
        await result;
    }

    [Fact]
    public async Task ExecuteAsync_WhenCalledMultipleTimes_RemainsAtPivotState()
    {
        var activity = new PivotActivity<TestSagaData>();
        var context = CreateContext();
        context.CompensationMessages.Add(TestDataGenerators.GenerateEvent());

        await activity.ExecuteAsync(context, CancellationToken.None);
        context.CompensationMessages.Add(TestDataGenerators.GenerateCommand());
        await activity.ExecuteAsync(context, CancellationToken.None);

        context.IsPivotReached.ShouldBeTrue();
        context.CompensationMessages.ShouldBeEmpty();
    }

    private static SagaExecutionContext<TestSagaData> CreateContext()
    {
        var sagaData = new TestSagaData();
        var message = TestDataGenerators.GenerateCommand();
        var messageContext = Substitute.For<ISagaMessageContext<TestCommand>>();
        messageContext.Message.Returns(message);
        var serviceProvider = Substitute.For<IServiceProvider>();

        return new SagaExecutionContext<TestSagaData>(
            sagaData,
            message,
            messageContext,
            serviceProvider);
    }
}
