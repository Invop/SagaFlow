using Microsoft.Extensions.Logging.Abstractions;
using SagaFlow.Messages;
using SagaFlow.Registration;
using SagaFlow.SagaDefinition;
using SagaFlow.SagaDefinition.Activities;

namespace SagaFlow.Unit.Tests.SagaDefinition;

/// <summary>
/// Unit tests for <see cref="SagaExecutor{TSaga,TInstance}"/>.
/// Tests instance lifecycle, activity matching, compensation, and persistence.
/// </summary>
public sealed class SagaExecutorTests
{
    private readonly ISagaRepository<TestSagaData> _repository;
    private readonly ISagaMessagePublisher _publisher;
    private readonly IServiceProvider _serviceProvider;

    public SagaExecutorTests()
    {
        _repository = Substitute.For<ISagaRepository<TestSagaData>>();
        _publisher = Substitute.For<ISagaMessagePublisher>();
        _serviceProvider = Substitute.For<IServiceProvider>();
    }

    [Fact]
    public async Task ExecuteAsync_WithNullMessage_ThrowsArgumentNullException()
    {
        var executor = CreateExecutor(new MinimalSaga());

        await Should.ThrowAsync<ArgumentNullException>(
            async () => await executor.ExecuteAsync(null!, true, CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_WhenIsInitiatorAndNoExistingInstance_CreatesNewInstance()
    {
        _repository.LoadAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((TestSagaData?)null);
        var saga = new SagaWithThenActivity();
        var executor = CreateExecutor(saga);
        var command = TestDataGenerators.GenerateCommand();

        await executor.ExecuteAsync(command, isInitiator: true, CancellationToken.None);

        await _repository.Received(1)
            .SaveAsync(Arg.Is<TestSagaData>(i => i.CorrelationId == command.CorrelationId),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenNotInitiatorAndNoExistingInstance_SkipsExecution()
    {
        _repository.LoadAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((TestSagaData?)null);
        var executor = CreateExecutor(new MinimalSaga());
        var command = TestDataGenerators.GenerateCommand();

        await executor.ExecuteAsync(command, isInitiator: false, CancellationToken.None);

        await _repository.DidNotReceive()
            .SaveAsync(Arg.Any<TestSagaData>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithExistingInstance_LoadsAndProcesses()
    {
        var correlationId = Guid.NewGuid();
        var existingInstance = new TestSagaData
        {
            CorrelationId = correlationId,
            CurrentState = "Initial"
        };
        _repository.LoadAsync(correlationId, Arg.Any<CancellationToken>())
            .Returns(existingInstance);
        var saga = new SagaWithThenActivity();
        var executor = CreateExecutor(saga);
        var command = TestDataGenerators.TestCommandFaker.Clone()
            .RuleFor(c => c.CorrelationId, _ => correlationId)
            .Generate();

        await executor.ExecuteAsync(command, isInitiator: false, CancellationToken.None);

        await _repository.Received(1)
            .SaveAsync(existingInstance, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenSagaReachesFinalState_DeletesInstance()
    {
        _repository.LoadAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((TestSagaData?)null);
        var saga = new SagaWithFinalTransition();
        var executor = CreateExecutor(saga);
        var command = TestDataGenerators.GenerateCommand();

        await executor.ExecuteAsync(command, isInitiator: true, CancellationToken.None);

        await _repository.Received(1)
            .DeleteAsync(command.CorrelationId, Arg.Any<CancellationToken>());
        await _repository.DidNotReceive()
            .SaveAsync(Arg.Any<TestSagaData>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoActivitiesMatch_SkipsSilently()
    {
        _repository.LoadAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((TestSagaData?)null);
        var executor = CreateExecutor(new MinimalSaga());
        // TestEvent is not handled in the Initially block of MinimalSaga
        var testEvent = TestDataGenerators.GenerateEvent();

        await executor.ExecuteAsync(testEvent, isInitiator: true, CancellationToken.None);

        await _repository.DidNotReceive()
            .SaveAsync(Arg.Any<TestSagaData>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenActivityThrows_ExecutesCompensations()
    {
        _repository.LoadAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((TestSagaData?)null);
        var saga = new SagaWithFailingActivity();
        var executor = CreateExecutor(saga);
        var command = TestDataGenerators.GenerateCommand();

        await Should.ThrowAsync<InvalidOperationException>(
            async () => await executor.ExecuteAsync(command, isInitiator: true, CancellationToken.None));

        await _publisher.Received(1)
            .PublishAsync(Arg.Any<ISagaMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenActivityThrows_DoesNotPersistInstance()
    {
        _repository.LoadAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((TestSagaData?)null);
        var saga = new SagaWithFailingActivity();
        var executor = CreateExecutor(saga);
        var command = TestDataGenerators.GenerateCommand();

        await Should.ThrowAsync<InvalidOperationException>(
            async () => await executor.ExecuteAsync(command, isInitiator: true, CancellationToken.None));

        await _repository.DidNotReceive()
            .SaveAsync(Arg.Any<TestSagaData>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ThenActivity_MutatesSagaData()
    {
        _repository.LoadAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((TestSagaData?)null);
        var saga = new SagaWithThenActivity();
        var executor = CreateExecutor(saga);
        var command = TestDataGenerators.GenerateCommand();

        await executor.ExecuteAsync(command, isInitiator: true, CancellationToken.None);

        await _repository.Received(1)
            .SaveAsync(
                Arg.Is<TestSagaData>(d => d.IsProcessed),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_PassesCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        _repository.LoadAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((TestSagaData?)null);
        var saga = new SagaWithThenActivity();
        var executor = CreateExecutor(saga);
        var command = TestDataGenerators.GenerateCommand();

        await executor.ExecuteAsync(command, isInitiator: true, cts.Token);

        await _repository.Received(1)
            .LoadAsync(command.CorrelationId, cts.Token);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCancelled_ThrowsOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        _repository.LoadAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns<TestSagaData?>(_ => throw new OperationCanceledException());
        var executor = CreateExecutor(new MinimalSaga());
        var command = TestDataGenerators.GenerateCommand();

        await Should.ThrowAsync<OperationCanceledException>(
            async () => await executor.ExecuteAsync(command, isInitiator: true, cts.Token));
    }

    private SagaExecutor<TSaga, TestSagaData> CreateExecutor<TSaga>(TSaga saga)
        where TSaga : SagaStateMachine<TestSagaData>
    {
        return new SagaExecutor<TSaga, TestSagaData>(
            saga,
            _repository,
            _publisher,
            _serviceProvider,
            NullLogger<SagaExecutor<TSaga, TestSagaData>>.Instance);
    }

    /// <summary>
    /// A minimal saga that only configures InstanceState with no event handlers.
    /// Useful for testing null/skip paths.
    /// </summary>
    private sealed class MinimalSaga : SagaStateMachine<TestSagaData>
    {
        public MinimalSaga()
        {
            InstanceState(x => x.CurrentState);

            Initially(
                When<TestCommand>()
            );
        }
    }

    /// <summary>
    /// A saga with a Then activity that sets <see cref="TestSagaData.IsProcessed"/> to true
    /// and transitions to a Processing state.
    /// </summary>
    private sealed class SagaWithThenActivity : SagaStateMachine<TestSagaData>
    {
        public SagaWithThenActivity()
        {
            InstanceState(x => x.CurrentState);

            var processing = State("Processing");

            Initially(
                When<TestCommand>()
                    .Then((ctx, data) => data.IsProcessed = true)
                    .TransitionTo(processing)
            );
        }
    }

    /// <summary>
    /// A saga that transitions directly to the Final state on the initial message.
    /// </summary>
    private sealed class SagaWithFinalTransition : SagaStateMachine<TestSagaData>
    {
        public SagaWithFinalTransition()
        {
            InstanceState(x => x.CurrentState);

            Initially(
                When<TestCommand>()
                    .TransitionTo(Final)
            );
        }
    }

    /// <summary>
    /// A saga whose Then activity throws, but has a CompensateWith registered.
    /// Verifies compensation is published when execution fails.
    /// </summary>
    private sealed class SagaWithFailingActivity : SagaStateMachine<TestSagaData>
    {
        public SagaWithFailingActivity()
        {
            InstanceState(x => x.CurrentState);

            Initially(
                When<TestCommand>()
                    .CompensateWith<TestEvent>((ctx, data) => new TestEvent
                    {
                        IdempotencyKey = Guid.NewGuid().ToString(),
                        CorrelationId = ctx.Message.CorrelationId,
                        Timestamp = DateTimeOffset.UtcNow,
                        StepId = "compensation",
                        IsSuccess = false,
                        EventData = "compensating"
                    })
                    .Then((_, _) => throw new InvalidOperationException("Boom"))
            );
        }
    }
}
