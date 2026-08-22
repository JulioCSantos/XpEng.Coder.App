using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using XpEng.Coder06.Data;
using XpEng.Coder06.ViewModels;
using XpEng.Coder09.Models.Interfaces;

namespace XpEng.Coder90.Tests.XpEng.Coder09.Models;

[TestClass]
public class ContractResolutionTests {
    [TestMethod]
    public void DIContainer_ShouldResolve_ModelsInterfaces_WhenFullyOrchestrated() {
        // Arrange: The Test project acts as the top-level Composition Root (replacing Views)
        var services = new ServiceCollection();

        // Use a dummy connection string since we only want to test DI resolution, not the actual DB connection
        string testConnectionString = "Server=TestServer;Database=TestDb;Integrated Security=True;";

        // Act: Orchestrate the DI exactly as the Views project would
        services.AddData(testConnectionString);
        services.AddViewModels();

        var provider = services.BuildServiceProvider();

        // Assert 1: Verify the Models contract (IUserRepository) is fulfilled by the Data layer
        var userRepository = provider.GetService<IUserRepository>();
        Assert.IsNotNull(userRepository, "Failed to resolve IUserRepository. The Data layer did not fulfill the Models contract.");

        // Assert 2: Verify that a ViewModel can also be resolved with its dependencies injected
        // (Assuming you have a ViewModel that requires IUserRepository in its constructor)
        // var userViewModel = provider.GetService<UserViewModel>();
        // Assert.IsNotNull(userViewModel, "Failed to resolve UserViewModel. Dependency injection tree is broken.");
    }
}