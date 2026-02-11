using SagaFlow.Messages;
using SagaFlow.SagaDefinition;

namespace SagaFlow.Unit.Tests.Registration;

/// <summary>
/// Saga instance type used for Registration tests.
/// </summary>
public sealed class RegistrationTestInstance : ISagaStateMachineInstance
{
    public Guid CorrelationId { get; set; } = Guid.NewGuid();
    public string CurrentState { get; set; } = "Initial";
}

/// <summary>
/// Second saga instance type to test multi-saga scenarios.
/// </summary>
public sealed class SecondRegistrationTestInstance : ISagaStateMachineInstance
{
    public Guid CorrelationId { get; set; } = Guid.NewGuid();
    public string CurrentState { get; set; } = "Initial";
}

/// <summary>
/// A valid saga that handles <see cref="TestCommand"/> in the Initially block.
/// </summary>
public sealed class ValidTestSaga : SagaStateMachine<RegistrationTestInstance>
{
    public ValidTestSaga()
    {
        InstanceState(x => x.CurrentState);

        Initially(
            When<TestCommand>()
        );
    }
}

/// <summary>
/// A second valid saga that also handles <see cref="TestCommand"/> for multi-registration tests.
/// </summary>
public sealed class SecondValidTestSaga : SagaStateMachine<SecondRegistrationTestInstance>
{
    public SecondValidTestSaga()
    {
        InstanceState(x => x.CurrentState);

        Initially(
            When<TestCommand>()
        );
    }
}

/// <summary>
/// An abstract saga type — should be rejected by <see cref="SagaDescriptor.Create"/>.
/// </summary>
public abstract class AbstractTestSaga : SagaStateMachine<RegistrationTestInstance>;

/// <summary>
/// An instance type without a parameterless constructor — should be rejected.
/// </summary>
public sealed class NoDefaultCtorInstance : ISagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; }

    public NoDefaultCtorInstance(string required)
    {
        CurrentState = required;
    }
}
