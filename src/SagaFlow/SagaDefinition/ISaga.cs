namespace SagaFlow.SagaDefinition;

/// <summary>
///     Defines a saga - a long-running business process that coordinates multiple steps and maintains state.
///     Each saga instance is uniquely identified by a correlation ID for message routing and state tracking.
/// </summary>
public interface ISaga
{
    /// <summary>
    ///     Identifies the saga instance uniquely, and is the primary correlation
    ///     for the instance. While the setter is not typically called, it is there
    ///     to support persistence consistently across implementations.
    /// </summary>
    Guid CorrelationId { get; set; }
}