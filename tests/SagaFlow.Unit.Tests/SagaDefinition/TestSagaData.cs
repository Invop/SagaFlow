using SagaFlow.SagaDefinition;

namespace SagaFlow.Unit.Tests.SagaDefinition;

/// <summary>
/// Test implementation of <see cref="ISagaStateMachineInstance"/> for unit tests.
/// </summary>
public sealed class TestSagaData : ISagaStateMachineInstance
{
    public Guid CorrelationId { get; set; } = Guid.NewGuid();
    public string CurrentState { get; set; } = "Initial";
    public string? OrderId { get; set; }
    public decimal Amount { get; set; }
    public bool IsProcessed { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
