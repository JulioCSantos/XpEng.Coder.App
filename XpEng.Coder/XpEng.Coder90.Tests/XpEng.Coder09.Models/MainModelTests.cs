using Microsoft.Extensions.DependencyInjection;
using XpEng.Coder09.Models;
using XpEng.Coder09.Models.Entities;
using XpEng.Coder80.Infrastructure;

namespace XpEng.Coder90.Tests.XpEng.Coder09.Models;

[TestClass]
public class MainModelTests {
    private ServiceProvider _provider = null!;
    private IDisposable _scope = null!;

    [TestInitialize]
    public void Setup() {
        // The collection is a local now — ApplicationServices holds only the built provider.
        var services = new ServiceCollection();
        global::XpEng.Coder09.Models.ServiceCollectionExtensions.AddRegistrations(services);
        _provider = services.BuildServiceProvider();

        // MainModel.Instance goes through the locator, so scope it to this test's container.
        // AsyncLocal, so it survives an await and does not leak into a parallel test.
        _scope = ServiceLocator.UseProvider(_provider);
    }

    [TestCleanup]
    public void Cleanup() {
        _scope.Dispose();
        _provider.Dispose();
    }

    [TestMethod]
    public void InstantiationTest() {
        var target = MainModel.Instance;
        Assert.IsNotNull(target);
    }

    [TestMethod]
    public void MainModel_SaveAndLoad_PreservesDataIntegrity() {
        // Arrange
        var model = MainModel.Instance;
        model.PlanOrchestrators.Clear();

        var originalPlan = new PlanOrchestrator("Persist Test");
        var originalSource = new SourceDirectory(new DirectoryInfo(@"C:\Data"));
        originalSource.TargetTemplates.Add(new TargetTemplate(
            new DirectoryInfo(@"C:\Out"),
            new FileInfo(@"C:\Template.tt"),
            false));
        originalPlan.SourceDirectories.Add(originalSource);

        model.PlanOrchestrators.Add(originalPlan);

        // Act
        model.SavePlans();
        model.PlanOrchestrators.Clear(); // Wipe memory
        model.LoadPlans(); // Hydrate from JSON

        // Assert
        Assert.AreEqual(1, model.PlanOrchestrators.Count);

        var loadedPlan = model.PlanOrchestrators.First();
        Assert.AreEqual("Persist Test", loadedPlan.PlanName);
        Assert.AreEqual(1, loadedPlan.SourceDirectories.Count);

        var loadedSource = loadedPlan.SourceDirectories.First();
        Assert.AreEqual(1, loadedSource.TargetTemplates.Count);
        Assert.IsFalse(loadedSource.TargetTemplates.First().IsMonitored);
    }

}