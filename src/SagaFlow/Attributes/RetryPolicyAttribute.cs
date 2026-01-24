namespace SagaFlow.Attributes;
/// <summary>
/// Configures retry policy for saga command/event handlers.
/// Apply to handler classes, not to command/event DTOs.
/// </summary>
/// <remarks>
/// <para>
/// This attribute controls retry behavior for transient failures
/// during handler execution (database timeouts, network issues, etc.).
/// </para>
/// <para>
/// For orchestrator-level timeout and resend behavior, use.....
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [RetryPolicy(MaxRetries = 3, Strategy = BackoffStrategy.Exponential)]
/// public class ReserveCreditHandler : ISagaCommandHandler&lt;ReserveCreditCommand&gt;
/// {
///     // Handler implementation
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class)]
public sealed class RetryPolicyAttribute : Attribute
{
    /// <summary>
    ///     Maximum number of retry attempts (0 = no retries, single attempt only).
    /// </summary>
    public int MaxRetries { get; init; } = 3;

    /// <summary>
    ///     Base delay in milliseconds between retry attempts.
    ///     The actual delay depends on the selected <see cref="Strategy" />.
    /// </summary>
    /// <remarks>
    ///     For <see cref="BackoffStrategy.Constant" />: delay remains constant (e.g., 1000ms, 1000ms, 1000ms).
    ///     For <see cref="BackoffStrategy.Linear" />: delay increases linearly (e.g., 1000ms, 2000ms, 3000ms).
    ///     For <see cref="BackoffStrategy.Exponential" />: delay doubles each attempt (e.g., 1000ms, 2000ms, 4000ms).
    /// </remarks>
    public int RetryDelayMilliseconds { get; init; } = 1000;

    /// <summary>
    ///     The backoff strategy to use for calculating delays between retry attempts.
    /// </summary>
    /// <value>
    ///     Default is <see cref="BackoffStrategy.Exponential" />.
    /// </value>
    public BackoffStrategy Strategy { get; init; } = BackoffStrategy.Exponential;

    /// <summary>
    ///     Whether to add random jitter to retry delays.
    /// </summary>
    /// <remarks>
    ///     Jitter helps prevent the "thundering herd" problem where multiple clients
    ///     retry at the same time, potentially overwhelming the system.
    ///     When enabled, a random value between 0% and 20% of the delay is added.
    /// </remarks>
    /// <value>
    ///     Default is <see langword="true" />.
    /// </value>
    public bool UseJitter { get; init; } = true;

    /// <summary>
    ///     Exception types that should trigger a retry attempt.
    /// </summary>
    /// <remarks>
    ///     If <see langword="null" /> or empty, all exceptions will trigger retries
    ///     (except those specified in <see cref="NonRetryableExceptions" />).
    /// </remarks>
    /// <example>
    ///     <code>
    /// RetryOnExceptions = new[] { typeof(HttpRequestException), typeof(TimeoutException) }
    /// </code>
    /// </example>
    public Type[]? RetryOnExceptions { get; init; }

    /// <summary>
    ///     Exception types that should NOT trigger a retry attempt.
    /// </summary>
    /// <remarks>
    ///     These exceptions will cause immediate failure without retries,
    ///     even if they match <see cref="RetryOnExceptions" /> or if <see cref="RetryOnExceptions" /> is <see langword="null" />.
    ///     Useful for exceptions that represent permanent failures (e.g., validation errors, authorization failures).
    /// </remarks>
    /// <example>
    ///     <code>
    /// NonRetryableExceptions = new[] { typeof(ArgumentException), typeof(UnauthorizedAccessException) }
    /// </code>
    /// </example>
    public Type[]? NonRetryableExceptions { get; init; }

    /// <summary>
    ///     Timeout duration in milliseconds.
    ///     0 means no timeout.
    /// </summary>
    public int TimeoutMilliseconds { get; init; }

    /// <summary>
    ///     Maximum delay cap in milliseconds (prevents exponential delays from growing too large).
    ///     Ensures retry delays don't exceed this value even with exponential backoff.
    /// </summary>
    public int MaxRetryDelayMilliseconds { get; init; } = 30000; // 30 seconds

    /// <summary>
    ///     Enable circuit breaker to prevent cascading failures.
    ///     When failures exceed threshold, circuit opens and requests fail fast.
    /// </summary>
    public bool EnableCircuitBreaker { get; init; }

    /// <summary>
    ///     Number of consecutive failures before circuit opens.
    ///     Only applies when <see cref="EnableCircuitBreaker" /> is true.
    /// </summary>
    public int CircuitBreakerThreshold { get; init; } = 5;

    /// <summary>
    ///     Duration in seconds the circuit stays open before attempting recovery.
    ///     Only applies when <see cref="EnableCircuitBreaker" /> is true.
    /// </summary>
    public int CircuitBreakerDurationSeconds { get; init; } = 60;
}

/// <summary>
///     Defines retry backoff strategies for calculating delays between retry attempts.
/// </summary>
public enum BackoffStrategy
{
    /// <summary>
    ///     The constant backoff type.
    /// </summary>
    /// <example>
    ///     200ms, 200ms, 200ms, etc.
    /// </example>
    /// <remarks>
    ///     Ensures a constant backoff for each attempt.
    /// </remarks>
    Constant,

    /// <summary>
    ///     The linear backoff type.
    /// </summary>
    /// <example>
    ///     100ms, 200ms, 300ms, 400ms, etc.
    /// </example>
    /// <remarks>
    ///     Generates backoffs in an linear manner.
    ///     In the case randomization introduced by the jitter and exponential growth are not appropriate,
    ///     the linear growth allows for more precise control over the backoff intervals.
    /// </remarks>
    Linear,

    /// <summary>
    ///     The exponential backoff type with the power of 2.
    /// </summary>
    /// <example>
    ///     200ms, 400ms, 800ms.
    /// </example>
    Exponential
}