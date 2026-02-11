using SagaFlow.SagaDefinition;

namespace SagaFlow.Registration;

/// <summary>
/// Defines a repository for persisting and retrieving saga instances.
/// </summary>
/// <typeparam name="TInstance">The saga instance type.</typeparam>
/// <remarks>
/// <para>
/// Saga repositories are responsible for:
/// </para>
/// <list type="bullet">
///     <item><description>Loading existing saga instances by correlation ID</description></item>
///     <item><description>Saving new and updated saga instances</description></item>
///     <item><description>Deleting completed saga instances</description></item>
/// </list>
/// <para>
/// Implementations should ensure thread-safety and handle concurrent access appropriately.
/// </para>
/// </remarks>
public interface ISagaRepository<TInstance>
    where TInstance : class, ISagaStateMachineInstance
{
    /// <summary>
    /// Loads a saga instance by its correlation ID.
    /// </summary>
    /// <param name="correlationId">The correlation ID of the saga instance.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The saga instance if found; otherwise, null.</returns>
    ValueTask<TInstance?> LoadAsync(Guid correlationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a saga instance (insert or update).
    /// </summary>
    /// <param name="instance">The saga instance to save.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    ValueTask SaveAsync(TInstance instance, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a saga instance.
    /// </summary>
    /// <param name="correlationId">The correlation ID of the saga instance to delete.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the instance was deleted; false if it was not found.</returns>
    ValueTask<bool> DeleteAsync(Guid correlationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a saga instance exists.
    /// </summary>
    /// <param name="correlationId">The correlation ID to check.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the instance exists; otherwise, false.</returns>
    ValueTask<bool> ExistsAsync(Guid correlationId, CancellationToken cancellationToken = default);
}
