using System.Reflection;
using SagaFlow.SagaDefinition;

namespace SagaFlow.Unit.Tests.SagaDefinition;

/// <summary>
/// Unit tests for <see cref="State{TSagaData}"/> class.
/// Tests typed state creation, entry/exit actions, and implicit conversion.
/// </summary>
public sealed class TypedStateTests
{
    [Fact]
    public void Constructor_WithValidState_CreatesTypedState()
    {
        var internalState = new State("Processing");

        var typedState = CreateTypedState(internalState);

        typedState.InternalState.ShouldBe(internalState);
        typedState.Name.ShouldBe("Processing");
    }

    [Fact]
    public void Constructor_WithNullState_ThrowsArgumentNullException()
    {
        Should.Throw<TargetInvocationException>(() => CreateTypedState(null!));
    }

    [Fact]
    public void Name_ReturnsInternalStateName()
    {
        var internalState = new State("WaitingForApproval");
        var typedState = CreateTypedState(internalState);

        typedState.Name.ShouldBe("WaitingForApproval");
    }

    [Fact]
    public void ToString_ReturnsStateName()
    {
        var internalState = new State("Completed");
        var typedState = CreateTypedState(internalState);

        var result = typedState.ToString();

        result.ShouldBe("Completed");
    }

    [Fact]
    public void OnEntry_WhenSet_CanBeRetrieved()
    {
        var internalState = new State("Active");
        var typedState = CreateTypedState(internalState);
        Func<TestSagaData, CancellationToken, ValueTask> entryAction = (_, _) => ValueTask.CompletedTask;

        typedState.OnEntry = entryAction;

        typedState.OnEntry.ShouldBe(entryAction);
    }

    [Fact]
    public void OnExit_WhenSet_CanBeRetrieved()
    {
        var internalState = new State("Active");
        var typedState = CreateTypedState(internalState);
        Func<TestSagaData, CancellationToken, ValueTask> exitAction = (_, _) => ValueTask.CompletedTask;

        typedState.OnExit = exitAction;

        typedState.OnExit.ShouldBe(exitAction);
    }

    [Fact]
    public async Task ExecuteOnEntryAsync_WithEntryAction_ExecutesAction()
    {
        var stateMachine = new TestStateMachineForTypedState();
        var typedState = stateMachine.GetProcessingState();
        var sagaData = new TestSagaData();
        var wasExecuted = false;
        typedState.OnEntry = (data, _) =>
        {
            wasExecuted = true;
            data.CurrentState = "EntryExecuted";
            return ValueTask.CompletedTask;
        };

        await InvokeExecuteOnEntryAsync(typedState, sagaData, CancellationToken.None);

        wasExecuted.ShouldBeTrue();
        sagaData.CurrentState.ShouldBe("EntryExecuted");
    }

    [Fact]
    public async Task ExecuteOnEntryAsync_WithoutEntryAction_DoesNotThrow()
    {
        var stateMachine = new TestStateMachineForTypedState();
        var typedState = stateMachine.GetProcessingState();
        var sagaData = new TestSagaData();

        await Should.NotThrowAsync(async () =>
            await InvokeExecuteOnEntryAsync(typedState, sagaData, CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteOnExitAsync_WithExitAction_ExecutesAction()
    {
        var stateMachine = new TestStateMachineForTypedState();
        var typedState = stateMachine.GetProcessingState();
        var sagaData = new TestSagaData();
        var wasExecuted = false;
        typedState.OnExit = (data, _) =>
        {
            wasExecuted = true;
            data.CurrentState = "ExitExecuted";
            return ValueTask.CompletedTask;
        };

        await InvokeExecuteOnExitAsync(typedState, sagaData, CancellationToken.None);

        wasExecuted.ShouldBeTrue();
        sagaData.CurrentState.ShouldBe("ExitExecuted");
    }

    [Fact]
    public async Task ExecuteOnExitAsync_WithoutExitAction_DoesNotThrow()
    {
        var stateMachine = new TestStateMachineForTypedState();
        var typedState = stateMachine.GetProcessingState();
        var sagaData = new TestSagaData();

        await Should.NotThrowAsync(async () =>
            await InvokeExecuteOnExitAsync(typedState, sagaData, CancellationToken.None));
    }

    [Fact]
    public void ImplicitConversion_ToState_ReturnsInternalState()
    {
        var internalState = new State("Processing");
        var typedState = CreateTypedState(internalState);

        State convertedState = typedState;

        convertedState.ShouldBe(internalState);
    }

    /// <summary>
    /// Creates a State{TestSagaData} instance using reflection since the constructor is internal.
    /// </summary>
    private static State<TestSagaData> CreateTypedState(State state)
    {
        var constructor = typeof(State<TestSagaData>).GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            [typeof(State)],
            null);

        return (State<TestSagaData>)constructor!.Invoke([state]);
    }

    /// <summary>
    /// Invokes ExecuteOnEntryAsync using reflection since the method is internal.
    /// </summary>
    private static async ValueTask InvokeExecuteOnEntryAsync(
        State<TestSagaData> state,
        TestSagaData sagaData,
        CancellationToken cancellationToken)
    {
        var method = typeof(State<TestSagaData>).GetMethod(
            "ExecuteOnEntryAsync",
            BindingFlags.NonPublic | BindingFlags.Instance);

        var task = (ValueTask)method!.Invoke(state, [sagaData, cancellationToken])!;
        await task;
    }

    /// <summary>
    /// Invokes ExecuteOnExitAsync using reflection since the method is internal.
    /// </summary>
    private static async ValueTask InvokeExecuteOnExitAsync(
        State<TestSagaData> state,
        TestSagaData sagaData,
        CancellationToken cancellationToken)
    {
        var method = typeof(State<TestSagaData>).GetMethod(
            "ExecuteOnExitAsync",
            BindingFlags.NonPublic | BindingFlags.Instance);

        var task = (ValueTask)method!.Invoke(state, [sagaData, cancellationToken])!;
        await task;
    }

    /// <summary>
    /// Test state machine to create typed states for testing.
    /// </summary>
    private sealed class TestStateMachineForTypedState : SagaStateMachine<TestSagaData>
    {
        private readonly State<TestSagaData> _processing;

        public TestStateMachineForTypedState()
        {
            _processing = State("Processing");
        }

        public State<TestSagaData> GetProcessingState() => _processing;
    }
}
