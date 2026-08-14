using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace XpEng.Coder90.Tests.XpEng.Coder06.ViewModels;

public abstract class ViewModelTestBase {
    protected ServiceProvider Provider = null!;

    [TestInitialize]
    public virtual void Setup() {
        // Build the provider using any overrides defined by the derived class
        Provider = TestDIConfig.BuildProvider(ConfigureServices);
    }

    [TestCleanup]
    public virtual void Teardown() {
        Provider?.Dispose();
    }

    // FOLDER LEVEL: Override this method to add folder-wide mocks
    // CLASS LEVEL: Derived classes override this to add class-specific mocks
    protected virtual void ConfigureServices(IServiceCollection services) {
        // Example: Mock out the NavigationService for all ViewModel tests
        // services.AddSingleton<INavigationService, MockNavigationService>();
    }

    // UNIT TEST LEVEL: Helper to rebuild the container mid-test
    protected void RebuildProviderWithOverrides(Action<IServiceCollection> testSpecificOverrides) {
        Provider?.Dispose();
        Provider = TestDIConfig.BuildProvider(services => {
            ConfigureServices(services);      // Keep the Folder/Class rules
            testSpecificOverrides(services);  // Add the Unit Test specific rules
        });
    }
}