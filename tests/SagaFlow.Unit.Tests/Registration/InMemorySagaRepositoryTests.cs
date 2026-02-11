using Bogus;
using SagaFlow.Registration;

namespace SagaFlow.Unit.Tests.Registration;

/// <summary>
/// Unit tests for <see cref="InMemorySagaRepository{TInstance}"/>.
/// Tests CRUD operations, existence checks, and utility methods.
/// </summary>
public sealed class InMemorySagaRepositoryTests
{
    private readonly InMemorySagaRepository<RegistrationTestInstance> _sut = new();

    private static readonly Faker<RegistrationTestInstance> InstanceFaker = new Faker<RegistrationTestInstance>()
        .RuleFor(i => i.CorrelationId, f => f.Random.Guid())
        .RuleFor(i => i.CurrentState, _ => "Initial");

    [Fact]
    public async Task LoadAsync_WhenEmpty_ReturnsNull()
    {
        var result = await _sut.LoadAsync(Guid.NewGuid());

        result.ShouldBeNull();
    }

    [Fact]
    public async Task LoadAsync_AfterSave_ReturnsInstance()
    {
        var instance = InstanceFaker.Generate();
        await _sut.SaveAsync(instance);

        var loaded = await _sut.LoadAsync(instance.CorrelationId);

        loaded.ShouldBeSameAs(instance);
    }

    [Fact]
    public async Task SaveAsync_WithNullInstance_ThrowsArgumentNullException()
    {
        await Should.ThrowAsync<ArgumentNullException>(
            async () => await _sut.SaveAsync(null!));
    }

    [Fact]
    public async Task SaveAsync_WithSameCorrelationId_OverwritesExisting()
    {
        var correlationId = Guid.NewGuid();
        var first = InstanceFaker.Clone()
            .RuleFor(i => i.CorrelationId, _ => correlationId)
            .RuleFor(i => i.CurrentState, _ => "Initial")
            .Generate();
        var second = InstanceFaker.Clone()
            .RuleFor(i => i.CorrelationId, _ => correlationId)
            .RuleFor(i => i.CurrentState, _ => "Updated")
            .Generate();

        await _sut.SaveAsync(first);
        await _sut.SaveAsync(second);

        var loaded = await _sut.LoadAsync(correlationId);
        loaded.ShouldBeSameAs(second);
        _sut.Count.ShouldBe(1);
    }

    [Fact]
    public async Task DeleteAsync_WithExistingInstance_ReturnsTrueAndRemoves()
    {
        var instance = InstanceFaker.Generate();
        await _sut.SaveAsync(instance);

        var result = await _sut.DeleteAsync(instance.CorrelationId);

        result.ShouldBeTrue();
        (await _sut.LoadAsync(instance.CorrelationId)).ShouldBeNull();
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentId_ReturnsFalse()
    {
        var result = await _sut.DeleteAsync(Guid.NewGuid());

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task ExistsAsync_WithExistingInstance_ReturnsTrue()
    {
        var instance = InstanceFaker.Generate();
        await _sut.SaveAsync(instance);

        var exists = await _sut.ExistsAsync(instance.CorrelationId);

        exists.ShouldBeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistentId_ReturnsFalse()
    {
        var exists = await _sut.ExistsAsync(Guid.NewGuid());

        exists.ShouldBeFalse();
    }

    [Fact]
    public async Task ExistsAsync_AfterDelete_ReturnsFalse()
    {
        var instance = InstanceFaker.Generate();
        await _sut.SaveAsync(instance);
        await _sut.DeleteAsync(instance.CorrelationId);

        var exists = await _sut.ExistsAsync(instance.CorrelationId);

        exists.ShouldBeFalse();
    }

    [Fact]
    public async Task GetAll_ReturnsAllStoredInstances()
    {
        var instances = InstanceFaker.Generate(3);
        foreach (var instance in instances)
        {
            await _sut.SaveAsync(instance);
        }

        var all = _sut.GetAll().ToList();

        all.Count.ShouldBe(3);
    }

    [Fact]
    public void Count_WhenEmpty_ReturnsZero()
    {
        _sut.Count.ShouldBe(0);
    }

    [Fact]
    public async Task Count_AfterSaves_ReturnsCorrectCount()
    {
        await _sut.SaveAsync(InstanceFaker.Generate());
        await _sut.SaveAsync(InstanceFaker.Generate());

        _sut.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Clear_RemovesAllInstances()
    {
        await _sut.SaveAsync(InstanceFaker.Generate());
        await _sut.SaveAsync(InstanceFaker.Generate());

        _sut.Clear();

        _sut.Count.ShouldBe(0);
        _sut.GetAll().ShouldBeEmpty();
    }

    [Fact]
    public async Task LoadAsync_WithDifferentCorrelationIds_ReturnsCorrectInstances()
    {
        var instance1 = InstanceFaker.Generate();
        var instance2 = InstanceFaker.Generate();
        await _sut.SaveAsync(instance1);
        await _sut.SaveAsync(instance2);

        var loaded1 = await _sut.LoadAsync(instance1.CorrelationId);
        var loaded2 = await _sut.LoadAsync(instance2.CorrelationId);

        loaded1.ShouldBeSameAs(instance1);
        loaded2.ShouldBeSameAs(instance2);
    }
}
