using Microsoft.Extensions.DependencyInjection;
using XpEng.Coder06.ViewModels;

namespace XpEng.Coder90.Tests.XpEng.Coder09.Models;

[TestClass]
public class ContractResolutionTests {

    /// The test project stands in for Views as the composition root: if the cascade wires up
    /// and the top-level ViewModel resolves, every tier beneath it registered correctly.
    [TestMethod]
    public void DIContainer_ResolvesTheFullCascade_WhenOrchestratedFromTheTestProject() {
        var services = new ServiceCollection();

        // Fully qualified because every project now declares AddRegistrations. The connection
        // string is no longer a parameter — AddRegistrations has a fixed signature, and
        // nothing in Coder.App consumes one yet.
        global::XpEng.Coder06.Data.ServiceCollectionExtensions.AddRegistrations(services);
        global::XpEng.Coder06.ViewModels.ServiceCollectionExtensions.AddRegistrations(services);

        var provider = services.BuildServiceProvider();

        var mainViewModel = provider.GetService<MainViewModel>();
        Assert.IsNotNull(mainViewModel, "MainViewModel should resolve — the ViewModels cascade is broken if it does not.");
    }
}