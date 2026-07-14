using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace XpEng.Coder80.Infrastructure.Services {
    public class DIExtensions {


        #region ServiceCollection
        private static ServiceCollection? _serviceCollection;
        public static ServiceCollection ServiceCollection {
            get { return _serviceCollection ??= []; }
            private set { _serviceCollection = value; DIExtensions._serviceProvider = null; }
        }
        #endregion ServiceCollection

        #region ServiceProvider
        private static IServiceProvider? _serviceProvider;
        public static IServiceProvider ServiceProvider {
            get {
                if (_serviceProvider != null) { return _serviceProvider; }

                _serviceProvider = DIExtensions.ServiceCollection.BuildServiceProvider();

                return _serviceProvider;
            }
            protected set => _serviceProvider = value;
        }
        #endregion ServiceProvider

        public static ServiceProvider GetServiceProvider(ServiceCollection? serviceCollection = null) {
            if (_serviceProvider == null) { serviceCollection = ServiceCollection; }

            var servProvider = serviceCollection!.BuildServiceProvider();

            return servProvider;
        }

        private const string Default = nameof(Default);
        public const string ServiceProviderDefaultName = nameof(Default);

        #region ServiceProviders
        private static ConcurrentDictionary<string, IServiceProvider>? _serviceProviders;
        public static ConcurrentDictionary<string, IServiceProvider> ServiceProviders {
            get {
                if (_serviceProviders != null) { return _serviceProviders; }
                
                _serviceProviders = new ConcurrentDictionary<string, IServiceProvider>();
                _serviceProviders.TryAdd(ServiceProviderDefaultName, ServiceProvider);

                return _serviceProviders;
            }
            protected set => _serviceProviders = value; 
        }
        #endregion ServiceProviders

        public IServiceScopeFactory? ServiceScopeFactory { get; set; }


    }
}
