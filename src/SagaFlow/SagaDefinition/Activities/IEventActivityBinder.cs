using SagaFlow.Messages;

namespace SagaFlow.SagaDefinition.Activities;

/// <summary>
/// Base non-generic interface for event activity binders.
/// Used for params arrays in Initially/During methods.
/// </summary>
public interface IEventActivityBinder
{
}

/// <summary>
/// Fluent interface for binding activities to a message event.
/// </summary>
/// <typeparam name="TSagaData">The type of saga data.</typeparam>
/// <typeparam name="TMessage">The type of message being handled.</typeparam>
public interface IEventActivityBinder<TSagaData, TMessage> : IEventActivityBinder
    where TSagaData : class
    where TMessage : ISagaMessage
{
    /// <summary>
    /// Adds a synchronous action to execute when the message is received.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <returns>The activity binder for method chaining.</returns>
    IEventActivityBinder<TSagaData, TMessage> Then(Action<ISagaMessageContext<TMessage>, TSagaData> action);

    /// <summary>
    /// Adds an asynchronous action to execute when the message is received.
    /// </summary>
    /// <param name="asyncAction">The asynchronous action to execute.</param>
    /// <returns>The activity binder for method chaining.</returns>
    IEventActivityBinder<TSagaData, TMessage> ThenAsync(
        Func<ISagaMessageContext<TMessage>, TSagaData, CancellationToken, ValueTask> asyncAction);

    /// <summary>
    /// Publishes a message through the Transactional Outbox pattern.
    /// </summary>
    /// <typeparam name="TPublishMessage">The type of message to publish.</typeparam>
    /// <param name="messageFactory">Factory to create the message.</param>
    /// <returns>The activity binder for method chaining.</returns>
    IEventActivityBinder<TSagaData, TMessage> Publish<TPublishMessage>(
        Func<ISagaMessageContext<TMessage>, TSagaData, TPublishMessage> messageFactory)
        where TPublishMessage : ISagaMessage;

    /// <summary>
    /// Publishes a message asynchronously through the Transactional Outbox pattern.
    /// </summary>
    /// <typeparam name="TPublishMessage">The type of message to publish.</typeparam>
    /// <param name="asyncMessageFactory">Async factory to create the message.</param>
    /// <returns>The activity binder for method chaining.</returns>
    IEventActivityBinder<TSagaData, TMessage> PublishAsync<TPublishMessage>(
        Func<ISagaMessageContext<TMessage>, TSagaData, CancellationToken, ValueTask<TPublishMessage>> asyncMessageFactory)
        where TPublishMessage : ISagaMessage;

    /// <summary>
    /// Sends a message locally (in-process) via ISagaMessageHandler.
    /// </summary>
    /// <typeparam name="TSendMessage">The type of message to send.</typeparam>
    /// <param name="messageFactory">Factory to create the message.</param>
    /// <returns>The activity binder for method chaining.</returns>
    IEventActivityBinder<TSagaData, TMessage> Send<TSendMessage>(
        Func<ISagaMessageContext<TMessage>, TSagaData, TSendMessage> messageFactory)
        where TSendMessage : ISagaMessage;

    /// <summary>
    /// Sends a message asynchronously locally (in-process) via ISagaMessageHandler.
    /// </summary>
    /// <typeparam name="TSendMessage">The type of message to send.</typeparam>
    /// <param name="asyncMessageFactory">Async factory to create the message.</param>
    /// <returns>The activity binder for method chaining.</returns>
    IEventActivityBinder<TSagaData, TMessage> SendAsync<TSendMessage>(
        Func<ISagaMessageContext<TMessage>, TSagaData, CancellationToken, ValueTask<TSendMessage>> asyncMessageFactory)
        where TSendMessage : ISagaMessage;

    /// <summary>
    /// Registers a compensation message for this step.
    /// </summary>
    /// <typeparam name="TCompensationMessage">The type of compensation message.</typeparam>
    /// <param name="compensationFactory">Factory to create the compensation message.</param>
    /// <returns>The activity binder for method chaining.</returns>
    IEventActivityBinder<TSagaData, TMessage> CompensateWith<TCompensationMessage>(
        Func<ISagaMessageContext<TMessage>, TSagaData, TCompensationMessage> compensationFactory)
        where TCompensationMessage : ISagaMessage;

    /// <summary>
    /// Marks this step as a pivot point (point of no return).
    /// </summary>
    /// <returns>The activity binder for method chaining.</returns>
    IEventActivityBinder<TSagaData, TMessage> Pivot();


    /// <summary>
    /// Transitions the saga to the specified state.
    /// </summary>
    /// <param name="targetState">The state to transition to.</param>
    /// <returns>The activity binder for method chaining.</returns>
    IEventActivityBinder<TSagaData, TMessage> TransitionTo(State<TSagaData> targetState);

    /// <summary>
    /// Marks the saga as completed and transitions to the Final state.
    /// </summary>
    /// <returns>The activity binder for method chaining.</returns>
    IEventActivityBinder<TSagaData, TMessage> Complete();
}
