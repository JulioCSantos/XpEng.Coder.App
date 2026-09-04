using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using XpEng.Coder06.ViewModels;
using XpEng.Coder90.Tests.XpEng.Coder06.ViewModels;

namespace XpEng.Coder90.Tests;

public static class TestDIConfig {

    /// Builds a container with the real wiring, then applies substitutions. Two knobs: the
    /// suite-wide overrides below, and whatever a caller passes — composable to as many
    /// levels as a test suite needs without a dedicated mechanism per level.
    public static ServiceProvider BuildProvider(Action<IServiceCollection>? overrides = null) {
        var services = new ServiceCollection();

        // 1. Production wiring. Fully qualified because every project now declares
        //    AddRegistrations; ViewModels cascades into Models and Services.
        global::XpEng.Coder06.ViewModels.ServiceCollectionExtensions.AddRegistrations(services);

        // 2. Suite-wide substitutions. Replace rather than Remove-then-Add: one statement,
        //    one intent, and it cannot leave a stale descriptor behind if the removal misses.
        services.Replace(ServiceDescriptor.Transient<MainViewModel, TestMainViewModel>());

        // 3. Per-test overrides.
        overrides?.Invoke(services);

        // 4. Build.
        return services.BuildServiceProvider();
    }
}