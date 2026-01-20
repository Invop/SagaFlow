using SagaFlow.Utils;

namespace SagaFlow.Unit.Tests.Utils;

public class JsonSerializerTests
{
    private readonly JsonSerializer _serializer = new();

    [Fact]
    public void Serialize_Command_ReturnsValidBytes()
    {
        var command = TestDataGenerators.GenerateCommand();

        var payload = _serializer.Serialize(command);

        payload.ShouldNotBeNull();
        payload.ShouldNotBeEmpty();
    }

    [Fact]
    public void Serialize_Event_ReturnsValidBytes()
    {
        var @event = TestDataGenerators.GenerateEvent();

        var payload = _serializer.Serialize(@event);

        payload.ShouldNotBeNull();
        payload.ShouldNotBeEmpty();
    }

    [Fact]
    public void Deserialize_Generic_ReturnsOriginalMessage()
    {
        var command = TestDataGenerators.GenerateCommand();
        var payload = _serializer.Serialize(command);

        var result = _serializer.Deserialize<TestCommand>(payload);

        result.ShouldNotBeNull();
        result.IdempotencyKey.ShouldBe(command.IdempotencyKey);
        result.CorrelationId.ShouldBe(command.CorrelationId);
        result.OrderId.ShouldBe(command.OrderId);
        result.Amount.ShouldBe(command.Amount);
    }

    [Fact]
    public void Deserialize_WithTypeName_ReturnsOriginalMessage()
    {
        var command = TestDataGenerators.GenerateCommand();
        var payload = _serializer.Serialize(command);
        var typeName = typeof(TestCommand).AssemblyQualifiedName!;

        var result = _serializer.Deserialize(payload, typeName);

        result.ShouldNotBeNull();
        result.ShouldBeOfType<TestCommand>();
        var typedResult = (TestCommand)result;
        typedResult.IdempotencyKey.ShouldBe(command.IdempotencyKey);
        typedResult.CorrelationId.ShouldBe(command.CorrelationId);
    }

    [Fact]
    public void Deserialize_Event_PreservesAllProperties()
    {
        var @event = TestDataGenerators.TestEventFaker.Clone()
            .RuleFor(e => e.IsSuccess, _ => false)
            .RuleFor(e => e.ErrorMessage, f => f.Lorem.Sentence())
            .Generate();
        var payload = _serializer.Serialize(@event);

        var result = _serializer.Deserialize<TestEvent>(payload);

        result.StepId.ShouldBe(@event.StepId);
        result.IsSuccess.ShouldBe(@event.IsSuccess);
        result.ErrorMessage.ShouldBe(@event.ErrorMessage);
        result.EventData.ShouldBe(@event.EventData);
    }

    [Fact]
    public void Serialize_NullMessage_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => _serializer.Serialize<TestCommand>(null!));
    }

    [Fact]
    public void Deserialize_NullPayload_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => _serializer.Deserialize<TestCommand>(null!));
    }

    [Fact]
    public void Deserialize_NullTypeName_ThrowsArgumentException()
    {
        var payload = new byte[] { 1, 2, 3 };

        Should.Throw<ArgumentException>(() => _serializer.Deserialize(payload, null!));
    }

    [Fact]
    public void Deserialize_EmptyTypeName_ThrowsArgumentException()
    {
        var payload = new byte[] { 1, 2, 3 };

        Should.Throw<ArgumentException>(() => _serializer.Deserialize(payload, string.Empty));
    }

    [Fact]
    public void Deserialize_InvalidTypeName_ThrowsInvalidOperationException()
    {
        var command = TestDataGenerators.GenerateCommand();
        var payload = _serializer.Serialize(command);

        Should.Throw<InvalidOperationException>(() =>
            _serializer.Deserialize(payload, "NonExistent.Type, NonExistent.Assembly"));
    }

    [Fact]
    public void Roundtrip_MultipleCommands_PreservesAllData()
    {
        var commands = Enumerable.Range(0, 10)
            .Select(_ => TestDataGenerators.GenerateCommand())
            .ToList();

        foreach (var original in commands)
        {
            var payload = _serializer.Serialize(original);
            var restored = _serializer.Deserialize<TestCommand>(payload);

            restored.IdempotencyKey.ShouldBe(original.IdempotencyKey);
            restored.CorrelationId.ShouldBe(original.CorrelationId);
            restored.Timestamp.ShouldBe(original.Timestamp);
            restored.OrderId.ShouldBe(original.OrderId);
            restored.Amount.ShouldBe(original.Amount);
        }
    }
}
