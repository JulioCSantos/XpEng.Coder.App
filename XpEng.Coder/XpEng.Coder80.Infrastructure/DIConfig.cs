using Microsoft.Extensions.DependencyInjection;

namespace XpEng.Coder80.Infrastructure {
    public class DIConfig {

        public static IServiceCollection Config(IServiceCollection serviceCollection) {
            serviceCollection.AddSingleton<Interfaces.IConfigPersistence, Services.JsonConfigPersistence>();
            serviceCollection.AddSingleton<Interfaces.IEngineLogger, Services.EngineLogger>();  
            return serviceCollection;
        }
    }
}
