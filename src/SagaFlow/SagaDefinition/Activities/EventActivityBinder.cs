using SagaFlow.Messages;

namespace SagaFlow.SagaDefinition.Activities;

/// <summary>
/// Implementation of the fluent interface for binding activities to a message event.
/// </summary>
/// <typeparam name="TSagaData">The type of saga data.</typeparam>
/// <typeparam name="TMessage">The type of message being handled.</typeparam>
internal sealed class EventActivityBinder<TSagaData, TMessage> : IEventActivityBinder<TSagaData, TMessage>
    where TSagaData : class
    where TMessage : ISagaMessage
{
    private readonly EventActivity<TSagaData> _eventActivity;
    private readonly Func<TSagaData, string> _stateAccessor;
    private readonly Action<TSagaData, string> _stateMutator;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventActivityBinder{TSagaData, TMessage}"/> class.
    /// </summary>
    /// <param name="eventActivity">The event activity to bind to.</param>
    /// <param name="stateAccessor">Function to get current state from saga data.</param>
    /// <param name="stateMutator">Action to set current state in saga data.</param>
    public EventActivityBinder(
        EventActivity<TSagaData> eventActivity,
        Func<TSagaData, string> stateAccessor,
        Action<TSagaData, string> stateMutator)
    {
        ArgumentNullException.ThrowIfNull(eventActivity);
        ArgumentNullException.ThrowIfNull(stateAccessor);
        ArgumentNullException.ThrowIfNull(stateMutator);

        _eventActivity = eventActivity;
        _stateAccessor = stateAccessor;
        _stateMutator = stateMutator;
    }

    /// <inheritdoc/>
    public IEventActivityBinder<TSagaData, TMessage> Then(Action<ISagaMessageContext<TMessage>, TSagaData> action)
    {
        var activity = new ThenActivity<TSagaData, TMessage>(action);
        _eventActivity.Activities.Add(activity);
        return this;
    }

    /// <inheritdoc/>
    public IEventActivityBinder<TSagaData, TMessage> ThenAsync(
        Func<ISagaMessageContext<TMessage>, TSagaData, CancellationToken, ValueTask> asyncAction)
    {
        var activity = new ThenAsyncActivity<TSagaData, TMessage>(asyncAction);
        _eventActivity.Activities.Add(activity);
        return this;
    }

    /// <inheritdoc/>
    public IEventActivityBinder<TSagaData, TMessage> Publish<TPublishMessage>(
        Func<ISagaMessageContext<TMessage>, TSagaData, TPublishMessage> messageFactory)
        where TPublishMessage : ISagaMessage
    {
        var activity = new PublishMessageActivity<TSagaData, TMessage, TPublishMessage>(messageFactory);
        _eventActivity.Activities.Add(activity);
        return this;
    }

    /// <inheritdoc/>
    public IEventActivityBinder<TSagaData, TMessage> PublishAsync<TPublishMessage>(
        Func<ISagaMessageContext<TMessage>, TSagaData, CancellationToken, ValueTask<TPublishMessage>> asyncMessageFactory)
        where TPublishMessage : ISagaMessage
    {
        var activity = new PublishMessageAsyncActivity<TSagaData, TMessage, TPublishMessage>(asyncMessageFactory);
        _eventActivity.Activities.Add(activity);
        return this;
    }

    /// <inheritdoc/>
    public IEventActivityBinder<TSagaData, TMessage> Send<TSendMessage>(
        Func<ISagaMessageContext<TMessage>, TSagaData, TSendMessage> messageFactory)
        where TSendMessage : ISagaMessage
    {
        var activity = new SendMessageActivity<TSagaData, TMessage, TSendMessage>(messageFactory);
        _eventActivity.Activities.Add(activity);
        return this;
    }

    /// <inheritdoc/>
    public IEventActivityBinder<TSagaData, TMessage> SendAsync<TSendMessage>(
        Func<ISagaMessageContext<TMessage>, TSagaData, CancellationToken, ValueTask<TSendMessage>> asyncMessageFactory)
        where TSendMessage : ISagaMessage
    {
        var activity = new SendMessageAsyncActivity<TSagaData, TMessage, TSendMessage>(asyncMessageFactory);
        _eventActivity.Activities.Add(activity);
        return this;
    }


    /// <inheritdoc/>
    public IEventActivityBinder<TSagaData, TMessage> CompensateWith<TCompensationMessage>(
        Func<ISagaMessageContext<TMessage>, TSagaData, TCompensationMessage> compensationFactory)
        where TCompensationMessage : ISagaMessage
    {
        var activity = new CompensateWithActivity<TSagaData, TMessage, TCompensationMessage>(compensationFactory);
        _eventActivity.Activities.Add(activity);
        return this;
    }

    /// <inheritdoc/>
    public IEventActivityBinder<TSagaData, TMessage> Pivot()
    {
        var activity = new PivotActivity<TSagaData>();
        _eventActivity.Activities.Add(activity);
        return this;
    }

    /// <inheritdoc/>
    public IEventActivityBinder<TSagaData, TMessage> TransitionTo(State<TSagaData> targetState)
    {
        var activity = new TransitionActivity<TSagaData>(targetState, _stateAccessor, _stateMutator);
        _eventActivity.Activities.Add(activity);
        return this;
    }

    /// <inheritdoc/>
    public IEventActivityBinder<TSagaData, TMessage> Complete()
    {
        var activity = new FinalizeActivity<TSagaData>(_stateAccessor, _stateMutator);
        _eventActivity.Activities.Add(activity);
        return this;
    }
}
