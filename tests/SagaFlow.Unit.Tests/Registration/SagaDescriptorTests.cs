using SagaFlow.Registration;
using SagaFlow.SagaDefinition;

namespace SagaFlow.Unit.Tests.Registration;

/// <summary>
/// Unit tests for <see cref="SagaDescriptor"/>.
/// Tests creation, type validation, and message type discovery.
/// </summary>
public sealed class SagaDescriptorTests
{
    [Fact]
    public void Create_Generic_ReturnsSagaDescriptor()
    {
        var descriptor = SagaDescriptor.Create<ValidTestSaga, RegistrationTestInstance>();

        descriptor.ShouldNotBeNull();
        descriptor.SagaType.ShouldBe(typeof(ValidTestSaga));
        descriptor.SagaInstanceType.ShouldBe(typeof(RegistrationTestInstance));
    }

    [Fact]
    public void Create_Generic_DiscoverInitiatorTypes()
    {
        var descriptor = SagaDescriptor.Create<ValidTestSaga, RegistrationTestInstance>();

        descriptor.InitiatorTypes.ShouldContain(typeof(TestCommand));
    }

    [Fact]
    public void Create_Generic_DiscoverHandledMessageTypes()
    {
        var descriptor = SagaDescriptor.Create<ValidTestSaga, RegistrationTestInstance>();

        descriptor.HandledMessageTypes.ShouldContain(typeof(TestCommand));
    }

    [Fact]
    public void Create_ByType_ReturnsSagaDescriptor()
    {
        var descriptor = SagaDescriptor.Create(typeof(ValidTestSaga), typeof(RegistrationTestInstance));

        descriptor.SagaType.ShouldBe(typeof(ValidTestSaga));
        descriptor.SagaInstanceType.ShouldBe(typeof(RegistrationTestInstance));
    }

    [Fact]
    public void Create_ByTypeAutoDiscover_ReturnsSagaDescriptor()
    {
        var descriptor = SagaDescriptor.Create(typeof(ValidTestSaga));

        descriptor.SagaType.ShouldBe(typeof(ValidTestSaga));
        descriptor.SagaInstanceType.ShouldBe(typeof(RegistrationTestInstance));
    }

    [Fact]
    public void Create_WithNullSagaType_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(
            () => SagaDescriptor.Create(null!, typeof(RegistrationTestInstance)));
    }

    [Fact]
    public void Create_WithNullInstanceType_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(
            () => SagaDescriptor.Create(typeof(ValidTestSaga), null!));
    }

    [Fact]
    public void Create_AutoDiscover_WithNullSagaType_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => SagaDescriptor.Create(null!));
    }

    [Fact]
    public void Create_WithAbstractSaga_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(
            () => SagaDescriptor.Create(typeof(AbstractTestSaga), typeof(RegistrationTestInstance)));
    }

    [Fact]
    public void Create_WithNonSagaType_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(
            () => SagaDescriptor.Create(typeof(string), typeof(RegistrationTestInstance)));
    }

    [Fact]
    public void Create_WithNonInstanceType_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(
            () => SagaDescriptor.Create(typeof(ValidTestSaga), typeof(string)));
    }

    [Fact]
    public void Create_WithInterfaceInstanceType_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(
            () => SagaDescriptor.Create(typeof(ValidTestSaga), typeof(ISagaStateMachineInstance)));
    }

    [Fact]
    public void Create_WithNoDefaultCtorInstance_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(
            () => SagaDescriptor.Create(typeof(ValidTestSaga), typeof(NoDefaultCtorInstance)));
    }

    [Fact]
    public void Create_WithMismatchedSagaAndInstance_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(
            () => SagaDescriptor.Create(typeof(ValidTestSaga), typeof(SecondRegistrationTestInstance)));
    }

    [Fact]
    public void GetSagaInstanceType_WithValidSaga_ReturnsInstanceType()
    {
        var instanceType = SagaDescriptor.GetSagaInstanceType(typeof(ValidTestSaga));

        instanceType.ShouldBe(typeof(RegistrationTestInstance));
    }

    [Fact]
    public void GetSagaInstanceType_WithNonSagaType_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(
            () => SagaDescriptor.GetSagaInstanceType(typeof(object)));
    }

    [Fact]
    public void Create_InitiatorTypesAreSubsetOfHandledTypes()
    {
        var descriptor = SagaDescriptor.Create<ValidTestSaga, RegistrationTestInstance>();

        descriptor.HandledMessageTypes.ShouldBe(
            descriptor.HandledMessageTypes.Union(descriptor.InitiatorTypes).ToHashSet(),
            ignoreOrder: true);
    }

    [Fact]
    public void Create_WithSameTypes_ProducesSameTypeProperties()
    {
        var descriptor1 = SagaDescriptor.Create<ValidTestSaga, RegistrationTestInstance>();
        var descriptor2 = SagaDescriptor.Create<ValidTestSaga, RegistrationTestInstance>();

        descriptor1.SagaType.ShouldBe(descriptor2.SagaType);
        descriptor1.SagaInstanceType.ShouldBe(descriptor2.SagaInstanceType);
        descriptor1.InitiatorTypes.SetEquals(descriptor2.InitiatorTypes).ShouldBeTrue();
        descriptor1.HandledMessageTypes.SetEquals(descriptor2.HandledMessageTypes).ShouldBeTrue();
    }
}
