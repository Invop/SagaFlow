using System.Collections.Concurrent;
using SagaFlow.SagaDefinition;

namespace SagaFlow.Registration;

/// <summary>
/// In-memory implementation of <see cref="ISagaRepository{TInstance}"/> for testing and development.
/// </summary>
/// <typeparam name="TInstance">The saga instance type.</typeparam>
/// <remarks>
/// <para>
/// This repository stores saga instances in memory and is suitable for:
/// </para>
/// <list type="bullet">
///     <item><description>Unit testing</description></item>
///     <item><description>Development and prototyping</description></item>
///     <item><description>Single-instance applications without persistence requirements</description></item>
/// </list>
/// <para>
/// <strong>Warning:</strong> All saga instances are lost when the application restarts.
/// For production use, consider a persistent repository implementation.
/// </para>
/// </remarks>
public sealed class InMemorySagaRepository<TInstance> : ISagaRepository<TInstance>
    where TInstance : class, ISagaStateMachineInstance
{
    private readonly ConcurrentDictionary<Guid, TInstance> _instances = new();

    /// <inheritdoc />
    public ValueTask<TInstance?> LoadAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        _instances.TryGetValue(correlationId, out var instance);
        return ValueTask.FromResult(instance);
    }

    /// <inheritdoc />
    public ValueTask SaveAsync(TInstance instance, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(instance);
        _instances[instance.CorrelationId] = instance;
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask<bool> DeleteAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(_instances.TryRemove(correlationId, out _));
    }

    /// <inheritdoc />
    public ValueTask<bool> ExistsAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(_instances.ContainsKey(correlationId));
    }

    /// <summary>
    /// Gets all stored instances (for testing purposes).
    /// </summary>
    public IEnumerable<TInstance> GetAll() => _instances.Values;

    /// <summary>
    /// Gets the number of stored instances.
    /// </summary>
    public int Count => _instances.Count;

    /// <summary>
    /// Clears all stored instances.
    /// </summary>
    public void Clear() => _instances.Clear();
}
