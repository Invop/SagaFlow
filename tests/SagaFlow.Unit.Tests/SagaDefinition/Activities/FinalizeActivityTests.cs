using SagaFlow.Messages;
using SagaFlow.SagaDefinition.Activities;

namespace SagaFlow.Unit.Tests.SagaDefinition.Activities;

/// <summary>
/// Unit tests for <see cref="FinalizeActivity{TSagaData}"/> class.
/// Tests saga finalization and transition to final state.
/// </summary>
public sealed class FinalizeActivityTests
{
    [Fact]
    public void Constructor_WithValidArguments_CreatesActivity()
    {
        Func<TestSagaData, string> accessor = data => data.CurrentState;
        Action<TestSagaData, string> mutator = (data, state) => data.CurrentState = state;

        var activity = new FinalizeActivity<TestSagaData>(accessor, mutator);

        activity.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_WithNullStateAccessor_ThrowsArgumentNullException()
    {
        Action<TestSagaData, string> mutator = (data, state) => data.CurrentState = state;

        Should.Throw<ArgumentNullException>(() =>
            new FinalizeActivity<TestSagaData>(null!, mutator));
    }

    [Fact]
    public void Constructor_WithNullStateMutator_ThrowsArgumentNullException()
    {
        Func<TestSagaData, string> accessor = data => data.CurrentState;

        Should.Throw<ArgumentNullException>(() =>
            new FinalizeActivity<TestSagaData>(accessor, null!));
    }

    [Fact]
    public async Task ExecuteAsync_WithNullContext_ThrowsArgumentNullException()
    {
        var activity = CreateActivity();

        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await activity.ExecuteAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_TransitionsToFinalState()
    {
        var activity = CreateActivity();
        var context = CreateContext();
        context.SagaData.CurrentState = "Processing";

        await activity.ExecuteAsync(context, CancellationToken.None);

        context.SagaData.CurrentState.ShouldBe(FinalizeActivity<TestSagaData>.FinalStateName);
    }

    [Fact]
    public async Task ExecuteAsync_FromInitialState_TransitionsToFinalState()
    {
        var activity = CreateActivity();
        var context = CreateContext();
        context.SagaData.CurrentState = "Initial";

        await activity.ExecuteAsync(context, CancellationToken.None);

        context.SagaData.CurrentState.ShouldBe("Final");
    }

    [Fact]
    public async Task ExecuteAsync_FromAnyState_TransitionsToFinalState()
    {
        var activity = CreateActivity();
        var context = CreateContext();
        context.SagaData.CurrentState = "WaitingForPayment";

        await activity.ExecuteAsync(context, CancellationToken.None);

        context.SagaData.CurrentState.ShouldBe("Final");
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

    [Fact]
    public void FinalStateName_IsFinal()
    {
        FinalizeActivity<TestSagaData>.FinalStateName.ShouldBe("Final");
    }

    private static FinalizeActivity<TestSagaData> CreateActivity()
    {
        Func<TestSagaData, string> accessor = data => data.CurrentState;
        Action<TestSagaData, string> mutator = (data, state) => data.CurrentState = state;

        return new FinalizeActivity<TestSagaData>(accessor, mutator);
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
