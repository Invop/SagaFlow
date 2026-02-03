using SagaFlow.Messages;
using SagaFlow.SagaDefinition;
using SagaFlow.SagaDefinition.Activities;

namespace SagaFlow.Unit.Tests.SagaDefinition.Activities;

/// <summary>
/// Unit tests for <see cref="EventActivity{TSagaData}"/> class.
/// Tests event activity creation, message type validation, and activity execution.
/// </summary>
public sealed class EventActivityTests
{
    [Fact]
    public void Constructor_WithValidMessageType_CreatesEventActivity()
    {
        var state = new State("Processing");

        var eventActivity = new EventActivity<TestSagaData>(typeof(TestCommand), state);

        eventActivity.MessageType.ShouldBe(typeof(TestCommand));
        eventActivity.State.ShouldBe(state);
        eventActivity.Activities.ShouldBeEmpty();
    }

    [Fact]
    public void Constructor_WithNullState_CreatesEventActivityWithNullState()
    {
        var eventActivity = new EventActivity<TestSagaData>(typeof(TestCommand), null);

        eventActivity.MessageType.ShouldBe(typeof(TestCommand));
        eventActivity.State.ShouldBeNull();
    }

    [Fact]
    public void Constructor_WithNullMessageType_ThrowsArgumentNullException()
    {
        var state = new State("Processing");

        Should.Throw<ArgumentNullException>(() =>
            new EventActivity<TestSagaData>(null!, state));
    }

    [Fact]
    public void Constructor_WithNonSagaMessageType_ThrowsArgumentException()
    {
        var state = new State("Processing");

        var exception = Should.Throw<ArgumentException>(() =>
            new EventActivity<TestSagaData>(typeof(string), state));

        exception.Message.ShouldContain(nameof(ISagaMessage));
    }

    [Fact]
    public void Activities_CanAddActivities()
    {
        var eventActivity = new EventActivity<TestSagaData>(typeof(TestCommand), null);
        var activity = Substitute.For<IActivity<TestSagaData>>();

        eventActivity.Activities.Add(activity);

        eventActivity.Activities.ShouldContain(activity);
        eventActivity.Activities.Count.ShouldBe(1);
    }

    [Fact]
    public async Task ExecuteAsync_WithNullContext_ThrowsArgumentNullException()
    {
        var eventActivity = new EventActivity<TestSagaData>(typeof(TestCommand), null);

        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await eventActivity.ExecuteAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_WithNoActivities_CompletesSuccessfully()
    {
        var eventActivity = new EventActivity<TestSagaData>(typeof(TestCommand), null);
        var context = CreateContext();

        await Should.NotThrowAsync(async () =>
            await eventActivity.ExecuteAsync(context, CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_WithMultipleActivities_ExecutesAllInOrder()
    {
        var eventActivity = new EventActivity<TestSagaData>(typeof(TestCommand), null);
        var executionOrder = new List<int>();

        var activity1 = Substitute.For<IActivity<TestSagaData>>();
        activity1.ExecuteAsync(Arg.Any<SagaExecutionContext<TestSagaData>>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                executionOrder.Add(1);
                return ValueTask.CompletedTask;
            });

        var activity2 = Substitute.For<IActivity<TestSagaData>>();
        activity2.ExecuteAsync(Arg.Any<SagaExecutionContext<TestSagaData>>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                executionOrder.Add(2);
                return ValueTask.CompletedTask;
            });

        var activity3 = Substitute.For<IActivity<TestSagaData>>();
        activity3.ExecuteAsync(Arg.Any<SagaExecutionContext<TestSagaData>>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                executionOrder.Add(3);
                return ValueTask.CompletedTask;
            });

        eventActivity.Activities.Add(activity1);
        eventActivity.Activities.Add(activity2);
        eventActivity.Activities.Add(activity3);
        var context = CreateContext();

        await eventActivity.ExecuteAsync(context, CancellationToken.None);

        executionOrder.ShouldBe([1, 2, 3]);
    }

    [Fact]
    public async Task ExecuteAsync_PassesContextToEachActivity()
    {
        var eventActivity = new EventActivity<TestSagaData>(typeof(TestCommand), null);
        var activity = Substitute.For<IActivity<TestSagaData>>();
        eventActivity.Activities.Add(activity);
        var context = CreateContext();

        await eventActivity.ExecuteAsync(context, CancellationToken.None);

        await activity.Received(1).ExecuteAsync(context, CancellationToken.None);
    }

    [Fact]
    public async Task ExecuteAsync_PassesCancellationTokenToActivities()
    {
        var eventActivity = new EventActivity<TestSagaData>(typeof(TestCommand), null);
        var activity = Substitute.For<IActivity<TestSagaData>>();
        eventActivity.Activities.Add(activity);
        var context = CreateContext();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        await eventActivity.ExecuteAsync(context, token);

        await activity.Received(1).ExecuteAsync(context, token);
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
