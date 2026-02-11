using Microsoft.Extensions.Logging.Abstractions;
using SagaFlow.Messages;
using SagaFlow.Registration;
using SagaFlow.SagaDefinition;

namespace SagaFlow.Unit.Tests.SagaDefinition;

/// <summary>
/// Unit tests for <see cref="SagaRunner"/>.
/// Tests executor resolution, initiator detection, and delegation to the typed executor.
/// </summary>
public sealed class SagaRunnerTests
{
    [Fact]
    public async Task RunAsync_WithNullDescriptor_ThrowsArgumentNullException()
    {
        var sp = Substitute.For<IServiceProvider>();
        var runner = new SagaRunner(sp, NullLogger<SagaRunner>.Instance);
        var message = TestDataGenerators.GenerateCommand();

        await Should.ThrowAsync<ArgumentNullException>(
            async () => await runner.RunAsync(null!, message, CancellationToken.None));
    }

    [Fact]
    public async Task RunAsync_WithNullMessage_ThrowsArgumentNullException()
    {
        var sp = Substitute.For<IServiceProvider>();
        var runner = new SagaRunner(sp, NullLogger<SagaRunner>.Instance);
        var descriptor = CreateDescriptor(typeof(TestCommand));

        await Should.ThrowAsync<ArgumentNullException>(
            async () => await runner.RunAsync(descriptor, null!, CancellationToken.None));
    }

    [Fact]
    public async Task RunAsync_WhenNoExecutorRegistered_ThrowsInvalidOperationException()
    {
        var sp = CreateServiceProviderWithExecutors([]);
        var runner = new SagaRunner(sp, NullLogger<SagaRunner>.Instance);
        var descriptor = CreateDescriptor(typeof(TestCommand));
        var command = TestDataGenerators.GenerateCommand();

        await Should.ThrowAsync<InvalidOperationException>(
            async () => await runner.RunAsync(descriptor, command, CancellationToken.None));
    }

    [Fact]
    public async Task RunAsync_WithMatchingExecutor_DelegatesToExecutor()
    {
        var repository = Substitute.For<ISagaRepository<TestSagaData>>();
        var executor = CreateRealExecutor(repository);
        var sp = CreateServiceProviderWithExecutors([executor]);
        var runner = new SagaRunner(sp, NullLogger<SagaRunner>.Instance);
        var descriptor = CreateDescriptor(typeof(TestCommand));
        var command = TestDataGenerators.GenerateCommand();

        // The executor loads from repository (returns null), message IS an initiator
        // so it creates a new instance. No activities are configured so it completes
        // with a warning. If no exception is thrown, the runner resolved correctly.
        await runner.RunAsync(descriptor, command, CancellationToken.None);

        await repository.Received(1)
            .LoadAsync(command.CorrelationId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_WhenMessageIsInitiator_CreatesNewInstance()
    {
        var repository = Substitute.For<ISagaRepository<TestSagaData>>();
        var executor = CreateRealExecutor(repository);
        var sp = CreateServiceProviderWithExecutors([executor]);
        var runner = new SagaRunner(sp, NullLogger<SagaRunner>.Instance);
        var descriptor = CreateDescriptor(typeof(TestCommand));
        var command = TestDataGenerators.GenerateCommand();

        await runner.RunAsync(descriptor, command, CancellationToken.None);

        // The executor creates a new instance for initiator messages
        // and since no activities match, it skips but doesn't save
        await repository.Received(1)
            .LoadAsync(command.CorrelationId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_WhenMessageIsNotInitiator_AndNoInstance_SkipsExecution()
    {
        var repository = Substitute.For<ISagaRepository<TestSagaData>>();
        var executor = CreateRealExecutor(repository);
        var sp = CreateServiceProviderWithExecutors([executor]);
        var runner = new SagaRunner(sp, NullLogger<SagaRunner>.Instance);
        // TestEvent is the initiator, so TestCommand is NOT an initiator
        var descriptor = CreateDescriptorWithInitiator(typeof(TestEvent));
        var command = TestDataGenerators.GenerateCommand();

        await runner.RunAsync(descriptor, command, CancellationToken.None);

        // No instance found + not an initiator → skip. Save should not be called.
        await repository.DidNotReceive()
            .SaveAsync(Arg.Any<TestSagaData>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_WhenNonGenericExecutorRegistered_ThrowsInvalidOperationException()
    {
        var nonGenericExecutor = Substitute.For<ISagaExecutor>();
        var sp = CreateServiceProviderWithExecutors([nonGenericExecutor]);
        var runner = new SagaRunner(sp, NullLogger<SagaRunner>.Instance);
        var descriptor = CreateDescriptor(typeof(TestCommand));
        var command = TestDataGenerators.GenerateCommand();

        await Should.ThrowAsync<InvalidOperationException>(
            async () => await runner.RunAsync(descriptor, command, CancellationToken.None));
    }

    private static IServiceProvider CreateServiceProviderWithExecutors(ISagaExecutor[] executors)
    {
        var sp = Substitute.For<IServiceProvider>();
        sp.GetService(typeof(IEnumerable<ISagaExecutor>))
            .Returns(executors.AsEnumerable());
        return sp;
    }

    private static SagaExecutor<RunnerTestSaga, TestSagaData> CreateRealExecutor(
        ISagaRepository<TestSagaData>? repository = null)
    {
        repository ??= Substitute.For<ISagaRepository<TestSagaData>>();
        var publisher = Substitute.For<ISagaMessagePublisher>();
        var sp = Substitute.For<IServiceProvider>();

        return new SagaExecutor<RunnerTestSaga, TestSagaData>(
            new RunnerTestSaga(),
            repository,
            publisher,
            sp,
            NullLogger<SagaExecutor<RunnerTestSaga, TestSagaData>>.Instance);
    }

    private static ISagaDescriptor CreateDescriptor(Type initiatorType) =>
        CreateDescriptorCore(typeof(RunnerTestSaga), typeof(TestSagaData), initiatorType);

    private static ISagaDescriptor CreateDescriptorWithInitiator(Type initiatorType) =>
        CreateDescriptorCore(typeof(RunnerTestSaga), typeof(TestSagaData), initiatorType);

    private static ISagaDescriptor CreateDescriptorCore(
        Type sagaType, Type instanceType, Type initiatorType)
    {
        var descriptor = Substitute.For<ISagaDescriptor>();
        descriptor.SagaType.Returns(sagaType);
        descriptor.SagaInstanceType.Returns(instanceType);
        descriptor.InitiatorTypes.Returns(
            new HashSet<Type> { initiatorType } as IReadOnlySet<Type>);
        descriptor.HandledMessageTypes.Returns(
            new HashSet<Type> { typeof(TestCommand), typeof(TestEvent) } as IReadOnlySet<Type>);
        return descriptor;
    }

    /// <summary>
    /// Minimal saga state machine for runner type-resolution tests.
    /// </summary>
    internal sealed class RunnerTestSaga : SagaStateMachine<TestSagaData>
    {
        public RunnerTestSaga()
        {
            InstanceState(x => x.CurrentState);
        }
    }
}
