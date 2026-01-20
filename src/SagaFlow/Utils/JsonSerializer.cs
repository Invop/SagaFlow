using System.Text.Json;
using SagaFlow.Messages;

namespace SagaFlow.Utils;

/// <summary>
///     Default JSON-based implementation of <see cref="ISerializer"/>.
///     Uses System.Text.Json for serialization and deserialization of message payloads.
/// </summary>
/// <remarks>
///     <para>
///     This serializer stores the full type name with each message to support polymorphic deserialization.
///     It uses <see cref="JsonSerializerOptions"/> with reasonable defaults for most scenarios.
///     </para>
///     <para>
///     For production use, consider configuring custom <see cref="JsonSerializerOptions"/>
///     to handle specific serialization requirements like custom converters or naming policies.
///     </para>
/// </remarks>
public sealed class JsonSerializer : ISerializer
{
    private readonly JsonSerializerOptions _options;

    /// <summary>
    ///     Initializes a new instance of the <see cref="JsonSerializer"/> class
    ///     with default serialization options.
    /// </summary>
    public JsonSerializer()
        : this(CreateDefaultOptions())
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="JsonSerializer"/> class
    ///     with custom serialization options.
    /// </summary>
    /// <param name="options">The JSON serialization options to use.</param>
    public JsonSerializer(JsonSerializerOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public byte[] Serialize<TMessage>(TMessage message) where TMessage : ISagaMessage
    {
        ArgumentNullException.ThrowIfNull(message);
        return System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(message, message.GetType(), _options);
    }

    /// <inheritdoc />
    public object Deserialize(byte[] payload, string messageType)
    {
        ArgumentNullException.ThrowIfNull(payload);

        if (string.IsNullOrWhiteSpace(messageType))
        {
            throw new ArgumentException("Message type cannot be null or empty.", nameof(messageType));
        }

        var type = Type.GetType(messageType);

        if (type is null)
        {
            throw new InvalidOperationException($"Cannot resolve type '{messageType}'. Ensure the assembly containing this type is loaded.");
        }

        var result = System.Text.Json.JsonSerializer.Deserialize(payload, type, _options);

        if (result is null)
        {
            throw new InvalidOperationException($"Deserialization returned null for message type '{messageType}'.");
        }

        return result;
    }

    /// <inheritdoc />
    public TMessage Deserialize<TMessage>(byte[] payload) where TMessage : ISagaMessage
    {
        ArgumentNullException.ThrowIfNull(payload);

        var result = System.Text.Json.JsonSerializer.Deserialize<TMessage>(payload, _options);

        if (result is null)
        {
            throw new InvalidOperationException($"Deserialization returned null for message type '{typeof(TMessage).FullName}'.");
        }

        return result;
    }

    private static JsonSerializerOptions CreateDefaultOptions() =>
        new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };
}
