using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace XpEng.Coder90.Tests.XpEng.Coder80.Infrastructure {



    [TestClass]

    public class TestDirectoriesTests {

        #region TestResultsPath
        private string? _testResultsPath;
        public string TestResultsPath {
            get { return _testResultsPath ??= TestDirectories.GetSlnxFolder("TestResults"); }
            set => _testResultsPath = value;
        }
        #endregion TestResultsPath  

        [TestMethod]
        public void GetTestResultsDirectoryTest() {
            var testResultsDirectory = TestDirectories.GetSlnxFolder("TestResults");
            Assert.IsNotNull(testResultsDirectory);
        }
    }

    [TestClass]
    public class SolutionDependencyGraphTests {

        public TestContext TestContext { get; set; } = null!;

        #region TestResultsPath
        private string? _testResultsPath;
        public string TestResultsPath {
            get { return _testResultsPath ??= TestDirectories.GetSlnxFolder("TestResults"); }
            set => _testResultsPath = value;
        }
        #endregion TestResultsPath

        [TestMethod]
        public void GetXpEngCoderGraphTest() {
            var graph = new SolutionDependencyGraph(@"C:\Users\julio\source\repos\JullioSanntos\Temp\TBQuiz.App\TBQuiz.App.slnx");
            Assert.IsNotNull(graph);
        }

        [TestMethod]
        public void DumpSolutionGraphTest() {
            const string solutionFilePath = @"C:\Users\julio\source\repos\JullioSanntos\Temp\TBQuiz.App\TBQuiz.App.slnx";

            var graph = new SolutionDependencyGraph(solutionFilePath);

            string report =
                graph.Describe() +
                Environment.NewLine +
                new string('=', 100) +
                Environment.NewLine + Environment.NewLine +
                graph.DescribeTree();

            string outputPath = Path.Combine(TestResultsPath, "SolutionGraph.txt");
            File.WriteAllText(outputPath, report);

            TestContext.WriteLine($"Graph written to: {outputPath}");
            TestContext.WriteLine(report);

            Assert.IsTrue(graph.Projects.Count > 0, "No projects were found in the solution.");
            Assert.AreEqual(1, graph.Diagnostics.Count,
                "Graph reported diagnostics:" + Environment.NewLine +
                string.Join(Environment.NewLine, graph.Diagnostics));
        }
    }


}
