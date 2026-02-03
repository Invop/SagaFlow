using SagaFlow.Messages;

namespace SagaFlow.SagaDefinition.Activities;

/// <summary>
/// Activity that executes an asynchronous action.
/// </summary>
/// <typeparam name="TSagaData">The type of saga data.</typeparam>
/// <typeparam name="TMessage">The type of message being processed.</typeparam>
public sealed class ThenAsyncActivity<TSagaData, TMessage> : IActivity<TSagaData>
    where TSagaData : class
    where TMessage : ISagaMessage
{
    private readonly Func<ISagaMessageContext<TMessage>, TSagaData, CancellationToken, ValueTask> _asyncAction;

    /// <summary>
    /// Initializes a new instance of the <see cref="ThenAsyncActivity{TSagaData, TMessage}"/> class.
    /// </summary>
    /// <param name="asyncAction">The asynchronous action to execute.</param>
    /// <exception cref="ArgumentNullException">Thrown when asyncAction is null.</exception>
    public ThenAsyncActivity(Func<ISagaMessageContext<TMessage>, TSagaData, CancellationToken, ValueTask> asyncAction)
    {
        ArgumentNullException.ThrowIfNull(asyncAction);
        _asyncAction = asyncAction;
    }

    /// <inheritdoc/>
    public async ValueTask ExecuteAsync(SagaExecutionContext<TSagaData> context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        var typedContext = (ISagaMessageContext<TMessage>)context.MessageContext;
        await _asyncAction(typedContext, context.SagaData, cancellationToken).ConfigureAwait(false);
    }
}
