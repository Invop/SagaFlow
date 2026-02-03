using SagaFlow.Messages;

namespace SagaFlow.SagaDefinition.Activities;

/// <summary>
/// Represents the execution context for a saga activity.
/// Provides access to saga data, current message, and services.
/// </summary>
/// <typeparam name="TSagaData">The type of saga data.</typeparam>
public sealed class SagaExecutionContext<TSagaData> where TSagaData : class
{
    /// <summary>
    /// Gets the saga data instance.
    /// </summary>
    public TSagaData SagaData { get; }

    /// <summary>
    /// Gets the current message being processed.
    /// </summary>
    public ISagaMessage Message { get; }

    /// <summary>
    /// Gets the message context.
    /// </summary>
    public object MessageContext { get; }

    /// <summary>
    /// Gets the service provider for dependency resolution.
    /// </summary>
    public IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// Gets or sets the collection of compensation messages to be executed on rollback.
    /// </summary>
    public List<ISagaMessage> CompensationMessages { get; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether the saga has reached a pivot point (point of no return).
    /// </summary>
    public bool IsPivotReached { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SagaExecutionContext{TSagaData}"/> class.
    /// </summary>
    /// <param name="sagaData">The saga data instance.</param>
    /// <param name="message">The current message being processed.</param>
    /// <param name="messageContext">The message context.</param>
    /// <param name="serviceProvider">The service provider.</param>
    public SagaExecutionContext(
        TSagaData sagaData,
        ISagaMessage message,
        object messageContext,
        IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(sagaData);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(messageContext);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        SagaData = sagaData;
        Message = message;
        MessageContext = messageContext;
        ServiceProvider = serviceProvider;
    }
}
