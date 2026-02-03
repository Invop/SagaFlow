using SagaFlow.Messages;
using SagaFlow.SagaDefinition;
using SagaFlow.SagaDefinition.Activities;

namespace SagaFlow.Unit.Tests.SagaDefinition.Activities;

/// <summary>
/// Unit tests for <see cref="EventActivityBinder{TSagaData,TMessage}"/> class.
/// Tests fluent interface for binding activities to message events.
/// </summary>
public sealed class EventActivityBinderTests
{
    private readonly EventActivity<TestSagaData> _eventActivity;
    private readonly Func<TestSagaData, string> _stateAccessor;
    private readonly Action<TestSagaData, string> _stateMutator;

    public EventActivityBinderTests()
    {
        _eventActivity = new EventActivity<TestSagaData>(typeof(TestCommand), null);
        _stateAccessor = data => data.CurrentState;
        _stateMutator = (data, state) => data.CurrentState = state;
    }

    [Fact]
    public void Constructor_WithValidArguments_CreatesBinder()
    {
        var binder = CreateBinder();

        binder.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_WithNullEventActivity_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new EventActivityBinder<TestSagaData, TestCommand>(null!, _stateAccessor, _stateMutator));
    }

    [Fact]
    public void Constructor_WithNullStateAccessor_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new EventActivityBinder<TestSagaData, TestCommand>(_eventActivity, null!, _stateMutator));
    }

    [Fact]
    public void Constructor_WithNullStateMutator_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new EventActivityBinder<TestSagaData, TestCommand>(_eventActivity, _stateAccessor, null!));
    }

    [Fact]
    public void Then_AddsThenActivityToEventActivity()
    {
        var binder = CreateBinder();

        binder.Then((_, _) => { });

        _eventActivity.Activities.Count.ShouldBe(1);
        _eventActivity.Activities[0].ShouldBeOfType<ThenActivity<TestSagaData, TestCommand>>();
    }

    [Fact]
    public void Then_ReturnsSameBinder()
    {
        var binder = CreateBinder();

        var result = binder.Then((_, _) => { });

        result.ShouldBe(binder);
    }

    [Fact]
    public void ThenAsync_AddsThenAsyncActivityToEventActivity()
    {
        var binder = CreateBinder();

        binder.ThenAsync((_, _, _) => ValueTask.CompletedTask);

        _eventActivity.Activities.Count.ShouldBe(1);
        _eventActivity.Activities[0].ShouldBeOfType<ThenAsyncActivity<TestSagaData, TestCommand>>();
    }

    [Fact]
    public void ThenAsync_ReturnsSameBinder()
    {
        var binder = CreateBinder();

        var result = binder.ThenAsync((_, _, _) => ValueTask.CompletedTask);

        result.ShouldBe(binder);
    }

    [Fact]
    public void Publish_AddsPublishMessageActivityToEventActivity()
    {
        var binder = CreateBinder();

        binder.Publish((ctx, _) => CreateTestEvent(ctx.Message.CorrelationId));

        _eventActivity.Activities.Count.ShouldBe(1);
        _eventActivity.Activities[0].ShouldBeOfType<PublishMessageActivity<TestSagaData, TestCommand, TestEvent>>();
    }

    [Fact]
    public void Publish_ReturnsSameBinder()
    {
        var binder = CreateBinder();

        var result = binder.Publish((ctx, _) => CreateTestEvent(ctx.Message.CorrelationId));

        result.ShouldBe(binder);
    }

    [Fact]
    public void PublishAsync_AddsPublishMessageAsyncActivityToEventActivity()
    {
        var binder = CreateBinder();

        binder.PublishAsync(async (ctx, _, _) =>
        {
            await Task.Yield();
            return CreateTestEvent(ctx.Message.CorrelationId);
        });

        _eventActivity.Activities.Count.ShouldBe(1);
        _eventActivity.Activities[0].ShouldBeOfType<PublishMessageAsyncActivity<TestSagaData, TestCommand, TestEvent>>();
    }

    [Fact]
    public void PublishAsync_ReturnsSameBinder()
    {
        var binder = CreateBinder();

        var result = binder.PublishAsync(async (ctx, _, _) =>
        {
            await Task.Yield();
            return CreateTestEvent(ctx.Message.CorrelationId);
        });

        result.ShouldBe(binder);
    }

    [Fact]
    public void Send_AddsSendMessageActivityToEventActivity()
    {
        var binder = CreateBinder();

        binder.Send((ctx, _) => CreateTestEvent(ctx.Message.CorrelationId));

        _eventActivity.Activities.Count.ShouldBe(1);
        _eventActivity.Activities[0].ShouldBeOfType<SendMessageActivity<TestSagaData, TestCommand, TestEvent>>();
    }

    [Fact]
    public void Send_ReturnsSameBinder()
    {
        var binder = CreateBinder();

        var result = binder.Send((ctx, _) => CreateTestEvent(ctx.Message.CorrelationId));

        result.ShouldBe(binder);
    }

    [Fact]
    public void SendAsync_AddsSendMessageAsyncActivityToEventActivity()
    {
        var binder = CreateBinder();

        binder.SendAsync(async (ctx, _, _) =>
        {
            await Task.Yield();
            return CreateTestEvent(ctx.Message.CorrelationId);
        });

        _eventActivity.Activities.Count.ShouldBe(1);
        _eventActivity.Activities[0].ShouldBeOfType<SendMessageAsyncActivity<TestSagaData, TestCommand, TestEvent>>();
    }

    [Fact]
    public void SendAsync_ReturnsSameBinder()
    {
        var binder = CreateBinder();

        var result = binder.SendAsync(async (ctx, _, _) =>
        {
            await Task.Yield();
            return CreateTestEvent(ctx.Message.CorrelationId);
        });

        result.ShouldBe(binder);
    }

    [Fact]
    public void CompensateWith_AddsCompensateWithActivityToEventActivity()
    {
        var binder = CreateBinder();

        binder.CompensateWith((ctx, _) => CreateTestEvent(ctx.Message.CorrelationId));

        _eventActivity.Activities.Count.ShouldBe(1);
        _eventActivity.Activities[0].ShouldBeOfType<CompensateWithActivity<TestSagaData, TestCommand, TestEvent>>();
    }

    [Fact]
    public void CompensateWith_ReturnsSameBinder()
    {
        var binder = CreateBinder();

        var result = binder.CompensateWith((ctx, _) => CreateTestEvent(ctx.Message.CorrelationId));

        result.ShouldBe(binder);
    }

    [Fact]
    public void Pivot_AddsPivotActivityToEventActivity()
    {
        var binder = CreateBinder();

        binder.Pivot();

        _eventActivity.Activities.Count.ShouldBe(1);
        _eventActivity.Activities[0].ShouldBeOfType<PivotActivity<TestSagaData>>();
    }

    [Fact]
    public void Pivot_ReturnsSameBinder()
    {
        var binder = CreateBinder();

        var result = binder.Pivot();

        result.ShouldBe(binder);
    }

    [Fact]
    public void TransitionTo_AddsTransitionActivityToEventActivity()
    {
        var binder = CreateBinder();
        var targetState = CreateState("Processing");

        binder.TransitionTo(targetState);

        _eventActivity.Activities.Count.ShouldBe(1);
        _eventActivity.Activities[0].ShouldBeOfType<TransitionActivity<TestSagaData>>();
    }

    [Fact]
    public void TransitionTo_ReturnsSameBinder()
    {
        var binder = CreateBinder();
        var targetState = CreateState("Processing");

        var result = binder.TransitionTo(targetState);

        result.ShouldBe(binder);
    }

    [Fact]
    public void Complete_AddsFinalizeActivityToEventActivity()
    {
        var binder = CreateBinder();

        binder.Complete();

        _eventActivity.Activities.Count.ShouldBe(1);
        _eventActivity.Activities[0].ShouldBeOfType<FinalizeActivity<TestSagaData>>();
    }

    [Fact]
    public void Complete_ReturnsSameBinder()
    {
        var binder = CreateBinder();

        var result = binder.Complete();

        result.ShouldBe(binder);
    }

    [Fact]
    public void FluentChaining_AddsMultipleActivitiesInOrder()
    {
        var binder = CreateBinder();
        var targetState = CreateState("Processing");

        binder
            .Then((_, _) => { })
            .CompensateWith((ctx, _) => CreateTestEvent(ctx.Message.CorrelationId))
            .TransitionTo(targetState);

        _eventActivity.Activities.Count.ShouldBe(3);
        _eventActivity.Activities[0].ShouldBeOfType<ThenActivity<TestSagaData, TestCommand>>();
        _eventActivity.Activities[1].ShouldBeOfType<CompensateWithActivity<TestSagaData, TestCommand, TestEvent>>();
        _eventActivity.Activities[2].ShouldBeOfType<TransitionActivity<TestSagaData>>();
    }

    [Fact]
    public void ComplexFluentChain_AddsAllActivitiesCorrectly()
    {
        var binder = CreateBinder();
        var targetState = CreateState("WaitingForPayment");

        binder
            .Then((_, data) => data.IsProcessed = true)
            .ThenAsync(async (_, data, _) =>
            {
                await Task.Delay(1);
                data.Amount = 100m;
            })
            .Publish((ctx, _) => CreateTestEvent(ctx.Message.CorrelationId))
            .CompensateWith((ctx, _) => CreateTestEvent(ctx.Message.CorrelationId))
            .Pivot()
            .Send((ctx, _) => CreateTestEvent(ctx.Message.CorrelationId))
            .TransitionTo(targetState);

        _eventActivity.Activities.Count.ShouldBe(7);
    }

    private EventActivityBinder<TestSagaData, TestCommand> CreateBinder()
    {
        return new EventActivityBinder<TestSagaData, TestCommand>(
            _eventActivity,
            _stateAccessor,
            _stateMutator);
    }

    private static State<TestSagaData> CreateState(string name)
    {
        return new TestStateMachineForBinder().CreateCustomState(name);
    }

    private static TestEvent CreateTestEvent(Guid correlationId)
    {
        return new TestEvent
        {
            IdempotencyKey = Guid.NewGuid().ToString(),
            CorrelationId = correlationId,
            Timestamp = DateTimeOffset.UtcNow,
            StepId = "test-step",
            IsSuccess = true,
            EventData = "Test event"
        };
    }

    /// <summary>
    /// Test state machine to create typed states for testing.
    /// </summary>
    private sealed class TestStateMachineForBinder : SagaStateMachine<TestSagaData>
    {
        public State<TestSagaData> CreateCustomState(string name) => State(name);
    }
}
