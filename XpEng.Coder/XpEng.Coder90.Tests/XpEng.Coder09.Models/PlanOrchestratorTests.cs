using XpEng.Coder09.Models.Entities;

namespace XpEng.Coder90.Tests.XpEng.Coder09.Models {
    [TestClass]
    public class PlanOrchestratorTests {

        [TestMethod]
        public void IsMonitored_DefaultsToTrue() {
            // Arrange & Act
            var plan = new PlanOrchestrator("Test Plan");

            // Assert
            Assert.IsTrue(plan.IsMonitored, "IsMonitored should default to true.");
        }

        [TestMethod]
        public void IsMonitored_IsIndependentOfChildTargetTemplates() {
            // Arrange: Plan.IsMonitored is now an explicit flag, not a rollup of its
            // children's IsMonitored values, so toggling a child must not affect it.
            var plan = new PlanOrchestrator("Test Plan");
            var source = new SourceDirectory(new DirectoryInfo(@"C:\Source"));
            var target1 = new TargetTemplate(new DirectoryInfo(@"C:\Out1"), new FileInfo(@"C:\Temp1.tt"), true);
            var target2 = new TargetTemplate(new DirectoryInfo(@"C:\Out2"), new FileInfo(@"C:\Temp2.tt"), true);

            source.TargetTemplates.Add(target1);
            source.TargetTemplates.Add(target2);
            plan.SourceDirectories.Add(source);

            bool parentNotified = false;
            plan.PropertyChanged += (s, e) => {
                if (e.PropertyName == nameof(PlanOrchestrator.IsMonitored))
                    parentNotified = true;
            };

            // Act: Uncheck one child
            target1.IsMonitored = false;

            // Assert: Parent state is untouched by child changes
            Assert.IsTrue(plan.IsMonitored, "Parent IsMonitored should remain unaffected by child TargetTemplate state.");
            Assert.IsFalse(parentNotified, "Parent should not raise PropertyChanged for IsMonitored when a child changes.");
        }

        [TestMethod]
        public void IsMonitored_SetDirectly_RaisesPropertyChanged() {
            // Arrange
            var plan = new PlanOrchestrator("Test Plan");
            bool notified = false;
            plan.PropertyChanged += (s, e) => {
                if (e.PropertyName == nameof(PlanOrchestrator.IsMonitored))
                    notified = true;
            };

            // Act
            plan.IsMonitored = false;

            // Assert
            Assert.IsFalse(plan.IsMonitored);
            Assert.IsTrue(notified, "Setting IsMonitored directly should raise PropertyChanged.");
        }
    }
}