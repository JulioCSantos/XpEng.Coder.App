using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using XpEng.Coder80.Infrastructure;

namespace XpEng.Coder90.Tests.XpEng.Coder80.Infrastructure;

/// <summary>
/// Proves that locator-based resolution can be redirected per test, and that the attribute
/// scan and the locator agree on lifetime — the two mechanisms that have to line up for
/// MainModel.Instance and friends to behave.
/// </summary>
[TestClass]
public class ServiceLocatorTests {

    // Two independent containers, each registering the same service type. Resolving through
    // the locator under each should yield instances from different containers.
    private static ServiceProvider BuildContainerWith<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService {
        var services = new ServiceCollection();
        services.AddSingleton<TService, TImplementation>();
        return services.BuildServiceProvider();
    }

    [TestCleanup]
    public void Cleanup() => ServiceLocator.Reset();

    [TestMethod]
    public void Provider_WithoutInitialize_ThrowsNamingItsOwnCause() {
        ServiceLocator.Reset();

        var exception = Assert.ThrowsException<InvalidOperationException>(() => _ = ServiceLocator.CurrentProvider);

        StringAssert.Contains(exception.Message, "has not been built",
            "The message should say what is wrong rather than surfacing as a null reference elsewhere.");
    }

    [TestMethod]
    public void UseProvider_RedirectsResolution_AwayFromTheDefault() {
        var defaultProvider = BuildContainerWith<IProbe, ProductionProbe>();
        ServiceLocator.Initialize(defaultProvider);

        var fromDefault = ServiceLocator.CurrentProvider.GetRequiredService<IProbe>();
        Assert.IsInstanceOfType(fromDefault, typeof(ProductionProbe));

        var testProvider = BuildContainerWith<IProbe, TestProbe>();
        using (ServiceLocator.UseProvider(testProvider)) {
            var fromScope = ServiceLocator.CurrentProvider.GetRequiredService<IProbe>();

            Assert.IsInstanceOfType(fromScope, typeof(TestProbe),
                "Inside the scope the locator should resolve from the scoped provider.");
            Assert.AreNotSame(fromDefault, fromScope,
                "The two containers are independent, so their instances must differ.");
        }

        var afterScope = ServiceLocator.CurrentProvider.GetRequiredService<IProbe>();
        Assert.IsInstanceOfType(afterScope, typeof(ProductionProbe),
            "Disposing the scope should restore the default provider.");
        Assert.AreSame(fromDefault, afterScope,
            "The default container is a singleton registration, so the same instance comes back.");
    }

    [TestMethod]
    public void UseProvider_Nested_RestoresTheOuterScope() {
        ServiceLocator.Initialize(BuildContainerWith<IProbe, ProductionProbe>());

        var outer = BuildContainerWith<IProbe, TestProbe>();
        var inner = BuildContainerWith<IProbe, OtherTestProbe>();

        using (ServiceLocator.UseProvider(outer)) {
            Assert.IsInstanceOfType(ServiceLocator.CurrentProvider.GetRequiredService<IProbe>(), typeof(TestProbe));

            using (ServiceLocator.UseProvider(inner)) {
                Assert.IsInstanceOfType(ServiceLocator.CurrentProvider.GetRequiredService<IProbe>(), typeof(OtherTestProbe));
            }

            Assert.IsInstanceOfType(ServiceLocator.CurrentProvider.GetRequiredService<IProbe>(), typeof(TestProbe),
                "Disposing the inner scope should restore the outer one, not the default.");
        }
    }

    /// AsyncLocal rather than ThreadStatic specifically so the scope survives an await — a
    /// ThreadStatic value would be lost when the continuation resumed on a different thread,
    /// and this test would silently see the default provider instead.
    [TestMethod]
    public async Task UseProvider_SurvivesAnAwait() {
        ServiceLocator.Initialize(BuildContainerWith<IProbe, ProductionProbe>());
        var testProvider = BuildContainerWith<IProbe, TestProbe>();

        using (ServiceLocator.UseProvider(testProvider)) {
            await Task.Delay(10).ConfigureAwait(false);

            var afterAwait = ServiceLocator.CurrentProvider.GetRequiredService<IProbe>();
            Assert.IsInstanceOfType(afterAwait, typeof(TestProbe),
                "The scoped provider should still be in effect after an await.");
        }
    }

    /// The attribute scan and the locator must agree: a [Register] Singleton resolved through
    /// the locator twice is the same instance; a Transient is not.
    [TestMethod]
    public void AttributeRegisteredSingleton_ResolvesToTheSameInstanceThroughTheLocator() {
        var services = new ServiceCollection();
        RegistrationScanner.ApplyRegistrations(services, typeof(ServiceLocatorTests).Assembly);
        ServiceLocator.Initialize(services.BuildServiceProvider());

        var first = ServiceLocator.CurrentProvider.GetRequiredService<AttributeSingletonProbe>();
        var second = ServiceLocator.CurrentProvider.GetRequiredService<AttributeSingletonProbe>();

        Assert.AreSame(first, second, "A [Register] Singleton should resolve to one instance.");
    }

    [TestMethod]
    public void AttributeRegisteredTransient_ResolvesToDistinctInstancesThroughTheLocator() {
        var services = new ServiceCollection();
        RegistrationScanner.ApplyRegistrations(services, typeof(ServiceLocatorTests).Assembly);
        ServiceLocator.Initialize(services.BuildServiceProvider());

        var first = ServiceLocator.CurrentProvider.GetRequiredService<AttributeTransientProbe>();
        var second = ServiceLocator.CurrentProvider.GetRequiredService<AttributeTransientProbe>();

        Assert.AreNotSame(first, second, "A [Register] Transient should resolve to a new instance each time.");
    }

    /// Two containers built from the same attribute scan hold genuinely separate instances —
    /// the property per-test isolation depends on.
    [TestMethod]
    public void SeparateContainers_FromTheSameScan_DoNotShareInstances() {
        var firstServices = new ServiceCollection();
        RegistrationScanner.ApplyRegistrations(firstServices, typeof(ServiceLocatorTests).Assembly);
        var firstProvider = firstServices.BuildServiceProvider();

        var secondServices = new ServiceCollection();
        RegistrationScanner.ApplyRegistrations(secondServices, typeof(ServiceLocatorTests).Assembly);
        var secondProvider = secondServices.BuildServiceProvider();

        var fromFirst = firstProvider.GetRequiredService<AttributeSingletonProbe>();
        var fromSecond = secondProvider.GetRequiredService<AttributeSingletonProbe>();

        Assert.AreNotSame(fromFirst, fromSecond,
            "Singleton means one instance per container, not one per process.");
    }

    // ---- probes ----

    public interface IProbe { }
    public class ProductionProbe : IProbe { }
    public class TestProbe : IProbe { }
    public class OtherTestProbe : IProbe { }

    [Register]
    public class AttributeSingletonProbe { }

    [Register(ServiceLifetime.Transient)]
    public class AttributeTransientProbe { }
}
