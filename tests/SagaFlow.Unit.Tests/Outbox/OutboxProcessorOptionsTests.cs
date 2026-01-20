using SagaFlow.Outbox;

namespace SagaFlow.Unit.Tests.Outbox;

/// <summary>
/// Unit tests for <see cref="OutboxProcessorOptions"/> configuration value object.
/// Tests default values and property assignments.
/// </summary>
public sealed class OutboxProcessorOptionsTests
{
    [Fact]
    public void Constructor_WithDefaults_SetsExpectedValues()
    {
        // Setup for the test and execution
        var options = new OutboxProcessorOptions();

        // Verification of the outcome
        options.BatchSize.ShouldBe(100);
        options.PollingIntervalSeconds.ShouldBe(5);
        options.MaxRetryCount.ShouldBe(3);
        options.BaseRetryDelaySeconds.ShouldBe(30);
        options.ProcessFailedMessages.ShouldBeTrue();
        options.ProcessedMessageRetentionDays.ShouldBe(7);
        options.CleanupIntervalHours.ShouldBe(24);
        options.Enabled.ShouldBeTrue();
    }

    [Fact]
    public void BatchSize_WhenSet_ReflectsCustomValue()
    {
        // Setup for the test
        var options = new OutboxProcessorOptions();

        // Execution of the method under test
        options.BatchSize = 50;

        // Verification of the outcome
        options.BatchSize.ShouldBe(50);
    }

    [Fact]
    public void PollingIntervalSeconds_WhenSet_ReflectsCustomValue()
    {
        // Setup for the test
        var options = new OutboxProcessorOptions();

        // Execution of the method under test
        options.PollingIntervalSeconds = 10;

        // Verification of the outcome
        options.PollingIntervalSeconds.ShouldBe(10);
    }

    [Fact]
    public void MaxRetryCount_WhenSet_ReflectsCustomValue()
    {
        // Setup for the test
        var options = new OutboxProcessorOptions();

        // Execution of the method under test
        options.MaxRetryCount = 5;

        // Verification of the outcome
        options.MaxRetryCount.ShouldBe(5);
    }

    [Fact]
    public void BaseRetryDelaySeconds_WhenSet_ReflectsCustomValue()
    {
        // Setup for the test
        var options = new OutboxProcessorOptions();

        // Execution of the method under test
        options.BaseRetryDelaySeconds = 60;

        // Verification of the outcome
        options.BaseRetryDelaySeconds.ShouldBe(60);
    }

    [Fact]
    public void ProcessFailedMessages_WhenDisabled_ReflectsFalseValue()
    {
        // Setup for the test
        var options = new OutboxProcessorOptions();

        // Execution of the method under test
        options.ProcessFailedMessages = false;

        // Verification of the outcome
        options.ProcessFailedMessages.ShouldBeFalse();
    }

    [Fact]
    public void ProcessedMessageRetentionDays_WhenSet_ReflectsCustomValue()
    {
        // Setup for the test
        var options = new OutboxProcessorOptions();

        // Execution of the method under test
        options.ProcessedMessageRetentionDays = 14;

        // Verification of the outcome
        options.ProcessedMessageRetentionDays.ShouldBe(14);
    }

    [Fact]
    public void CleanupIntervalHours_WhenSet_ReflectsCustomValue()
    {
        // Setup for the test
        var options = new OutboxProcessorOptions();

        // Execution of the method under test
        options.CleanupIntervalHours = 12;

        // Verification of the outcome
        options.CleanupIntervalHours.ShouldBe(12);
    }

    [Fact]
    public void Enabled_WhenDisabled_ReflectsFalseValue()
    {
        // Setup for the test
        var options = new OutboxProcessorOptions();

        // Execution of the method under test
        options.Enabled = false;

        // Verification of the outcome
        options.Enabled.ShouldBeFalse();
    }

    [Fact]
    public void MultipleProperties_WhenSet_AllReflectCustomValues()
    {
        // Setup for the test
        var options = new OutboxProcessorOptions
        {
            BatchSize = 25,
            PollingIntervalSeconds = 15,
            MaxRetryCount = 10,
            BaseRetryDelaySeconds = 45,
            ProcessFailedMessages = false,
            ProcessedMessageRetentionDays = 30,
            CleanupIntervalHours = 48,
            Enabled = false
        };

        // Verification of the outcome
        options.BatchSize.ShouldBe(25);
        options.PollingIntervalSeconds.ShouldBe(15);
        options.MaxRetryCount.ShouldBe(10);
        options.BaseRetryDelaySeconds.ShouldBe(45);
        options.ProcessFailedMessages.ShouldBeFalse();
        options.ProcessedMessageRetentionDays.ShouldBe(30);
        options.CleanupIntervalHours.ShouldBe(48);
        options.Enabled.ShouldBeFalse();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    public void BatchSize_WithVariousValues_AcceptsValidValues(int batchSize)
    {
        // Setup for the test
        var options = new OutboxProcessorOptions();

        // Execution of the method under test
        options.BatchSize = batchSize;

        // Verification of the outcome
        options.BatchSize.ShouldBe(batchSize);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(30)]
    [InlineData(60)]
    public void PollingIntervalSeconds_WithVariousValues_AcceptsValidValues(int intervalSeconds)
    {
        // Setup for the test
        var options = new OutboxProcessorOptions();

        // Execution of the method under test
        options.PollingIntervalSeconds = intervalSeconds;

        // Verification of the outcome
        options.PollingIntervalSeconds.ShouldBe(intervalSeconds);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(10)]
    public void MaxRetryCount_WithVariousValues_AcceptsValidValues(int retryCount)
    {
        // Setup for the test
        var options = new OutboxProcessorOptions();

        // Execution of the method under test
        options.MaxRetryCount = retryCount;

        // Verification of the outcome
        options.MaxRetryCount.ShouldBe(retryCount);
    }
}
