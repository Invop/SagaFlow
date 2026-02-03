using SagaFlow.Messages;
using SagaFlow.SagaDefinition.Activities;

namespace SagaFlow.Unit.Tests.SagaDefinition.Activities;

/// <summary>
/// Unit tests for <see cref="SagaExecutionContext{TSagaData}"/> class.
/// Tests context creation, property access, and compensation message management.
/// </summary>
public sealed class SagaExecutionContextTests
{
    private readonly IServiceProvider _serviceProvider = Substitute.For<IServiceProvider>();

    [Fact]
    public void Constructor_WithValidArguments_CreatesContext()
    {
        var sagaData = new TestSagaData();
        var message = TestDataGenerators.GenerateCommand();
        var messageContext = CreateMessageContext(message);

        var context = new SagaExecutionContext<TestSagaData>(
            sagaData,
            message,
            messageContext,
            _serviceProvider);

        context.SagaData.ShouldBe(sagaData);
        context.Message.ShouldBe(message);
        context.MessageContext.ShouldBe(messageContext);
        context.ServiceProvider.ShouldBe(_serviceProvider);
    }

    [Fact]
    public void Constructor_WithNullSagaData_ThrowsArgumentNullException()
    {
        var message = TestDataGenerators.GenerateCommand();
        var messageContext = CreateMessageContext(message);

        Should.Throw<ArgumentNullException>(() =>
            new SagaExecutionContext<TestSagaData>(null!, message, messageContext, _serviceProvider));
    }

    [Fact]
    public void Constructor_WithNullMessage_ThrowsArgumentNullException()
    {
        var sagaData = new TestSagaData();
        var message = TestDataGenerators.GenerateCommand();
        var messageContext = CreateMessageContext(message);

        Should.Throw<ArgumentNullException>(() =>
            new SagaExecutionContext<TestSagaData>(sagaData, null!, messageContext, _serviceProvider));
    }

    [Fact]
    public void Constructor_WithNullMessageContext_ThrowsArgumentNullException()
    {
        var sagaData = new TestSagaData();
        var message = TestDataGenerators.GenerateCommand();

        Should.Throw<ArgumentNullException>(() =>
            new SagaExecutionContext<TestSagaData>(sagaData, message, null!, _serviceProvider));
    }

    [Fact]
    public void Constructor_WithNullServiceProvider_ThrowsArgumentNullException()
    {
        var sagaData = new TestSagaData();
        var message = TestDataGenerators.GenerateCommand();
        var messageContext = CreateMessageContext(message);

        Should.Throw<ArgumentNullException>(() =>
            new SagaExecutionContext<TestSagaData>(sagaData, message, messageContext, null!));
    }

    [Fact]
    public void CompensationMessages_InitiallyEmpty()
    {
        var context = CreateContext();

        context.CompensationMessages.ShouldBeEmpty();
    }

    [Fact]
    public void CompensationMessages_CanAddMessages()
    {
        var context = CreateContext();
        var compensationMessage = TestDataGenerators.GenerateEvent();

        context.CompensationMessages.Add(compensationMessage);

        context.CompensationMessages.ShouldContain(compensationMessage);
        context.CompensationMessages.Count.ShouldBe(1);
    }

    [Fact]
    public void IsPivotReached_DefaultsToFalse()
    {
        var context = CreateContext();

        context.IsPivotReached.ShouldBeFalse();
    }

    [Fact]
    public void IsPivotReached_CanBeSetToTrue()
    {
        var context = CreateContext();

        context.IsPivotReached = true;

        context.IsPivotReached.ShouldBeTrue();
    }

    private SagaExecutionContext<TestSagaData> CreateContext()
    {
        var sagaData = new TestSagaData();
        var message = TestDataGenerators.GenerateCommand();
        var messageContext = CreateMessageContext(message);

        return new SagaExecutionContext<TestSagaData>(
            sagaData,
            message,
            messageContext,
            _serviceProvider);
    }

    private static ISagaMessageContext<TestCommand> CreateMessageContext(TestCommand message)
    {
        var context = Substitute.For<ISagaMessageContext<TestCommand>>();
        context.Message.Returns(message);
        context.CorrelationId.Returns(message.CorrelationId);
        context.MessageId.Returns(Guid.NewGuid().ToString());
        context.SenderId.Returns("TestSender");
        return context;
    }
}
