using Microsoft.Extensions.DependencyInjection;
using XpEng.Coder06.Data;
using XpEng.Coder06.ViewModels;

namespace XpEng.Coder90.Tests.XpEng.Coder09.Models;

[TestClass]
public class ContractResolutionTests {

    /// The test project stands in for Views as the composition root: if the cascade wires up
    /// and the top-level ViewModel resolves, every tier beneath it registered correctly.
    [TestMethod]
    public void DIContainer_ResolvesTheFullCascade_WhenOrchestratedFromTheTestProject() {
        var services = new ServiceCollection();

        services.AddData("Server=TestServer;Database=TestDb;Integrated Security=True;");
        services.AddViewModels();

        var provider = services.BuildServiceProvider();

        var mainViewModel = provider.GetService<MainViewModel>();
        Assert.IsNotNull(mainViewModel, "MainViewModel should resolve — the ViewModels cascade is broken if it does not.");
    }
}