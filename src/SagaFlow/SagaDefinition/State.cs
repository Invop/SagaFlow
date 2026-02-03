namespace SagaFlow.SagaDefinition;

/// <summary>
/// Represents a state in a saga state machine.
/// States define the current position in the saga workflow and can have associated entry/exit actions.
/// </summary>
public sealed class State : IEquatable<State>
{
    /// <summary>
    /// Gets the name of the state.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="State"/> class.
    /// </summary>
    /// <param name="name">The name of the state.</param>
    /// <exception cref="ArgumentNullException">Thrown when name is null or whitespace.</exception>
    public State(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }
    /// <inheritdoc/>
    public bool Equals(State? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Name == other.Name;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => Equals(obj as State);

    /// <inheritdoc/>
    public override int GetHashCode() => Name.GetHashCode();

    /// <inheritdoc/>
    public override string ToString() => Name;

    /// <summary>
    /// Determines whether two states are equal.
    /// </summary>
    public static bool operator ==(State? left, State? right)
    {
        if (left is null)
        {
            return right is null;
        }

        return left.Equals(right);
    }

    /// <summary>
    /// Determines whether two states are not equal.
    /// </summary>
    public static bool operator !=(State? left, State? right) => !(left == right);
}
/// <summary>
/// Represents a typed state in a saga state machine with associated entry and exit actions.
/// </summary>
/// <typeparam name="TSagaData">The type of saga data.</typeparam>
public sealed class State<TSagaData> where TSagaData : class
{
    /// <summary>
    /// Gets the underlying untyped state.
    /// </summary>
    public State InternalState { get; }

    /// <summary>
    /// Gets the name of the state.
    /// </summary>
    public string Name => InternalState.Name;

    /// <summary>
    /// Gets or sets the entry action executed when entering this state.
    /// </summary>
    public Func<TSagaData, CancellationToken, ValueTask>? OnEntry { get; set; }

    /// <summary>
    /// Gets or sets the exit action executed when leaving this state.
    /// </summary>
    public Func<TSagaData, CancellationToken, ValueTask>? OnExit { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="State{TSagaData}"/> class.
    /// </summary>
    /// <param name="state">The underlying untyped state.</param>
    /// <exception cref="ArgumentNullException">Thrown when state is null.</exception>
    internal State(State state)
    {
        ArgumentNullException.ThrowIfNull(state);
        InternalState = state;
    }

    /// <summary>
    /// Executes the entry action for this state.
    /// </summary>
    /// <param name="sagaData">The saga data instance.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    internal async ValueTask ExecuteOnEntryAsync(TSagaData sagaData, CancellationToken cancellationToken)
    {
        if (OnEntry is not null)
        {
            await OnEntry(sagaData, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Executes the exit action for this state.
    /// </summary>
    /// <param name="sagaData">The saga data instance.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    internal async ValueTask ExecuteOnExitAsync(TSagaData sagaData, CancellationToken cancellationToken)
    {
        if (OnExit is not null)
        {
            await OnExit(sagaData, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    public override string ToString() => Name;

    /// <summary>
    /// Implicitly converts a typed state to an untyped state.
    /// </summary>
    public static implicit operator State(State<TSagaData> typedState) => typedState.InternalState;
}