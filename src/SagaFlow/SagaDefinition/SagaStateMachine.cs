using System.Linq.Expressions;
using SagaFlow.Messages;
using SagaFlow.SagaDefinition.Activities;

namespace SagaFlow.SagaDefinition;

/// <summary>
/// Base class for defining saga state machines using a fluent API.
/// Provides methods for defining states, events, and transitions.
/// </summary>
/// <typeparam name="TSagaData">The type of saga data that maintains saga state.</typeparam>
public abstract class SagaStateMachine<TSagaData> : ISagaStateMachine<TSagaData>
    where TSagaData : class, ISagaStateMachineInstance
{
    private readonly Dictionary<string, State<TSagaData>> _states = new();
    private readonly Dictionary<Type, List<EventActivity<TSagaData>>> _messageHandlers = new();
    private Func<TSagaData, string>? _stateAccessor;
    private Action<TSagaData, string>? _stateMutator;

    /// <summary>
    /// Gets the initial state of the saga.
    /// </summary>
    protected State<TSagaData> Initial { get; }

    /// <summary>
    /// Gets the final state of the saga.
    /// </summary>
    protected State<TSagaData> Final { get; }

    // ISagaStateMachine implementation
    State ISagaStateMachine<TSagaData>.Initial => Initial.InternalState;
    State ISagaStateMachine<TSagaData>.Final => Final.InternalState;
    /// <summary>
    /// Gets all registered states as <see cref="State"/> objects.
    /// </summary>
    protected IEnumerable<State> States => _states.Values.Select(s => s.InternalState);

    IEnumerable<State> ISagaStateMachine<TSagaData>.States => States;

    /// <summary>
    /// Determines if the saga instance has completed and transitioned to the Final state.
    /// </summary>
    public virtual Task<bool> IsCompleted(TSagaData instance)
    {
        ArgumentNullException.ThrowIfNull(instance);
        var currentState = instance.CurrentState;
        return Task.FromResult(currentState == Final.InternalState.Name);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SagaStateMachine{TSagaData}"/> class.
    /// </summary>
    protected SagaStateMachine()
    {
        // Create default states
        Initial = CreateState(nameof(Initial));
        Final = CreateState(nameof(Final));
    }

    /// <summary>
    /// Configures which property of the saga data holds the current state.
    /// </summary>
    /// <param name="stateProperty">Expression pointing to the state property.</param>
    /// <exception cref="ArgumentNullException">Thrown when stateProperty is null.</exception>
    /// <exception cref="ArgumentException">Thrown when expression is not a valid property accessor.</exception>
    protected void InstanceState(Expression<Func<TSagaData, string>> stateProperty)
    {
        ArgumentNullException.ThrowIfNull(stateProperty);

        if (stateProperty.Body is not MemberExpression memberExpression)
        {
            throw new ArgumentException("Expression must be a property accessor.", nameof(stateProperty));
        }

        // Create getter
        _stateAccessor = stateProperty.Compile();

        // Create setter
        var parameter = stateProperty.Parameters[0];
        var valueParameter = Expression.Parameter(typeof(string), "value");
        var assignExpression = Expression.Assign(memberExpression, valueParameter);
        var lambda = Expression.Lambda<Action<TSagaData, string>>(
            assignExpression,
            parameter,
            valueParameter);
        _stateMutator = lambda.Compile();
    }

    /// <summary>
    /// Creates a new state with the specified name.
    /// </summary>
    /// <param name="name">The name of the state.</param>
    /// <returns>The created state.</returns>
    /// <exception cref="ArgumentException">Thrown when a state with the same name already exists.</exception>
    protected State<TSagaData> State(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return _states.ContainsKey(name) 
                ? throw new ArgumentException($"State '{name}' already exists.", nameof(name))
                : CreateState(name);
    }

    private State? _currentContextState;

    /// <summary>
    /// Defines initial event handlers for when the saga is created.
    /// </summary>
    /// <param name="activities">The event activity binders for the initial state.</param>
    protected internal void Initially(params IEventActivityBinder[] activities)
    {
        _currentContextState = Initial.InternalState;
        // Activities are built via When<TMessage>() and already registered
        _currentContextState = null;
    }

    /// <summary>
    /// Defines event handlers for a specific state.
    /// </summary>
    /// <param name="state">The state for which to define event handlers.</param>
    /// <param name="activities">The event activity binders for this state.</param>
    protected internal void During(State<TSagaData> state, params IEventActivityBinder[] activities)
    {
        ArgumentNullException.ThrowIfNull(state);
        _currentContextState = state.InternalState;
        // Activities are built via When<TMessage>() and already registered
        _currentContextState = null;
    }

    /// <summary>
    /// Defines an event handler for a specific message type.
    /// Must be called after Initially() or During() to set the context.
    /// </summary>
    /// <typeparam name="TMessage">The type of message to handle.</typeparam>
    /// <returns>An event activity binder for fluent configuration.</returns>
    protected IEventActivityBinder<TSagaData, TMessage> When<TMessage>() where TMessage : ISagaMessage
    {
        var messageType = typeof(TMessage);

        // Use current context state (set by Initially or During)
        var currentState = _currentContextState;

        // Create event activity
        var eventActivity = new EventActivity<TSagaData>(messageType, currentState);

        // Register in message handlers dictionary
        if (!_messageHandlers.TryGetValue(messageType, out List<EventActivity<TSagaData>>? value))
        {
            value = [];
            _messageHandlers[messageType] = value;
        }

        value.Add(eventActivity);

        // Ensure state accessor and mutator are configured
        if (_stateAccessor is null || _stateMutator is null)
        {
            throw new InvalidOperationException(
                "InstanceState must be called before defining event handlers.");
        }

        // Create and return binder
        var binder = new EventActivityBinder<TSagaData, TMessage>(
            eventActivity,
            _stateAccessor,
            _stateMutator);

        return binder;
    }

    /// <summary>
    /// Gets all event activities registered for the specified message type.
    /// </summary>
    /// <param name="messageType">The message type.</param>
    /// <returns>List of event activities for the message type.</returns>
    internal IReadOnlyList<EventActivity<TSagaData>> GetEventActivitiesForMessage(Type messageType)
    {
        ArgumentNullException.ThrowIfNull(messageType);

        return _messageHandlers.TryGetValue(messageType, out var activities)
            ? activities.AsReadOnly()
            : Array.Empty<EventActivity<TSagaData>>();
    }

    /// <summary>
    /// Gets the state accessor function.
    /// </summary>
    internal Func<TSagaData, string> GetStateAccessor()
    {
        return _stateAccessor is null
            ? throw new InvalidOperationException("InstanceState has not been configured.")
            : _stateAccessor;
    }

    /// <summary>
    /// Gets the state mutator action.
    /// </summary>
    internal Action<TSagaData, string> GetStateMutator()
    {
        return _stateMutator is null
            ? throw new InvalidOperationException("InstanceState has not been configured.")
            : _stateMutator;
    }

    /// <summary>
    /// Gets a state by name.
    /// </summary>
    /// <param name="name">The state name.</param>
    /// <returns>The state, or null if not found.</returns>
    internal State<TSagaData>? GetState(string name)
    {
        return _states.TryGetValue(name, out var state) ? state : null;
    }

    /// <summary>
    /// Gets all registered states.
    /// </summary>
    internal IReadOnlyDictionary<string, State<TSagaData>> GetAllStates() => _states;

    private State<TSagaData> CreateState(string name)
    {
        var state = new State(name);
        var typedState = new State<TSagaData>(state);
        _states[name] = typedState;
        return typedState;
    }
}
