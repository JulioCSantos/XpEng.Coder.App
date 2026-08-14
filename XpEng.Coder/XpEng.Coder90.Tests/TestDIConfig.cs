using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using XpEng.Coder06.ViewModels;
using XpEng.Coder90.Tests.XpEng.Coder06.ViewModels; // Location of TestMainViewModel

namespace XpEng.Coder90.Tests;

public static class TestDIConfig {
    // The delegate allows calling code to inject its own overrides
    public static ServiceProvider BuildProvider(Action<IServiceCollection>? overrides = null) {
        var services = new ServiceCollection();

        // 1. Load Production Config
        DIConfig.Config(services);

        // 2. Project-Level Overrides (Global for all tests)
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(MainViewModel));
        if (descriptor != null) services.Remove(descriptor);
        services.AddTransient<MainViewModel, TestMainViewModel>();

        // 3. Apply Granular Overrides (Folder, Class, or Unit Test level)
        overrides?.Invoke(services);

        // 4. Build the final immutable container
        return services.BuildServiceProvider();
    }
}