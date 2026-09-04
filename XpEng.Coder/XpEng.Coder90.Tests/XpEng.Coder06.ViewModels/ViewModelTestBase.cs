using Microsoft.Extensions.DependencyInjection;
using XpEng.Coder80.Infrastructure;

namespace XpEng.Coder90.Tests.XpEng.Coder06.ViewModels;

public abstract class ViewModelTestBase {

    protected ServiceProvider Provider { get; private set; } = null!;
    private IDisposable? _scope;

    [TestInitialize]
    public void Initialize() => BuildProvider(null);

    [TestCleanup]
    public void Teardown() => ReleaseProvider();

    /// Folder-level registrations. Overrides call base first, then layer their own on top.
    protected virtual void ConfigureServices(IServiceCollection services) {
        // Fully qualified because every project now declares AddRegistrations; an
        // unqualified call would be ambiguous wherever two of those namespaces are imported.
        // ViewModels cascades into Models and Services, so this wires the whole branch.
        global::XpEng.Coder06.ViewModels.ServiceCollectionExtensions.AddRegistrations(services);
    }

    /// Rebuilds with additional per-test overrides, applied after ConfigureServices.
    protected void RebuildProviderWithOverrides(Action<IServiceCollection> overrides) {
        ReleaseProvider();
        BuildProvider(overrides);
    }

    private void BuildProvider(Action<IServiceCollection>? overrides) {
        var services = new ServiceCollection();
        ConfigureServices(services);
        overrides?.Invoke(services);

        Provider = services.BuildServiceProvider();
        _scope = ServiceLocator.UseProvider(Provider);
    }

    private void ReleaseProvider() {
        // Provider first: disposing it disposes the services it created, and their Dispose
        // methods may resolve through the locator. Releasing the scope first would leave
        // them with no provider to reach.
        Provider?.Dispose();
        _scope?.Dispose();
        _scope = null;
    }
}