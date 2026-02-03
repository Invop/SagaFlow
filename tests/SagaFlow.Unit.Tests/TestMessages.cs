using SagaFlow.Messages;

namespace SagaFlow.Unit.Tests;

/// <summary>
/// Test command implementation for unit tests.
/// </summary>
public sealed record TestCommand : ISagaMessage
{
    public required string IdempotencyKey { get; init; }
    public required Guid CorrelationId { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public required string OrderId { get; init; }
    public required decimal Amount { get; init; }
}

/// <summary>
/// Test event implementation for unit tests.
/// </summary>
public sealed record TestEvent : ISagaMessage
{
    public required string IdempotencyKey { get; init; }
    public required Guid CorrelationId { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public required string StepId { get; init; }
    public required bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public required string EventData { get; init; }
}
