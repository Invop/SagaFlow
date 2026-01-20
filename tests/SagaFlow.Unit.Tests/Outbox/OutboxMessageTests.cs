using SagaFlow.Outbox;

namespace SagaFlow.Unit.Tests.Outbox;

/// <summary>
/// Unit tests for <see cref="OutboxMessage"/> domain aggregate.
/// Tests message state initialization and properties.
/// </summary>
public sealed class OutboxMessageTests
{
    [Fact]
    public void Create_WithValidProperties_SuccessfullyCreatesMessage()
    {
        // Setup for the test
        var messageId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var idempotencyKey = "test-key-123";
        var messageType = typeof(TestCommand).AssemblyQualifiedName!;
        var payload = new byte[] { 1, 2, 3, 4 };
        var occurredOn = DateTimeOffset.UtcNow;

        // Execution of the method under test
        var message = new OutboxMessage
        {
            Id = messageId,
            CorrelationId = correlationId,
            IdempotencyKey = idempotencyKey,
            MessageType = messageType,
            Payload = payload,
            OccurredOnUtc = occurredOn
        };

        // Verification of the outcome
        message.Id.ShouldBe(messageId);
        message.CorrelationId.ShouldBe(correlationId);
        message.IdempotencyKey.ShouldBe(idempotencyKey);
        message.MessageType.ShouldBe(messageType);
        message.Payload.ShouldBe(payload);
        message.OccurredOnUtc.ShouldBe(occurredOn);
        message.Status.ShouldBe(OutboxMessageStatus.Pending);
        message.RetryCount.ShouldBe(0);
        message.ProcessedOnUtc.ShouldBeNull();
        message.LastError.ShouldBeNull();
        message.LastAttemptOnUtc.ShouldBeNull();
    }

    [Fact]
    public void Status_WhenUpdated_ReflectsNewValue()
    {
        // Setup for the test
        var message = TestDataGenerators.GenerateOutboxMessage();

        // Execution of the method under test
        message.Status = OutboxMessageStatus.Processing;

        // Verification of the outcome
        message.Status.ShouldBe(OutboxMessageStatus.Processing);
    }

    [Fact]
    public void ProcessedOnUtc_WhenSet_ReflectsNewValue()
    {
        // Setup for the test
        var message = TestDataGenerators.GenerateOutboxMessage();
        var processedOn = DateTimeOffset.UtcNow;

        // Execution of the method under test
        message.ProcessedOnUtc = processedOn;

        // Verification of the outcome
        message.ProcessedOnUtc.ShouldBe(processedOn);
    }

    [Fact]
    public void RetryCount_WhenIncremented_ReflectsNewValue()
    {
        // Setup for the test
        var message = TestDataGenerators.GenerateOutboxMessage();

        // Execution of the method under test
        message.RetryCount = 3;

        // Verification of the outcome
        message.RetryCount.ShouldBe(3);
    }

    [Fact]
    public void LastError_WhenSet_ReflectsErrorMessage()
    {
        // Setup for the test
        var message = TestDataGenerators.GenerateOutboxMessage();
        var errorMessage = "Connection timeout occurred";

        // Execution of the method under test
        message.LastError = errorMessage;

        // Verification of the outcome
        message.LastError.ShouldBe(errorMessage);
    }

    [Fact]
    public void LastAttemptOnUtc_WhenSet_ReflectsAttemptTime()
    {
        // Setup for the test
        var message = TestDataGenerators.GenerateOutboxMessage();
        var attemptTime = DateTimeOffset.UtcNow;

        // Execution of the method under test
        message.LastAttemptOnUtc = attemptTime;

        // Verification of the outcome
        message.LastAttemptOnUtc.ShouldBe(attemptTime);
    }

    [Fact]
    public void Create_WithFailedStatus_MaintainsRetryInformation()
    {
        // Setup for the test
        var message = TestDataGenerators.GenerateFailedMessage(retryCount: 2);

        // Execution of the method under test
        var status = message.Status;
        var retryCount = message.RetryCount;

        // Verification of the outcome
        status.ShouldBe(OutboxMessageStatus.Failed);
        retryCount.ShouldBe(2);
        message.LastError.ShouldNotBeNullOrEmpty();
        message.LastAttemptOnUtc.ShouldNotBeNull();
    }

    [Fact]
    public void Create_WithProcessedStatus_HasProcessedTimestamp()
    {
        // Setup for the test
        var message = TestDataGenerators.GenerateProcessedMessage();

        // Execution of the method under test
        var status = message.Status;
        var processedOn = message.ProcessedOnUtc;

        // Verification of the outcome
        status.ShouldBe(OutboxMessageStatus.Processed);
        processedOn.ShouldNotBeNull();
    }
}
