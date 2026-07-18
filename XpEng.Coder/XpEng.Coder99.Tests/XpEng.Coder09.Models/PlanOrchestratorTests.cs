using XpEng.Coder09.Models.Entities;

namespace XpEng.Coder90.Tests.XpEng.Coder09.Models {
    [TestClass]
    public class PlanOrchestratorTests {
        [TestMethod]
        public void IsMonitored_ChildToggled_UpdatesParentState() {
            // Arrange: Create valid objects
            var plan = new PlanOrchestrator("Test Plan", new DirectoryInfo(@"C:\Source"));
            var target1 = new TemplateTarget(new DirectoryInfo(@"C:\Out1"), new FileInfo(@"C:\Temp1.tt"), true);
            var target2 = new TemplateTarget(new DirectoryInfo(@"C:\Out2"), new FileInfo(@"C:\Temp2.tt"), true);

            plan.TemplateTargets.Add(target1);
            plan.TemplateTargets.Add(target2);

            bool parentNotified = false;
            plan.PropertyChanged += (s, e) => {
                if (e.PropertyName == nameof(PlanOrchestrator.IsMonitored))
                    parentNotified = true;
            };

            // Act: Uncheck one child
            target1.IsMonitored = false;

            // Assert: Parent must immediately recalculate and fire notification
            Assert.IsFalse(plan.IsMonitored, "Parent IsMonitored should be false since one child is unchecked.");
            Assert.IsTrue(parentNotified, "Parent failed to raise PropertyChanged for IsMonitored.");
        }
    }
}