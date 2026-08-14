using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using XpEng.Coder06.Data;
using XpEng.Coder09.Models;
using XpEng.Coder09.Models.Interfaces;
using DIConfig = XpEng.Coder06.Data.DIConfig;

namespace XpEng.Coder90.Tests.XpEng.Coder06.Data;

[TestClass]
public class DataDIConfigTests {
    [TestMethod]
    public void Config_WhenPassedConnectionString_SuccessfullyRegistersRepository() {
        // Arrange: Create a fresh service collection and a test connection string
        var services = new ServiceCollection();
        string testConnectionString = "Server=TestServer;Database=TestDb;Integrated Security=True;";

        // Act: Call the Data project's DI configuration directly
        DIConfig.Config(services, testConnectionString);
        var provider = services.BuildServiceProvider();

        // Assert: Verify that the container can successfully resolve the interface
        var repository = provider.GetService<IUserRepository>();

        Assert.IsNotNull(repository, "IUserRepository should be registered and resolvable.");
    }

    [TestMethod]
    public void Config_WithoutConnectionString_SuccessfullyResolvesUsingInternalDefault() {
        // Arrange
        var services = new ServiceCollection();

        // Act: Call the config without passing an override (using the optional parameter)
        DIConfig.Config(services);
        var provider = services.BuildServiceProvider();

        // Assert
        var repository = provider.GetService<IUserRepository>();

        Assert.IsNotNull(repository, "IUserRepository should resolve using the internal default connection string.");
    }
}