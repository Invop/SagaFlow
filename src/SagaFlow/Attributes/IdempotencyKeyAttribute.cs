namespace SagaFlow.Attributes;

/// <summary>
///     Marks a property as part of the idempotency key for saga message deduplication.
///     Multiple properties can be marked to form a composite idempotency key,
///     which will be calculated as a SHA256 hash of all marked fields.
/// </summary>
/// <remarks>
///     Apply this attribute to properties that together uniquely identify a message
///     to prevent duplicate processing. The idempotency key (SHA256 hash) ensures that the same
///     operation is not executed multiple times even if the message is received more than once.
/// </remarks>
/// <example>
///     <code>
/// public record ProcessPaymentCommand : ISagaCommand
/// {
///     public Guid OrderId { get; init; }
///     
///     [IdempotencyKey(Order = 1)]
///     public Guid PaymentId { get; init; }
///     
///     [IdempotencyKey(Order = 2)]
///     public string TransactionType { get; init; }
///     
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property)]
public sealed class IdempotencyKeyAttribute : Attribute
{
    /// <summary>
    ///     Gets or sets the order of this property in the composite idempotency key.
    ///     Lower values are processed first. Default is 0.
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    ///     Gets or sets the format string to use when converting the property value to string.
    ///     If not specified, <see cref="object.ToString" /> will be used.
    /// </summary>
    public string? Format { get; set; }
}