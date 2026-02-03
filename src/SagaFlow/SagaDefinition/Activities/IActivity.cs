namespace SagaFlow.SagaDefinition.Activities;

/// <summary>
/// Base interface for all saga activities.
/// Activities represent actions that occur during saga execution in response to events.
/// </summary>
/// <typeparam name="TSagaData">The type of saga data.</typeparam>
public interface IActivity<TSagaData> where TSagaData : class
{
    /// <summary>
    /// Executes the activity.
    /// </summary>
    /// <param name="context">The saga execution context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    ValueTask ExecuteAsync(SagaExecutionContext<TSagaData> context, CancellationToken cancellationToken);
}
