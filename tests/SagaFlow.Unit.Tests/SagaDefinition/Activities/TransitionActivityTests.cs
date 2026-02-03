using SagaFlow.Messages;
using SagaFlow.SagaDefinition;
using SagaFlow.SagaDefinition.Activities;

namespace SagaFlow.Unit.Tests.SagaDefinition.Activities;

/// <summary>
/// Unit tests for <see cref="TransitionActivity{TSagaData}"/> class.
/// Tests state transition behavior within saga workflow.
/// </summary>
public sealed class TransitionActivityTests
{
    [Fact]
    public void Constructor_WithValidArguments_CreatesActivity()
    {
        var targetState = CreateState("Processing");
        Func<TestSagaData, string> accessor = data => data.CurrentState;
        Action<TestSagaData, string> mutator = (data, state) => data.CurrentState = state;

        var activity = new TransitionActivity<TestSagaData>(targetState, accessor, mutator);

        activity.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_WithNullTargetState_ThrowsArgumentNullException()
    {
        Func<TestSagaData, string> accessor = data => data.CurrentState;
        Action<TestSagaData, string> mutator = (data, state) => data.CurrentState = state;

        Should.Throw<ArgumentNullException>(() =>
            new TransitionActivity<TestSagaData>(null!, accessor, mutator));
    }

    [Fact]
    public void Constructor_WithNullStateAccessor_ThrowsArgumentNullException()
    {
        var targetState = CreateState("Processing");
        Action<TestSagaData, string> mutator = (data, state) => data.CurrentState = state;

        Should.Throw<ArgumentNullException>(() =>
            new TransitionActivity<TestSagaData>(targetState, null!, mutator));
    }

    [Fact]
    public void Constructor_WithNullStateMutator_ThrowsArgumentNullException()
    {
        var targetState = CreateState("Processing");
        Func<TestSagaData, string> accessor = data => data.CurrentState;

        Should.Throw<ArgumentNullException>(() =>
            new TransitionActivity<TestSagaData>(targetState, accessor, null!));
    }

    [Fact]
    public async Task ExecuteAsync_WithNullContext_ThrowsArgumentNullException()
    {
        var activity = CreateActivity("Processing");

        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await activity.ExecuteAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_TransitionsToTargetState()
    {
        var activity = CreateActivity("Processing");
        var context = CreateContext();
        context.SagaData.CurrentState = "Initial";

        await activity.ExecuteAsync(context, CancellationToken.None);

        context.SagaData.CurrentState.ShouldBe("Processing");
    }

    [Fact]
    public async Task ExecuteAsync_TransitionsFromAnyState()
    {
        var activity = CreateActivity("Completed");
        var context = CreateContext();
        context.SagaData.CurrentState = "Processing";

        await activity.ExecuteAsync(context, CancellationToken.None);

        context.SagaData.CurrentState.ShouldBe("Completed");
    }

    [Fact]
    public async Task ExecuteAsync_TransitionsFromEmptyState()
    {
        var activity = CreateActivity("Started");
        var context = CreateContext();
        context.SagaData.CurrentState = "";

        await activity.ExecuteAsync(context, CancellationToken.None);

        context.SagaData.CurrentState.ShouldBe("Started");
    }

    [Fact]
    public async Task ExecuteAsync_TransitionsToSameState()
    {
        var activity = CreateActivity("Processing");
        var context = CreateContext();
        context.SagaData.CurrentState = "Processing";

        await activity.ExecuteAsync(context, CancellationToken.None);

        context.SagaData.CurrentState.ShouldBe("Processing");
    }

    private static TransitionActivity<TestSagaData> CreateActivity(string targetStateName)
    {
        var targetState = CreateState(targetStateName);
        Func<TestSagaData, string> accessor = data => data.CurrentState;
        Action<TestSagaData, string> mutator = (data, state) => data.CurrentState = state;

        return new TransitionActivity<TestSagaData>(targetState, accessor, mutator);
    }

    private static State<TestSagaData> CreateState(string name)
    {
        return new TestStateMachineForTransition().CreateCustomState(name);
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

    /// <summary>
    /// Test state machine to create typed states for testing.
    /// </summary>
    private sealed class TestStateMachineForTransition : SagaStateMachine<TestSagaData>
    {
        public State<TestSagaData> CreateCustomState(string name) => State(name);
    }
}
