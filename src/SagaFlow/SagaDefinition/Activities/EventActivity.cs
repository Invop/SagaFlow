using SagaFlow.Messages;

namespace SagaFlow.SagaDefinition.Activities;

/// <summary>
/// Represents an event activity that contains all activities to execute when a specific message type is received.
/// </summary>
/// <typeparam name="TSagaData">The type of saga data.</typeparam>
public sealed class EventActivity<TSagaData> where TSagaData : class
{
    /// <summary>
    /// Gets the type of message that triggers this event activity.
    /// </summary>
    public Type MessageType { get; }

    /// <summary>
    /// Gets the state in which this event activity is active.
    /// </summary>
    public State? State { get; }

    /// <summary>
    /// Gets the list of activities to execute for this event.
    /// </summary>
    public List<IActivity<TSagaData>> Activities { get; } = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="EventActivity{TSagaData}"/> class.
    /// </summary>
    /// <param name="messageType">The type of message that triggers this event.</param>
    /// <param name="state">The state in which this event is active (null for Initially).</param>
    /// <exception cref="ArgumentNullException">Thrown when messageType is null.</exception>
    public EventActivity(Type messageType, State? state)
    {
        ArgumentNullException.ThrowIfNull(messageType);

        if (!typeof(ISagaMessage).IsAssignableFrom(messageType))
        {
            throw new ArgumentException(
                $"Message type must implement {nameof(ISagaMessage)}.",
                nameof(messageType));
        }

        MessageType = messageType;
        State = state;
    }

    /// <summary>
    /// Executes all activities in sequence.
    /// </summary>
    /// <param name="context">The saga execution context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async ValueTask ExecuteAsync(SagaExecutionContext<TSagaData> context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        foreach (var activity in Activities)
        {
            await activity.ExecuteAsync(context, cancellationToken).ConfigureAwait(false);
        }
    }
}
