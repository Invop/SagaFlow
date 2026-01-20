using Bogus;
using SagaFlow.Outbox;

namespace SagaFlow.Unit.Tests;

/// <summary>
/// Bogus fake data generators
/// </summary>
public static class TestDataGenerators
{
    public static Faker<TestCommand> TestCommandFaker { get; } = new Faker<TestCommand>()
        .RuleFor(c => c.IdempotencyKey, f => f.Random.Guid().ToString())
        .RuleFor(c => c.CorrelationId, f => f.Random.Guid())
        .RuleFor(c => c.Timestamp, f => f.Date.RecentOffset())
        .RuleFor(c => c.OrderId, f => f.Commerce.Ean13())
        .RuleFor(c => c.Amount, f => f.Finance.Amount());

    public static Faker<TestEvent> TestEventFaker { get; } = new Faker<TestEvent>()
        .RuleFor(e => e.IdempotencyKey, f => f.Random.Guid().ToString())
        .RuleFor(e => e.CorrelationId, f => f.Random.Guid())
        .RuleFor(e => e.Timestamp, f => f.Date.RecentOffset())
        .RuleFor(e => e.StepId, f => f.Random.AlphaNumeric(10))
        .RuleFor(e => e.IsSuccess, f => f.Random.Bool())
        .RuleFor(e => e.ErrorMessage, (f, e) => e.IsSuccess ? null : f.Lorem.Sentence())
        .RuleFor(e => e.EventData, f => f.Lorem.Paragraph());

    public static Faker<OutboxMessage> OutboxMessageFaker { get; } = new Faker<OutboxMessage>()
        .RuleFor(m => m.Id, f => f.Random.Guid())
        .RuleFor(m => m.CorrelationId, f => f.Random.Guid())
        .RuleFor(m => m.IdempotencyKey, f => f.Random.Guid().ToString())
        .RuleFor(m => m.MessageType, f => typeof(TestCommand).AssemblyQualifiedName!)
        .RuleFor(m => m.Payload, f => f.Random.Bytes(100))
        .RuleFor(m => m.OccurredOnUtc, f => f.Date.RecentOffset())
        .RuleFor(m => m.Status, _ => OutboxMessageStatus.Pending)
        .RuleFor(m => m.RetryCount, _ => 0)
        .RuleFor(m => m.ProcessedOnUtc, _ => null)
        .RuleFor(m => m.LastError, _ => null)
        .RuleFor(m => m.LastAttemptOnUtc, _ => null);

    public static TestCommand GenerateCommand() => TestCommandFaker.Generate();

    public static TestEvent GenerateEvent() => TestEventFaker.Generate();

    public static OutboxMessage GenerateOutboxMessage() => OutboxMessageFaker.Generate();

    public static List<OutboxMessage> GenerateOutboxMessages(int count) =>
        OutboxMessageFaker.Generate(count);

    public static OutboxMessage GeneratePendingMessage() =>
        OutboxMessageFaker.Clone()
            .RuleFor(m => m.Status, _ => OutboxMessageStatus.Pending)
            .Generate();

    public static OutboxMessage GenerateFailedMessage(int retryCount = 1) =>
        OutboxMessageFaker.Clone()
            .RuleFor(m => m.Status, _ => OutboxMessageStatus.Failed)
            .RuleFor(m => m.RetryCount, _ => retryCount)
            .RuleFor(m => m.LastError, f => f.Lorem.Sentence())
            .RuleFor(m => m.LastAttemptOnUtc, f => f.Date.RecentOffset())
            .Generate();

    public static OutboxMessage GenerateProcessedMessage() =>
        OutboxMessageFaker.Clone()
            .RuleFor(m => m.Status, _ => OutboxMessageStatus.Processed)
            .RuleFor(m => m.ProcessedOnUtc, f => f.Date.RecentOffset())
            .Generate();
}
