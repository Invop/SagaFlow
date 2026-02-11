using Microsoft.Extensions.Logging.Abstractions;
using SagaFlow.Messages;
using SagaFlow.Outbox;
using SagaFlow.Registration;
using SagaFlow.SagaDefinition;
using SagaFlow.Utils;

namespace SagaFlow.Unit.Tests.Messages;

/// <summary>
/// Unit tests for <see cref="SagaMessageProcessor"/>.
/// Tests the entry point of the orchestration pipeline: deserialization, registry lookup, and fan-out dispatch.
/// </summary>
public sealed class SagaMessageProcessorTests
{
    private readonly ISagaRegistry _registry;
    private readonly ISagaRunner _runner;
    private readonly ISerializer _serializer;
    private readonly SagaMessageProcessor _sut;

    public SagaMessageProcessorTests()
    {
        _registry = Substitute.For<ISagaRegistry>();
        _runner = Substitute.For<ISagaRunner>();
        _serializer = Substitute.For<ISerializer>();
        _sut = new SagaMessageProcessor(
            _registry,
            _runner,
            _serializer,
            NullLogger<SagaMessageProcessor>.Instance);
    }

    [Fact]
    public async Task ProcessAsync_WithNullMessage_ThrowsArgumentNullException()
    {
        await Should.ThrowAsync<ArgumentNullException>(
            async () => await _sut.ProcessAsync(null!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ProcessAsync_WithUnresolvableMessageType_DoesNotCallRunner(string? messageType)
    {
        var message = TestDataGenerators.OutboxMessageFaker.Clone()
            .RuleFor(m => m.MessageType, _ => messageType!)
            .Generate();

        await _sut.ProcessAsync(message);

        await _runner.DidNotReceive()
            .RunAsync(Arg.Any<ISagaDescriptor>(), Arg.Any<ISagaMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_WhenDeserializationThrows_DoesNotCallRunner()
    {
        var message = TestDataGenerators.GenerateOutboxMessage();
        _serializer.Deserialize(Arg.Any<byte[]>(), Arg.Any<string>())
            .Returns(_ => throw new InvalidOperationException("bad payload"));

        await _sut.ProcessAsync(message);

        await _runner.DidNotReceive()
            .RunAsync(Arg.Any<ISagaDescriptor>(), Arg.Any<ISagaMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_WhenDeserializedObjectIsNotISagaMessage_DoesNotCallRunner()
    {
        var message = TestDataGenerators.GenerateOutboxMessage();
        _serializer.Deserialize(Arg.Any<byte[]>(), Arg.Any<string>())
            .Returns(new object());

        await _sut.ProcessAsync(message);

        await _runner.DidNotReceive()
            .RunAsync(Arg.Any<ISagaDescriptor>(), Arg.Any<ISagaMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_WhenNoSagaRegistered_DoesNotCallRunner()
    {
        var command = TestDataGenerators.GenerateCommand();
        var message = CreateOutboxMessageFor(command);
        _serializer.Deserialize(Arg.Any<byte[]>(), Arg.Any<string>())
            .Returns(command);
        _registry.ResolveByMessageType(typeof(TestCommand))
            .Returns(Enumerable.Empty<ISagaDescriptor>());

        await _sut.ProcessAsync(message);

        await _runner.DidNotReceive()
            .RunAsync(Arg.Any<ISagaDescriptor>(), Arg.Any<ISagaMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_WithSingleMatchingSaga_CallsRunnerOnce()
    {
        var command = TestDataGenerators.GenerateCommand();
        var message = CreateOutboxMessageFor(command);
        var descriptor = CreateDescriptor(typeof(TestCommand));

        _serializer.Deserialize(Arg.Any<byte[]>(), Arg.Any<string>())
            .Returns(command);
        _registry.ResolveByMessageType(typeof(TestCommand))
            .Returns(new[] { descriptor });

        await _sut.ProcessAsync(message);

        await _runner.Received(1)
            .RunAsync(descriptor, command, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_WithMultipleMatchingSagas_CallsRunnerForEach()
    {
        var command = TestDataGenerators.GenerateCommand();
        var message = CreateOutboxMessageFor(command);
        var descriptor1 = CreateDescriptor(typeof(TestCommand));
        var descriptor2 = CreateDescriptor(typeof(TestCommand));

        _serializer.Deserialize(Arg.Any<byte[]>(), Arg.Any<string>())
            .Returns(command);
        _registry.ResolveByMessageType(typeof(TestCommand))
            .Returns(new[] { descriptor1, descriptor2 });

        await _sut.ProcessAsync(message);

        await _runner.Received(2)
            .RunAsync(Arg.Any<ISagaDescriptor>(), command, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_PassesCancellationTokenToRunner()
    {
        using var cts = new CancellationTokenSource();
        var command = TestDataGenerators.GenerateCommand();
        var message = CreateOutboxMessageFor(command);
        var descriptor = CreateDescriptor(typeof(TestCommand));

        _serializer.Deserialize(Arg.Any<byte[]>(), Arg.Any<string>())
            .Returns(command);
        _registry.ResolveByMessageType(typeof(TestCommand))
            .Returns(new[] { descriptor });

        await _sut.ProcessAsync(message, cts.Token);

        await _runner.Received(1)
            .RunAsync(descriptor, command, cts.Token);
    }

    private static OutboxMessage CreateOutboxMessageFor(ISagaMessage sagaMessage) =>
        TestDataGenerators.OutboxMessageFaker.Clone()
            .RuleFor(m => m.MessageType, _ => sagaMessage.GetType().AssemblyQualifiedName!)
            .RuleFor(m => m.CorrelationId, _ => sagaMessage.CorrelationId)
            .Generate();

    private static ISagaDescriptor CreateDescriptor(Type handledMessageType)
    {
        var descriptor = Substitute.For<ISagaDescriptor>();
        descriptor.HandledMessageTypes.Returns(
            new HashSet<Type> { handledMessageType } as IReadOnlySet<Type>);
        descriptor.InitiatorTypes.Returns(
            new HashSet<Type> { handledMessageType } as IReadOnlySet<Type>);
        return descriptor;
    }
}
