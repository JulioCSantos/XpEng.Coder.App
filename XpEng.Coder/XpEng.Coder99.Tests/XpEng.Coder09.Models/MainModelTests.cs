using XpEng.Coder09.Models;

namespace XpEng.Coder90.Tests.XpEng.Coder09.Models; 
[TestClass]
public class MainModelTests {
    [TestMethod]
    public void InstantiationTest() {
        var target = new MainModel();
        Assert.IsNotNull(target);
    }
}
