using Microsoft.Extensions.DependencyInjection;

namespace XpEng.Coder80.Infrastructure;

/// <summary>
/// Holds the application's built container so code that cannot receive a dependency through
/// its constructor can still resolve one — a static Instance property, a XAML-constructed
/// view, a design-time fallback.
///
/// Two levels:
///
///   The DEFAULT provider is set once at startup and serves the whole application.
///
///   A SCOPED provider, set through UseProvider, overrides it for the current asynchronous
///   context. AsyncLocal rather than ThreadStatic: a ThreadStatic value does not survive an
///   await — the continuation can resume on a different pool thread — so an async test would
///   silently fall back to the default. AsyncLocal flows with the context, which is what it
///   exists for.
///
/// The scoped level exists so a test can redirect locator-based resolution (MainModel.Instance
/// and similar) at its own container. Code that resolves directly from its own provider never
/// touches any of this.
/// </summary>
public static class ApplicationServices {

    private static readonly AsyncLocal<IServiceProvider?> _scopedProvider = new();
    private static IServiceProvider? _defaultProvider;

    /// <summary>
    /// The provider in effect: the scoped one when the current context has set one, otherwise
    /// the application default. Throws rather than returning null when neither exists, so the
    /// failure names its own cause instead of surfacing as a NullReferenceException somewhere
    /// further along.
    /// </summary>
    public static IServiceProvider Provider {
        get {
            var provider = _scopedProvider.Value ?? _defaultProvider;

            if (provider == null)
                throw new InvalidOperationException(
                    "The service provider has not been built. Call ApplicationServices.Initialize(provider) " +
                    "during startup, after AddRegistrations and BuildServiceProvider — or, in a test, " +
                    "wrap the code under test in ApplicationServices.UseProvider(...).");

            return provider;
        }
    }

    /// <summary>
    /// True when a provider is available. Lets a caller fall back rather than throw — the
    /// design-time case, where startup never runs at all.
    /// </summary>
    public static bool IsInitialized => (_scopedProvider.Value ?? _defaultProvider) != null;

    /// <summary>Sets the application-wide provider. Called once, from the composition root.</summary>
    public static void Initialize(IServiceProvider provider) {
        _defaultProvider = provider;
    }

    /// <summary>
    /// Overrides the provider for the current asynchronous context until the returned scope is
    /// disposed, which restores whatever was in effect before — so nested and sequential uses
    /// both behave.
    /// </summary>
    public static IDisposable UseProvider(IServiceProvider provider) {
        if (provider == null) throw new ArgumentNullException(nameof(provider));
        return new ProviderScope(provider);
    }

    /// <summary>
    /// Clears the application-wide provider. For a test process that builds several containers
    /// and needs one not to leak into the next. Does not affect scoped providers.
    /// </summary>
    public static void Reset() {
        _defaultProvider = null;
    }

    private sealed class ProviderScope : IDisposable {

        private readonly IServiceProvider? _previous;
        private bool _disposed;

        internal ProviderScope(IServiceProvider provider) {
            _previous = _scopedProvider.Value;
            _scopedProvider.Value = provider;
        }

        public void Dispose() {
            if (_disposed) return;
            _disposed = true;
            _scopedProvider.Value = _previous;
        }
    }
}
