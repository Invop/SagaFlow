using SagaFlow.Messages;

namespace SagaFlow.SagaDefinition.Activities;

/// <summary>
/// Activity that registers a specific compensation message for a compensable step.
/// </summary>
/// <typeparam name="TSagaData">The type of saga data.</typeparam>
/// <typeparam name="TMessage">The type of message being processed.</typeparam>
/// <typeparam name="TCompensationMessage">The type of compensation message.</typeparam>
public sealed class CompensateWithActivity<TSagaData, TMessage, TCompensationMessage> : IActivity<TSagaData>
    where TSagaData : class
    where TMessage : ISagaMessage
    where TCompensationMessage : ISagaMessage
{
    private readonly Func<ISagaMessageContext<TMessage>, TSagaData, TCompensationMessage> _compensationFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompensateWithActivity{TSagaData, TMessage, TCompensationMessage}"/> class.
    /// </summary>
    /// <param name="compensationFactory">Factory to create the compensation message.</param>
    /// <exception cref="ArgumentNullException">Thrown when compensationFactory is null.</exception>
    public CompensateWithActivity(Func<ISagaMessageContext<TMessage>, TSagaData, TCompensationMessage> compensationFactory)
    {
        ArgumentNullException.ThrowIfNull(compensationFactory);
        _compensationFactory = compensationFactory;
    }

    /// <inheritdoc/>
    public ValueTask ExecuteAsync(SagaExecutionContext<TSagaData> context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Don't add compensation if pivot has been reached
        if (context.IsPivotReached)
        {
            return ValueTask.CompletedTask;
        }

        var typedContext = (ISagaMessageContext<TMessage>)context.MessageContext;
        var compensationMessage = _compensationFactory(typedContext, context.SagaData);

        ArgumentNullException.ThrowIfNull(compensationMessage, nameof(compensationMessage));

        // Add to compensation stack (LIFO)
        context.CompensationMessages.Insert(0, compensationMessage);

        return ValueTask.CompletedTask;
    }
}
