using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace XpEng.Coder80.Infrastructure.Services {
    public class DIExtensions {

        #region ServiceCollection
        private static ServiceCollection? _serviceCollection;
        public static ServiceCollection ServiceCollection => _serviceCollection ??= [];
        #endregion ServiceCollection

        #region ServiceProvider
        private static IServiceProvider? _serviceProvider;
        public static IServiceProvider ServiceProvider {
            get {
                if (_serviceProvider == null) {
                    throw new InvalidOperationException("ServiceProvider has not been built yet. Call DIExtensions.Build() from the composition root after all DIConfig.Config() calls have run.");
                }
                return _serviceProvider;
            }
        }
        #endregion ServiceProvider

        /// <summary>
        /// Builds the ServiceProvider from the fully-populated ServiceCollection.
        /// Must be called exactly once, from the composition root (App), AFTER
        /// every tier's DIConfig.Config() has finished registering its services.
        /// </summary>
        public static void Build() {
            if (_serviceProvider != null) {
                throw new InvalidOperationException("ServiceProvider has already been built.");
            }
            _serviceProvider = ServiceCollection.BuildServiceProvider();
            ServiceProviders.TryAdd(ServiceProviderDefaultName, _serviceProvider);
        }

        private const string Default = nameof(Default);
        public const string ServiceProviderDefaultName = nameof(Default);

        #region ServiceProviders
        private static ConcurrentDictionary<string, IServiceProvider>? _serviceProviders;
        public static ConcurrentDictionary<string, IServiceProvider> ServiceProviders => _serviceProviders ??= new();
        #endregion ServiceProviders

        public IServiceScopeFactory? ServiceScopeFactory { get; set; }
    }
}