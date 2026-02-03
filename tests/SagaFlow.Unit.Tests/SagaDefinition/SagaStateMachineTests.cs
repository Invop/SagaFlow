using SagaFlow.SagaDefinition;

namespace SagaFlow.Unit.Tests.SagaDefinition;

/// <summary>
/// Unit tests for <see cref="SagaStateMachine{TSagaData}"/> class.
/// Tests state machine creation, state registration, and event handling configuration.
/// </summary>
public sealed class SagaStateMachineTests
{
    [Fact]
    public void Constructor_CreatesInitialAndFinalStates()
    {
        var stateMachine = new TestSagaStateMachine();

        var states = ((ISagaStateMachine<TestSagaData>)stateMachine).States.ToList();

        states.ShouldContain(s => s.Name == "Initial");
        states.ShouldContain(s => s.Name == "Final");
    }

    [Fact]
    public void Initial_ReturnsInitialState()
    {
        var stateMachine = new TestSagaStateMachine();

        var initial = ((ISagaStateMachine<TestSagaData>)stateMachine).Initial;

        initial.Name.ShouldBe("Initial");
    }

    [Fact]
    public void Final_ReturnsFinalState()
    {
        var stateMachine = new TestSagaStateMachine();

        var final = ((ISagaStateMachine<TestSagaData>)stateMachine).Final;

        final.Name.ShouldBe("Final");
    }

    [Fact]
    public void State_WithValidName_CreatesNewState()
    {
        var stateMachine = new TestSagaStateMachine();

        stateMachine.CreateCustomState("Processing");
        var states = ((ISagaStateMachine<TestSagaData>)stateMachine).States.ToList();

        states.ShouldContain(s => s.Name == "Processing");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void State_WithNullOrWhitespaceName_ThrowsArgumentException(string? invalidName)
    {
        var stateMachine = new TestSagaStateMachine();

        Should.Throw<ArgumentException>(() => stateMachine.CreateCustomState(invalidName!));
    }

    [Fact]
    public void State_WithDuplicateName_ThrowsArgumentException()
    {
        var stateMachine = new TestSagaStateMachine();
        stateMachine.CreateCustomState("Processing");

        var exception = Should.Throw<ArgumentException>(() => stateMachine.CreateCustomState("Processing"));

        exception.Message.ShouldContain("Processing");
        exception.Message.ShouldContain("already exists");
    }

    [Fact]
    public async Task IsCompleted_WhenInFinalState_ReturnsTrue()
    {
        var stateMachine = new TestSagaStateMachine();
        var sagaData = new TestSagaData { CurrentState = "Final" };

        var result = await stateMachine.IsCompleted(sagaData);

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task IsCompleted_WhenNotInFinalState_ReturnsFalse()
    {
        var stateMachine = new TestSagaStateMachine();
        var sagaData = new TestSagaData { CurrentState = "Processing" };

        var result = await stateMachine.IsCompleted(sagaData);

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task IsCompleted_WhenInInitialState_ReturnsFalse()
    {
        var stateMachine = new TestSagaStateMachine();
        var sagaData = new TestSagaData { CurrentState = "Initial" };

        var result = await stateMachine.IsCompleted(sagaData);

        result.ShouldBeFalse();
    }

    [Fact]
    public void IsCompleted_WithNullInstance_ThrowsArgumentNullException()
    {
        var stateMachine = new TestSagaStateMachine();

        Should.ThrowAsync<ArgumentNullException>(async () => await stateMachine.IsCompleted(null!));
    }

    [Fact]
    public void GetStateAccessor_WhenNotConfigured_ThrowsInvalidOperationException()
    {
        var stateMachine = new TestSagaStateMachine();

        Should.Throw<InvalidOperationException>(() => stateMachine.GetAccessor());
    }

    [Fact]
    public void GetStateMutator_WhenNotConfigured_ThrowsInvalidOperationException()
    {
        var stateMachine = new TestSagaStateMachine();

        Should.Throw<InvalidOperationException>(() => stateMachine.GetMutator());
    }

    [Fact]
    public void InstanceState_WithValidExpression_ConfiguresAccessorAndMutator()
    {
        var stateMachine = new TestSagaStateMachineWithConfig();

        var accessor = stateMachine.GetAccessor();
        var mutator = stateMachine.GetMutator();

        accessor.ShouldNotBeNull();
        mutator.ShouldNotBeNull();
    }

    [Fact]
    public void InstanceState_Accessor_CorrectlyReadsState()
    {
        var stateMachine = new TestSagaStateMachineWithConfig();
        var sagaData = new TestSagaData { CurrentState = "Processing" };

        var accessor = stateMachine.GetAccessor();
        var result = accessor(sagaData);

        result.ShouldBe("Processing");
    }

    [Fact]
    public void InstanceState_Mutator_CorrectlySetsState()
    {
        var stateMachine = new TestSagaStateMachineWithConfig();
        var sagaData = new TestSagaData { CurrentState = "Initial" };

        var mutator = stateMachine.GetMutator();
        mutator(sagaData, "Completed");

        sagaData.CurrentState.ShouldBe("Completed");
    }

    [Fact]
    public void InstanceState_WithNullExpression_ThrowsArgumentNullException()
    {
        var stateMachine = new TestSagaStateMachineWithNullConfig();

        Should.Throw<ArgumentNullException>(() => stateMachine.Configure());
    }

    [Fact]
    public void GetState_WithExistingName_ReturnsState()
    {
        var stateMachine = new TestSagaStateMachine();
        stateMachine.CreateCustomState("Processing");

        var state = stateMachine.GetStateByName("Processing");

        state.ShouldNotBeNull();
        state.Name.ShouldBe("Processing");
    }

    [Fact]
    public void GetState_WithNonExistingName_ReturnsNull()
    {
        var stateMachine = new TestSagaStateMachine();

        var state = stateMachine.GetStateByName("NonExistent");

        state.ShouldBeNull();
    }

    [Fact]
    public void GetAllStates_ReturnsAllRegisteredStates()
    {
        var stateMachine = new TestSagaStateMachine();
        stateMachine.CreateCustomState("Processing");
        stateMachine.CreateCustomState("WaitingForPayment");

        var allStates = stateMachine.GetAllRegisteredStates();

        allStates.Count.ShouldBe(4); // Initial, Final, Processing, WaitingForPayment
        allStates.ShouldContainKey("Initial");
        allStates.ShouldContainKey("Final");
        allStates.ShouldContainKey("Processing");
        allStates.ShouldContainKey("WaitingForPayment");
    }

    [Fact]
    public void GetEventActivitiesForMessage_WithNoRegisteredHandlers_ReturnsEmptyList()
    {
        var stateMachine = new TestSagaStateMachine();

        var activities = stateMachine.GetActivitiesForMessage(typeof(TestCommand));

        activities.ShouldBeEmpty();
    }

    [Fact]
    public void GetEventActivitiesForMessage_WithNullMessageType_ThrowsArgumentNullException()
    {
        var stateMachine = new TestSagaStateMachine();

        Should.Throw<ArgumentNullException>(() => stateMachine.GetActivitiesForMessage(null!));
    }

    /// <summary>
    /// Test saga state machine implementation for testing base functionality.
    /// </summary>
    private sealed class TestSagaStateMachine : SagaStateMachine<TestSagaData>
    {
        public void CreateCustomState(string name) => State(name);

        public Func<TestSagaData, string> GetAccessor() => GetStateAccessor();

        public Action<TestSagaData, string> GetMutator() => GetStateMutator();

        public State<TestSagaData>? GetStateByName(string name) => GetState(name);

        public IReadOnlyDictionary<string, State<TestSagaData>> GetAllRegisteredStates() => GetAllStates();

        public IReadOnlyList<SagaFlow.SagaDefinition.Activities.EventActivity<TestSagaData>> GetActivitiesForMessage(Type messageType)
            => GetEventActivitiesForMessage(messageType);
    }

    /// <summary>
    /// Test saga state machine with InstanceState configured.
    /// </summary>
    private sealed class TestSagaStateMachineWithConfig : SagaStateMachine<TestSagaData>
    {
        public TestSagaStateMachineWithConfig()
        {
            InstanceState(x => x.CurrentState);
        }

        public Func<TestSagaData, string> GetAccessor() => GetStateAccessor();

        public Action<TestSagaData, string> GetMutator() => GetStateMutator();
    }

    /// <summary>
    /// Test saga state machine that attempts null configuration.
    /// </summary>
    private sealed class TestSagaStateMachineWithNullConfig : SagaStateMachine<TestSagaData>
    {
        public void Configure() => InstanceState(null!);
    }
}
