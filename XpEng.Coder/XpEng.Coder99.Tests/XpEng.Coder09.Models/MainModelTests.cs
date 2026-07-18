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
        servColl = DIConfig.Config(servColl);
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

        var originalPlan = new PlanOrchestrator("Persist Test", new DirectoryInfo(@"C:\Data"));
        originalPlan.TemplateTargets.Add(new TemplateTarget(
            new DirectoryInfo(@"C:\Out"),
            new FileInfo(@"C:\Template.tt"),
            false));

        model.PlanOrchestrators.Add(originalPlan);

        // Act
        model.SavePlans();
        model.PlanOrchestrators.Clear(); // Wipe memory
        model.LoadPlans(); // Hydrate from JSON

        // Assert
        Assert.AreEqual(1, model.PlanOrchestrators.Count);

        var loadedPlan = model.PlanOrchestrators.First();
        Assert.AreEqual("Persist Test", loadedPlan.PlanName);
        Assert.AreEqual(1, loadedPlan.TemplateTargets.Count);
        Assert.IsFalse(loadedPlan.TemplateTargets.First().IsMonitored);
    }

}
