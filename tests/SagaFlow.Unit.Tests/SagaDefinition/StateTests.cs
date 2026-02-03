using SagaFlow.SagaDefinition;

namespace SagaFlow.Unit.Tests.SagaDefinition;

/// <summary>
/// Unit tests for <see cref="State"/> class.
/// Tests state creation, equality, and hash code behavior.
/// </summary>
public sealed class StateTests
{
    [Fact]
    public void Constructor_WithValidName_CreatesState()
    {
        var stateName = "Processing";

        var state = new State(stateName);

        state.Name.ShouldBe(stateName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithNullOrWhitespaceName_ThrowsArgumentException(string? invalidName)
    {
        Should.Throw<ArgumentException>(() => new State(invalidName!));
    }

    [Fact]
    public void ToString_ReturnsStateName()
    {
        var stateName = "Completed";
        var state = new State(stateName);

        var result = state.ToString();

        result.ShouldBe(stateName);
    }

    [Fact]
    public void Equals_WithSameNameStates_ReturnsTrue()
    {
        var state1 = new State("Active");
        var state2 = new State("Active");

        var result = state1.Equals(state2);

        result.ShouldBeTrue();
    }

    [Fact]
    public void Equals_WithDifferentNameStates_ReturnsFalse()
    {
        var state1 = new State("Active");
        var state2 = new State("Inactive");

        var result = state1.Equals(state2);

        result.ShouldBeFalse();
    }

    [Fact]
    public void Equals_WithNull_ReturnsFalse()
    {
        var state = new State("Active");

        var result = state.Equals(null);

        result.ShouldBeFalse();
    }

    [Fact]
    public void Equals_WithSameReference_ReturnsTrue()
    {
        var state = new State("Active");

        var result = state.Equals(state);

        result.ShouldBeTrue();
    }

    [Fact]
    public void Equals_WithObjectOfSameName_ReturnsTrue()
    {
        var state1 = new State("Active");
        object state2 = new State("Active");

        var result = state1.Equals(state2);

        result.ShouldBeTrue();
    }

    [Fact]
    public void Equals_WithObjectOfDifferentType_ReturnsFalse()
    {
        var state = new State("Active");
        var notAState = "Active";

        var result = state.Equals(notAState);

        result.ShouldBeFalse();
    }

    [Fact]
    public void GetHashCode_WithSameNameStates_ReturnsSameHashCode()
    {
        var state1 = new State("Active");
        var state2 = new State("Active");

        state1.GetHashCode().ShouldBe(state2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_WithDifferentNameStates_ReturnsDifferentHashCodes()
    {
        var state1 = new State("Active");
        var state2 = new State("Inactive");

        state1.GetHashCode().ShouldNotBe(state2.GetHashCode());
    }

    [Fact]
    public void EqualityOperator_WithSameNameStates_ReturnsTrue()
    {
        var state1 = new State("Active");
        var state2 = new State("Active");

        var result = state1 == state2;

        result.ShouldBeTrue();
    }

    [Fact]
    public void EqualityOperator_WithDifferentNameStates_ReturnsFalse()
    {
        var state1 = new State("Active");
        var state2 = new State("Inactive");

        var result = state1 == state2;

        result.ShouldBeFalse();
    }

    [Fact]
    public void InequalityOperator_WithDifferentNameStates_ReturnsTrue()
    {
        var state1 = new State("Active");
        var state2 = new State("Inactive");

        var result = state1 != state2;

        result.ShouldBeTrue();
    }

    [Fact]
    public void InequalityOperator_WithSameNameStates_ReturnsFalse()
    {
        var state1 = new State("Active");
        var state2 = new State("Active");

        var result = state1 != state2;

        result.ShouldBeFalse();
    }

    [Fact]
    public void EqualityOperator_WithLeftNull_ReturnsFalseWhenRightNotNull()
    {
        State? state1 = null;
        var state2 = new State("Active");

        var result = state1 == state2;

        result.ShouldBeFalse();
    }

    [Fact]
    public void EqualityOperator_WithBothNull_ReturnsTrue()
    {
        State? state1 = null;
        State? state2 = null;

        var result = state1 == state2;

        result.ShouldBeTrue();
    }

    [Fact]
    public void EqualityOperator_WithRightNull_ReturnsFalseWhenLeftNotNull()
    {
        var state1 = new State("Active");
        State? state2 = null;

        var result = state1 == state2;

        result.ShouldBeFalse();
    }
}
