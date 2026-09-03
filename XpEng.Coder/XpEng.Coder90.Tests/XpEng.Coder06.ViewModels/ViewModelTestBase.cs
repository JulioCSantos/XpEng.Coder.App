using Microsoft.Extensions.DependencyInjection;
using XpEng.Coder06.ViewModels;
using XpEng.Coder80.Infrastructure;

public abstract class ViewModelTestBase {

    protected ServiceProvider Provider { get; private set; } = null!;
    private IDisposable? _scope;

    [TestInitialize]
    public void Initialize() => BuildProvider(null);

    [TestCleanup]
    public void Teardown() => ReleaseProvider();

    /// Folder-level registrations. Overrides call base first, then layer their own on top.
    protected virtual void ConfigureServices(IServiceCollection services) {
        services.AddViewModels();
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
        _scope = ApplicationServices.UseProvider(Provider);
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