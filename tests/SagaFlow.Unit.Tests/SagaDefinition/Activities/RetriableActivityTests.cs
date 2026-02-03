using SagaFlow.SagaDefinition.Activities;

namespace SagaFlow.Unit.Tests.SagaDefinition.Activities;

/// <summary>
/// Unit tests for <see cref="RetriableActivity{TSagaData,TMessage}"/> class.
/// Tests retry configuration and exception handling.
/// </summary>
public sealed class RetriableActivityTests
{
    [Fact]
    public void Constructor_WithDefaultParameters_CreatesActivityWithDefaults()
    {
        var activity = new RetriableActivity<TestSagaData, TestCommand>();

        activity.MaxRetries.ShouldBe(3);
        activity.RetryDelay.ShouldBe(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Constructor_WithCustomMaxRetries_SetsMaxRetries()
    {
        var activity = new RetriableActivity<TestSagaData, TestCommand>(maxRetries: 5);

        activity.MaxRetries.ShouldBe(5);
    }

    [Fact]
    public void Constructor_WithCustomRetryDelay_SetsRetryDelay()
    {
        var delay = TimeSpan.FromSeconds(10);

        var activity = new RetriableActivity<TestSagaData, TestCommand>(retryDelay: delay);

        activity.RetryDelay.ShouldBe(delay);
    }

    [Fact]
    public void Constructor_WithNegativeMaxRetries_ThrowsArgumentOutOfRangeException()
    {
        var exception = Should.Throw<ArgumentOutOfRangeException>(() =>
            new RetriableActivity<TestSagaData, TestCommand>(maxRetries: -1));

        exception.ParamName.ShouldBe("maxRetries");
    }

    [Fact]
    public void Constructor_WithZeroMaxRetries_CreatesActivity()
    {
        var activity = new RetriableActivity<TestSagaData, TestCommand>(maxRetries: 0);

        activity.MaxRetries.ShouldBe(0);
    }

    [Fact]
    public void ShouldRetry_WithNullException_ThrowsArgumentNullException()
    {
        var activity = new RetriableActivity<TestSagaData, TestCommand>();

        Should.Throw<ArgumentNullException>(() => activity.ShouldRetry(null!));
    }

    [Fact]
    public void ShouldRetry_WithoutPredicate_ReturnsTrue()
    {
        var activity = new RetriableActivity<TestSagaData, TestCommand>();

        var result = activity.ShouldRetry(new InvalidOperationException());

        result.ShouldBeTrue();
    }

    [Fact]
    public void ShouldRetry_WithPredicateThatReturnsTrue_ReturnsTrue()
    {
        var activity = new RetriableActivity<TestSagaData, TestCommand>(
            retryPredicate: ex => ex is InvalidOperationException);

        var result = activity.ShouldRetry(new InvalidOperationException());

        result.ShouldBeTrue();
    }

    [Fact]
    public void ShouldRetry_WithPredicateThatReturnsFalse_ReturnsFalse()
    {
        var activity = new RetriableActivity<TestSagaData, TestCommand>(
            retryPredicate: ex => ex is InvalidOperationException);

        var result = activity.ShouldRetry(new ArgumentException());

        result.ShouldBeFalse();
    }

    [Fact]
    public void ShouldRetry_WithSpecificExceptionTypePredicate_MatchesCorrectly()
    {
        var activity = new RetriableActivity<TestSagaData, TestCommand>(
            retryPredicate: ex => ex is TimeoutException or HttpRequestException);

        activity.ShouldRetry(new TimeoutException()).ShouldBeTrue();
        activity.ShouldRetry(new HttpRequestException()).ShouldBeTrue();
        activity.ShouldRetry(new InvalidOperationException()).ShouldBeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(10)]
    [InlineData(100)]
    public void MaxRetries_ReturnsConfiguredValue(int expectedMaxRetries)
    {
        var activity = new RetriableActivity<TestSagaData, TestCommand>(maxRetries: expectedMaxRetries);

        activity.MaxRetries.ShouldBe(expectedMaxRetries);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(30)]
    [InlineData(60)]
    public void RetryDelay_ReturnsConfiguredValue(int seconds)
    {
        var expectedDelay = TimeSpan.FromSeconds(seconds);

        var activity = new RetriableActivity<TestSagaData, TestCommand>(retryDelay: expectedDelay);

        activity.RetryDelay.ShouldBe(expectedDelay);
    }

    [Fact]
    public void Constructor_WithAllParameters_ConfiguresCorrectly()
    {
        var maxRetries = 7;
        var delay = TimeSpan.FromMinutes(1);
        Func<Exception, bool> predicate = ex => ex.Message.Contains("retry");

        var activity = new RetriableActivity<TestSagaData, TestCommand>(
            maxRetries: maxRetries,
            retryDelay: delay,
            retryPredicate: predicate);

        activity.MaxRetries.ShouldBe(maxRetries);
        activity.RetryDelay.ShouldBe(delay);
        activity.ShouldRetry(new Exception("Please retry")).ShouldBeTrue();
        activity.ShouldRetry(new Exception("No chance")).ShouldBeFalse();
    }
}
