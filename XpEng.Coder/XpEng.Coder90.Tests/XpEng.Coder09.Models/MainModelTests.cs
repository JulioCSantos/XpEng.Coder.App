using Microsoft.Extensions.DependencyInjection;
using XpEng.Coder09.Models;
using XpEng.Coder09.Models.Entities;
using XpEng.Coder80.Infrastructure.Services;

namespace XpEng.Coder90.Tests.XpEng.Coder09.Models;

[TestClass]
public class MainModelTests {
    [TestInitialize]
    public void Setup() {
        //Set DI factories
        IServiceCollection servColl = DIExtensions.ServiceCollection;
        servColl.AddModels();
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