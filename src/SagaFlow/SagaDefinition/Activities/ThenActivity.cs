using SagaFlow.Messages;

namespace SagaFlow.SagaDefinition.Activities;

/// <summary>
/// Activity that executes a synchronous action.
/// </summary>
/// <typeparam name="TSagaData">The type of saga data.</typeparam>
/// <typeparam name="TMessage">The type of message being processed.</typeparam>
public sealed class ThenActivity<TSagaData, TMessage> : IActivity<TSagaData>
    where TSagaData : class
    where TMessage : ISagaMessage
{
    private readonly Action<ISagaMessageContext<TMessage>, TSagaData> _action;

    /// <summary>
    /// Initializes a new instance of the <see cref="ThenActivity{TSagaData, TMessage}"/> class.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <exception cref="ArgumentNullException">Thrown when action is null.</exception>
    public ThenActivity(Action<ISagaMessageContext<TMessage>, TSagaData> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        _action = action;
    }

    /// <inheritdoc/>
    public ValueTask ExecuteAsync(SagaExecutionContext<TSagaData> context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        var typedContext = (ISagaMessageContext<TMessage>)context.MessageContext;
        _action(typedContext, context.SagaData);

        return ValueTask.CompletedTask;
    }
}
