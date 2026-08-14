using System;
using System.Collections.Generic;
using System.Text;
using XpEng.Coder09.Models.Entities;

namespace XpEng.Coder90.Tests.XpEng.Coder09.Models {
    [TestClass]
    public class TargetTemplateTests {
        [TestMethod]
        public void TargetTemplateInstantiationTest() {
            var actual = new TargetTemplate(new DirectoryInfo("C:\\"), new FileInfo("C:\\swapfile.sys"), false);
            Assert.IsNotNull(actual);
            Assert.IsNotNull(actual.TargetDirectory);
        }
    }
}