using SagaFlow.Registration;

namespace SagaFlow.Unit.Tests.Registration;

/// <summary>
/// Unit tests for <see cref="SagaRegistry"/>.
/// Tests registration, message-type resolution, initiator lookup, and thread-safety guarantees.
/// </summary>
public sealed class SagaRegistryTests
{
    private readonly SagaRegistry _sut = new();

    [Fact]
    public void Register_WithNullDescriptor_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => _sut.Register((ISagaDescriptor)null!));
    }

    [Fact]
    public void Register_WithValidDescriptor_IncrementsCount()
    {
        var descriptor = SagaDescriptor.Create<ValidTestSaga, RegistrationTestInstance>();

        _sut.Register(descriptor);

        _sut.Count.ShouldBe(1);
    }

    [Fact]
    public void Register_DuplicateSagaType_ThrowsInvalidOperationException()
    {
        var descriptor = SagaDescriptor.Create<ValidTestSaga, RegistrationTestInstance>();
        _sut.Register(descriptor);

        Should.Throw<InvalidOperationException>(() => _sut.Register(descriptor));
    }

    [Fact]
    public void Descriptors_AfterRegistration_ContainsDescriptor()
    {
        var descriptor = SagaDescriptor.Create<ValidTestSaga, RegistrationTestInstance>();
        _sut.Register(descriptor);

        _sut.Descriptors.ShouldContain(descriptor);
    }

    [Fact]
    public void Descriptors_WhenEmpty_ReturnsEmptyCollection()
    {
        _sut.Descriptors.ShouldBeEmpty();
    }

    [Fact]
    public void RegisteredMessageTypes_AfterRegistration_ContainsHandledTypes()
    {
        var descriptor = SagaDescriptor.Create<ValidTestSaga, RegistrationTestInstance>();
        _sut.Register(descriptor);

        _sut.RegisteredMessageTypes.ShouldContain(typeof(TestCommand));
    }

    [Fact]
    public void ResolveByMessageType_WithRegisteredType_ReturnsDescriptor()
    {
        var descriptor = SagaDescriptor.Create<ValidTestSaga, RegistrationTestInstance>();
        _sut.Register(descriptor);

        var resolved = _sut.ResolveByMessageType(typeof(TestCommand));

        resolved.ShouldContain(descriptor);
    }

    [Fact]
    public void ResolveByMessageType_WithUnregisteredType_ReturnsEmpty()
    {
        var resolved = _sut.ResolveByMessageType(typeof(TestEvent));

        resolved.ShouldBeEmpty();
    }

    [Fact]
    public void ResolveByMessageType_WithNullType_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => _sut.ResolveByMessageType(null!));
    }

    [Fact]
    public void ResolveByMessageType_Generic_ReturnsDescriptor()
    {
        var descriptor = SagaDescriptor.Create<ValidTestSaga, RegistrationTestInstance>();
        _sut.Register(descriptor);

        var resolved = _sut.ResolveByMessageType<TestCommand>();

        resolved.ShouldContain(descriptor);
    }

    [Fact]
    public void ResolveByMessageType_MultipleSagas_ReturnsBoth()
    {
        var descriptor1 = SagaDescriptor.Create<ValidTestSaga, RegistrationTestInstance>();
        var descriptor2 = SagaDescriptor.Create<SecondValidTestSaga, SecondRegistrationTestInstance>();
        _sut.Register(descriptor1);
        _sut.Register(descriptor2);

        var resolved = _sut.ResolveByMessageType(typeof(TestCommand)).ToList();

        resolved.Count.ShouldBe(2);
        resolved.ShouldContain(descriptor1);
        resolved.ShouldContain(descriptor2);
    }

    [Fact]
    public void IsInitiator_WithInitiatorType_ReturnsTrue()
    {
        var descriptor = SagaDescriptor.Create<ValidTestSaga, RegistrationTestInstance>();
        _sut.Register(descriptor);

        _sut.IsInitiator(typeof(TestCommand)).ShouldBeTrue();
    }

    [Fact]
    public void IsInitiator_WithNonInitiatorType_ReturnsFalse()
    {
        _sut.IsInitiator(typeof(TestEvent)).ShouldBeFalse();
    }

    [Fact]
    public void IsInitiator_WithNullType_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => _sut.IsInitiator(null!));
    }

    [Fact]
    public void ResolveInitiators_WithInitiatorType_ReturnsDescriptor()
    {
        var descriptor = SagaDescriptor.Create<ValidTestSaga, RegistrationTestInstance>();
        _sut.Register(descriptor);

        var resolved = _sut.ResolveInitiators(typeof(TestCommand));

        resolved.ShouldContain(descriptor);
    }

    [Fact]
    public void ResolveInitiators_WithNonInitiatorType_ReturnsEmpty()
    {
        var resolved = _sut.ResolveInitiators(typeof(TestEvent));

        resolved.ShouldBeEmpty();
    }

    [Fact]
    public void ResolveInitiators_WithNullType_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => _sut.ResolveInitiators(null!));
    }

    [Fact]
    public void GetDescriptor_WithRegisteredSaga_ReturnsDescriptor()
    {
        var descriptor = SagaDescriptor.Create<ValidTestSaga, RegistrationTestInstance>();
        _sut.Register(descriptor);

        var resolved = _sut.GetDescriptor(typeof(ValidTestSaga));

        resolved.ShouldBe(descriptor);
    }

    [Fact]
    public void GetDescriptor_WithUnregisteredSaga_ReturnsNull()
    {
        var resolved = _sut.GetDescriptor(typeof(ValidTestSaga));

        resolved.ShouldBeNull();
    }

    [Fact]
    public void GetDescriptor_WithNullType_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => _sut.GetDescriptor(null!));
    }

    [Fact]
    public void GetDescriptor_Generic_ReturnsDescriptor()
    {
        var descriptor = SagaDescriptor.Create<ValidTestSaga, RegistrationTestInstance>();
        _sut.Register(descriptor);

        var resolved = _sut.GetDescriptor<ValidTestSaga>();

        resolved.ShouldBe(descriptor);
    }

    [Fact]
    public void IsRegistered_WithRegisteredSaga_ReturnsTrue()
    {
        var descriptor = SagaDescriptor.Create<ValidTestSaga, RegistrationTestInstance>();
        _sut.Register(descriptor);

        _sut.IsRegistered(typeof(ValidTestSaga)).ShouldBeTrue();
    }

    [Fact]
    public void IsRegistered_WithUnregisteredSaga_ReturnsFalse()
    {
        _sut.IsRegistered(typeof(ValidTestSaga)).ShouldBeFalse();
    }

    [Fact]
    public void IsRegistered_Generic_ReturnsTrue()
    {
        var descriptor = SagaDescriptor.Create<ValidTestSaga, RegistrationTestInstance>();
        _sut.Register(descriptor);

        _sut.IsRegistered<ValidTestSaga>().ShouldBeTrue();
    }

    [Fact]
    public void IsRegistered_WithNullType_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => _sut.IsRegistered(null!));
    }

    [Fact]
    public void Count_WhenEmpty_ReturnsZero()
    {
        _sut.Count.ShouldBe(0);
    }

    [Fact]
    public void Count_AfterMultipleRegistrations_ReturnsCorrectCount()
    {
        _sut.Register(SagaDescriptor.Create<ValidTestSaga, RegistrationTestInstance>());
        _sut.Register(SagaDescriptor.Create<SecondValidTestSaga, SecondRegistrationTestInstance>());

        _sut.Count.ShouldBe(2);
    }

    [Fact]
    public void Clear_RemovesAllRegistrations()
    {
        _sut.Register(SagaDescriptor.Create<ValidTestSaga, RegistrationTestInstance>());
        _sut.Register(SagaDescriptor.Create<SecondValidTestSaga, SecondRegistrationTestInstance>());

        _sut.Clear();

        _sut.Count.ShouldBe(0);
        _sut.Descriptors.ShouldBeEmpty();
        _sut.RegisteredMessageTypes.ShouldBeEmpty();
    }

    [Fact]
    public void Clear_AllowsReRegistration()
    {
        var descriptor = SagaDescriptor.Create<ValidTestSaga, RegistrationTestInstance>();
        _sut.Register(descriptor);
        _sut.Clear();

        _sut.Register(descriptor);

        _sut.Count.ShouldBe(1);
    }
}
